using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：Adventure(動作, [參數])
    ///
    /// 抓場上唯一的 AdventureCardPresenter / AdventureController（各自的 static Instance）。
    ///
    /// 【卡片演出】（走 Presenter）
    ///   Adventure(Dismiss)    -> 讓「結果已呈現、正在等待淡出」的牌提前消失
    ///                            （把 Wait After Outcome 切短；不在該窗口時忽略）
    ///   Adventure(Draw)       -> 發下一張牌並演到必有效果為止（繼續前進 / 繞遠路）
    ///   Adventure(Challenge)  -> 要挑戰：跑成功率判定、演出結果、收牌
    ///
    /// 【流程】（走 Controller）
    ///   Adventure(Rest)                  -> 休息一次（次數不足自動無效）
    ///   Adventure(GoHome)                -> 回家，結束這趟
    ///   Adventure(ResetRest)             -> 休息次數重設為上限
    ///   Adventure(AddRequiredMileage, 2) -> 本輪里程目標 +2（繞遠路，不改 SO）
    /// </summary>
    public class SequencerCommandAdventure : SequencerCommand
    {
        public void Awake()
        {
            string action = GetParameter(0);

            // ── 暫時的診斷 log，確認命令有被 Dialogue System 認出來；確認沒問題後可刪 ──
            Debug.Log($"[Adventure] 命令已觸發，action='{action}'，" +
                      $"Presenter={(AdventureCardPresenter.Instance != null ? "有" : "null")}，" +
                      $"Controller={(AdventureController.Instance != null ? "有" : "null")}");

            if (string.IsNullOrEmpty(action))
            {
                Debug.LogWarning("[Adventure] 缺少動作。用法：Adventure(動作, [參數])");
                Stop();
                return;
            }

            // ── 卡片演出：走 Presenter ──
            if (IsAction(action, "Dismiss", "DismissCard", "HideCard"))
            {
                if (RequirePresenter(action, out var presenter)) presenter.DismissCard();
            }
            else if (IsAction(action, "Draw", "DrawCard", "Next"))
            {
                if (RequirePresenter(action, out var presenter)) presenter.PlayDraw();
            }
            else if (IsAction(action, "Challenge", "Outcome", "Resolve"))
            {
                if (RequirePresenter(action, out var presenter)) presenter.PlayOutcome();
            }
            // ── 流程：走 Controller ──
            else if (IsAction(action, "Rest"))
            {
                if (RequireController(action, out var controller)) controller.Rest();
            }
            else if (IsAction(action, "GoHome", "Home"))
            {
                if (RequireController(action, out var controller)) controller.GoHome();
            }
            else if (IsAction(action, "ResetRest"))
            {
                if (RequireController(action, out var controller)) controller.ResetRest();
            }
            else if (IsAction(action, "AddRequiredMileage", "AddRequired", "LongerRoad"))
            {
                if (RequireController(action, out var controller))
                {
                    int amount = GetParameterAsInt(1, 1);
                    controller.AddRequiredMileage(amount);
                }
            }
            else
            {
                Debug.LogWarning($"[Adventure] 未知的動作類型: {action}。可用選項: " +
                                 "Dismiss, Draw, Challenge, Rest, GoHome, ResetRest, AddRequiredMileage。");
            }

            Stop();
        }

        private bool RequirePresenter(string action, out AdventureCardPresenter presenter)
        {
            presenter = AdventureCardPresenter.Instance;
            if (presenter == null)
            {
                Debug.LogWarning($"[Adventure] 場上找不到 AdventureCardPresenter，略過動作「{action}」。");
                return false;
            }
            return true;
        }

        private bool RequireController(string action, out AdventureController controller)
        {
            controller = AdventureController.Instance;
            if (controller == null)
            {
                Debug.LogWarning($"[Adventure] 場上找不到 AdventureController，略過動作「{action}」。");
                return false;
            }
            return true;
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
