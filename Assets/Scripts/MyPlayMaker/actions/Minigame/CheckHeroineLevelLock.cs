using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("檢查指定女主角的特定興奮等級是否被鎖定。")]
    public class CheckHeroineLevelLock : FsmStateAction
    {
        [RequiredField]
        [Tooltip("女主角的唯一識別 ID (例如: sister)")]
        public FsmString heroineID;

        [RequiredField]
        [Tooltip("要檢查哪一個等級的鎖定狀態？")]
        public FsmInt level;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("儲存結果：True 代表已鎖定，False 代表未鎖定。")]
        public FsmBool isLocked;

        [Tooltip("如果已鎖定 (True)，則觸發此事件。")]
        public FsmEvent lockedEvent;

        [Tooltip("如果未鎖定 (False)，則觸發此事件。")]
        public FsmEvent notLockedEvent;

        [Tooltip("是否每幀執行？")]
        public bool everyFrame;

        public override void Reset()
        {
            heroineID = null;
            level = 0;
            isLocked = null;
            lockedEvent = null;
            notLockedEvent = null;
            everyFrame = false;
        }

        public override void OnEnter()
        { Debug.LogWarning("[CheckHeroineLevelLock] 舊等級／經驗門檻功能已退役，請移除此 FSM Action。"); Finish(); }

        public override void OnUpdate()
        {}

    }
}