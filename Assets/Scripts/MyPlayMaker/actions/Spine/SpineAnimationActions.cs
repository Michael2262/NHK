using UnityEngine;
using HutongGames.PlayMaker;
using MySpineSystem; // 引用你的 AnimationTrack 列舉所在的命名空間
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MySpineSystem.PlayMaker
{
    // ==========================================
    // 1. 播放動畫的 Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("透過 Controller ID 播放指定的 Spine 動畫。")]
    public class PlaySpineAnimationByID : FsmStateAction
    {
        [RequiredField]
        [Tooltip("在 SpineAnimationController 上設定的 ID")]
        public FsmString controllerID;

        [Tooltip("要播放的軌道")]
        [ObjectType(typeof(AnimationTrack))]
        public FsmEnum track;

        [RequiredField]
        [Tooltip("動畫名稱 (例如: 'idle', 'walk')")]
        public FsmString animationName;

        [Tooltip("播放結束後的處理模式")]
        [ObjectType(typeof(SpineAnimationController.ClearMode))]
        public FsmEnum clearMode;

        [Tooltip("若選擇 ClearAfterDelay，則設定延遲秒數。-1 為使用預設值。")]
        public FsmFloat delaySeconds;

        [Tooltip("動畫開始播放後立刻觸發此事件")]
        public FsmEvent finishEvent;

        public override void Reset()
        {
            controllerID = null;
            track = AnimationTrack.Skin; // 假設 Base 是預設值
            animationName = null;
            clearMode = SpineAnimationController.ClearMode.ClearOnComplete;
            delaySeconds = -1f;
            finishEvent = null;
        }

        public override void OnEnter()
        {
            DoPlayAnimation();
            Finish();

            if (finishEvent != null)
            {
                Fsm.Event(finishEvent);
            }
        }

        void DoPlayAnimation()
        {
            if (string.IsNullOrEmpty(controllerID.Value)) return;

            // 透過你寫的靜態註冊表取得控制器
            var controller = SpineAnimationController.GetByID(controllerID.Value);

            if (controller != null)
            {
                controller.PlayAnimation(
                    (AnimationTrack)track.Value,
                    animationName.Value,
                    (SpineAnimationController.ClearMode)clearMode.Value,
                    delaySeconds.Value
                );
            }
        }
    }

    // ==========================================
    // 2. 停止動畫的 Action
    // ==========================================
    [ActionCategory("Spine Custom")]
    [Tooltip("透過 Controller ID 停止指定軌道的動畫。")]
    public class StopSpineAnimationByID : FsmStateAction
    {
        [RequiredField]
        public FsmString controllerID;

        [ObjectType(typeof(AnimationTrack))]
        public FsmEnum track;

        public override void Reset()
        {
            controllerID = null;
            track = AnimationTrack.Skin;
        }

        public override void OnEnter()
        {
            if (!string.IsNullOrEmpty(controllerID.Value))
            {
                var controller = SpineAnimationController.GetByID(controllerID.Value);
                if (controller != null)
                {
                    controller.StopAnimation((AnimationTrack)track.Value);
                }
            }
            Finish();
        }
    }
}