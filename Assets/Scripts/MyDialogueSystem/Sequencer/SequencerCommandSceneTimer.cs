using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 場景計時器轉接；最後的目標物件名稱可省略，省略時必須只有一個可用 Controller。
    /// SceneTimer(StartTimer, 行為ID [, 目標物件名稱])
    /// SceneTimer(CancelID, 行為ID [, 目標物件名稱])
    /// SceneTimer(PauseTimer [, Timer名稱或編號 [, 目標物件名稱]])
    /// SceneTimer(CancelTimer [, Timer名稱或編號 [, 目標物件名稱]])
    /// SceneTimer(CancelAllTimers [, 目標物件名稱])
    /// SceneTimer(CheckTimer, Timer名稱或編號, 結果變數名 [, 目標物件名稱])
    /// SceneTimer(CheckID, 行為ID, 結果變數名 [, 目標物件名稱])
    /// SceneTimer(EvaluateTimer [, Timer名稱或編號 [, 目標物件名稱]])
    /// SceneTimer(EvaluateID, 行為ID [, 目標物件名稱])
    /// Timer 留空代表 Main；Check 寫入布林 Dialogue System 變數，不觸發 UnityEvent。
    /// Evaluate 觸發 Controller 的判斷 UnityEvent，不寫入結果變數。
    /// 操作名稱不分大小寫，其餘名稱區分大小寫。Start、Pause、Cancel、CancelAll 為簡寫。
    /// 啟動模式沿用行為設定，包含 Parallel；執行後立即結束，不等待倒數或阻擋對話。
    /// </summary>
    public sealed class SequencerCommandSceneTimer : SequencerCommand
    {
        private void Awake()
        {
            try { ExecuteCommand(); }
            finally { Stop(); }
        }

        private void ExecuteCommand()
        {
            string operation = (GetParameter(0) ?? string.Empty).Trim().ToLowerInvariant();
            switch (operation)
            {
                case "start": operation = "starttimer"; break;
                case "pause": operation = "pausetimer"; break;
                case "cancel": operation = "canceltimer"; break;
                case "cancelall": operation = "cancelalltimers"; break;
            }

            bool check = operation == "checktimer" || operation == "checkid";
            bool byID = operation == "starttimer" || operation == "cancelid"
                || operation == "checkid" || operation == "evaluateid";
            bool byTimer = operation == "pausetimer" || operation == "canceltimer"
                || operation == "checktimer" || operation == "evaluatetimer";
            if (!byID && !byTimer && operation != "cancelalltimers")
            {
                Warn("未知操作。請使用 StartTimer、PauseTimer、CancelTimer、CancelID、CancelAllTimers、CheckTimer、CheckID、EvaluateTimer 或 EvaluateID。");
                return;
            }

            int count = parameters == null ? 0 : parameters.Length;
            int targetIndex = check ? 3 : operation == "cancelalltimers" ? 1 : 2;
            int minimum = check ? 3 : byID ? 2 : 1;
            if (count < minimum || count > targetIndex + 1)
            {
                Warn("參數數量錯誤。Check 需要查詢名稱與結果變數名；行為操作需要行為 ID；最後可加目標物件名稱。");
                return;
            }

            string value = count > 1 && operation != "cancelalltimers" ? GetParameter(1) : null;
            if (byID && (string.IsNullOrWhiteSpace(value) || value != value.Trim()))
            {
                Warn("行為 ID 不可空白或含前後空格。");
                return;
            }
            if (byTimer)
            {
                if (string.IsNullOrWhiteSpace(value)) value = "Main";
                TimerId timer;
                if (value.IndexOf(',') >= 0 || !Enum.TryParse(value.Trim(), false, out timer)
                    || !Enum.IsDefined(typeof(TimerId), timer))
                {
                    Warn("無效的 Timer 名稱或編號：" + value);
                    return;
                }
            }

            string resultVariable = check ? GetParameter(2) : null;
            if (check && (string.IsNullOrWhiteSpace(resultVariable) || resultVariable != resultVariable.Trim()))
            {
                Warn("Check 必須提供非空白的結果變數名，不含 Variable[] 包裝或前後空格。");
                return;
            }
            string targetName = count > targetIndex ? GetParameter(targetIndex) : null;
            if (count > targetIndex && string.IsNullOrWhiteSpace(targetName))
            {
                Warn("目標物件名稱不可空白；自動尋找時請省略最後的目標參數。");
                return;
            }

            // 找不到控制器時，清除舊的查詢結果，避免後續分支誤用先前的 true。
            if (check) DialogueLua.SetVariable(resultVariable, false);
            SceneTimerController controller = ResolveController(targetName);
            if (controller == null) return;
            switch (operation)
            {
                case "starttimer": controller.StartTimer(value); break;
                case "pausetimer": controller.PauseTimer(value); break;
                case "canceltimer": controller.CancelTimer(value); break;
                case "cancelid": controller.CancelID(value); break;
                case "cancelalltimers": controller.CancelAllTimers(); break;
                case "checktimer": DialogueLua.SetVariable(resultVariable, controller.CheckTimer(value)); break;
                case "checkid": DialogueLua.SetVariable(resultVariable, controller.CheckID(value)); break;
                case "evaluatetimer": controller.EvaluateTimer(value); break;
                case "evaluateid": controller.EvaluateID(value); break;
            }
        }

        private SceneTimerController ResolveController(string targetName)
        {
            // 不快取場景參照；多個符合項目時拒絕執行，避免操作錯誤的計時器。
            var candidates = UnityEngine.Object.FindObjectsByType<SceneTimerController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var matches = new List<SceneTimerController>();
            foreach (SceneTimerController candidate in candidates)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (targetName != null && !string.Equals(candidate.gameObject.name, targetName, StringComparison.Ordinal)) continue;
                matches.Add(candidate);
            }
            if (matches.Count == 1) return matches[0];
            Warn(matches.Count == 0
                ? "找不到可用的 SceneTimerController" + (targetName == null ? "。" : "：" + targetName + "。") + "請確認物件與元件已啟用。"
                : "找到多個符合的 SceneTimerController。請在最後一個參數指定唯一的物件名稱，並確認沒有重複掛載元件；本次不執行。");
            return null;
        }

        private void Warn(string message) { Debug.LogWarning("[SceneTimer Sequence] " + message, this); }
    }
}
