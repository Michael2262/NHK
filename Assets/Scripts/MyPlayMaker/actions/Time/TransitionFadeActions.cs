using System.Collections;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  TransitionFadeOut — 壓幕（淡出至遮蔽色）                          ║
    // ║  對應 Sequencer 指令：FadeOut(轉場色Phase, 持續時間)                ║
    // ║  執行後畫面停留在遮蔽狀態，直到 TransitionFadeIn 才恢復。           ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Transition")]
    [Tooltip("用 TransitionFader 壓幕。phaseIndex：-1=依當前 Phase 自動決定顏色，0=白天 1=黃昏 2=晚上 3=深夜。")]
    public class TransitionFadeOut : FsmStateAction
    {
        private const float TIMEOUT = 10f;

        [Tooltip("轉場色 Phase（-1 = 依當前 Phase 自動決定）")]
        public FsmInt phaseIndex;

        [Tooltip("淡出持續時間（秒）。0 或負數 = 使用 TransitionFader 預設值")]
        public FsmFloat duration;

        [Tooltip("壓幕完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        private Coroutine _routine;
        private bool _done;
        private float _elapsed;

        public override void Reset()
        {
            phaseIndex = -1;
            duration = -1f;
            finishedEvent = null;
        }

        public override void OnEnter()
        {
            _done = false;
            _elapsed = 0f;
            _routine = null;

            if (TransitionFader.Instance == null)
            {
                Debug.LogWarning("[TransitionFadeOut] TransitionFader 不存在，直接結束。");
                Fsm.Event(finishedEvent);
                Finish();
                return;
            }

            // 寄宿在 TransitionFader（DontDestroyOnLoad）上執行，不受 FSM 物件存亡影響
            _routine = TransitionFader.Instance.StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            if (duration.Value > 0f)
                yield return TransitionFader.Instance.FadeToColor(phaseIndex.Value, duration.Value);
            else
                yield return TransitionFader.Instance.FadeToColor(phaseIndex.Value);

            Debug.Log($"[TransitionFadeOut] 壓幕完成 (Phase: {phaseIndex.Value})");
            _done = true;
        }

        public override void OnUpdate()
        {
            if (_done)
            {
                _routine = null;
                Fsm.Event(finishedEvent);
                Finish();
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= TIMEOUT)
            {
                Debug.LogWarning($"[TransitionFadeOut] 等待超時（{TIMEOUT}秒）！強制結束。");
                StopRoutine();
                Fsm.Event(finishedEvent);
                Finish();
            }
        }

        public override void OnExit()
        {
            StopRoutine();
        }

        private void StopRoutine()
        {
            if (_routine != null && TransitionFader.Instance != null)
                TransitionFader.Instance.StopCoroutine(_routine);
            _routine = null;
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  TransitionFadeIn — 開幕（從遮蔽色淡入至透明）                     ║
    // ║  對應 Sequencer 指令：FadeIn(持續時間)                              ║
    // ║  必須在先前壓幕（TransitionFadeOut）之後使用才有意義。              ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Transition")]
    [Tooltip("用 TransitionFader 開幕，畫面從遮蔽狀態恢復至透明。")]
    public class TransitionFadeIn : FsmStateAction
    {
        private const float TIMEOUT = 10f;

        [Tooltip("淡入持續時間（秒）。0 或負數 = 使用 TransitionFader 預設值")]
        public FsmFloat duration;

        [Tooltip("開幕完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        private Coroutine _routine;
        private bool _done;
        private float _elapsed;

        public override void Reset()
        {
            duration = -1f;
            finishedEvent = null;
        }

        public override void OnEnter()
        {
            _done = false;
            _elapsed = 0f;
            _routine = null;

            if (TransitionFader.Instance == null)
            {
                Debug.LogWarning("[TransitionFadeIn] TransitionFader 不存在，直接結束。");
                Fsm.Event(finishedEvent);
                Finish();
                return;
            }

            _routine = TransitionFader.Instance.StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            if (duration.Value > 0f)
                yield return TransitionFader.Instance.FadeToClear(duration.Value);
            else
                yield return TransitionFader.Instance.FadeToClear();

            Debug.Log("[TransitionFadeIn] 開幕完成。");
            _done = true;
        }

        public override void OnUpdate()
        {
            if (_done)
            {
                _routine = null;
                Fsm.Event(finishedEvent);
                Finish();
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= TIMEOUT)
            {
                Debug.LogWarning($"[TransitionFadeIn] 等待超時（{TIMEOUT}秒）！強制結束。");
                StopRoutine();
                Fsm.Event(finishedEvent);
                Finish();
            }
        }

        public override void OnExit()
        {
            StopRoutine();
        }

        private void StopRoutine()
        {
            if (_routine != null && TransitionFader.Instance != null)
                TransitionFader.Instance.StopCoroutine(_routine);
            _routine = null;
        }
    }
}
