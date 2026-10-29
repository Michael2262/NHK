using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Scenario")]
    [Tooltip("指定角色與活動 ID，固定執行該活動，並自動開啟所屬行程 Flag、關閉同角色其餘行程 Flag。")]
    public class ActivateHeroineSchedule : FsmStateAction
    {
        [RequiredField] public FsmString heroineID;
        [RequiredField, Tooltip("行程活動清單中的活動 ID；本次固定使用此活動，讀檔時可重新抽選。")]
        public FsmString activityID;
        public FsmEvent finishedEvent;
        public FsmEvent errorEvent;

        public override void Reset()
        {
            heroineID = null;
            activityID = null;
            finishedEvent = null;
            errorEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.HeroineFlagSchedule;
            bool success = model != null && model.TryActivateActivity(heroineID?.Value, activityID?.Value);
            if (model == null) Debug.LogWarning("[ActivateHeroineSchedule] 未啟用 Flag 行程模式。");
            Fsm.Event(success ? finishedEvent : errorEvent);
            Finish();
        }
    }
}
