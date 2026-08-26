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
    ///   Adventure(Draw)             -> 發下一張牌（依牌池抽）並演到必有效果為止
    ///   Adventure(DrawByID, 卡片ID) -> 發「指定 ID」的牌並演到必有效果為止（卡片資產名 = ID）
    ///   Adventure(Challenge)  -> 要挑戰：跑成功率判定、演出結果、收牌
    ///
    /// 【流程】（走 Controller）
    ///   Adventure(Start, CStore)         -> 用 Dungeon ID 開始一趟（查 Controller 上的 Database）
    ///   Adventure(Start)                 -> 沒帶 ID 時用 Controller 的預設地點開始
    ///   Adventure(GoHome)                -> 回家，結束這趟
    ///   Adventure(SpendMove)             -> 消耗一次行動（= AddMoves,-1）。扣到 0 會自動發 onMovesExhausted，時機＝你呼叫這行的當下
    ///   Adventure(AddMoves, -1)          -> 行動次數變化（不帶參數預設 -1；正數補充）
    ///   Adventure(ResetMoves)            -> 行動次數重設為 Dungeon 的上限
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
            else if (IsAction(action, "DrawByID", "DrawCardByID", "NextByID"))
            {
                string cardID = GetParameter(1);
                if (string.IsNullOrEmpty(cardID))
                    Debug.LogWarning("[Adventure] DrawByID 缺少卡片 ID。用法：Adventure(DrawByID, 卡片ID)");
                else if (RequirePresenter(action, out var presenter))
                    presenter.PlayDrawByID(cardID);
            }
            else if (IsAction(action, "Challenge", "Outcome", "Resolve"))
            {
                if (RequirePresenter(action, out var presenter)) presenter.PlayOutcome();
            }
            // ── 流程：走 Controller ──
            else if (IsAction(action, "Start", "StartDungeon", "Begin"))
            {
                if (RequireController(action, out var controller))
                {
                    string dungeonID = GetParameter(1);
                    if (string.IsNullOrEmpty(dungeonID))
                        controller.StartDefaultAdventure();
                    else
                        controller.StartDungeonByID(dungeonID);
                }
            }
            else if (IsAction(action, "GoHome", "Home"))
            {
                if (RequireController(action, out var controller)) controller.GoHome();
            }
            else if (IsAction(action, "AddMoves", "Moves"))
            {
                if (RequireController(action, out var controller))
                {
                    int amount = GetParameterAsInt(1, -1); // 預設 -1（消耗一次行動）
                    controller.AddMoves(amount);
                }
            }
            else if (IsAction(action, "SpendMove", "UseMove"))
            {
                if (RequireController(action, out var controller)) controller.AddMoves(-1);
            }
            else if (IsAction(action, "ResetMoves"))
            {
                if (RequireController(action, out var controller)) controller.ResetMoves();
            }
            else
            {
                Debug.LogWarning($"[Adventure] 未知的動作類型: {action}。可用選項: " +
                                 "Dismiss, Draw, DrawByID, Challenge, Start, GoHome, SpendMove, AddMoves, ResetMoves。");
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
