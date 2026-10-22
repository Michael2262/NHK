// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardUISubtitlePanel，讓「額外敘述」成為字幕面板自己的一部分：
//   - 敘述框（文字元件 + 可選容器）是面板底下的子物件，造型跟著該面板走
//   - 不同的 Dialogue UI Panel 各自繼承本元件、各自擺自己的敘述框造型
//   - 面板顯示某句字幕時（SetContent），讀該行節點的自訂 Text 欄位（預設 "Narration"）
//       有字 → 填入敘述框並開啟；空白 → 關閉
//   - 面板關閉（Close）→ 敘述框一併收掉
//   - 沒指定敘述框 UI → 什麼都不做（不顯示）
//
// 填法與對話文字相同：直接在欄位打字，要多語系就加 "Narration ja" 等語言變體，不需要 text table。
//
// 使用方式：
//   1. 在字幕面板 prefab 底下做一個敘述框（容器 GameObject + 文字元件 TMP 或 Text），平時可關閉
//   2. 將該面板上的 StandardUISubtitlePanel 元件換成本元件
//      （換完記得重新拖面板原有的參照：Panel / Subtitle Text / Portrait 等）
//   3. Inspector 拖入 Narration Text（與可選的 Narration Container）
//   4. 需要敘述的對話節點：新增 Text 欄位 "Narration"，直接填敘述文字

using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/Dialogue/Nhk UI Subtitle Panel")]
    public class NhkUISubtitlePanel : StandardUISubtitlePanel
    {
        [Header("Extra Narration Settings")]
        [Tooltip("顯示額外敘述的文字元件（面板底下的子物件，TMP 或 Text）")]
        [SerializeField] private UITextField narrationText;

        [Tooltip("（可選）要跟著一起開關的容器 GameObject，例如帶背景的敘述框。留空則只開關文字元件本身。")]
        [SerializeField] private GameObject narrationContainer;

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，直接填敘述文字。空白 = 不顯示。")]
        [SerializeField] private string narrationFieldName = "Narration";

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型。該欄位值填 ProgressFlag 名稱：\n" +
                 "留空 = 敘述照常顯示；\n" +
                 "填 FlagName = 只有該 Flag 為 true 時才顯示敘述；\n" +
                 "填 !FlagName = 反向，只有該 Flag 不為 true 時才顯示敘述。")]
        [SerializeField] private string narrationFlagFieldName = "NarrationFlag";

        [Header("角色名字框")]
        [Tooltip("啟用後，只有清單內的角色會顯示名字文字與背景框；關閉時沿用原生行為。")]
        [SerializeField] private bool controlNameVisibility = false;

        [Tooltip("包含 Portrait Name 與背景框的共同父物件。請勿指定整個字幕面板或其祖先。")]
        [SerializeField] private GameObject nameContainer;

        [Tooltip("填入 Dialogue Database 的角色 Name（不是 Display Name），不分大小寫。清單外角色隱藏名字框。")]
        [SerializeField] private List<string> actorsWithVisibleNames = new List<string>();

        // 保留最後一位說話者的設定，供 SubtitlePanel(hide/show) 還原使用。
        private bool currentNameVisible;

        private void SetNameActor(string actorName)
        {
            currentNameVisible = false;
            if (string.IsNullOrEmpty(actorName) || actorsWithVisibleNames == null) return;
            foreach (var actor in actorsWithVisibleNames)
            {
                if (!string.IsNullOrWhiteSpace(actor) &&
                    string.Equals(actor.Trim(), actorName, System.StringComparison.OrdinalIgnoreCase))
                {
                    currentNameVisible = true;
                    break;
                }
            }
        }

        private void ApplyNameVisibility(bool uiVisible = true)
        {
            if (!controlNameVisibility || nameContainer == null) return;
            // 避免誤拖整個面板，導致字幕元件或對話內容一起被停用。
            if (transform.IsChildOf(nameContainer.transform) ||
                (panel != null && panel.transform.IsChildOf(nameContainer.transform)) ||
                (!UITextField.IsNull(subtitleText) && subtitleText.gameObject.transform.IsChildOf(nameContainer.transform)))
                return;

            bool show = uiVisible && currentNameVisible;
            nameContainer.SetActive(show);
            if (!UITextField.IsNull(portraitName)) portraitName.SetActive(show);
        }

        protected override void SetUIElementsActive(bool value)
        {
            base.SetUIElementsActive(value);
            ApplyNameVisibility(value);
        }

        public override void OpenOnStartConversation(Sprite portraitSprite, string portraitName, DialogueActor dialogueActor)
        {
            // 尚無實際字幕且找不到角色時，先隱藏名字框，避免沿用上一段對話的名字。
            SetNameActor(dialogueActor != null ? dialogueActor.actor : null);
            base.OpenOnStartConversation(portraitSprite, portraitName, dialogueActor);
            ApplyNameVisibility();
        }

        public override void SetContent(Subtitle subtitle)
        {
            PrepareSlowTypewriter(subtitle);
            settingSubtitleContent = true;
            try
            {
                base.SetContent(subtitle);
            }
            finally
            {
                settingSubtitleContent = false;
            }
            UpdateNarration(subtitle);
            SetNameActor(subtitle != null && subtitle.speakerInfo != null ? subtitle.speakerInfo.nameInDatabase : null);
            ApplyNameVisibility();
        }

        public override void Close()
        {
            RestoreTypewriterSpeed();
            HideNarration();
            base.Close();
        }

        // ===================== 單句慢速打字（Sequence: SlowTypewriter()）=====================

        private AbstractTypewriterEffect slowTypewriter;
        private Subtitle slowSubtitle;
        private float originalTypewriterSpeed;
        private bool slowTypingStarted;
        private bool slowTypingFinished;
        private bool settingSubtitleContent;
        private UnityEvent slowTypingBeginEvent;
        private UnityEvent slowTypingEndEvent;

        protected override void OnDisable()
        {
            RestoreTypewriterSpeed();
            base.OnDisable();
        }

        private void PrepareSlowTypewriter(Subtitle subtitle)
        {
            RestoreTypewriterSpeed();
            if (subtitle == null || subtitle.formattedText == null ||
                string.IsNullOrEmpty(subtitle.formattedText.text) ||
                string.IsNullOrEmpty(subtitle.sequence) || !subtitle.sequence.Contains("SlowTypewriter")) return;

            // 原生流程先 ShowSubtitle 再 PlaySequence，必須在 base.SetContent 前套用速度。
            // 使用原生解析器，避免把註解或其他指令的字串參數誤認成指令。
            float speed = 15f;
            bool found = false;
            foreach (var command in new SequenceParser().Parse(subtitle.sequence))
            {
                if (command.command != "SlowTypewriter") continue;
                if (command.startTime > 0 || !string.IsNullOrEmpty(command.messageToWaitFor))
                {
                    Debug.LogWarning("[SlowTypewriter] 此指令只支援整句慢速，請移除 @時間 或 @Message 排程。", this);
                    continue;
                }

                string value = SequencerTools.GetParameter(command.parameters, 0);
                speed = 15f;
                if (!string.IsNullOrWhiteSpace(value) &&
                    (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out speed) ||
                     float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0))
                {
                    Debug.LogWarning("[SlowTypewriter] 速度必須是大於 0 的數字，本句改用每秒 15 字。", this);
                    speed = 15f;
                }
                found = true;
            }
            if (!found) return;

            var typewriter = GetTypewriter();
            if (typewriter == null || !typewriter.enabled)
            {
                Debug.LogWarning("[SlowTypewriter] Subtitle Text 需要掛上並啟用原生打字機元件。", this);
                return;
            }

            // 事件定義在兩種原生實作，不在 AbstractTypewriterEffect 上。
            if (typewriter is UnityUITypewriterEffect unityTypewriter)
            {
                slowTypingBeginEvent = unityTypewriter.onBegin;
                slowTypingEndEvent = unityTypewriter.onEnd;
            }
#if TMP_PRESENT
            else if (typewriter is TextMeshProTypewriterEffect tmpTypewriter)
            {
                slowTypingBeginEvent = tmpTypewriter.onBegin;
                slowTypingEndEvent = tmpTypewriter.onEnd;
            }
#endif
            else
            {
                Debug.LogWarning("[SlowTypewriter] 目前只支援原生 Unity UI／TextMesh Pro 打字機。", this);
                return;
            }

            slowTypewriter = typewriter;
            slowSubtitle = subtitle;
            originalTypewriterSpeed = typewriter.GetSpeed();
            slowTypingStarted = false;
            slowTypingFinished = false;
            slowTypingBeginEvent.AddListener(OnSlowTypingBegin);
            slowTypingEndEvent.AddListener(OnSlowTypingEnd);
            typewriter.SetSpeed(speed);
        }

        private void OnSlowTypingBegin()
        {
            slowTypingStarted = true;
        }

        private void OnSlowTypingEnd()
        {
            // base.SetContent 會先停止上一句；不能把上一句的 onEnd 當成本句完成。
            // 若啟用了 Wait One Frame，玩家也可能在 onBegin 之前快轉完成。
            if (slowTypingStarted || !settingSubtitleContent) slowTypingFinished = true;
        }

        /// <summary>包含等待面板開啟／取得焦點的期間，避免 Sequence 提早結束。</summary>
        public bool IsWaitingForSlowTypewriter(Subtitle subtitle)
        {
            return ReferenceEquals(slowSubtitle, subtitle) && slowTypewriter != null &&
                   slowTypewriter.enabled && isActiveAndEnabled && !slowTypingFinished;
        }

        private void RestoreTypewriterSpeed()
        {
            if (slowTypewriter != null)
            {
                slowTypewriter.SetSpeed(originalTypewriterSpeed);
            }
            slowTypingBeginEvent?.RemoveListener(OnSlowTypingBegin);
            slowTypingEndEvent?.RemoveListener(OnSlowTypingEnd);
            slowTypingBeginEvent = null;
            slowTypingEndEvent = null;
            slowTypewriter = null;
            slowSubtitle = null;
            slowTypingStarted = false;
            slowTypingFinished = false;
        }

        public override void Open()
        {
            // 只要有字幕板被對話系統自然開啟，就代表畫面已經進入新狀態；
            // 先前 SubtitlePanel(hide) 記住的面板快照不再可用，避免未來 show 又叫回舊面板。
            // SubtitlePanel(show) 會在呼叫 Open 前先取出快照並清空清單，所以正常還原不受影響。
            s_commandHiddenPanels.Clear();

            base.Open();
            ApplyNameVisibility();

            // 面板可能在 inactive 期間才被上鎖，當時 PushContinueButtonLock 未必找得到它。
            // 每次 Open 都再檢查一次，避免舊的 Continue Button 跟著父面板重新出現。
            if (IsContinueButtonLocked) HideContinueButton();
        }

        /// <summary>
        /// 依當前字幕行節點的欄位取敘述：有字 → 顯示；否則隱藏。
        /// 用 LookupLocalizedValue，多語系機制與對話文字相同（維護 "Narration ja" 等變體）。
        /// 文字會經 FormattedText 管線處理，支援 [em#]、[var=]、[lua(...)] 等標記（與對話文字一致）。
        /// </summary>
        private void UpdateNarration(Subtitle subtitle)
        {
            if (UITextField.IsNull(narrationText)) return;

            string narration = null;
            if (subtitle != null && subtitle.dialogueEntry != null)
            {
                narration = Field.LookupLocalizedValue(subtitle.dialogueEntry.fields, narrationFieldName);
            }

            if (!string.IsNullOrWhiteSpace(narration) && IsNarrationAllowedByFlag(subtitle.dialogueEntry))
            {
                if (narrationContainer != null) narrationContainer.SetActive(true);
                narrationText.text = UITools.GetUIFormattedText(FormattedText.Parse(narration));
                narrationText.SetActive(true);
            }
            else
            {
                HideNarration();
            }
        }

        /// <summary>
        /// 依 NarrationFlag 欄位決定是否允許顯示敘述：
        ///   - 欄位空白 → 允許（照常顯示）
        ///   - 填 FlagName  → 只有該 Flag 在 ProgressFlags 中為 true 時才允許（正向）
        ///   - 填 !FlagName → 反向，只有該 Flag 不為 true 時才允許
        /// 數值型 Flag > 0 也會被 Contains() 視為 true（沿用專案慣例）。
        /// </summary>
        private bool IsNarrationAllowedByFlag(DialogueEntry entry)
        {
            if (entry == null) return false;
            string flag = Field.LookupValue(entry.fields, narrationFlagFieldName);
            if (string.IsNullOrWhiteSpace(flag)) return true;

            // 解析反向前綴 "!"：!FlagName = 只有 Flag 不為 true 時才顯示
            flag = flag.Trim();
            bool negate = flag.StartsWith("!");
            if (negate) flag = flag.Substring(1).Trim();
            if (string.IsNullOrWhiteSpace(flag)) return true; // 只有 "!" 沒接名 → 視為不設限

            var flags = (GameStatusService.Instance != null) ? GameStatusService.Instance.ProgressFlags : null;
            bool has = flags != null && flags.Contains(flag);
            return negate ? !has : has;
        }

        private void HideNarration()
        {
            if (!UITextField.IsNull(narrationText))
            {
                narrationText.text = string.Empty;
                narrationText.SetActive(false);
            }
            if (narrationContainer != null) narrationContainer.SetActive(false);
        }

        // ===================== 繼續鈕暫時鎖（給 Sequencer / 劇情演出期間用）=====================
        //
        // 用途：行動演出跑條、動畫過場等「不希望玩家中途按繼續跳過」的期間，暫時壓住繼續鈕。
        //
        // 為什麼需要它（而不是只用 SetContinueMode(false)）：
        //   SetContinueMode 只在「字幕開始顯示」那一刻套用繼續鈕狀態，且套用的是「當前有顯示字幕」的那句。
        //   若演出節點本身沒有對白文字（例如只有 ActionOverlay 的空句），它不會顯示字幕、DS 不會為它重設
        //   繼續鈕，於是畫面上仍停留前一句遺留、還亮著的繼續鈕，玩家一點就把還在跑的演出跳過去。
        //   本鎖直接從「顯示繼續鈕」的所有路徑攔截，不依賴當前句有沒有字幕，因此擋得住上述情況。
        //
        // 可重入：用計數配對，每個 Push 都要有對應的 Pop。
        private static int s_continueLockCount = 0;

        /// <summary>繼續鈕是否正被鎖定（鎖定期間所有 NhkUISubtitlePanel 都不顯示繼續鈕）。</summary>
        public static bool IsContinueButtonLocked =>
            s_continueLockCount > 0 || NhkAutoDialogueBridge.IsBlockingContinue;

        /// <summary>
        /// 壓住繼續鈕：鎖定期間，任何來源（換句、typewriter 完成、SetContinueMode 重刷、endOfFrame 延遲顯示…）
        /// 想顯示繼續鈕都會被吞掉，玩家因而無法在此期間按繼續前進。與 <see cref="PopContinueButtonLock"/> 配對。
        /// </summary>
        public static void PushContinueButtonLock()
        {
            s_continueLockCount++;
            if (s_continueLockCount != 1) return;

            // 由「不鎖」轉為「鎖」的當下：把所有繼續鈕立刻藏起來。
            // 必須包含 inactive 面板，否則它們日後 Open 時可能把舊的亮著按鈕一起帶回來。
            var panels = FindObjectsByType<NhkUISubtitlePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null) panels[i].HideContinueButton();
            }
        }

        /// <summary>
        /// 解除一層繼續鈕鎖（與 <see cref="PushContinueButtonLock"/> 配對）。計數歸零後不主動顯示繼續鈕，
        /// 交還 DS，由後續換句 / SetContinueMode 依當前繼續模式自然重刷。
        /// </summary>
        public static void PopContinueButtonLock()
        {
            if (s_continueLockCount <= 0) return;
            s_continueLockCount--;
        }

        /// <summary>保底：強制清空鎖計數（例如對話異常中斷、擔心 Push/Pop 沒配對時）。一般不需手動呼叫。</summary>
        public static void ResetContinueButtonLock()
        {
            s_continueLockCount = 0;
        }

        public override void ShowContinueButton()
        {
            if (IsContinueButtonLocked)
            {
                HideContinueButton();
                return;
            }
            base.ShowContinueButton();
        }

        protected override void ShowContinueButtonNow()
        {
            // 攔截「延遲到 endOfFrame 才顯示」等繞過 ShowContinueButton() 的路徑，確保鎖定期間絕不冒出繼續鈕。
            if (IsContinueButtonLocked)
            {
                Tools.SetGameObjectActive(continueButton, false);
                return;
            }
            base.ShowContinueButtonNow();
        }

        // ===================== 整個字幕面板 隱藏 / 還原（給 Sequencer 命令用）=====================
        //
        // 用途：演出期間想把「整個對話框」暫時收起來（例如秀立繪 / 過場），演完再叫回來。
        //   Hide → 對當下開著的所有 NhkUISubtitlePanel 呼叫 Close()（會播 Hide 動畫），並記住它們。
        //   Show → 對剛剛記住的面板呼叫 Open()（會播 Show 動畫）還原。
        //   Reset → 只清掉記錄、不還原（保底用）。
        //
        // 注意：走面板原生的 Open()/Close()，動畫、敘述框收合、繼續鈕鎖等都照常。
        //   面板若勾了 Clear Text On Close，Hide 後文字會被清掉，Show 回來是空框（下一句才會填字）。
        private static readonly List<NhkUISubtitlePanel> s_commandHiddenPanels = new List<NhkUISubtitlePanel>();

        /// <summary>隱藏當下開著的所有字幕面板（播 Hide 動畫），並記住以便 <see cref="ShowHiddenPanels"/> 還原。</summary>
        public static void HideOpenPanels()
        {
            var panels = FindObjectsByType<NhkUISubtitlePanel>(FindObjectsSortMode.None);
            for (int i = 0; i < panels.Length; i++)
            {
                var p = panels[i];
                if (p == null || !p.isOpen) continue;
                if (!s_commandHiddenPanels.Contains(p)) s_commandHiddenPanels.Add(p);
                p.Close(); // 播 Hide 動畫
            }
        }

        /// <summary>還原先前被 <see cref="HideOpenPanels"/> 隱藏的面板（播 Show 動畫），並清空記錄。</summary>
        public static void ShowHiddenPanels()
        {
            // 先取出快照並清空原清單，再逐一 Open。
            // 如此 Open() 內的「自然開啟會清過期記錄」不會干擾這次明確的 show 還原。
            var panelsToShow = s_commandHiddenPanels.ToArray();
            s_commandHiddenPanels.Clear();

            for (int i = 0; i < panelsToShow.Length; i++)
            {
                var p = panelsToShow[i];
                if (p != null) p.Open(); // 播 Show 動畫
            }
        }

        /// <summary>只清空「被命令隱藏」的記錄，不還原（保底用，擔心 Hide/Show 沒配對時）。</summary>
        public static void ResetHiddenPanels()
        {
            s_commandHiddenPanels.Clear();
        }
    }
}
