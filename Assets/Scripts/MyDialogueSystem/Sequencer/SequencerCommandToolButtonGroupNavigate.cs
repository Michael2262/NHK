using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 透過場景單例操作按鈕群組，不需要指定 Target 或掛載 Bridge。
    /// 用法：
    /// ToolButtonGroupNavigate(ShowGroup, 組名)
    /// ToolButtonGroupNavigate(LastGroup)
    /// ToolButtonGroupNavigate(BackGroup)
    /// ToolButtonGroupNavigate(MainGroup)
    /// ToolButtonGroupNavigate(Show)
    /// ToolButtonGroupNavigate(Hide)
    /// 操作名稱不分大小寫；組名區分大小寫，與控制器設定一致。
    /// ShowGroup 僅切組；Show／Hide 啟動淡入淡出後立即結束指令，不等待動畫。
    /// </summary>
    public class SequencerCommandToolButtonGroupNavigate : SequencerCommand
    {
        private void Awake()
        {
            try
            {
                Navigate();
            }
            finally
            {
                Stop();
            }
        }

        private void Navigate()
        {
            string operation = (GetParameter(0) ?? string.Empty).Trim().ToLowerInvariant();
            switch (operation)
            {
                case "showgroup":
                case "lastgroup":
                case "backgroup":
                case "maingroup":
                case "show":
                case "hide":
                    break;
                default:
                    Debug.LogWarning("[ToolButtonGroupNavigate] 請指定有效操作：ShowGroup、LastGroup、BackGroup、MainGroup、Show 或 Hide。", this);
                    return;
            }

            string groupName = operation == "showgroup" ? GetParameter(1) : null;
            if (operation == "showgroup" && string.IsNullOrWhiteSpace(groupName))
            {
                Debug.LogWarning("[ToolButtonGroupNavigate] ShowGroup 必須在第二個參數設定有效的組名。", this);
                return;
            }

            var controller = ToolButtonGroupDisplayControl.Instance;
            if (controller == null)
            {
                Debug.LogWarning("[ToolButtonGroupNavigate] 場景尚未初始化 ToolButtonGroupDisplayControl，請保持控制器物件啟用，改用 CanvasGroup 隱藏。", this);
                return;
            }

            switch (operation)
            {
                case "showgroup": controller.ShowGroup(groupName); break;
                case "lastgroup": controller.LastGroup(); break;
                case "backgroup": controller.BackGroup(); break;
                case "maingroup": controller.MainGroup(); break;
                case "show": controller.Show(); break;
                case "hide": controller.Hide(); break;
            }
        }
    }
}
