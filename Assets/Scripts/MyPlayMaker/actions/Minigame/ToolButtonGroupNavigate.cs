using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("透過場景單例操作按鈕群組，不需要 Target 或 Bridge。進入 State 執行一次；Show／Hide 啟動淡入淡出後立即 Finish，不等待動畫。")]
    public class ToolButtonGroupNavigate : FsmStateAction
    {
        public enum NavigationOperation
        {
            ShowGroup,
            LastGroup,
            BackGroup,
            MainGroup,
            Show,
            Hide
        }

        [Tooltip("要執行的群組操作。")]
        public NavigationOperation operation;

        [Tooltip("僅 ShowGroup 使用；區分大小寫，可填入固定名稱或 FSM String 變數。")]
        public FsmString groupName;

        public override void Reset()
        {
            operation = NavigationOperation.ShowGroup;
            groupName = string.Empty;
        }

        public override void OnEnter()
        {
            Navigate();
            Finish();
        }

        private void Navigate()
        {
            var controller = ToolButtonGroupDisplayControl.Instance;
            if (controller == null)
            {
                LogError("[ToolButtonGroupNavigate] 場景尚未初始化 ToolButtonGroupDisplayControl，請保持控制器物件啟用，改用 CanvasGroup 隱藏。");
                return;
            }

            switch (operation)
            {
                case NavigationOperation.ShowGroup:
                    if (groupName == null || groupName.IsNone || string.IsNullOrWhiteSpace(groupName.Value))
                    {
                        LogError("[ToolButtonGroupNavigate] ShowGroup 必須設定有效的 Group Name。");
                        return;
                    }
                    controller.ShowGroup(groupName.Value);
                    break;
                case NavigationOperation.LastGroup:
                    controller.LastGroup();
                    break;
                case NavigationOperation.BackGroup:
                    controller.BackGroup();
                    break;
                case NavigationOperation.MainGroup:
                    controller.MainGroup();
                    break;
                case NavigationOperation.Show:
                    controller.Show();
                    break;
                case NavigationOperation.Hide:
                    controller.Hide();
                    break;
                default:
                    LogError("[ToolButtonGroupNavigate] 未知的群組操作。");
                    break;
            }
        }
    }
}
