using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Scenario")]
    [Tooltip("指定角色與行程 Flag，關閉同角色其他行程 Flag，並從此行程抽選一次活動。再次呼叫會重新抽選。")]
    public class ActivateHeroineScheduleByFlag : FsmStateAction
    {
        [RequiredField] public FsmString heroineID;
        [RequiredField, ObjectType(typeof(ProgressFlagDefinition))]
        public FsmObject stateFlag;
        public FsmEvent finishedEvent;
        public FsmEvent errorEvent;

        public override void Reset()
        {
            heroineID = null;
            stateFlag = null;
            finishedEvent = null;
            errorEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.HeroineFlagSchedule;
            var flag = stateFlag?.Value as ProgressFlagDefinition;
            bool success = model != null && flag != null && model.TryActivateFlag(heroineID?.Value, flag.FlagID);
            if (model == null || flag == null)
                Debug.LogWarning("[ActivateHeroineScheduleByFlag] 未啟用 Flag 行程模式或未指定行程 Flag。");
            Fsm.Event(success ? finishedEvent : errorEvent);
            Finish();
        }
    }
}
