using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // 對應 Sequencer 指令 CGControl(BGMode, ...) / CGControl(CGMode, ...)
    // 兩者最終都呼叫 CGController.SetBGMode / SetCGMode，會自動寫回 VisualModeModel，
    // 因此切換的 mode 會被存檔保存、跨場景維持、開新檔重置。

    [ActionCategory("CG")]
    [Tooltip("切換背景 BG mode（日/夜等變體）。會寫回存檔；若目前有背景會立刻重抓圖。")]
    public class CGSetBGMode : FsmStateAction
    {
        [RequiredField]
        [Tooltip("要切換到的 BG mode 名稱")]
        public FsmString modeName;

        [Tooltip("切換時間（秒），負值 = 使用控制器預設")]
        public FsmFloat duration;

        public override void Reset()
        {
            modeName = null;
            duration = -1f;
        }

        public override void OnEnter()
        {
            if (CGController.Instance == null)
                Debug.LogWarning("[CGSetBGMode] 場景中找不到 CGController 實例。");
            else if (string.IsNullOrEmpty(modeName.Value))
                Debug.LogWarning("[CGSetBGMode] modeName 未設定。");
            else
                CGController.Instance.SetBGMode(modeName.Value, duration.Value);

            Finish();
        }
    }

    [ActionCategory("CG")]
    [Tooltip("切換插圖 CG mode（換衣服等變體）。會寫回存檔；若目前有插圖會立刻重抓圖。")]
    public class CGSetCGMode : FsmStateAction
    {
        [RequiredField]
        [Tooltip("要切換到的 CG mode 名稱")]
        public FsmString modeName;

        [Tooltip("切換時間（秒），負值 = 使用控制器預設")]
        public FsmFloat duration;

        public override void Reset()
        {
            modeName = null;
            duration = -1f;
        }

        public override void OnEnter()
        {
            if (CGController.Instance == null)
                Debug.LogWarning("[CGSetCGMode] 場景中找不到 CGController 實例。");
            else if (string.IsNullOrEmpty(modeName.Value))
                Debug.LogWarning("[CGSetCGMode] modeName 未設定。");
            else
                CGController.Instance.SetCGMode(modeName.Value, duration.Value);

            Finish();
        }
    }
}
