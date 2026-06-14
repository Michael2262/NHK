using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("佇列旗標變更（存入 SceneActionQueue）。於下一個場景淡入前執行；" +
             "小遊戲中則等小遊戲結束、時間推進後，由返回場景執行。")]
    public class QueueFlagChange : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入旗標定義（asset 檔名即 FlagID）")]
        public ProgressFlagDefinition flagDef;

        [Tooltip("true = 新增旗標, false = 移除旗標")]
        public FsmBool shouldAdd;

        [ObjectType(typeof(FlagLifetime))]
        [Tooltip("Flag 的生命週期 (僅在 shouldAdd 為 true 時有效)")]
        public FsmEnum lifetime;

        [Tooltip("是否每幀執行 (通常保持 false)")]
        public bool everyFrame;

        public override void Reset()
        {
            flagDef = null;
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
            if (GameStatusService.Instance == null)
            {
                Debug.LogWarning("[QueueFlagChange] GameStatusService.Instance 不存在！");
                return;
            }

            if (flagDef == null)
            {
                Debug.LogWarning("[QueueFlagChange] flagDef 未指定！");
                return;
            }

            FlagLifetime flagLifetime = (FlagLifetime)lifetime.Value;

            GameStatusService.Instance.SceneActionQueue.EnqueueFlagChange(
                flagDef.FlagID,
                shouldAdd.Value,
                flagLifetime
            );
        }
    }
}
