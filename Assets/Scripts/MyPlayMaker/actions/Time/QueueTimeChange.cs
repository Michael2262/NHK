using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  QueueTimeChange — 佇列時間推進                                    ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  命令存入 SceneActionQueue，於「下一個場景」的 SceneReadyCoordinator ║
    // ║  (Task_ExecuteSceneActionQueue) 在淡入前執行。                     ║
    // ║                                                                    ║
    // ║  【用法約定（方案 A）】登記後請馬上切場景。佇列一律在下一站被消費， ║
    // ║  撐過一次轉場仍沒被消費的命令會被過期保險丟棄。                     ║
    // ║                                                                    ║
    // ║  Day 類型為「直接跳日」（不播隔天演出，因為執行時機在轉場壓幕中）。 ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("佇列時間推進，於下一個場景載入後、淡入前執行。登記後請馬上切場景。" +
             "Day 為直接跳日（無隔天演出）。")]
    public class QueueTimeChange : FsmStateAction
    {
        [Tooltip("推進類型：Slot = 推進 N 格 / Phase = 跳到下一階段 / Day = 直接跳日")]
        public SceneActionQueueModel.TimeAdvanceType advanceType;

        [Tooltip("推進格數（僅 Slot 類型有效，預設 1）")]
        public FsmInt slots;

        public override void Reset()
        {
            advanceType = SceneActionQueueModel.TimeAdvanceType.Slot;
            slots = 1;
        }

        public override void OnEnter()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[QueueTimeChange] 找不到 GameStatusService！");
            }
            else
            {
                service.SceneActionQueue.EnqueueTimeChange(advanceType, slots.Value);
            }

            Finish();
        }
    }
}
