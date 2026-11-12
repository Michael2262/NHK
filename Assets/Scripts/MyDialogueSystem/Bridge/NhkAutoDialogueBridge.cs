using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// 對話播放期間的轉接元件，由 SetAutoDialogue / SetActorAutoDialogue / SetDefaultWait 自動掛到 Dialogue Manager。
    /// 接收逐句通知；不改寫 Dialogue Database，也不攔截其他場景互動。
    /// </summary>
    [DisallowMultipleComponent]
    public class NhkAutoDialogueBridge : MonoBehaviour
    {
        private readonly NhkAutoDialogueSession session = new NhkAutoDialogueSession();
        private readonly NhkDefaultWaitSession defaultWaitSession = new NhkDefaultWaitSession();
        private readonly NhkActorAutoDialogueSession actorAutoSession = new NhkActorAutoDialogueSession();
        private Subtitle injectedSubtitle;

        public bool IsActive => session.IsActive;
        public int Generation => session.Generation;
        public bool IsDefaultWaitActive => defaultWaitSession.IsActive;
        public int DefaultWaitGeneration => defaultWaitSession.Generation;
        public bool IsActorAutoActive => actorAutoSession.IsActive;

        public static NhkAutoDialogueBridge Find()
        {
            return DialogueManager.instance != null
                ? DialogueManager.instance.GetComponent<NhkAutoDialogueBridge>() : null;
        }

        public static bool IsBlockingContinue => Find()?.IsActive == true;

        public void EnableAutoDialogue()
        {
            if (!IsActive) defaultWaitSession.InvalidateCurrentWait();
            session.Enable(DialogueManager.conversationView.displaySettings);
            DialogueManager.conversationView.SetupContinueButton();
        }

        public void SetDefaultWait(bool value)
        {
            // 明確切回一般等待設定時，先結束分流，避免下一句又覆蓋使用者指定的模式。
            DisableActorAutoDialogue();
            defaultWaitSession.SetActive(value);
        }

        public void EnableActorAutoDialogue()
        {
            if (!IsActorAutoActive)
            {
                actorAutoSession.Enable(IsActive, IsDefaultWaitActive);
                // 若原本已開全域 Auto，先釋放它，讓共用框能取回原本的手動設定。
                session.Disable();
                session.InvalidateCurrentWait();
                defaultWaitSession.InvalidateCurrentWait();
                injectedSubtitle = null;
            }
            ApplyActorAutoDialogue(DialogueManager.currentConversationState?.subtitle);
            RefreshContinueButton();
        }

        public void DisableActorAutoDialogue()
        {
            if (!IsActorAutoActive) return;
            session.Disable();
            session.InvalidateCurrentWait();
            defaultWaitSession.InvalidateCurrentWait();
            defaultWaitSession.SetActive(actorAutoSession.OriginalDefaultWait);
            bool restoreAuto = actorAutoSession.OriginalAuto;
            actorAutoSession.Disable();
            injectedSubtitle = null;
            if (restoreAuto && DialogueManager.isConversationActive && DialogueManager.conversationView != null)
                session.Enable(DialogueManager.conversationView.displaySettings);
            RefreshContinueButton();
        }

        private void ApplyActorAutoDialogue(Subtitle subtitle)
        {
            if (!IsActorAutoActive) return;
            // Default Wait 保持待命；Auto 生效的句子由 TryBeginDefaultWait 排除。
            defaultWaitSession.SetActive(true);
            if (UsesActorCustomPanel(subtitle))
            {
                if (!IsActive) defaultWaitSession.InvalidateCurrentWait();
                session.Enable(DialogueManager.conversationView.displaySettings);
            }
            else
            {
                // 必須在主框 ShowSubtitle / SetupContinueButton 之前解除上一句的鎖。
                session.Disable();
            }
        }

        private static bool UsesActorCustomPanel(Subtitle subtitle)
        {
            if (!ShouldAddDialogueWait(subtitle) || subtitle.speakerInfo == null) return false;
            var ui = DialogueManager.dialogueUI as StandardDialogueUI;
            if (ui == null) return false;
            // 走原生解析，尊重 SetPanel、[panel=#] 等覆寫，不能只看說話者有沒有 DialogueActor。
            var panel = ui.conversationUIElements.standardSubtitleControls.GetPanel(subtitle, out _);
            if (panel == null) return false;
            var actors = FindObjectsByType<DialogueActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (actor.standardDialogueUISettings.customSubtitlePanel == panel) return true;
            }
            return false;
        }

        private static void RefreshContinueButton()
        {
            if (DialogueManager.isConversationActive && DialogueManager.conversationView != null)
                DialogueManager.conversationView.SetupContinueButton();
        }

        public void DisableWaitModes()
        {
            // 對話結束／Queue 交接只做清理，不還原分流前的 Auto 開關到下一段對話。
            actorAutoSession.Disable();
            defaultWaitSession.SetActive(false);
            DisableAutoDialogue();
            session.InvalidateCurrentWait();
            injectedSubtitle = null;
        }

        public void DisableAutoDialogue()
        {
            if (!session.IsActive) return;
            session.Disable();
            injectedSubtitle = null;
            if (DialogueManager.isConversationActive && DialogueManager.conversationView != null)
                DialogueManager.conversationView.SetupContinueButton();
        }

        // 此通知早於 Sequence 解析。必須修改當次 subtitle，而不是資料庫的 Entry。
        public void OnConversationLine(Subtitle subtitle)
        {
            // 此時 ConversationView 還記著上一句，這裡只切換設定，不重刷上一句的按鈕。
            ApplyActorAutoDialogue(subtitle);
            if (!IsActive && !IsDefaultWaitActive) return;
            session.Enforce();
            if (subtitle == null || ReferenceEquals(injectedSubtitle, subtitle)) return;
            if (!HasDialogueText(subtitle))
            {
                // 空白條件／轉接節點若仍留空，原生會補 Default Sequence，可能多等 20 秒。
                // 只替當次播放補 None()；明確指定的 Delay 或演出指令完整保留。
                if (string.IsNullOrWhiteSpace(subtitle.sequence))
                    subtitle.sequence = "None()";
                return;
            }
            // 不顯示玩家字幕時，選項文字只供選單使用，不應再補一段閱讀等待。
            // 保留原 Sequence，讓原生流程處理 Default Player Sequence 與明確的演出指令。
            if (!ShouldAddDialogueWait(subtitle)) return;
            injectedSubtitle = subtitle;
            string sequence = (subtitle.sequence ?? string.Empty).TrimEnd();
            // 原生會特別辨識整句 None()/Continue()；一般模式不要加命令而破壞其隱藏字幕語意。
            if (!IsActive)
            {
                string singleCommand = sequence.Trim().TrimEnd(';').TrimEnd();
                if (singleCommand == "None()" || singleCommand == "Continue()") return;
            }
            if (sequence.Length > 0 && !sequence.EndsWith(";")) sequence += ";";
            // 兩個開關可同時保留，等待命令依執行當下的模式決定是否生效。
            // 例如當句先 SetAutoDialogue(false)，後面的 Default Wait 就能接手。
            if (IsActive || IsActorAutoActive) sequence += "\nNhkAutoDialogueWait();";
            if (IsDefaultWaitActive) sequence += "\nNhkDefaultDialogueWait();";
            subtitle.sequence = sequence;
        }

        public bool TryBeginWait(Subtitle subtitle)
        {
            return ShouldAddDialogueWait(subtitle) && session.TryBeginWait(subtitle);
        }

        public bool TryBeginDefaultWait(Subtitle subtitle)
        {
            return !IsActive && ShouldAddDialogueWait(subtitle) && defaultWaitSession.TryBeginWait(subtitle);
        }

        private static bool ShouldAddDialogueWait(Subtitle subtitle)
        {
            if (!HasDialogueText(subtitle)) return false;
            var settings = DialogueManager.conversationView?.displaySettings;
            // 同時供逐句注入與 Set*(true) 當句直接啟動等待的路徑使用。
            return subtitle.speakerInfo.characterType != CharacterType.PC ||
                (settings != null && settings.GetShowPCSubtitlesDuringLine());
        }

        private static bool HasDialogueText(Subtitle subtitle)
        {
            return subtitle?.formattedText != null && !subtitle.formattedText.noSubtitle &&
                !string.IsNullOrWhiteSpace(subtitle.formattedText.text);
        }

        private void Update()
        {
            // Request / ActionOverlay 等命令可能還原 Continue Mode；自動模式期間維持 Never。
            if (!IsActive) return;
            if (session.Enforce() && DialogueManager.conversationView != null)
                DialogueManager.conversationView.SetupContinueButton();
        }

        private void OnDisable() => DisableWaitModes();
        private void OnDestroy() => DisableWaitModes();
    }

    /// <summary>分流模式的還原快照；重複啟用不覆蓋最初設定，不持有場景物件或存檔資料。</summary>
    internal sealed class NhkActorAutoDialogueSession
    {
        public bool IsActive { get; private set; }
        public bool OriginalAuto { get; private set; }
        public bool OriginalDefaultWait { get; private set; }

        public void Enable(bool auto, bool defaultWait)
        {
            if (IsActive) return;
            OriginalAuto = auto;
            OriginalDefaultWait = defaultWait;
            IsActive = true;
        }

        public void Disable() => IsActive = false;
    }

    /// <summary>一般對話的預設等待開關；不持有或修改 Continue Mode 與輸入設定。</summary>
    internal sealed class NhkDefaultWaitSession
    {
        private Subtitle waitingSubtitle;
        public bool IsActive { get; private set; }
        public int Generation { get; private set; }

        public void SetActive(bool value)
        {
            if (IsActive == value) return;
            IsActive = value;
            InvalidateCurrentWait();
        }

        public void InvalidateCurrentWait()
        {
            Generation++;
            waitingSubtitle = null;
        }

        public bool TryBeginWait(Subtitle subtitle)
        {
            if (!IsActive || ReferenceEquals(waitingSubtitle, subtitle)) return false;
            waitingSubtitle = subtitle;
            return true;
        }
    }

    /// <summary>純 C# 的暫時播放狀態；由 UI 轉接元件持有，不屬於遊戲存檔。</summary>
    internal sealed class NhkAutoDialogueSession
    {
        private DisplaySettings settings;
        private ConversationOverrideDisplaySettings overrides;
        private DisplaySettings.SubtitleSettings.ContinueButtonMode originalMode;
        private DisplaySettings.SubtitleSettings.ContinueButtonMode originalOverrideMode;
        private InputTrigger originalCancel;
        private InputTrigger originalOverrideCancel;
        private readonly InputTrigger blockedCancel = new InputTrigger();
        private Subtitle waitingSubtitle;

        public bool IsActive { get; private set; }
        public int Generation { get; private set; }

        public void Enable(DisplaySettings currentSettings)
        {
            if (IsActive) return;
            settings = currentSettings;
            overrides = settings.conversationOverrideSettings;
            originalMode = settings.subtitleSettings.continueButton;
            originalCancel = settings.inputSettings.cancel;
            if (overrides != null)
            {
                originalOverrideMode = overrides.continueButton;
                originalOverrideCancel = overrides.cancelSubtitle;
            }
            IsActive = true;
            Generation++;
            Enforce();
        }

        public bool Enforce()
        {
            if (!IsActive) return false;
            var never = DisplaySettings.SubtitleSettings.ContinueButtonMode.Never;
            bool changed = settings.subtitleSettings.continueButton != never;
            settings.subtitleSettings.continueButton = never;
            settings.inputSettings.cancel = blockedCancel;
            if (overrides != null)
            {
                changed |= overrides.continueButton != never;
                overrides.continueButton = never;
                overrides.cancelSubtitle = blockedCancel;
            }
            return changed;
        }

        public bool TryBeginWait(Subtitle subtitle)
        {
            if (!IsActive || ReferenceEquals(waitingSubtitle, subtitle)) return false;
            waitingSubtitle = subtitle;
            return true;
        }

        public void InvalidateCurrentWait()
        {
            Generation++;
            waitingSubtitle = null;
        }

        public void Disable()
        {
            if (!IsActive) return;
            IsActive = false;
            Generation++;
            settings.subtitleSettings.continueButton = originalMode;
            settings.inputSettings.cancel = originalCancel;
            if (overrides != null)
            {
                overrides.continueButton = originalOverrideMode;
                overrides.cancelSubtitle = originalOverrideCancel;
            }
            settings = null;
            overrides = null;
            waitingSubtitle = null;
        }
    }
}
