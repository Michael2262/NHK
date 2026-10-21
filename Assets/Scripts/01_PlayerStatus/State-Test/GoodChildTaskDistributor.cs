using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NHK.StateTest
{
    /// <summary>場景內的好寶寶任務清單，每次依序發放兩筆給指定接收器。</summary>
    [AddComponentMenu("Game/State Test/Good Child Task Distributor")]
    public class GoodChildTaskDistributor : MonoBehaviour
    {
        [Serializable]
        public class TaskDefinition
        {
            [Tooltip("完成這筆任務需要的點擊次數，至少為 1。")]
            [Min(1)] public int clickCount = 4;

            [InspectorName("一日限點擊一次")]
            [Tooltip("勾選後，同一筆任務每天只能有效點擊一次；隔天自動解鎖並保留剩餘次數。重新發放不清除當日點擊紀錄。")]
            public bool oncePerDay = false;

            [Tooltip("每次點擊前檢查；不足只顯示提示，不減少剩餘次數。")]
            public TaskClickConditions conditions = new TaskClickConditions();

            [Tooltip("每次有效點擊都執行，包含最後一次。")]
            public UnityEvent onClick = new UnityEvent();

            [Tooltip("最後一次點擊的一般事件執行後，再執行此事件。")]
            public UnityEvent onCompleted = new UnityEvent();

            // 執行期間的任務紀錄，不存檔；跟著任務而非接收器保存。
            [NonSerialized] private TimeSystemModel lastClickTime;
            [NonSerialized] private int lastClickDay;

            public bool CanPerform(TimeSystemModel time)
                => !oncePerDay || (time != null
                    && (!ReferenceEquals(lastClickTime, time) || lastClickDay != time.DayIndex));

            public void RecordClick(TimeSystemModel time)
            {
                if (!oncePerDay || time == null) return;
                lastClickTime = time;
                lastClickDay = time.DayIndex;
            }
        }

        [Header("兩個任務接收器")]
        [SerializeField] private ClickCountEventTrigger firstReceiver;
        [SerializeField] private ClickCountEventTrigger secondReceiver;

        [Header("依序發放的好寶寶任務")]
        [Tooltip("第一次發第 1、2 筆，第二次發第 3、4 筆。")]
        [SerializeField] private List<TaskDefinition> tasks = new List<TaskDefinition>();

        [Tooltip("循環發放：清單發完後，下次發任務會從第一筆重新開始。最後只剩一筆時仍清空第二個接收器。")]
        [SerializeField] private bool loopTasks = false;

        private int nextTaskIndex;

        /// <summary>尚未發放的任務數量，不包含接收器正在進行的任務。</summary>
        public int RemainingTaskCount => Mathf.Max(0, (tasks?.Count ?? 0) - nextTaskIndex);

        /// <summary>
        /// 綁定至發任務按鈕或 UnityEvent。直接取代未完成任務；
        /// 剩一筆時清空第二個接收器；全部發完後依循環設定決定是否從頭發放。
        /// </summary>
        public void DistributeTasks()
        {
            if (tasks == null || tasks.Count == 0) return;
            if (RemainingTaskCount == 0 && !loopTasks) return;
            // 先使用本次起點，成功發放後才更新進度。
            int startIndex = RemainingTaskCount == 0 ? 0 : nextTaskIndex;
            if (firstReceiver == null || secondReceiver == null || firstReceiver == secondReceiver)
            {
                Debug.LogWarning("[GoodChildTaskDistributor] 請指定兩個不同且有效的任務接收器；本次未發放任務。", this);
                return;
            }

            var firstTask = tasks[startIndex];
            bool hasSecondTask = tasks.Count - startIndex >= 2;
            var secondTask = hasSecondTask ? tasks[startIndex + 1] : null;
            // 整批先檢查，避免只換掉其中一個接收器或跳過錯誤任務。
            if (!IsValid(firstTask) || (hasSecondTask && !IsValid(secondTask)))
            {
                Debug.LogWarning("[GoodChildTaskDistributor] 待發任務不可為空，點擊次數必須至少為 1；本次未發放任務。", this);
                return;
            }

            if ((firstTask.oncePerDay || (hasSecondTask && secondTask.oncePerDay)) && CurrentTime == null)
            {
                Debug.LogWarning("[GoodChildTaskDistributor] 每日任務需要已初始化的時間系統；本次未發放任務。", this);
                return;
            }

            Assign(firstReceiver, firstTask);
            if (hasSecondTask)
                Assign(secondReceiver, secondTask);
            else
                secondReceiver.ClearTask();

            nextTaskIndex = startIndex + (hasSecondTask ? 2 : 1);
        }

        private static bool IsValid(TaskDefinition task)
            => task != null && task.clickCount >= 1;

        private static TimeSystemModel CurrentTime
            => GameStatusService.Instance != null ? GameStatusService.Instance.Time : null;

        private static void Assign(ClickCountEventTrigger receiver, TaskDefinition task)
        {
            // 替換任務時一併替換檢查及記錄入口，不向 UnityEvent 累加監聽。
            receiver.AssignTask(task.clickCount, task.onClick, task.onCompleted,
                () => task.CanPerform(CurrentTime),
                () => task.RecordClick(CurrentTime), task.conditions);
        }
    }
}
