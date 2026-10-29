using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Scenario")]
    [Tooltip("關閉指定角色全部行程 Flag，清除位置與活動並刷新圖像。存讀檔後仍離場，直到再次啟用行程。")]
    public class DeactivateHeroineSchedule : FsmStateAction
    {
        [RequiredField] public FsmString heroineID;
        public FsmEvent finishedEvent;
        public FsmEvent errorEvent;

        public override void Reset()
        {
            heroineID = null;
            finishedEvent = null;
            errorEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.HeroineFlagSchedule;
            bool success = model != null && model.TryDeactivate(heroineID?.Value);
            if (model == null) Debug.LogWarning("[DeactivateHeroineSchedule] 未啟用 Flag 行程模式。");
            Fsm.Event(success ? finishedEvent : errorEvent);
            Finish();
        }
    }
}
