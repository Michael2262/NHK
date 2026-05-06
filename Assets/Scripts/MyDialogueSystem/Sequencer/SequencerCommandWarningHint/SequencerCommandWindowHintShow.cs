using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: WindowHintShow(id, [fadeDuration])
    /// 顯示指定 ID 的 WindowHint，淡入出現。
    /// fadeDuration 可省略，省略則使用 Inspector 預設值。
    /// </summary>
    public class SequencerCommandWindowHintShow : SequencerCommand
    {
        public void Awake()
        {
            string id = GetParameter(0);
            float fade = GetParameterAsFloat(1, -1f);

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("SequencerCommandWindowHintShow: 未提供 ID 參數。");
            }
            else if (WindowHintController.Instance != null)
            {
                WindowHintController.Instance.Show(id, fade);
            }

            Stop();
        }
    }
}