using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NHK.StateTest
{
    /// <summary>
    /// 接收發放者的任務後由外部呼叫 Click 計次；完成後停在 0，等待新任務。
    /// 不自行接收滑鼠輸入，可由 Button.onClick 或其他 UnityEvent 驅動。
    /// </summary>
    [AddComponentMenu("Game/State Test/Click Count Event Trigger")]
    public class ClickCountEventTrigger : MonoBehaviour
    {
        [Header("剩餘次數顯示（可留空）")]
        [Tooltip("只顯示剩餘次數，不附加名稱。")]
        [SerializeField] private TMP_Text remainingCountText;

        [Header("任務按鈕（留空會使用同物件的 Button）")]
        [Tooltip("無任務、剩餘次數為 0 或今日已點擊時禁用。每日鎖定保留剩餘次數，隔天自動解鎖。")]
        [SerializeField] private Button taskButton;

        // 任務資料僅在執行期間由發放者提供，不沿用元件原有的 Inspector 事件。
        private int targetCount;
        private UnityEvent onClick;
        private UnityEvent onCompleted;
        private int currentCount;
        private bool invokingEvents;
        private Func<bool> canPerformTask;
        private Action recordClick;
        private TaskClickConditions conditions;
        private bool dailyAllowed = true;

        public int TargetCount => targetCount;
        public int CurrentCount => currentCount;
        public int RemainingCount => Mathf.Max(0, TargetCount - currentCount);
        public bool HasActiveTask => RemainingCount > 0;

        // 關閉再開啟不重置進度，也不會重新啟動已完成任務。
        private void OnEnable() => RefreshUI();

        private void OnDisable()
        {
            if (taskButton != null) taskButton.interactable = false;
        }

        private void Update()
        {
            // 同步其他接收器的當日點擊與跨日解鎖；不改變任務進度。
            if (HasActiveTask && dailyAllowed != (canPerformTask?.Invoke() ?? true))
                RefreshUI();
        }

        /// <summary>替換任務及事件，進度從頭開始；載入本身不觸發任何任務事件。</summary>
        public void AssignTask(int clicksRequired, UnityEvent clickEvent, UnityEvent completedEvent)
            => AssignTask(clicksRequired, clickEvent, completedEvent, null, null);

        public void AssignTask(int clicksRequired, UnityEvent clickEvent, UnityEvent completedEvent,
            Func<bool> canPerform, Action onRecordClick)
            => AssignTask(clicksRequired, clickEvent, completedEvent, canPerform, onRecordClick, null);

        public void AssignTask(int clicksRequired, UnityEvent clickEvent, UnityEvent completedEvent,
            Func<bool> canPerform, Action onRecordClick, TaskClickConditions clickConditions)
        {
            targetCount = Mathf.Max(1, clicksRequired);
            currentCount = 0;
            onClick = clickEvent;
            onCompleted = completedEvent;
            canPerformTask = canPerform;
            recordClick = onRecordClick;
            // 取本次發放的快照；舊入口或未設定條件的任務會清除前一筆限制。
            conditions = clickConditions?.Copy();
            RefreshUI();
        }

        /// <summary>綁定至 Button.onClick；停用時不計次，避免事件誤綁自身造成遞迴。</summary>
        public void Click()
        {
            // 邏輯層也檢查鎖定；直接呼叫 Click 無法繞過按鈕禁用。
            RefreshUI();
            if (!isActiveAndEnabled || invokingEvents || !HasActiveTask || !dailyAllowed) return;

            // 資源不足不計次，也不消耗當日點擊機會。
            if (conditions != null && !ActionPrerequisiteChecker.CheckAndNotify(
                conditions.prerequisites, conditions.requiredStamina, conditions.requiredPhaseSlots, this))
                return;

            // 固定本次事件來源，避免一般事件發放新任務後誤觸發新任務的完成事件。
            var clickEvent = onClick;
            var completedEvent = onCompleted;
            var clickRecorder = recordClick;
            currentCount++;
            bool completed = currentCount >= TargetCount;

            // 每次有效點擊都先記錄當天；事件內換日則歸屬點擊開始的那一天。
            clickRecorder?.Invoke();

            // 完成後保留已滿的計數，剩餘次數為 0；事件執行前先更新 UI。
            RefreshUI();
            invokingEvents = true;
            try
            {
                clickEvent?.Invoke();
                if (completed) completedEvent?.Invoke();
            }
            finally
            {
                invokingEvents = false;
                // 事件可能推進日期或發放新任務，立即同步最新狀態。
                RefreshUI();
            }
        }

        /// <summary>清空任務與事件，顯示 0 並停止接收點擊。</summary>
        public void ClearTask()
        {
            targetCount = 0;
            currentCount = 0;
            onClick = null;
            onCompleted = null;
            canPerformTask = null;
            recordClick = null;
            conditions = null;
            RefreshUI();
        }

        /// <summary>保留原 UnityEvent 入口；現在會清空任務，不會重新開始已完成任務。</summary>
        public void ResetCount() => ClearTask();

        public void RefreshUI()
        {
            // 每日鎖定只影響互動，不把剩餘次數清零；任務完成才維持 0。
            dailyAllowed = canPerformTask?.Invoke() ?? true;
            if (taskButton == null) taskButton = GetComponent<Button>();
            if (remainingCountText != null)
                remainingCountText.text = RemainingCount.ToString();
            if (taskButton != null)
                taskButton.interactable = isActiveAndEnabled && HasActiveTask && dailyAllowed;
        }
    }
}
