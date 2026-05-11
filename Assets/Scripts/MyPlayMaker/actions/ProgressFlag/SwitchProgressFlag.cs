using System;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Logic")]
    [Tooltip("欄目式旗標分歧：每一列對應一個 Flag 與一個 Event，從上到下依序檢查，碰到第一個開啟的旗標就發送對應事件。")]
    public class SwitchProgressFlag : FsmStateAction
    {
        [Serializable]
        public class FlagEventEntry
        {
            [Tooltip("拖入旗標定義")]
            public ProgressFlagDefinition flagDef;

            [Tooltip("該旗標開啟時要發送的事件")]
            public FsmEvent sendEvent;
        }

        [Tooltip("由上而下依序檢查的旗標-事件對照表")]
        public FlagEventEntry[] entries;

        [Tooltip("全部都沒命中時發送的事件（可為空）")]
        public FsmEvent noneMatchedEvent;

        public override void OnEnter()
        {
            var model = GameStatusService.Instance.ProgressFlags;

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.flagDef == null)
                        continue;

                    if (model.Contains(entry.flagDef.FlagID))
                    {
                        Fsm.Event(entry.sendEvent);
                        Finish();
                        return;
                    }
                }
            }

            // 全部沒命中
            Fsm.Event(noneMatchedEvent);
            Finish();
        }
    }
}
