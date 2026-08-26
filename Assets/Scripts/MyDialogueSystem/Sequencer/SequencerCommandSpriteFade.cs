using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：SpriteFade(管道ID, 圖片名稱[, 秒數])
    ///
    /// 驅動場景上的 <see cref="CrossfadeSpriteActor"/>，以交叉淡化方式換場景 Sprite。
    /// 通用於任何掛了 CrossfadeSpriteActor 的場景 SpriteRenderer（背景圖只是其中一種用途，
    /// 這條 Sprite 與 BGCG／立繪那條 BG 無關）。
    ///
    /// 用法：
    ///   SpriteFade(BG, night)        -> 交叉淡化到名為 night 的圖（用元件預設秒數）
    ///   SpriteFade(BG, day, 1.5)     -> 指定 1.5 秒淡化
    ///   SpriteFade(BG, none)         -> 淡出清空（none / hide / off 皆可）
    ///
    /// 前置：目標物件上需掛 CrossfadeSpriteActor，actorID 對應第一個參數，
    ///       圖片名稱需在其 spriteList 內。
    /// </summary>
    public class SequencerCommandSpriteFade : SequencerCommand
    {
        public void Awake()
        {
            string actorID = GetParameter(0);
            string spriteName = GetParameter(1);
            float duration = GetParameterAsFloat(2, -1f); // 未給 → -1 → 用元件預設秒數

            if (string.IsNullOrEmpty(actorID))
            {
                Debug.LogWarning("[SequencerCommandSpriteFade] 缺少管道ID。用法：SpriteFade(管道ID, 圖片名稱[, 秒數])");
                Stop();
                return;
            }

            if (CrossfadeSpriteActor.Find(actorID) == null)
            {
                Debug.LogWarning($"[SequencerCommandSpriteFade] 找不到 CrossfadeSpriteActor：{actorID}，請確認目標物件已掛此元件並填好 actorID。");
                Stop();
                return;
            }

            // none / hide / off 統一視為清空
            if (IsAction(spriteName, "none", "hide", "off", "stop", "close"))
                spriteName = "none";

            CrossfadeSpriteActor.Set(actorID, spriteName, duration);

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            if (string.IsNullOrEmpty(input)) return false;
            foreach (var target in targets)
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
