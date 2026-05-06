using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("佇列旗標變更。此動作會在小遊戲結束後、時間推進後、場景淡入前執行。")]
    public class QueueFlagChange : FsmStateAction
    {
        [RequiredField]
        [Tooltip("旗標的唯一識別 ID")]
        public FsmString flagID;

        [Tooltip("true = 新增旗標, false = 移除旗標")]
        public FsmBool shouldAdd;

        [ObjectType(typeof(FlagLifetime))]
        [Tooltip("Flag 的生命週期 (僅在 shouldAdd 為 true 時有效)")]
        public FsmEnum lifetime;

        [Tooltip("是否每幀執行 (通常保持 false)")]
        public bool everyFrame;

        public override void Reset()
        {
            flagID = null;
            shouldAdd = true;
            lifetime = new FsmEnum { Value = FlagLifetime.Persistent };
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoQueueFlagChange();

            if (!everyFrame)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            if (everyFrame)
            {
                DoQueueFlagChange();
            }
        }

        private void DoQueueFlagChange()
        {
            if (MinigameManager.Instance == null)
            {
                Debug.LogWarning("[QueueFlagChange] MinigameManager.Instance 不存在！");
                return;
            }

            if (string.IsNullOrEmpty(flagID.Value))
            {
                Debug.LogWarning("[QueueFlagChange] flagID 為空！");
                return;
            }

            FlagLifetime flagLifetime = (FlagLifetime)lifetime.Value;

            MinigameManager.Instance.QueueFlagChange(
                flagID.Value,
                shouldAdd.Value,
                flagLifetime
            );
        }
    }
}
