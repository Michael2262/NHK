using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// 對話播放期間的轉接元件，由 SetAutoDialogue / SetDefaultWait 自動掛到 Dialogue Manager。
    /// 接收逐句通知；不改寫 Dialogue Database，也不攔截其他場景互動。
    /// </summary>
    [DisallowMultipleComponent]
    public class NhkAutoDialogueBridge : MonoBehaviour
    {
        private readonly NhkAutoDialogueSession session = new NhkAutoDialogueSession();
        private readonly NhkDefaultWaitSession defaultWaitSession = new NhkDefaultWaitSession();
        private Subtitle injectedSubtitle;

        public bool IsActive => session.IsActive;
        public int Generation => session.Generation;
        public bool IsDefaultWaitActive => defaultWaitSession.IsActive;
        public int DefaultWaitGeneration => defaultWaitSession.Generation;

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
            defaultWaitSession.SetActive(value);
        }

        public void DisableWaitModes()
        {
            SetDefaultWait(false);
            DisableAutoDialogue();
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
            if (IsActive) sequence += "\nNhkAutoDialogueWait();";
            if (IsDefaultWaitActive) sequence += "\nNhkDefaultDialogueWait();";
            subtitle.sequence = sequence;
        }

        public bool TryBeginWait(Subtitle subtitle)
        {
            return HasDialogueText(subtitle) && session.TryBeginWait(subtitle);
        }

        public bool TryBeginDefaultWait(Subtitle subtitle)
        {
            return !IsActive && HasDialogueText(subtitle) && defaultWaitSession.TryBeginWait(subtitle);
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
