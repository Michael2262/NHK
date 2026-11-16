using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>GrayOverlay(On) 開啟共用壓灰圖層，GrayOverlay(Off) 關閉。</summary>
    public class SequencerCommandGrayOverlay : SequencerCommand
    {
        public void Awake()
        {
            string action = GetParameter(0)?.Trim();
            if (string.Equals(action, "On", StringComparison.OrdinalIgnoreCase))
                GrayOverlay.SetVisible(true);
            else if (string.Equals(action, "Off", StringComparison.OrdinalIgnoreCase))
                GrayOverlay.SetVisible(false);
            else
                Debug.LogWarning($"[GrayOverlay] 無法辨識參數「{action}」，請使用 GrayOverlay(On) 或 GrayOverlay(Off)。");

            Stop();
        }
    }
}
