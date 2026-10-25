using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using HutongGames.PlayMaker;
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 已退役的開發度結算 Slider 演出（保留舊場景欄位）
///
/// 演出順序：
///   1. 基礎得分 → slider 填入
///   2. 額外加分項目（逐項顯示，值為 0 跳過）→ slider 填入
///   3. 額外乘倍項目（逐項顯示，false 跳過）→ 每個乘倍讓 slider 再填入目前累積量
///   4. 累積填入量超過 colorThreshold1 / colorThreshold2 時換色
/// </summary>
public class LewdnessSliderPerformance : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    // Inspector 設定
    // ═══════════════════════════════════════════════

    [Header("UI 元件")]
    public Slider lewdSlider;
    public TextMeshProUGUI levelText;
    [Tooltip("即時顯示累積開發度增加量，例如 +123")]
    public TextMeshProUGUI totalExpText;
    [Tooltip("即時顯示當前 exp 進度，例如 (123/500)")]
    public TextMeshProUGUI expProgressText;

    [Header("遊戲得分列（獨立、必顯示）")]
    public ScoreRowUI gameScoreRow;

    [Header("興奮度列（獨立、必顯示）")]
    public ExcitedLvRowUI excitedLvRow;

    [Header("分項顯示列（按順序對應）")]
    [Tooltip("依序: 高潮次數、射精次數、超越極限射精")]
    public List<ScoreRowUI> bonusRows = new List<ScoreRowUI>();

    [Header("結束方式列（最後顯示）")]
    public ScoreRowUI reasonRow;

    [Tooltip("依序: 旁邊很危險、接受邀約")]
    public List<MultiplierRowUI> multiplierRows = new List<MultiplierRowUI>();

    [Header("動畫設定")]
    public float fillSpeed = 0.6f;  // slider 每秒填入比例（相對 maxValue）
    public float levelUpPause = 0.5f;  // 升等後停頓秒數
    public float rowRevealDelay = 0.3f;  // 每列顯示前的停頓秒數

    [Header("Slider 顏色（第1次乘倍/第2次乘倍時觸發）")]
    public Color colorDefault = Color.green;
    public Color colorStage1 = Color.yellow;
    public Color colorStage2 = Color.red;

    [Header("totalExpText 顏色（獨立調整）")]
    public Color totalExpColorDefault = Color.white;
    public Color totalExpColorStage1 = Color.yellow;
    public Color totalExpColorStage2 = Color.red;

    [Header("totalExpText 放大倍率（第1次/第2次乘倍時觸發）")]
    public float totalExpScaleStage1 = 1.1f;
    public float totalExpScaleStage2 = 1.25f;

    [Header("PlayMaker 設定")]
    [Tooltip("動畫結束後發送給 FSM 的事件名稱")]
    public string finishEventName = "FINISH_SLIDER_ANIM";

    // ═══════════════════════════════════════════════
    // 子 UI 結構
    // ═══════════════════════════════════════════════

    [System.Serializable]
    public class ScoreRowUI
    {
        public GameObject rowRoot;        // 整列根物件（預設隱藏）
        public TextMeshProUGUI countText; // 顯示 "X5" 之類的次數
        public TextMeshProUGUI valueText; // 顯示加分數值
    }

    [System.Serializable]
    public class MultiplierRowUI
    {
        public GameObject rowRoot;
        public TextMeshProUGUI multiplierText; // 顯示 "X2!"
    }

    [System.Serializable]
    public class ExcitedLvRowUI
    {
        public GameObject rowRoot;        // 必顯示，不需要隱藏
        public TextMeshProUGUI lvText;    // 顯示目前 Lv，例如 "Lv.1"
        public TextMeshProUGUI valueText; // 顯示加分值
    }

    // ═══════════════════════════════════════════════
    // 私有狀態
    // ═══════════════════════════════════════════════

    // 保留元件與序列化欄位；舊經驗演出不再運作，也不偽造完成事件。
    public void StartSliderPerformance(LewdnessBreakdown breakdown, PlayMakerFSM targetFsm) { WarnRetired(); }
    public void PrepareBreakdown(LewdnessBreakdown breakdown, PlayMakerFSM targetFsm) { WarnRetired(); }
    public void StartSliderPerformance() { WarnRetired(); }
    private void WarnRetired() => Debug.LogWarning("[LewdnessSliderPerformance] 舊開發度經驗演出已退役，請移除 FSM 的演出及等待完成步驟。", this);
}
