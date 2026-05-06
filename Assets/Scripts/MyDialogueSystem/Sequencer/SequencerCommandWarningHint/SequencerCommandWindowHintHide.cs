using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: WindowHintHide([fadeDuration])
    /// 淡出隱藏 WindowHint。
    /// fadeDuration 可省略，省略則使用 Inspector 預設值。
    /// </summary>
    public class SequencerCommandWindowHintHide : SequencerCommand
    {
        public void Awake()
        {
            float fade = GetParameterAsFloat(0, -1f);

            if (WindowHintController.Instance != null)
                WindowHintController.Instance.Hide(fade);

            Stop();
        }
    }
}