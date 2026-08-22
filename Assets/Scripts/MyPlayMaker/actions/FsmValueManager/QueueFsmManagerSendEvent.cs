using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  QueueFsmManagerSendEvent — 佇列「透過 FsmValueManager 發 FSM 事件」║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  同 QueueFlagChange / QueueTimeChange：命令存入 SceneActionQueue，   ║
    // ║  於「下一個場景」Ready、淡入前由 Task_ExecuteSceneActionQueue 執行。 ║
    // ║                                                                    ║
    // ║  執行時 FsmValueManager.Instance 已是新場景那份，因此 target        ║
    // ║  （ID 或 Group 名）指向的是新場景註冊的 FSM。                        ║
    // ║                                                                    ║
    // ║  【用法約定】登記後請馬上切場景，佇列一律在下一站被消費。            ║
    // ║  可被 ClearPendingActions 一併清除。                                ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("FsmValueManager")]
    [Tooltip("佇列一個 FSM 事件（存入 SceneActionQueue），於下一個場景 Ready、淡入前，" +
             "透過 FsmValueManager 對新場景註冊的 FSM 發送。登記後請馬上切場景。")]
    public class QueueFsmManagerSendEvent : FsmStateAction
    {
        [RequiredField]
        [Tooltip("目標 ID 或 Group 名稱（指下一場景 FsmValueManager 上註冊的 ID/Group）")]
        public FsmString targetIdentifier;

        [Tooltip("勾選後以 Group 模式發送，否則以單一 ID 發送")]
        public bool useGroupMode;

        [RequiredField]
        [Tooltip("要發送的 Event 名稱")]
        public FsmString eventName;

        public override void Reset()
        {
            targetIdentifier = null;
            useGroupMode = false;
            eventName = null;
        }

        public override void OnEnter()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[QueueFsmManagerSendEvent] 找不到 GameStatusService！");
            }
            else if (string.IsNullOrEmpty(targetIdentifier.Value) || string.IsNullOrEmpty(eventName.Value))
            {
                Debug.LogWarning($"[QueueFsmManagerSendEvent] target 或 eventName 為空" +
                                 $"（target={targetIdentifier.Value}, event={eventName.Value}），未佇列");
            }
            else
            {
                service.SceneActionQueue.EnqueueSendEvent(
                    targetIdentifier.Value,
                    useGroupMode,
                    eventName.Value
                );
            }

            Finish();
        }
    }
}
