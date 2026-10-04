using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 透過場景單例操作單顆射擊按鈕，不需要指定物件或掛載 Bridge。
    /// ShootButtonGroup(SetID, 組名)
    /// ShootButtonGroup(RemoveID)
    /// ShootButtonGroup(Show)
    /// ShootButtonGroup(Hide)
    /// ShootButtonGroup(Refresh)
    /// 操作名稱不分大小寫，組名精確比對且區分大小寫。
    /// SetID 不自動顯示；Hide 保留組名；RemoveID 不自動隱藏。
    /// Show／Hide 啟動淡入淡出後立即結束指令，不等待動畫完成。
    /// </summary>
    public sealed class SequencerCommandShootButtonGroup : SequencerCommand
    {
        private void Awake()
        {
            try
            {
                ExecuteCommand();
            }
            finally
            {
                // 包含參數錯誤或找不到控制器，都要結束指令，避免 Sequence 等待。
                Stop();
            }
        }

        private void ExecuteCommand()
        {
            string operation = (GetParameter(0) ?? string.Empty).Trim().ToLowerInvariant();
            switch (operation)
            {
                case "setid":
                case "removeid":
                case "show":
                case "hide":
                case "refresh":
                    break;
                default:
                    Warn("請指定有效操作：SetID、RemoveID、Show、Hide 或 Refresh。");
                    return;
            }

            int requiredCount = operation == "setid" ? 2 : 1;
            int count = parameters == null ? 0 : parameters.Length;
            if (count != requiredCount)
            {
                Warn(operation == "setid"
                    ? "用法：ShootButtonGroup(SetID, 組名)。"
                    : "此操作只需要一個參數，例如 ShootButtonGroup(Show)。");
                return;
            }

            string groupName = operation == "setid" ? GetParameter(1) : null;
            if (operation == "setid" && string.IsNullOrWhiteSpace(groupName))
            {
                Warn("SetID 必須提供非空白組名；如需清除組名，請使用 ShootButtonGroup(RemoveID)。");
                return;
            }

            var controller = global::ShootButtonGroup.Instance;
            if (controller == null)
            {
                Warn("場景尚未初始化 ShootButtonGroup。請保持控制器物件啟用，改用 Show／Hide 控制顯示。");
                return;
            }

            // 組名查找、Semen 滿值條件 與顯示規則沿用原控制器。
            switch (operation)
            {
                case "setid": controller.SetID(groupName); break;
                case "removeid": controller.RemoveID(); break;
                case "show": controller.Show(); break;
                case "hide": controller.Hide(); break;
                case "refresh": controller.Refresh(); break;
            }
        }

        private void Warn(string message)
        {
            Debug.LogWarning("[ShootButtonGroup Sequence] " + message, this);
        }
    }
}
