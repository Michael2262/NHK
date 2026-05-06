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
    /// 7. Move: TachieControl(Move, Sister, 500, 0.5)
    /// 8. Left/Right/Center: TachieControl(Left, Sister, 0.5)
    /// 9. Clear: TachieControl(Clear, 0.5)
    /// </summary>


    public class SequencerCommandTachieControl : SequencerCommand
    {
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

            // --- 4. 身體與位置控制 ---
            else if (IsAction(action, "Body")) TachieController.Instance.ChangeBody(actorID, GetParameter(2));
            else if (IsAction(action, "Move"))
            {
                float xValue = GetParameterAsFloat(2);
                float duration = GetParameterAsFloat(3, -1f);
                TachieController.Instance.MoveToX(actorID, xValue, duration);
            }
            else if (IsAction(action, "Left")) TachieController.Instance.MoveToLeft(actorID, GetParameterAsFloat(2, -1f));
            else if (IsAction(action, "Right")) TachieController.Instance.MoveToRight(actorID, GetParameterAsFloat(2, -1f));
            else if (IsAction(action, "Center")) TachieController.Instance.MoveToCenter(actorID, GetParameterAsFloat(2, -1f));
            else if (IsAction(action, "Clear")) TachieController.Instance.ClearAll(GetParameterAsFloat(1, -1f));
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