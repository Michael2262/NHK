using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>

    /// 語法：TachieControl(動作, 角色ID, 參數1, 參數2, ...)

    /// 

    /// 支援動作：
    /// 1. Show: TachieControl(Show, Sister, 0.5) -> 自動設為顯示
    /// 2. Hide: TachieControl(Hide, Sister, 0.5) -> 自動設為隱藏
    /// 3. Visible: TachieControl(Visible, Sister, true, 0.5) -> 原本的寫法
    /// 4. Face (一次換多個): TachieControl(Face, Sister, Eye, Mouth, Eyebrow, Blush, Other, Above)
    /// 5. Brow/Eye/Mouth/Blush/Other/Above (單獨換): TachieControl(Eye, Sister, Cry)
    /// 6. Body: TachieControl(Body, Sister, Uniform)
    /// 7. Expression (表情預設): TachieControl(Expression, Sister, Angry)
    /// 8. Move: TachieControl(Move, Sister, 500, 0.5)
    ///    也可用具名預設: TachieControl(Move, Sister, Adv) -> 位置與時間查 MovePresets（可加第4參數覆寫時間）
    /// 9. Left/Right/Center: TachieControl(Left, Sister, 0.5)
    /// 10. Clear: TachieControl(Clear, 0.5) -> 邊淡出邊移回原位（過程看得到）
    /// 10.5 HideAndClear: 先原地淡出，看不見之後才歸零（過程看不到）
    ///      TachieControl(HideAndClear, 0.5, 0.1)         -> 全部角色
    ///      TachieControl(HideAndClear, Sister, 0.5, 0.1) -> 指定角色或群組
    ///      參數 = 淡出時間, 淡出後額外緩衝秒數；留空用 Inspector 預設
    /// 11. Mode (切身體 mode，會立刻判定): TachieControl(Mode, Sister, Weekend)
    /// 12. ModeAll (所有角色一起切): TachieControl(ModeAll, Weekend)
    /// 13. Small (縮小): TachieControl(Small, Sister, 0.25)
    /// 14. Normal (回正常大小): TachieControl(Normal, Sister, 0.25)
    /// 15. Scale (指定倍率): TachieControl(Scale, Sister, 0.88, 0.25)
    /// 16. Shock (嚇到，上下晃一下): TachieControl(Shock, Sister, 0.4, 22, 10)
    ///     參數 = 時間, 位移強度, 晃動次數；留空則用 Inspector 預設
    /// </summary>


    public class SequencerCommandTachieControl : SequencerCommand
    {
        // ── Move 具名位置預設 ─────────────────────────────
        // 用法：TachieControl(Move, Sister, Adv);  等同  TachieControl(Move, Sister, 250, 0.5);
        // 要改某個預設的含義，只要改這裡的 (x 位置, 移動時間) 即可，所有用到該名稱的劇本一起變。
        // 名稱大小寫不敏感。
        private static readonly System.Collections.Generic.Dictionary<string, (float x, float duration)> MovePresets =
            new System.Collections.Generic.Dictionary<string, (float x, float duration)>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Adv", (350f, 0.5f) },
        };

        public void Awake()
        {
            if (TachieController.Instance == null)
            {
                Debug.LogWarning("[TachieControl] 場景中找不到 TachieController 實例。");
                Stop();
                return;
            }

            string action = GetParameter(0);
            string actorID = GetParameter(1);

            // --- 1. 顯示/隱藏 (優化邏輯) ---
            if (IsAction(action, "Show"))
            {
                // 用法：TachieControl(Show, Sister, duration)
                float duration = GetParameterAsFloat(2, -1f);
                TachieController.Instance.SetVisibility(actorID, true, duration);
            }
            else if (IsAction(action, "Hide"))
            {
                // 用法：TachieControl(Hide, Sister, duration)
                float duration = GetParameterAsFloat(2, -1f);
                TachieController.Instance.SetVisibility(actorID, false, duration);
            }
            else if (IsAction(action, "Visible"))
            {
                // 用法：TachieControl(Visible, Sister, bool, duration)
                bool isVisible = GetParameterAsBool(2);
                float duration = GetParameterAsFloat(3, -1f);
                TachieController.Instance.SetVisibility(actorID, isVisible, duration);
            }

            // --- 2. 綜合表情控制 ---
            else if (IsAction(action, "Face"))
            {
                string eye = GetParameter(2);
                string mouth = GetParameter(3);
                string eyebrow = GetParameter(4);
                string blush = GetParameter(5);
                string other = GetParameter(6);
                string above = GetParameter(7);

                if (!string.IsNullOrEmpty(eye)) TachieController.Instance.ChangeEye(actorID, eye);
                if (!string.IsNullOrEmpty(mouth)) TachieController.Instance.ChangeMouth(actorID, mouth);
                if (!string.IsNullOrEmpty(eyebrow)) TachieController.Instance.ChangeEyebrow(actorID, eyebrow);
                if (!string.IsNullOrEmpty(blush)) TachieController.Instance.ChangeBlush(actorID, blush);
                if (!string.IsNullOrEmpty(other)) TachieController.Instance.ChangeOther(actorID, other);
                if (!string.IsNullOrEmpty(above)) TachieController.Instance.ChangeAbove(actorID, above);
            }
            // --- 3. 單一部位控制 ---
            else if (IsAction(action, "Brow")) TachieController.Instance.ChangeEyebrow(actorID, GetParameter(2));
            else if (IsAction(action, "Eye")) TachieController.Instance.ChangeEye(actorID, GetParameter(2));
            else if (IsAction(action, "Mouth")) TachieController.Instance.ChangeMouth(actorID, GetParameter(2));
            else if (IsAction(action, "Blush")) TachieController.Instance.ChangeBlush(actorID, GetParameter(2));
            else if (IsAction(action, "Other")) TachieController.Instance.ChangeOther(actorID, GetParameter(2));
            else if (IsAction(action, "Above")) TachieController.Instance.ChangeAbove(actorID, GetParameter(2));

            // --- 4. 身體與表情預設控制 ---
            else if (IsAction(action, "Body")) TachieController.Instance.ChangeBody(actorID, GetParameter(2));
            else if (IsAction(action, "Expression", "Expr")) TachieController.Instance.ChangeExpression(actorID, GetParameter(2));

            // --- 4.5 身體 mode 切換 ---
            // Mode: TachieControl(Mode, 角色ID或群組, 模式名) -> 切該對象的 mode（連動群組一起）
            else if (IsAction(action, "Mode")) TachieController.Instance.SetMode(actorID, GetParameter(2));
            // ModeAll: TachieControl(ModeAll, 模式名) -> 所有角色一起切，模式名在參數1
            else if (IsAction(action, "ModeAll")) TachieController.Instance.SetModeAll(GetParameter(1));
            // --- 4.6 縮放與嚇到 ---
            // Small: TachieControl(Small, Sister, duration)
            else if (IsAction(action, "Small")) TachieController.Instance.ScaleSmall(actorID, GetParameterAsFloat(2, -1f));
            // Big / Normal: TachieControl(Big, Sister, duration)
            else if (IsAction(action, "Normal")) TachieController.Instance.ScaleNormal(actorID, GetParameterAsFloat(2, -1f));
            // Scale: TachieControl(Scale, Sister, 倍率, duration)
            else if (IsAction(action, "Scale"))
            {
                float scale = GetParameterAsFloat(2, -1f);
                float duration = GetParameterAsFloat(3, -1f);
                if (scale <= 0f)
                    Debug.LogWarning($"[TachieControl] Scale 需要一個大於 0 的倍率，收到: {GetParameter(2)}");
                else
                    TachieController.Instance.ScaleTo(actorID, scale, duration);
            }
            // Shock: TachieControl(Shock, Sister, 時間, 強度, 次數)
            else if (IsAction(action, "Shock", "Shake"))
            {
                float duration = GetParameterAsFloat(2, -1f);
                float strength = GetParameterAsFloat(3, -1f);
                int vibrato = GetParameterAsInt(4, -1);
                TachieController.Instance.Shock(actorID, duration, strength, vibrato);
            }

            else if (IsAction(action, "Move"))
            {
                // 兩種寫法：
                //   TachieControl(Move, Sister, 250, 0.5)  -> 直接指定 x 與時間
                //   TachieControl(Move, Sister, Adv)       -> 用具名預設（見 MovePresets），第4參數可覆寫時間
                string p2 = GetParameter(2);
                float xValue;
                float duration;
                if (float.TryParse(p2, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out xValue))
                {
                    // 數字寫法
                    duration = GetParameterAsFloat(3, -1f);
                }
                else if (MovePresets.TryGetValue(p2, out var preset))
                {
                    // 具名預設；第4參數若有給就覆寫預設時間
                    xValue = preset.x;
                    duration = GetParameterAsFloat(3, preset.duration);
                }
                else
                {
                    Debug.LogWarning($"[TachieControl] Move 的位置參數無法解析（不是數字也不是已知預設）: {p2}");
                    Stop();
                    return;
                }
                TachieController.Instance.MoveToX(actorID, xValue, duration);
            }
            else if (IsAction(action, "Left")) TachieController.Instance.MoveToLeft(actorID, GetParameterAsFloat(2, -1f));
            else if (IsAction(action, "Right")) TachieController.Instance.MoveToRight(actorID, GetParameterAsFloat(2, -1f));
            else if (IsAction(action, "Center")) TachieController.Instance.MoveToCenter(actorID, GetParameterAsFloat(2, -1f));
            else if (IsAction(action, "Clear")) TachieController.Instance.ClearAll(GetParameterAsFloat(1, -1f));

            // HideAndClear: 先原地淡出，完全看不見之後才歸零（玩家不會看到位移/縮放的變化過程）
            // 兩種寫法，用參數1 是不是數字來判斷：
            //   TachieControl(HideAndClear, 0.5, 0.1)         -> 全部角色
            //   TachieControl(HideAndClear, Sister, 0.5, 0.1) -> 指定角色或群組
            else if (IsAction(action, "HideAndClear", "HideClear"))
            {
                string first = GetParameter(1);
                bool isAllActors = string.IsNullOrEmpty(first) ||
                                   float.TryParse(first, System.Globalization.NumberStyles.Float,
                                                  System.Globalization.CultureInfo.InvariantCulture, out _);

                if (isAllActors)
                {
                    TachieController.Instance.HideAndClearAll(
                        GetParameterAsFloat(1, -1f), GetParameterAsFloat(2, -1f));
                }
                else
                {
                    TachieController.Instance.HideAndClear(
                        actorID, GetParameterAsFloat(2, -1f), GetParameterAsFloat(3, -1f));
                }
            }
            else
            {
                Debug.LogWarning($"[TachieControl] 未知的動作類型: {action}");
            }

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            foreach (var target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}