using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>每筆任務的點擊條件；預設不限制，發放時複製給接收器。</summary>
    [Serializable]
    public sealed class TaskClickConditions
    {
        public ButtonPrerequisites prerequisites = ButtonPrerequisites.None;
        [Min(0)] public int requiredStamina = 1;
        [Min(0)] public int requiredPhaseSlots = 1;

        public TaskClickConditions Copy() => new TaskClickConditions
        {
            prerequisites = this.prerequisites,
            requiredStamina = Math.Max(0, this.requiredStamina),
            requiredPhaseSlots = Math.Max(0, this.requiredPhaseSlots)
        };
    }

    /// <summary>共用點擊條件檢查與失敗提示，不扣資源、不改任務進度。</summary>
    public static class ActionPrerequisiteChecker
    {
        public static bool CheckAndNotify(ButtonPrerequisites prerequisites,
            int requiredStamina, int requiredPhaseSlots, UnityEngine.Object context)
        {
            var service = GameStatusService.Instance;
            if ((prerequisites & ButtonPrerequisites.Stamina) != 0)
            {
                if (service == null || service.TestProtagonist == null)
                {
                    Debug.LogWarning("[ActionPrerequisiteChecker] 測試版主角尚未初始化，取消執行。", context);
                    return false;
                }
                if (service.TestProtagonist.Stamina < Math.Max(0, requiredStamina))
                {
                    ShowFailure("Ststem.Content_StaminaNotEnough", context);
                    return false;
                }
            }

            if ((prerequisites & ButtonPrerequisites.PhaseSlots) != 0)
            {
                if (service == null || service.Time == null || service.TimeConfig == null)
                {
                    Debug.LogWarning("[ActionPrerequisiteChecker] 時間系統尚未初始化，取消執行。", context);
                    return false;
                }
                // 最後一格剩餘為 0，不把目前格重複算入。
                int remainingSlots = Math.Max(0,
                    service.TimeConfig.GetTotalSlots(service.Time.CurrentPhaseIndex)
                    - service.Time.CurrentSlotInPhase);
                if (remainingSlots < Math.Max(0, requiredPhaseSlots))
                {
                    ShowFailure("Ststem.Content_TimeNotEnough", context);
                    return false;
                }
            }
            return true;
        }

        private static void ShowFailure(string key, UnityEngine.Object context)
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.ShowLocalizedBadMessage(key);
                return;
            }
            string message = DialogueManager.GetLocalizedText(key);
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning($"[ActionPrerequisiteChecker] Text Table 找不到 Key: {key}", context);
                message = key;
            }
            message = $"[em2]{message}[/em2]";
            var toast = AlertToastManager.Instance;
            if (toast != null) toast.Show(message, 3f);
            else DialogueManager.ShowAlert(message, 3f);
        }
    }
}
