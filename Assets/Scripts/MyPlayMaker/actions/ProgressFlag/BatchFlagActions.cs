using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // =================================================================
    //  批次開啟多個 Flag
    // =================================================================
    [ActionCategory("Progress - Flags")]
    [Tooltip("一次開啟多個旗標，支援拖入定義清單與 FlagBundle。")]
    public class AddMultipleFlags : FsmStateAction
    {
        [Tooltip("拖入要開啟的旗標定義")]
        public ProgressFlagDefinition[] flags;

        [Tooltip("額外引用的 FlagBundle（可為空）")]
        public FlagBundle[] bundles;

        [Tooltip("旗標的生命週期")]
        [ObjectType(typeof(FlagLifetime))]
        public FsmEnum lifetime;

        public override void OnEnter()
        {
            var model = GameStatusService.Instance.ProgressFlags;
            var lt = (FlagLifetime)lifetime.Value;

            if (flags != null)
            {
                foreach (var f in flags)
                {
                    if (f != null)
                        model.AddFlag(f.FlagID, lt);
                }
            }

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle == null) continue;
                    foreach (var id in bundle.GetFlagIDs())
                        model.AddFlag(id, lt);
                }
            }

            Finish();
        }
    }

    // =================================================================
    //  批次關閉多個 Flag
    // =================================================================
    [ActionCategory("Progress - Flags")]
    [Tooltip("一次關閉（移除）多個旗標，支援拖入定義清單與 FlagBundle。")]
    public class RemoveMultipleFlags : FsmStateAction
    {
        [Tooltip("拖入要關閉的旗標定義")]
        public ProgressFlagDefinition[] flags;

        [Tooltip("額外引用的 FlagBundle（可為空）")]
        public FlagBundle[] bundles;

        public override void OnEnter()
        {
            var model = GameStatusService.Instance.ProgressFlags;

            if (flags != null)
            {
                foreach (var f in flags)
                {
                    if (f != null)
                        model.RemoveFlag(f.FlagID);
                }
            }

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle == null) continue;
                    foreach (var id in bundle.GetFlagIDs())
                        model.RemoveFlag(id);
                }
            }

            Finish();
        }
    }
}
