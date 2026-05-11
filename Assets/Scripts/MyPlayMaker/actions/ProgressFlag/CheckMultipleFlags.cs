using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Logic")]
    [Tooltip("一次檢查多個旗標是否存在，支援 Any（任一成立）或 All（全部成立）模式，也支援 FlagBundle。")]
    public class CheckMultipleFlags : FsmStateAction
    {
        public enum CheckMode
        {
            All,  // 全部都必須存在
            Any   // 任一存在即可
        }

        [Tooltip("檢查模式：All = 全部存在才為 true；Any = 任一存在即為 true")]
        public CheckMode checkMode = CheckMode.All;

        [Tooltip("拖入要檢查的旗標定義")]
        public ProgressFlagDefinition[] flags;

        [Tooltip("額外引用的 FlagBundle（可為空）")]
        public FlagBundle[] bundles;

        [Tooltip("條件成立時發送")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立時發送")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("將檢查結果存入此變數")]
        public FsmBool storeResult;

        public bool everyFrame;

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            DoCheck();
        }

        private void DoCheck()
        {
            var model = GameStatusService.Instance.ProgressFlags;

            var keys = new List<string>();

            if (flags != null)
            {
                foreach (var f in flags)
                {
                    if (f != null)
                        keys.Add(f.FlagID);
                }
            }

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle != null)
                        keys.AddRange(bundle.GetFlagIDs());
                }
            }

            bool result;
            if (keys.Count == 0)
            {
                result = (checkMode == CheckMode.All);
            }
            else if (checkMode == CheckMode.All)
            {
                result = true;
                foreach (var key in keys)
                {
                    if (!model.Contains(key))
                    {
                        result = false;
                        break;
                    }
                }
            }
            else // Any
            {
                result = false;
                foreach (var key in keys)
                {
                    if (model.Contains(key))
                    {
                        result = true;
                        break;
                    }
                }
            }

            if (!storeResult.IsNone)
                storeResult.Value = result;

            Fsm.Event(result ? trueEvent : falseEvent);
        }
    }
}
