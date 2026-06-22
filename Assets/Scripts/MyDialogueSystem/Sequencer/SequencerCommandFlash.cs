using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  Flash — 全螢幕閃一下某顏色（閃完自動回到透明）                    ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：Flash([顏色], [峰值Alpha], [總時間], [峰值停留], [是否等待]) ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 顏色       (選填, 預設 white)                                   ║
    // ║      具名色：red / white / black / blue / green / yellow /        ║
    // ║              cyan / magenta / gray …                              ║
    // ║      或十六進位：#FF0000 / #FFAA00 等                              ║
    // ║  - 峰值Alpha  (選填, 預設 0.5)  閃光最濃時的不透明度 (0~1)         ║
    // ║  - 總時間     (選填, 預設 0.2)  淡入+淡出總秒數（不含停留）         ║
    // ║  - 峰值停留   (選填, 預設 0)    在最濃處停留的秒數                  ║
    // ║  - 是否等待   (選填, 預設 false)                                   ║
    // ║      false = 開火即放行，不打斷對話（推薦）                        ║
    // ║      true  = 等閃光跑完才繼續下一個指令                            ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  Flash()                  → 白閃，峰值 0.5，0.2 秒                ║
    // ║  Flash(red)               → 紅閃（受傷感）                        ║
    // ║  Flash(red, 0.6, 0.25)    → 紅色、峰值 0.6、總長 0.25 秒          ║
    // ║  Flash(white, 1, 0.1)     → 強烈白閃（雷電）                      ║
    // ║  Flash(#FFAA00, 0.5, 0.3, 0.1) → 橘閃，峰值停 0.1 秒             ║
    // ║  Flash(red, 0.6, 0.25, 0, true) → 等閃完才放行                   ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandFlash : SequencerCommand
    {
        public void Awake()
        {
            string colorStr = GetParameter(0, "white");
            float peakAlpha = GetParameterAsFloat(1, 0.5f);
            float duration = GetParameterAsFloat(2, 0.2f);
            float hold = GetParameterAsFloat(3, 0f);
            bool wait = GetParameterAsBool(4, false);

            if (ScreenFlasher.Instance == null)
            {
                Debug.LogWarning("[Flash] ScreenFlasher 不存在，直接結束。");
                Stop();
                return;
            }

            // 解析顏色（具名色 + 十六進位皆可），失敗則退回白色
            if (!ColorUtility.TryParseHtmlString(colorStr, out Color color))
            {
                Debug.LogWarning($"[Flash] 無法解析顏色 \"{colorStr}\"，改用 white。");
                color = Color.white;
            }

            ScreenFlasher.Instance.Flash(color, peakAlpha, duration, hold);

            if (wait)
            {
                StartCoroutine(WaitRoutine(duration + hold));
            }
            else
            {
                Debug.Log($"[Flash] 閃光觸發 (顏色: {colorStr}, 峰值: {peakAlpha}, 時間: {duration}s, 停留: {hold}s)");
                Stop();
            }
        }

        private System.Collections.IEnumerator WaitRoutine(float total)
        {
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Debug.Log("[Flash] 閃光完成（等待模式）。");
            Stop();
        }
    }
}
