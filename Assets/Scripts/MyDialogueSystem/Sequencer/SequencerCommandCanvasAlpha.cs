using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandCanvasAlpha : SequencerCommand
    {
        public void Awake()
        {
            // 取得參數：第一個是物件名稱，第二個是 Alpha 值，第三個是漸變時間(選擇性)
            Transform target = GetSubject(0);
            float targetAlpha = GetParameterAsFloat(1);

            if (target != null)
            {
                CanvasGroup cg = target.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = targetAlpha;
            }
            Stop();
        }
    }
}