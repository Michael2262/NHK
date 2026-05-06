using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  FadeOut — 壓幕（淡出至遮蔽色）                                   ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：FadeOut([轉場色Phase], [持續時間])                          ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 轉場色Phase (選填, 預設 -1)                                    ║
    // ║      -1 = 依照目前 Phase 自動決定顏色                              ║
    // ║       0 = 白天, 1 = 黃昏, 2 = 晚上, 3 = 深夜                      ║
    // ║  - 持續時間   (選填, 預設使用 TransitionFader 的預設值)             ║
    // ║      傳入秒數可自訂淡出速度                                        ║
    // ║                                                                    ║
    // ║  說明：                                                            ║
    // ║  執行後畫面會停留在遮蔽狀態，直到呼叫 FadeIn() 才會恢復。          ║
    // ║  適合搭配 ChangeScene(場景, -2) 在壓幕中切場景。                   ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  FadeOut()            → 用當前 Phase 色，預設速度淡出              ║
    // ║  FadeOut(-1)          → 同上                                       ║
    // ║  FadeOut(2)           → 用晚上色淡出                               ║
    // ║  FadeOut(-1, 0.5)     → 用當前 Phase 色，0.5 秒淡出               ║
    // ║  FadeOut(2, 0.3)      → 用晚上色，0.3 秒淡出                      ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandFadeOut : SequencerCommand
    {
        public void Awake()
        {
            int phaseIndex = GetParameterAsInt(0, -1);
            float duration = GetParameterAsFloat(1, -1f);

            if (TransitionFader.Instance == null)
            {
                Debug.LogWarning("[FadeOut] TransitionFader 不存在，直接結束。");
                Stop();
                return;
            }

            if (duration > 0f)
            {
                // 自訂時間
                StartCoroutine(FadeRoutine(phaseIndex, duration));
            }
            else
            {
                // 使用 TransitionFader 預設時間
                StartCoroutine(FadeRoutineDefault(phaseIndex));
            }
        }

        private System.Collections.IEnumerator FadeRoutine(int phaseIndex, float duration)
        {
            yield return TransitionFader.Instance.FadeToColor(phaseIndex, duration);
            Debug.Log($"[FadeOut] 壓幕完成 (Phase: {phaseIndex}, Duration: {duration}s)");
            Stop();
        }

        private System.Collections.IEnumerator FadeRoutineDefault(int phaseIndex)
        {
            yield return TransitionFader.Instance.FadeToColor(phaseIndex);
            Debug.Log($"[FadeOut] 壓幕完成 (Phase: {phaseIndex}, 預設速度)");
            Stop();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  FadeIn — 開幕（從遮蔽色淡入至透明）                              ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：FadeIn([持續時間])                                          ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 持續時間   (選填, 預設使用 TransitionFader 的預設值)             ║
    // ║      傳入秒數可自訂淡入速度                                        ║
    // ║                                                                    ║
    // ║  說明：                                                            ║
    // ║  將畫面從遮蔽狀態恢復至透明。                                      ║
    // ║  必須在先前呼叫過 FadeOut() 之後使用才有意義。                     ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  FadeIn()             → 用預設速度淡入                             ║
    // ║  FadeIn(0.5)          → 0.5 秒淡入                                ║
    // ║  FadeIn(0.2)          → 0.2 秒快速淡入                            ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandFadeIn : SequencerCommand
    {
        public void Awake()
        {
            float duration = GetParameterAsFloat(0, -1f);

            if (TransitionFader.Instance == null)
            {
                Debug.LogWarning("[FadeIn] TransitionFader 不存在，直接結束。");
                Stop();
                return;
            }

            if (duration > 0f)
            {
                StartCoroutine(FadeRoutine(duration));
            }
            else
            {
                StartCoroutine(FadeRoutineDefault());
            }
        }

        private System.Collections.IEnumerator FadeRoutine(float duration)
        {
            yield return TransitionFader.Instance.FadeToClear(duration);
            Debug.Log($"[FadeIn] 開幕完成 (Duration: {duration}s)");
            Stop();
        }

        private System.Collections.IEnumerator FadeRoutineDefault()
        {
            yield return TransitionFader.Instance.FadeToClear();
            Debug.Log("[FadeIn] 開幕完成 (預設速度)");
            Stop();
        }
    }
}
