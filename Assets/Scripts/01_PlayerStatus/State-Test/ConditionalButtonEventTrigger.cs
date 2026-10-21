using System;
using UnityEngine;
using UnityEngine.Events;

namespace NHK.StateTest
{
    [Flags]
    public enum ButtonPrerequisites
    {
        [InspectorName("無")] None = 0,
        [InspectorName("主角體力")] Stamina = 1,
        [InspectorName("當前 Phase 剩餘 Slot")] PhaseSlots = 2
    }

    /// <summary>Button.onClick 呼叫 Trigger；條件全部通過才執行後續事件，不自動消耗資源。</summary>
    [AddComponentMenu("Game/State Test/Conditional Button Event Trigger")]
    public class ConditionalButtonEventTrigger : MonoBehaviour
    {
        [SerializeField] private ButtonPrerequisites prerequisites = ButtonPrerequisites.None;
        [SerializeField, Min(0)] private int requiredStamina = 1;
        [SerializeField, Min(0)] private int requiredPhaseSlots = 1;
        [SerializeField] private UnityEvent onTriggered = new UnityEvent();

        private bool invokingEvents;

        private void OnValidate()
        {
            requiredStamina = Mathf.Max(0, requiredStamina);
            requiredPhaseSlots = Mathf.Max(0, requiredPhaseSlots);
        }

        /// <summary>先檢查體力，再檢查時間；失敗只提示第一個原因。</summary>
        public void Trigger()
        {
            if (!isActiveAndEnabled || invokingEvents) return;
            if (!ActionPrerequisiteChecker.CheckAndNotify(
                prerequisites, requiredStamina, requiredPhaseSlots, this)) return;

            invokingEvents = true;
            try
            {
                onTriggered?.Invoke();
            }
            finally
            {
                invokingEvents = false;
            }
        }

    }
}
