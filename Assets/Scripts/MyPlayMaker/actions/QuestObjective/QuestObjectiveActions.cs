using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Quest - Objectives")]
    [Tooltip("顯示任務目標（未顯示 → 已顯示未完成）。")]
    public class RevealQuestObjective : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        public override void OnEnter()
        {
            if (objectiveDef != null)
                GameStatusService.Instance.QuestObjectives.Reveal(objectiveDef.ObjectiveID);
            Finish();
        }
    }

    [ActionCategory("Quest - Objectives")]
    [Tooltip("完成任務目標（任何狀態 → 已完成）。若 SO 勾了 MirrorFlagOnComplete，會同步加 Persistent Flag：ObjDone_目標ID。")]
    public class CompleteQuestObjective : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        public override void OnEnter()
        {
            if (objectiveDef != null)
                GameStatusService.Instance.QuestObjectives.Complete(objectiveDef.ObjectiveID);
            Finish();
        }
    }

    [ActionCategory("Quest - Objectives")]
    [Tooltip("隱藏任務目標（回到未顯示）。主要給除錯 / 特殊劇情用。")]
    public class HideQuestObjective : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入任務目標定義")]
        public QuestObjectiveDefinition objectiveDef;

        public override void OnEnter()
        {
            if (objectiveDef != null)
                GameStatusService.Instance.QuestObjectives.Hide(objectiveDef.ObjectiveID);
            Finish();
        }
    }

    [ActionCategory("Quest - Objectives")]
    [Tooltip("檢查任務目標狀態，儲存布林結果並可發送事件。")]
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
            if (objectiveDef != null)
            {
                var current = GameStatusService.Instance.QuestObjectives.GetState(objectiveDef.ObjectiveID);
                bool match = current == (QuestObjectiveState)compareState.Value;

                if (!storeResult.IsNone) storeResult.Value = match;

                if (match && isMatchEvent != null) Fsm.Event(isMatchEvent);
                else if (!match && isNotMatchEvent != null) Fsm.Event(isNotMatchEvent);
            }
            Finish();
        }
    }
}
