using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("透過 FSM 所在物件的 ToolButtonGroupFsmBridge 操作按鈕群組。進入 State 時執行一次，不需要逐個 Action 設定 Target。")]
    public class ToolButtonGroupNavigate : FsmStateAction
    {
        public enum NavigationOperation
        {
            ShowGroup,
            LastGroup,
            BackGroup,
            MainGroup
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
            var bridge = Owner != null ? Owner.GetComponent<ToolButtonGroupFsmBridge>() : null;
            if (bridge == null)
            {
                LogError("[ToolButtonGroupNavigate] FSM 所在物件缺少 ToolButtonGroupFsmBridge，請在同一物件新增元件並指定控制器。");
                return;
            }

            var controller = bridge.Controller;
            if (controller == null)
            {
                LogError("[ToolButtonGroupNavigate] ToolButtonGroupFsmBridge 尚未指定群組控制器。");
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
                default:
                    LogError("[ToolButtonGroupNavigate] 未知的群組操作。");
                    break;
            }
        }
    }
}
