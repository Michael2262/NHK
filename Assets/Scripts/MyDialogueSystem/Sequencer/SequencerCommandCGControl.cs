using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：CGControl(動作, 參數1, 參數2, 參數3)
    /// 
    /// 支援動作：
    /// 1. ShowBG:   CGControl(ShowBG, 背景名稱, Alpha[1], 時間[預設])
    /// 2. HideBG:   CGControl(HideBG, 時間[預設])
    /// 3. SwitchBG: CGControl(SwitchBG, 背景名稱, Alpha[1], 時間[預設])
    /// 4. ShowCG:   CGControl(ShowCG, 插圖名稱, Alpha[1], 時間[預設])
    /// 5. HideCG:   CGControl(HideCG, 時間[預設])
    /// 6. SwitchCG: CGControl(SwitchCG, 插圖名稱, Alpha[1], 時間[預設])
    /// 7. ZoomIn:   CGControl(ZoomIn, 目標物件名稱, 縮放倍率[1.5], 時間[1.0])
    /// 8. ZoomOut:  CGControl(ZoomOut, 目標物件名稱, 縮放倍率[0.5], 時間[1.0])
    /// 9. ResetZoom:CGControl(ResetZoom, 時間[1.0])
    /// 10. Focus:   CGControl(Focus, 目標物件名稱, 時間[1.0])
    /// 11.震動
    /// 12. CGMode:  CGControl(CGMode, 模式名稱, 時間[預設]) — 換衣服/變體，畫面上的 CG 自動換成該 mode 的版本
    /// 13. BGMode:  CGControl(BGMode, 模式名稱, 時間[預設]) — 背景變體（日/夜…），自動換成該 mode 的版本
    /// </summary>
    public class SequencerCommandCGControl : SequencerCommand
    {
        public void Awake()
        {
            if (CGController.Instance == null)
            {
                Debug.LogWarning("[CGControl] 場景中找不到 CGController 實例。");
                Stop();
                return;
            }

            string action = GetParameter(0);

            // --- 背景控制 (BG) ---
            if (IsAction(action, "ShowBG"))
            {
                string bgName = GetParameter(1);
                float alpha = GetParameterAsFloat(2, 1f);
                float duration = GetParameterAsFloat(3, -1f);
                CGController.Instance.ShowBG(bgName, alpha, duration);
            }
            else if (IsAction(action, "HideBG"))
            {
                float duration = GetParameterAsFloat(1, -1f);
                CGController.Instance.HideBG(duration);
            }
            else if (IsAction(action, "SwitchBG"))
            {
                string bgName = GetParameter(1);
                float alpha = GetParameterAsFloat(2, 1f);
                float duration = GetParameterAsFloat(3, -1f);
                CGController.Instance.SwitchBG(bgName, alpha, duration);
            }

            // --- 插圖控制 (CG) ---
            else if (IsAction(action, "ShowCG"))
            {
                string cgName = GetParameter(1);
                float alpha = GetParameterAsFloat(2, 1f);
                float duration = GetParameterAsFloat(3, -1f);
                CGController.Instance.ShowCG(cgName, alpha, duration);
            }
            else if (IsAction(action, "HideCG"))
            {
                float duration = GetParameterAsFloat(1, -1f);
                CGController.Instance.HideCG(duration);
            }
            else if (IsAction(action, "SwitchCG"))
            {
                string cgName = GetParameter(1);
                float alpha = GetParameterAsFloat(2, 1f);
                float duration = GetParameterAsFloat(3, -1f);
                CGController.Instance.SwitchCG(cgName, alpha, duration);
            }

            // --- 縮放與聚焦 (Zoom/Focus) ---
            else if (IsAction(action, "ZoomIn"))
            {
                Transform target = GetSubject(1); // 取得場景中的 GameObject
                float scale = GetParameterAsFloat(2, 1.5f);
                float duration = GetParameterAsFloat(3, 1.0f);
                if (target != null) CGController.Instance.ZoomIn(target.gameObject, scale, duration);
            }
            else if (IsAction(action, "ZoomOut"))
            {
                Transform target = GetSubject(1);
                float scale = GetParameterAsFloat(2, 0.5f);
                float duration = GetParameterAsFloat(3, 1.0f);
                if (target != null) CGController.Instance.ZoomOut(target.gameObject, scale, duration);
            }
            else if (IsAction(action, "ResetZoom"))
            {
                float duration = GetParameterAsFloat(1, 1.0f);
                CGController.Instance.ResetZoom(duration);
            }
            else if (IsAction(action, "Focus"))
            {
                Transform target = GetSubject(1);
                float duration = GetParameterAsFloat(2, 1.0f);
                if (target != null) CGController.Instance.MoveFocusTo(target.gameObject, duration);
            }
            // --- 震動控制 (Shake) ---
            else if (IsAction(action, "ShakeBG"))
            {
                float duration = GetParameterAsFloat(1, 0.5f);
                float strength = GetParameterAsFloat(2, -1f);
                CGController.Instance.ShakeBG(duration, strength);
            }
            else if (IsAction(action, "ShakeCG"))
            {
                float duration = GetParameterAsFloat(1, 0.5f);
                float strength = GetParameterAsFloat(2, -1f);
                CGController.Instance.ShakeCG(duration, strength);
            }

            // --- Mode 切換（換衣服 / 變體） ---
            else if (IsAction(action, "CGMode", "SetCGMode"))
            {
                string mode = GetParameter(1);
                float duration = GetParameterAsFloat(2, -1f);
                CGController.Instance.SetCGMode(mode, duration);
            }
            else if (IsAction(action, "BGMode", "SetBGMode"))
            {
                string mode = GetParameter(1);
                float duration = GetParameterAsFloat(2, -1f);
                CGController.Instance.SetBGMode(mode, duration);
            }
            else
            {
                Debug.LogWarning($"[CGControl] 未知的動作類型: {action}");
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