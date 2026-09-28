using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// 對話播放期間的轉接元件，由 SetAutoDialogue 自動掛到 Dialogue Manager。
    /// 接收逐句通知；不改寫 Dialogue Database，也不攔截其他場景互動。
    /// </summary>
    [DisallowMultipleComponent]
    public class NhkAutoDialogueBridge : MonoBehaviour
    {
        private readonly NhkAutoDialogueSession session = new NhkAutoDialogueSession();
        private Subtitle injectedSubtitle;

        public bool IsActive => session.IsActive;
        public int Generation => session.Generation;

        public static NhkAutoDialogueBridge Find()
        {
            return DialogueManager.instance != null
                ? DialogueManager.instance.GetComponent<NhkAutoDialogueBridge>() : null;
        }

        public static bool IsBlockingContinue => Find()?.IsActive == true;

        public void EnableAutoDialogue()
        {
            session.Enable(DialogueManager.conversationView.displaySettings);
            DialogueManager.conversationView.SetupContinueButton();
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
            if (!IsActive) return;
            session.Enforce();
            if (!HasDialogueText(subtitle) || ReferenceEquals(injectedSubtitle, subtitle)) return;
            injectedSubtitle = subtitle;
            string sequence = (subtitle.sequence ?? string.Empty).TrimEnd();
            if (sequence.Length > 0 && !sequence.EndsWith(";")) sequence += ";";
            subtitle.sequence = sequence + " NhkAutoDialogueWait();";
        }

        public bool TryBeginWait(Subtitle subtitle)
        {
            return HasDialogueText(subtitle) && session.TryBeginWait(subtitle);
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

        private void OnDisable() => DisableAutoDialogue();
        private void OnDestroy() => DisableAutoDialogue();
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
