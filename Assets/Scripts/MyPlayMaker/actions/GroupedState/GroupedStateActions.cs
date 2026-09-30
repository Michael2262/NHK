using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("NHK/Grouped State")]
    [Tooltip("切換指定分組的狀態，執行原狀態的 OnExit 與新狀態的 OnEnter；不等待事件啟動的動畫。")]
    public sealed class GroupedStateSet : FsmStateAction
    {
        [Tooltip("選填：Specify Game Object 留空時為 Auto，自動尋找已載入場景中唯一啟用的 GroupedStateController；有多個時請指定物件。Use Owner 仍使用 FSM 所在物件；已選變數但值為空時不會自動改找其他目標。")]
        public FsmOwnerDefault targetObject;

        [RequiredField]
        [Tooltip("組名，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
        public FsmString groupName;

        [RequiredField]
        [Tooltip("狀態名，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
        public FsmString stateName;

        public override void Reset()
        {
            targetObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            groupName = null;
            stateName = null;
        }

        public override void OnEnter()
        {
            try
            {
                if (!GroupedStateActionUtility.ValidateName(this, groupName, "組名") ||
                    !GroupedStateActionUtility.ValidateName(this, stateName, "狀態名")) return;

                var controller = GroupedStateActionUtility.ResolveController(this, targetObject);
                if (controller != null) controller.SetState(groupName.Value, stateName.Value);
            }
            finally
            {
                Finish();
            }
        }
    }

    [ActionCategory("NHK/Grouped State")]
    [Tooltip("清除指定分組的目前狀態並執行 OnExit；原本沒有狀態時不做事，不等待事件啟動的動畫。")]
    public sealed class GroupedStateClear : FsmStateAction
    {
        [Tooltip("選填：Specify Game Object 留空時為 Auto，自動尋找已載入場景中唯一啟用的 GroupedStateController；有多個時請指定物件。Use Owner 仍使用 FSM 所在物件；已選變數但值為空時不會自動改找其他目標。")]
        public FsmOwnerDefault targetObject;

        [RequiredField]
        [Tooltip("組名，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
        public FsmString groupName;

        public override void Reset()
        {
            targetObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            groupName = null;
        }

        public override void OnEnter()
        {
            try
            {
                if (!GroupedStateActionUtility.ValidateName(this, groupName, "組名")) return;

                var controller = GroupedStateActionUtility.ResolveController(this, targetObject);
                if (controller != null) controller.ClearState(groupName.Value);
            }
            finally
            {
                Finish();
            }
        }
    }

    [ActionCategory("NHK/Grouped State")]
    [Tooltip("進入 State 時判斷一次分組狀態，儲存 Bool 並發送 FSM 事件，不觸發 Controller 的判斷 UnityEvent。無此組或狀態時結果為 False。")]
    public sealed class GroupedStateCheck : FsmStateAction
    {
        [Tooltip("選填：Specify Game Object 留空時為 Auto，自動尋找已載入場景中唯一啟用的 GroupedStateController；有多個時請指定物件。Use Owner 仍使用 FSM 所在物件；已選變數但值為空時不會自動改找其他目標。")]
        public FsmOwnerDefault targetObject;

        [RequiredField]
        [Tooltip("組名，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
        public FsmString groupName;

        [RequiredField]
        [Tooltip("狀態名，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
        public FsmString stateName;

        [UIHint(UIHint.Variable)]
        [Tooltip("選填：儲存判斷結果。目標或參數格式錯誤時重設為 False，且不發送判斷事件。")]
        public FsmBool storeResult;

        [Tooltip("選填：狀態成立時發送的 FSM 事件。")]
        public FsmEvent trueEvent;

        [Tooltip("選填：狀態不成立時發送的 FSM 事件。")]
        public FsmEvent falseEvent;

        public override void Reset()
        {
            targetObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            groupName = null;
            stateName = null;
            storeResult = null;
            trueEvent = null;
            falseEvent = null;
        }

        public override void OnEnter()
        {
            try
            {
                // 避免本次無法執行時留下上一次的 True 結果。
                if (storeResult != null && !storeResult.IsNone) storeResult.Value = false;
                if (!GroupedStateActionUtility.ValidateName(this, groupName, "組名") ||
                    !GroupedStateActionUtility.ValidateName(this, stateName, "狀態名")) return;

                var controller = GroupedStateActionUtility.ResolveController(this, targetObject);
                if (controller == null) return;

                bool result = controller.IsState(groupName.Value, stateName.Value);
                if (storeResult != null && !storeResult.IsNone) storeResult.Value = result;
                FsmEvent resultEvent = result ? trueEvent : falseEvent;
                if (resultEvent != null) Fsm.Event(resultEvent);
            }
            finally
            {
                Finish();
            }
        }
    }

    // 共用參數驗證與元件查找；狀態規則仍由 Controller 及其 Model 負責。
    internal static class GroupedStateActionUtility
    {
        internal static bool ValidateName(FsmStateAction action, FsmString field, string label)
        {
            string value = field == null || field.IsNone ? null : field.Value;
            if (!string.IsNullOrWhiteSpace(value) && !value.Contains("/") && value == value.Trim())
                return true;

            Warn(action, label + "不可空白、包含 / 或帶有頭尾空白；組名與狀態名請分開填寫。");
            return false;
        }

        internal static GroupedStateController ResolveController(FsmStateAction action, FsmOwnerDefault target)
        {
            // 保留原有 Use Owner 與明確物件參照的行為；只有未指定目標才使用 Auto。
            // Unity 序列化的空參照不一定是 CLR null，必須使用 Unity 的 == null 判斷。
            // PlayMaker 的 None 也視為留空；具名變數即使值為空，仍維持指定目標模式。
            bool auto = target == null || (target.OwnerOption == OwnerDefaultOption.SpecifyGameObject
                && (target.GameObject == null || target.GameObject.IsNone
                    || (!target.GameObject.UseVariable && target.GameObject.Value == null)));
            if (auto) return FindUniqueController(action);

            // 每次進入 State 重新取得目標，支援 FSM 變數更換物件，避免保留失效參照。
            GameObject targetObject = action.Fsm.GetOwnerDefaultTarget(target);
            if (targetObject == null)
            {
                string source = target.OwnerOption == OwnerDefaultOption.SpecifyGameObject
                    ? "指定變數「" + target.GameObject.Name + "」目前沒有目標物件"
                    : "Use Owner 無法取得 FSM 所在物件";
                Warn(action, source + "。若要使用 Auto，請選 Specify Game Object 並將目標留空（None）。");
                return null;
            }

            var controllers = targetObject.GetComponents<GroupedStateController>();
            if (controllers.Length != 1)
            {
                Warn(action, "目標「" + targetObject.name + "」必須恰好掛有一個 GroupedStateController，本次不執行。");
                return null;
            }

            var controller = controllers[0];
            var scene = targetObject.scene;
            if (!controller.isActiveAndEnabled || !scene.IsValid() || !scene.isLoaded)
            {
                Warn(action, "目標「" + targetObject.name + "」必須位於已載入場景，且物件與元件皆已啟用。");
                return null;
            }

            return controller;
        }

        private static GroupedStateController FindUniqueController(FsmStateAction action)
        {
            var controllers = UnityEngine.Object.FindObjectsByType<GroupedStateController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            GroupedStateController result = null;
            int count = 0;
            foreach (GroupedStateController candidate in controllers)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                result = candidate;
                count++;
            }

            if (count == 1) return result;
            Warn(action, count == 0
                ? "Auto 找不到可用的 GroupedStateController，請確認已載入場景中有物件與元件皆啟用的 Controller。"
                : "Auto 找到 " + count + " 個可用的 GroupedStateController，請在 Target Object 指定目標物件，本次不執行。");
            return null;
        }

        private static void Warn(FsmStateAction action, string message)
        {
            Debug.LogWarning("[" + action.GetType().Name + "] " + message, action.Owner);
        }
    }
}
