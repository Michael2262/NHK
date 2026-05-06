using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandTimeScale : SequencerCommand
    {
        public void Awake()
        {
            // 取得指令括號內的第一個參數
            float scale = GetParameterAsFloat(0);
            Time.timeScale = scale;

            // 告訴系統這個指令已經執行完畢
            Stop();
        }
    }
}