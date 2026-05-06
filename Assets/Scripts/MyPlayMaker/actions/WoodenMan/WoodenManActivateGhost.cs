using HutongGames.PlayMaker;
using WoodenMan;
using UnityEngine;
using Tooltip=HutongGames.PlayMaker.TooltipAttribute;

namespace WoodenMan.PlayMakerActions
{
    // 1. 啟動鬼怪
    [ActionCategory("Wooden Man")]
    [Tooltip("根據 ActionID 啟用對應的鬼怪物件並加入監測清單。")]
    public class WoodenManActivateGhost : FsmStateAction
    {
        [RequiredField]
        [Tooltip("對應 RiskAction 的 inspectionTypeID")]
        public FsmString actionID;

        public override void OnEnter()
        {
            if (WoodenManGameManager.Instance != null)
                WoodenManGameManager.Instance.ActivateGhostByID(actionID.Value); //
            Finish();
        }
    }

    // 2. 關閉鬼怪
    [ActionCategory("Wooden Man")]
    [Tooltip("停止該鬼怪行為並將其隱藏。")]
    public class WoodenManDeactivateGhost : FsmStateAction
    {
        [RequiredField]
        [Tooltip("對應 RiskAction 的 inspectionTypeID")]
        public FsmString actionID;

        public override void OnEnter()
        {
            if (WoodenManGameManager.Instance != null)
                WoodenManGameManager.Instance.DeactivateGhostByID(actionID.Value); //
            Finish();
        }
    }

    // 3. 開始計時
    [ActionCategory("Wooden Man")]
    [Tooltip("啟動所有已啟用鬼怪的行為計時器（開始隨機回頭邏輯）。")]
    public class WoodenManStartTimer : FsmStateAction
    {
        public override void OnEnter()
        {
            if (WoodenManGameManager.Instance != null)
                WoodenManGameManager.Instance.StartGhostTimer(); //
            Finish();
        }
    }

    // 4. 停止計時
    [ActionCategory("Wooden Man")]
    [Tooltip("暫停所有鬼怪的行為。")]
    public class WoodenManStopTimer : FsmStateAction
    {
        public override void OnEnter()
        {
            if (WoodenManGameManager.Instance != null)
                WoodenManGameManager.Instance.StopGhostTimer(); //
            Finish();
        }
    }

    // 5. 強制檢查
    [ActionCategory("Wooden Man")]
    [Tooltip("手動觸發一次鬼怪檢查。")]
    public class WoodenManTriggerGhostCheck : FsmStateAction
    {
        [HasFloatSlider(0, 1)]
        [Tooltip("觸發機率 (0~1)。若設為 -1 則使用 Manager 預設值。")]
        public FsmFloat probability = -1f;

        public override void OnEnter()
        {
            if (WoodenManGameManager.Instance != null)
                WoodenManGameManager.Instance.TriggerGhostCheck(probability.Value); //
            Finish();
        }
    }

    // 6. 增加危險度
    [ActionCategory("Wooden Man")]
    [Tooltip("如果鬼怪正在注視，則立即增加指定數值的危險點數。")]
    public class WoodenManAddDangerPoints : FsmStateAction
    {
        [RequiredField]
        public FsmInt amount;

        public override void OnEnter()
        {
            if (WoodenManGameManager.Instance != null)
                WoodenManGameManager.Instance.AddDangerPoints(amount.Value); //
            Finish();
        }
    }
}