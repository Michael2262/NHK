using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // 對應 Sequencer 指令 TachieControl(Mode, ...) / TachieControl(ModeAll, ...)
    // 兩者最終都呼叫 TachieController.SetMode / SetModeAll，會自動寫回 VisualModeModel，
    // 因此切換的 mode 會被存檔保存、跨場景維持、開新檔重置。

    [ActionCategory("Tachie")]
    [Tooltip("切換指定角色 / 連動群組的身體 mode（服裝變體）。會立刻重新判定圖片並寫回存檔。")]
    public class TachieSetBodyMode : FsmStateAction
    {
        [RequiredField]
        [Tooltip("角色 ID 或連動群組名")]
        public FsmString idOrGroup;

        [RequiredField]
        [Tooltip("要切換到的 mode 名稱")]
        public FsmString modeName;

        public override void Reset()
        {
            idOrGroup = null;
            modeName = null;
        }

        public override void OnEnter()
        {
            if (TachieController.Instance == null)
                Debug.LogWarning("[TachieSetBodyMode] 場景中找不到 TachieController 實例。");
            else if (string.IsNullOrEmpty(idOrGroup.Value) || string.IsNullOrEmpty(modeName.Value))
                Debug.LogWarning("[TachieSetBodyMode] idOrGroup 或 modeName 未設定。");
            else
                TachieController.Instance.SetMode(idOrGroup.Value, modeName.Value);

            Finish();
        }
    }

    [ActionCategory("Tachie")]
    [Tooltip("把所有角色一起切換到同一個身體 mode。會寫回存檔。")]
    public class TachieSetBodyModeAll : FsmStateAction
    {
        [RequiredField]
        [Tooltip("要切換到的 mode 名稱")]
        public FsmString modeName;

        public override void Reset()
        {
            modeName = null;
        }

        public override void OnEnter()
        {
            if (TachieController.Instance == null)
                Debug.LogWarning("[TachieSetBodyModeAll] 場景中找不到 TachieController 實例。");
            else if (string.IsNullOrEmpty(modeName.Value))
                Debug.LogWarning("[TachieSetBodyModeAll] modeName 未設定。");
            else
                TachieController.Instance.SetModeAll(modeName.Value);

            Finish();
        }
    }
}
