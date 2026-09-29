using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 場景分組互斥狀態命令（最後的目標物件名稱可省略）：
    /// GroupedState(Set, 組名, 狀態名 [, 目標物件名稱])
    /// GroupedState(Clear, 組名 [, 目標物件名稱])
    /// GroupedState(Check, 組名, 狀態名 [, 目標物件名稱])
    /// 例如：GroupedState(Set, 房門, 上鎖, RoomStateController)
    /// 操作名稱不分大小寫；其他名稱精確比對、區分大小寫。
    /// 只查找已載入場景中物件與元件皆啟用的 Controller，包含常駐場景。
    /// 省略目標時必須只有一個可用 Controller；明確指定時也必須唯一。
    /// Check 觸發元件的判斷 UnityEvent，不寫入 Lua 變數或存檔。
    /// 呼叫後立即結束命令，不等待 UnityEvent 啟動的動畫或協程。
    /// </summary>
    public sealed class SequencerCommandGroupedState : SequencerCommand
    {
        private void Awake()
        {
            try
            {
                ExecuteCommand();
            }
            finally
            {
                // 包含參數錯誤及找不到目標的情況，皆需結束以免 Sequence 等待。
                Stop();
            }
        }

        private void ExecuteCommand()
        {
            string operation = (GetParameter(0) ?? string.Empty).Trim().ToLowerInvariant();
            if (operation != "set" && operation != "clear" && operation != "check")
            {
                Warn("操作必須是 Set、Clear 或 Check。");
                return;
            }

            int requiredCount = operation == "clear" ? 2 : 3;
            int count = parameters == null ? 0 : parameters.Length;
            if (count < requiredCount || count > requiredCount + 1)
            {
                Warn(operation == "clear"
                    ? "參數數量錯誤。用法：GroupedState(Clear, 組名 [, 目標物件名稱])。"
                    : "參數數量錯誤。用法：GroupedState(Set 或 Check, 組名, 狀態名 [, 目標物件名稱])。");
                return;
            }

            string groupName = GetParameter(1);
            if (!ValidateName(groupName, "組名")) return;
            string stateName = operation == "clear" ? null : GetParameter(2);
            if (operation != "clear" && !ValidateName(stateName, "狀態名")) return;

            bool hasTarget = count == requiredCount + 1;
            string targetName = hasTarget ? GetParameter(requiredCount) : null;
            if (hasTarget && string.IsNullOrWhiteSpace(targetName))
            {
                Warn("已提供目標參數，但名稱為空白。請填入物件名稱，或省略整個目標參數。");
                return;
            }

            GroupedStateController controller = ResolveController(targetName);
            if (controller == null) return;

            // 狀態規則與事件執行由原元件及其 Model 處理，命令只做轉接。
            switch (operation)
            {
                case "set": controller.SetState(groupName, stateName); break;
                case "clear": controller.ClearState(groupName); break;
                case "check": controller.CheckState(groupName + "/" + stateName); break;
            }
        }

        private bool ValidateName(string value, string label)
        {
            if (!string.IsNullOrWhiteSpace(value) && !value.Contains("/") && value == value.Trim()) return true;
            Warn(label + "不可空白、包含 / 或帶有頭尾空白；組名與狀態名請分開填寫。");
            return false;
        }

        private GroupedStateController ResolveController(string targetName)
        {
            // 每次重新查找，避免切場景後使用失效參照；不依賴查找回傳的順序。
            var controllers = UnityEngine.Object.FindObjectsByType<GroupedStateController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var matches = new List<GroupedStateController>();
            foreach (GroupedStateController candidate in controllers)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (targetName != null && !string.Equals(candidate.gameObject.name, targetName, StringComparison.Ordinal)) continue;
                matches.Add(candidate);
            }

            if (matches.Count == 1) return matches[0];
            if (matches.Count == 0)
            {
                Warn(targetName == null
                    ? "已載入場景中找不到可用的 GroupedStateController。請確認物件與元件皆已啟用。"
                    : "找不到目標「" + targetName + "」上的可用 GroupedStateController。請確認物件名稱（區分大小寫）、場景載入狀態，以及物件與元件皆已啟用。");
                return null;
            }

            var locations = new List<string>();
            foreach (GroupedStateController match in matches)
            {
                string path = match.gameObject.name;
                for (Transform parent = match.transform.parent; parent != null; parent = parent.parent)
                    path = parent.name + "/" + path;
                locations.Add(match.gameObject.scene.name + ":" + path);
            }
            Warn((targetName == null
                ? "找到多個可用 Controller，請在最後一個參數指定唯一的目標物件名稱。"
                : "目標「" + targetName + "」符合多個 Controller，請使用唯一物件名稱，並確認同一物件沒有重複掛載元件。")
                + " 本次不執行。符合項目：" + string.Join("、", locations));
            return null;
        }

        private void Warn(string message)
        {
            Debug.LogWarning("[GroupedState] " + message, this);
        }
    }
}
