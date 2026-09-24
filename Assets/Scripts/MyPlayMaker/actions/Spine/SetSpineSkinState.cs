using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Spine Custom")]
    [Tooltip("保存接收器 ID 對應的 skin 組 ID，跨場景保留並隨遊戲存檔。預設在下次進場／啟用時呈現，不使用 Queue。")]
    public class SetSpineSkinState : FsmStateAction
    {
        [RequiredField] public FsmString receiverID;
        [Tooltip("清除紀錄時可留空；組合由各接收器自行定義。")]
        public FsmString presetID;
        [Tooltip("清除此 ID 的保存紀錄，之後使用接收器自身預設。")]
        public FsmBool clearRecord;
        [Tooltip("同時通知目前啟用的同 ID 接收器立即更新外觀。")]
        public FsmBool applyImmediately;

        public override void Reset()
        {
            receiverID = null;
            presetID = null;
            clearRecord = false;
            applyImmediately = false;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.SpineSkins;
            if (model == null)
                Debug.LogWarning("[SetSpineSkinState] GameStatusService.SpineSkins 尚未初始化。");
            else
            {
                bool immediate = applyImmediately != null && applyImmediately.Value;
                bool valid = clearRecord != null && clearRecord.Value
                    ? model.ClearPreset(receiverID?.Value, immediate)
                    : model.SetPreset(receiverID?.Value, presetID?.Value, immediate);
                if (!valid) Debug.LogWarning("[SetSpineSkinState] 請填入接收器 ID；設定外觀時也必須填入 skin 組 ID。");
            }
            Finish();
        }
    }
}
