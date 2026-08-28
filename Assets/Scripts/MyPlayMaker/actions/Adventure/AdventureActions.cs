using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ==========================================================
    // 大冒險 FSM Actions
    // 全部抓場上唯一的 AdventureController.Instance / AdventureCardPresenter.Instance，
    // 跟 Sequencer 命令 Adventure(...) 對應。找不到 Instance 就跳警告並 Finish（不卡 FSM）。
    // ==========================================================

    internal static class AdventureActionUtil
    {
        public static bool RequireController(string who, out AdventureController controller)
        {
            controller = AdventureController.Instance;
            if (controller == null)
            {
                Debug.LogWarning($"[{who}] 場上找不到 AdventureController，略過。");
                return false;
            }
            return true;
        }

        public static bool RequirePresenter(string who, out AdventureCardPresenter presenter)
        {
            presenter = AdventureCardPresenter.Instance;
            if (presenter == null)
            {
                Debug.LogWarning($"[{who}] 場上找不到 AdventureCardPresenter，略過。");
                return false;
            }
            return true;
        }
    }

    // ── 流程（走 Controller）──

    [ActionCategory("Adventure")]
    [Tooltip("開始一趟大冒險。填 Dungeon ID 用 Database 查；留空用 Controller 的預設地點。")]
    public class AdventureStart : FsmStateAction
    {
        [Tooltip("Dungeon ID（留空 = 用 Controller 的預設地點）")]
        public FsmString dungeonID;

        public override void Reset() { dungeonID = ""; }

        public override void OnEnter()
        {
            if (AdventureActionUtil.RequireController(nameof(AdventureStart), out var c))
            {
                if (string.IsNullOrEmpty(dungeonID.Value)) c.StartDefaultAdventure();
                else c.StartDungeonByID(dungeonID.Value);
            }
            Finish();
        }
    }

    [ActionCategory("Adventure")]
    [Tooltip("回家，結束這趟大冒險。")]
    public class AdventureGoHome : FsmStateAction
    {
        public override void OnEnter()
        {
            if (AdventureActionUtil.RequireController(nameof(AdventureGoHome), out var c)) c.GoHome();
            Finish();
        }
    }

    // ── 卡片演出（走 Presenter）──

    [ActionCategory("Adventure")]
    [Tooltip("發下一張牌並演出（依牌池抽）。等同對話 Adventure(Draw)。")]
    public class AdventureDraw : FsmStateAction
    {
        public override void OnEnter()
        {
            if (AdventureActionUtil.RequirePresenter(nameof(AdventureDraw), out var p)) p.PlayDraw();
            Finish();
        }
    }

    [ActionCategory("Adventure")]
    [Tooltip("發『指定 ID』的牌並演出（略過牌池）。等同對話 Adventure(DrawByID, 卡片ID)。")]
    public class AdventureDrawByID : FsmStateAction
    {
        [RequiredField]
        [Tooltip("卡片 ID（= 卡片資產名）")]
        public FsmString cardID;

        public override void Reset() { cardID = ""; }

        public override void OnEnter()
        {
            if (AdventureActionUtil.RequirePresenter(nameof(AdventureDrawByID), out var p))
                p.PlayDrawByID(cardID.Value);
            Finish();
        }
    }

    [ActionCategory("Adventure")]
    [Tooltip("要挑戰：跑成功率判定並演出結果。只有牌停在等挑戰時有效。")]
    public class AdventureChallenge : FsmStateAction
    {
        public override void OnEnter()
        {
            if (AdventureActionUtil.RequirePresenter(nameof(AdventureChallenge), out var p)) p.PlayOutcome();
            Finish();
        }
    }

    [ActionCategory("Adventure")]
    [Tooltip("把當前的牌收掉（等挑戰時撤牌 / 等淡出時切短）。等同對話 Adventure(Dismiss)。")]
    public class AdventureDismiss : FsmStateAction
    {
        public override void OnEnter()
        {
            if (AdventureActionUtil.RequirePresenter(nameof(AdventureDismiss), out var p)) p.DismissCard();
            Finish();
        }
    }

    // ── 行動次數（走 Controller）──

    [ActionCategory("Adventure")]
    [Tooltip("變更行動次數（負=消耗、正=補充）。抽牌不會動它，時機由你控制。由正數扣到 0 會發 onMovesExhausted。")]
    public class AdventureAddMoves : FsmStateAction
    {
        [Tooltip("變化量（預設 -1 消耗一次）")]
        public FsmInt amount;

        public override void Reset() { amount = -1; }

        public override void OnEnter()
        {
            if (AdventureActionUtil.RequireController(nameof(AdventureAddMoves), out var c))
                c.AddMoves(amount.Value);
            Finish();
        }
    }

    [ActionCategory("Adventure")]
    [Tooltip("行動次數重設為 Dungeon 的上限。")]
    public class AdventureResetMoves : FsmStateAction
    {
        public override void OnEnter()
        {
            if (AdventureActionUtil.RequireController(nameof(AdventureResetMoves), out var c)) c.ResetMoves();
            Finish();
        }
    }

    [ActionCategory("Adventure")]
    [Tooltip("讀取剩餘行動次數（存到變數），並可依『還有沒有』分支。")]
    public class AdventureGetMoves : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        [Tooltip("存放剩餘行動次數")]
        public FsmInt storeMoves;

        [Tooltip("剩餘 > 0 時送這個事件")]
        public FsmEvent hasMovesEvent;

        [Tooltip("剩餘 <= 0 時送這個事件")]
        public FsmEvent noMovesEvent;

        public override void Reset()
        {
            storeMoves = null;
            hasMovesEvent = null;
            noMovesEvent = null;
        }

        public override void OnEnter()
        {
            var run = AdventureController.Instance != null ? AdventureController.Instance.Run : null;
            int moves = run != null ? run.MovesRemaining : 0;

            if (storeMoves != null && !storeMoves.IsNone) storeMoves.Value = moves;
            Fsm.Event(moves > 0 ? hasMovesEvent : noMovesEvent);
            Finish();
        }
    }
}
