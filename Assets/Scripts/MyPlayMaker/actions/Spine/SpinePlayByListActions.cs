using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MySpineSystem.PlayMaker
{
    // ==========================================
    // 1. PlayGroup Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("透過 Controller ID 呼叫 SpinePlayByList.PlayGroup()，開始播放指定的動畫組。")]
    public class SpinePlayByListPlayGroup : FsmStateAction
    {
        [RequiredField]
        [Tooltip("SpineAnimationController 上設定的 ID")]
        public FsmString controllerID;

        [RequiredField]
        [Tooltip("要播放的動畫組名稱")]
        public FsmString groupName;

        [Tooltip("播放開始後立刻觸發此事件")]
        public FsmEvent finishEvent;

        public override void Reset()
        {
            controllerID = null;
            groupName = null;
            finishEvent = null;
        }

        public override void OnEnter()
        {
            if (!string.IsNullOrEmpty(controllerID.Value) && !string.IsNullOrEmpty(groupName.Value))
            {
                var playByList = SpinePlayByList.GetByControllerID(controllerID.Value);
                if (playByList != null)
                {
                    playByList.PlayGroup(groupName.Value);
                }
            }

            Finish();

            if (finishEvent != null)
            {
                Fsm.Event(finishEvent);
            }
        }
    }

    // ==========================================
    // 2. PlayGroupAndGoBack Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("透過 Controller ID 呼叫 SpinePlayByList.PlayGroupAndGoBack()，播放指定組一次後返回先前的組。")]
    public class SpinePlayByListPlayGroupAndGoBack : FsmStateAction
    {
        [RequiredField]
        [Tooltip("SpineAnimationController 上設定的 ID")]
        public FsmString controllerID;

        [RequiredField]
        [Tooltip("要臨時播放的動畫組名稱")]
        public FsmString groupName;

        [Tooltip("呼叫後立刻觸發此事件")]
        public FsmEvent finishEvent;

        public override void Reset()
        {
            controllerID = null;
            groupName = null;
            finishEvent = null;
        }

        public override void OnEnter()
        {
            if (!string.IsNullOrEmpty(controllerID.Value) && !string.IsNullOrEmpty(groupName.Value))
            {
                var playByList = SpinePlayByList.GetByControllerID(controllerID.Value);
                if (playByList != null)
                {
                    playByList.PlayGroupAndGoBack(groupName.Value);
                }
            }

            Finish();

            if (finishEvent != null)
            {
                Fsm.Event(finishEvent);
            }
        }
    }

    // ==========================================
    // 3. StopPlaying Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("透過 Controller ID 呼叫 SpinePlayByList.StopPlaying()，停止目前的動畫組播放。")]
    public class SpinePlayByListStopPlaying : FsmStateAction
    {
        [RequiredField]
        [Tooltip("SpineAnimationController 上設定的 ID")]
        public FsmString controllerID;

        public override void Reset()
        {
            controllerID = null;
        }

        public override void OnEnter()
        {
            if (!string.IsNullOrEmpty(controllerID.Value))
            {
                var playByList = SpinePlayByList.GetByControllerID(controllerID.Value);
                if (playByList != null)
                {
                    playByList.StopPlaying();
                }
            }

            Finish();
        }
    }

    // ==========================================
    // 4. WaitForGroupCompleted Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("等待 SpinePlayByList 的動畫組鏈播放完畢後，發送指定事件。可選擇是否只監聽特定組名。")]
    public class SpinePlayByListWaitForGroupCompleted : FsmStateAction
    {
        [RequiredField]
        [Tooltip("SpineAnimationController 上設定的 ID")]
        public FsmString controllerID;

        [Tooltip("若有填，只在完成的組名與此相符時才觸發事件；留空則任何組完成都觸發。")]
        public FsmString filterGroupName;

        [Tooltip("將完成的組名存入此變數（可選）")]
        [UIHint(UIHint.Variable)]
        public FsmString storeCompletedGroupName;

        [RequiredField]
        [Tooltip("動畫組播放完畢時觸發此事件")]
        public FsmEvent completedEvent;

        private SpinePlayByList _cachedPlayByList;

        public override void Reset()
        {
            controllerID = null;
            filterGroupName = new FsmString { UseVariable = false, Value = "" };
            storeCompletedGroupName = null;
            completedEvent = null;
        }

        public override void OnEnter()
        {
            _cachedPlayByList = null;

            if (string.IsNullOrEmpty(controllerID.Value)) return;

            _cachedPlayByList = SpinePlayByList.GetByControllerID(controllerID.Value);
            if (_cachedPlayByList != null)
            {
                _cachedPlayByList.OnGroupCompleted += HandleGroupCompleted;
            }
        }

        public override void OnExit()
        {
            if (_cachedPlayByList != null)
            {
                _cachedPlayByList.OnGroupCompleted -= HandleGroupCompleted;
                _cachedPlayByList = null;
            }
        }

        private void HandleGroupCompleted(string completedName)
        {
            // 若有指定過濾組名，且不相符，就忽略
            if (!string.IsNullOrEmpty(filterGroupName.Value)
                && !string.Equals(filterGroupName.Value, completedName, System.StringComparison.Ordinal))
            {
                return;
            }

            if (storeCompletedGroupName != null && !storeCompletedGroupName.IsNone)
            {
                storeCompletedGroupName.Value = completedName;
            }

            if (completedEvent != null)
            {
                Fsm.Event(completedEvent);
            }
        }
    }

    // ==========================================
    // 5. IsPlaying 狀態查詢 Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("查詢 SpinePlayByList 是否正在播放，並可依結果發送不同事件。")]
    public class SpinePlayByListIsPlaying : FsmStateAction
    {
        [RequiredField]
        [Tooltip("SpineAnimationController 上設定的 ID")]
        public FsmString controllerID;

        [Tooltip("將 IsPlaying 結果存入此 Bool 變數")]
        [UIHint(UIHint.Variable)]
        public FsmBool storeResult;

        [Tooltip("將目前播放中的組名存入此變數（未播放時為空字串）")]
        [UIHint(UIHint.Variable)]
        public FsmString storeCurrentGroupName;

        [Tooltip("正在播放時觸發")]
        public FsmEvent isPlayingEvent;

        [Tooltip("未在播放時觸發")]
        public FsmEvent isNotPlayingEvent;

        public override void Reset()
        {
            controllerID = null;
            storeResult = null;
            storeCurrentGroupName = null;
            isPlayingEvent = null;
            isNotPlayingEvent = null;
        }

        public override void OnEnter()
        {
            bool playing = false;
            string currentName = "";

            if (!string.IsNullOrEmpty(controllerID.Value))
            {
                var playByList = SpinePlayByList.GetByControllerID(controllerID.Value);
                if (playByList != null)
                {
                    playing = playByList.IsPlaying;
                    currentName = playByList.CurrentGroupName ?? "";
                }
            }

            if (storeResult != null && !storeResult.IsNone)
            {
                storeResult.Value = playing;
            }

            if (storeCurrentGroupName != null && !storeCurrentGroupName.IsNone)
            {
                storeCurrentGroupName.Value = currentName;
            }

            Finish();

            if (playing && isPlayingEvent != null)
            {
                Fsm.Event(isPlayingEvent);
            }
            else if (!playing && isNotPlayingEvent != null)
            {
                Fsm.Event(isNotPlayingEvent);
            }
        }
    }
}
