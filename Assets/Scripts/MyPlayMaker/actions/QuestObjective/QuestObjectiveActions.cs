using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Quest - Objectives")]
    [Tooltip("已退役：不再顯示任務目標，請移除此 Action。")]
    public class RevealQuestObjective : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        public override void OnEnter()
        {
            Debug.LogWarning("[RevealQuestObjective] 舊任務系統已退役，請在 Unity Editor 移除此 Action。");
            Finish();
        }
    }

    [ActionCategory("Quest - Objectives")]
    [Tooltip("已退役：不再完成任務或設定 Flag，請移除此 Action。")]
    public class CompleteQuestObjective : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        public override void OnEnter()
        {
            Debug.LogWarning("[CompleteQuestObjective] 舊任務系統已退役，請在 Unity Editor 移除此 Action。");
            Finish();
        }
    }

    [ActionCategory("Quest - Objectives")]
    [Tooltip("已退役：不再隱藏任務目標，請移除此 Action。")]
    public class HideQuestObjective : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        public override void OnEnter()
        {
            Debug.LogWarning("[HideQuestObjective] 舊任務系統已退役，請在 Unity Editor 移除此 Action。");
            Finish();
        }
    }

    [ActionCategory("Quest - Objectives")]
    [Tooltip("已退役：固定回傳 false 並發送不相符事件，請移除此 Action。")]
    public class CheckQuestObjectiveState : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        [ObjectType(typeof(QuestObjectiveState))]
        [Tooltip("要比對的狀態（Hidden / Revealed / Completed）")]
        public FsmEnum compareState;

        [UIHint(UIHint.Variable)]
        [Tooltip("儲存比對結果（可留空）")]
        public FsmBool storeResult;

        [Tooltip("狀態相符時發送的事件（可留空）")]
        public FsmEvent isMatchEvent;

        [Tooltip("狀態不符時發送的事件（可留空）")]
        public FsmEvent isNotMatchEvent;

        public override void OnEnter()
        {
            Debug.LogWarning("[CheckQuestObjectiveState] 舊任務系統已退役，固定視為不相符；請在 Unity Editor 移除此 Action。");
            if (storeResult != null && !storeResult.IsNone) storeResult.Value = false;
            if (isNotMatchEvent != null) Fsm.Event(isNotMatchEvent);
            Finish();
        }
    }
}
