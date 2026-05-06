using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using HutongGames.PlayMaker;
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 開發度結算 Slider 演出
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

    private Coroutine _currentCoroutine;
    private Image _sliderFillImage;
    private int _totalFilledExp; // 本次動畫累積填入量，用於換色判斷
    private int _colorStage;     // 目前換色階段（0/1/2），只升不降

    // FillExp 使用欄位取代 ref 參數（IEnumerator 不支援 ref）
    private int _currentLv;
    private int _currentExp;

    // PlayMaker 無參數呼叫用的暫存
    private LewdnessBreakdown _pendingBreakdown;
    private PlayMakerFSM _targetFsm;

    void Awake()
    {
        // 快取 slider fill 的 Image 元件
        if (lewdSlider != null && lewdSlider.fillRect != null)
            _sliderFillImage = lewdSlider.fillRect.GetComponent<Image>();

        SetSliderColor(colorDefault);
        HideAllRows();
    }

    // ═══════════════════════════════════════════════
    // 公開入口（由 ResultHandler / FSM 呼叫）
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 開始演出。由 ResultHandler 呼叫，傳入資料並立即執行。
    /// </summary>
    public void StartSliderPerformance(LewdnessBreakdown breakdown, PlayMakerFSM targetFsm)
    {
        if (breakdown == null) return;
        _pendingBreakdown = breakdown;
        _targetFsm = targetFsm;
        StartSliderPerformance();
    }
    /// <summary>
    /// 由 ResultHandler 呼叫，只儲存資料，不執行動畫。
    /// FSM 在適當時機呼叫無參數的 StartSliderPerformance() 來觸發演出。
    /// </summary>
    public void PrepareBreakdown(LewdnessBreakdown breakdown, PlayMakerFSM targetFsm)
    {
        if (breakdown == null) return;
        _pendingBreakdown = breakdown;
        _targetFsm = targetFsm;
    }

    /// <summary>
    /// 無參數版本，供 PlayMaker Call Method 使用。
    /// ResultHandler 已在呼叫有參數版本時把資料存好，FSM 直接呼叫此方法即可。
    /// </summary>
    public void StartSliderPerformance()
    {
        
        if (_pendingBreakdown == null)
        {
            Debug.LogWarning("[LewdnessSliderPerformance] 尚未設定 breakdown，無法開始演出。");
            return;
        }

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);

        _totalFilledExp = 0;
        _colorStage = 0;
        SetSliderColor(colorDefault);
        if (totalExpText != null) totalExpText.text = "+0";
        SetTotalExpStyle(totalExpColorDefault, 1f);
        UpdateExpProgressUI();
        HideAllRows();

        _currentCoroutine = StartCoroutine(DoPerformance(_pendingBreakdown, _targetFsm));
    }

    // ═══════════════════════════════════════════════
    // 主演出流程
    // ═══════════════════════════════════════════════

    private IEnumerator DoPerformance(LewdnessBreakdown bd, PlayMakerFSM targetFsm)
    {
        _currentLv = bd.StartLevel;
        _currentExp = bd.StartExp;

        UpdateLevelUI(_currentLv);
        RefreshSlider(_currentLv, _currentExp);

        // ── 步驟 1：遊戲得分（必顯示）────────────────────
        yield return new WaitForSeconds(rowRevealDelay);
        RevealGameScoreRow(bd.GameScore, bd.GameScoreConverted);
        if (bd.GameScoreConverted > 0)
            yield return StartCoroutine(FillExp(bd.GameScoreConverted));

        // ── 步驟 2：興奮度（必顯示）────────────────────
        yield return new WaitForSeconds(rowRevealDelay);
        RevealExcitedLvRow(bd.LocalExcitedLv, bd.ExcitedLvBonus);
        if (bd.ExcitedLvBonus > 0)
            yield return StartCoroutine(FillExp(bd.ExcitedLvBonus));

        // ── 步驟 3：其他額外加分項目（0 則跳過）────────
        var bonusItems = new (string count, int value)[]
        {
            ($"X{bd.OrgasmTimes}",    bd.OrgasmTimesBonus),
            ($"X{bd.ShootTimes}",     bd.ShootTimesBonus),
            ($"X{bd.OverShootTimes}", bd.OverShootTimesBonus),
        };

        int rowIdx = 0;
        foreach (var item in bonusItems)
        {
            if (item.value > 0 && rowIdx < bonusRows.Count)
            {
                yield return new WaitForSeconds(rowRevealDelay);
                RevealBonusRow(rowIdx, item.count, item.value);
                yield return StartCoroutine(FillExp(item.value));
                rowIdx++;
            }
        }

        // ── 步驟 4：結束方式（必顯示，可為負數但不讓總累積低於 0）──
        yield return new WaitForSeconds(rowRevealDelay);
        RevealReasonRow(bd.ReasonDisplayName, bd.BaseScore);
        if (bd.BaseScore != 0)
        {
            if (bd.BaseScore > 0)
            {
                yield return StartCoroutine(FillExp(bd.BaseScore));
            }
            else
            {
                // 負數：扣分但不讓總累積低於 0
                int maxDeduction = _totalFilledExp; // 目前累積了多少
                int actualDeduction = Mathf.Min(maxDeduction, Mathf.Abs(bd.BaseScore));
                if (actualDeduction > 0)
                    yield return StartCoroutine(DrainExp(actualDeduction));
            }
        }

        // ── 步驟 5：額外乘倍項目 ──────────────────────
        var multiplierItems = new (string label, bool active, int multiplier)[]
        {
            ("旁邊很危險", bd.DangerScene,       bd.DangerSceneMultiplier),
            ("接受邀約",   bd.ChallengeAccepted, bd.ChallengeAcceptedMultiplier),
        };

        int mRowIdx = 0;
        foreach (var item in multiplierItems)
        {
            if (item.active && mRowIdx < multiplierRows.Count)
            {
                yield return new WaitForSeconds(rowRevealDelay);
                RevealMultiplierRow(mRowIdx, item.multiplier);

                // 乘倍觸發時換色 + totalExpText 變色放大
                if (mRowIdx == 0)
                {
                    _colorStage = 1;
                    SetSliderColor(colorStage1);
                    SetTotalExpStyle(totalExpColorStage1, totalExpScaleStage1);
                }
                else if (mRowIdx == 1)
                {
                    _colorStage = 2;
                    SetSliderColor(colorStage2);
                    SetTotalExpStyle(totalExpColorStage2, totalExpScaleStage2);
                }

                // 乘倍演出：補填「當下全程累積值 × (multiplier - 1)」
                int extraExp = _totalFilledExp * (item.multiplier - 1);
                yield return StartCoroutine(FillExp(extraExp));

                mRowIdx++;
            }
        }

        // ── 完成 ──────────────────────────────────────
        Debug.Log($"[LewdnessSliderPerformance] 演出完成，發送事件: {finishEventName}");
        if (targetFsm != null)
            targetFsm.SendEvent(finishEventName);
    }

    // ═══════════════════════════════════════════════
    // Slider 填入協程
    // ═══════════════════════════════════════════════

    // 使用 _currentLv / _currentExp 欄位，避免 IEnumerator 不支援 ref 參數
    // _totalFilledExp 是全程累積值，跨 row、跨升等都不歸零
    private IEnumerator FillExp(int expAmount)
    {
        int remaining = expAmount;
        int totalAtStart = _totalFilledExp; // 記錄本次 FillExp 開始前的累積值
        int filledThisCall = 0;

        while (remaining > 0)
        {
            int threshold = GetThreshold(_currentLv);
            int spaceInLevel = threshold - _currentExp;

            // 保護：如果當前等級已滿，先升等再繼續
            if (spaceInLevel <= 0)
            {
                _currentLv++;
                _currentExp = 0;
                UpdateLevelUI(_currentLv);
                lewdSlider.value = 0;
                UpdateExpProgressUI();
                yield return new WaitForSeconds(levelUpPause);
                continue;
            }

            int fillAmount = Mathf.Min(remaining, spaceInLevel);
            int targetExp = _currentExp + fillAmount;

            lewdSlider.maxValue = threshold;

            // 平滑填入
            while (lewdSlider.value < targetExp)
            {
                lewdSlider.value += threshold * fillSpeed * Time.deltaTime;

                // 每幀更新全程累積量
                _totalFilledExp = totalAtStart + filledThisCall
                    + Mathf.RoundToInt(lewdSlider.value - _currentExp);
                UpdateSliderColor();
                UpdateTotalExpUI();
                UpdateExpProgressUI();

                yield return null;
            }

            lewdSlider.value = targetExp;
            filledThisCall += fillAmount;
            remaining -= fillAmount;
            _currentExp = targetExp;
            _totalFilledExp = totalAtStart + filledThisCall;
            UpdateSliderColor();
            UpdateTotalExpUI();
            UpdateExpProgressUI();

            // 升等
            if (_currentExp >= threshold)
            {
                _currentLv++;
                _currentExp = 0;
                UpdateLevelUI(_currentLv);
                lewdSlider.value = 0;
                UpdateExpProgressUI();
                yield return new WaitForSeconds(levelUpPause);
            }
        }
    }

    /// <summary>
    /// 扣除經驗的動畫（用於結束方式扣分）。
    /// 只扣 slider 視覺與 _totalFilledExp，不會降級。
    /// </summary>
    private IEnumerator DrainExp(int expAmount)
    {
        int remaining = expAmount;
        int totalAtStart = _totalFilledExp;
        int drainedThisCall = 0;

        while (remaining > 0)
        {
            int drainThisFrame = Mathf.Min(remaining, Mathf.Max(1,
                Mathf.RoundToInt(GetThreshold(_currentLv) * fillSpeed * Time.deltaTime)));
            drainThisFrame = Mathf.Min(drainThisFrame, remaining);

            _currentExp = Mathf.Max(0, _currentExp - drainThisFrame);
            lewdSlider.value = _currentExp;
            drainedThisCall += drainThisFrame;
            remaining -= drainThisFrame;

            _totalFilledExp = Mathf.Max(0, totalAtStart - drainedThisCall);
            UpdateTotalExpUI();
            UpdateExpProgressUI();

            yield return null;
        }

        _totalFilledExp = Mathf.Max(0, totalAtStart - expAmount);
        UpdateTotalExpUI();
        UpdateExpProgressUI();
    }

    // ═══════════════════════════════════════════════
    // UI 輔助
    // ═══════════════════════════════════════════════

    private void RefreshSlider(int lv, int exp)
    {
        int threshold = GetThreshold(lv);
        lewdSlider.maxValue = threshold;
        lewdSlider.value = exp;
    }

    private void UpdateLevelUI(int lv)
    {
        if (levelText != null) levelText.text = $" Lv.{lv}";
    }

    private void UpdateSliderColor()
    {
        // 換色由乘倍事件觸發，此處不做任何事
    }

    private void SetSliderColor(Color color)
    {
        if (_sliderFillImage != null)
            _sliderFillImage.color = color;
    }

    private void RevealBonusRow(int idx, string count, int value)
    {
        if (idx >= bonusRows.Count) return;
        var row = bonusRows[idx];
        if (row.rowRoot != null) row.rowRoot.SetActive(true);
        if (row.countText != null) row.countText.text = count;
        if (row.valueText != null) row.valueText.text = value.ToString();
    }

    private void RevealGameScoreRow(int gameScore, int convertedExp)
    {
        if (gameScoreRow == null) return;
        if (gameScoreRow.rowRoot != null) gameScoreRow.rowRoot.SetActive(true);
        if (gameScoreRow.countText != null) gameScoreRow.countText.text = gameScore.ToString();
        if (gameScoreRow.valueText != null) gameScoreRow.valueText.text = convertedExp.ToString();
    }

    private void RevealReasonRow(string displayName, int score)
    {
        if (reasonRow == null) return;
        if (reasonRow.rowRoot != null) reasonRow.rowRoot.SetActive(true);
        if (reasonRow.countText != null) reasonRow.countText.text = displayName;
        if (reasonRow.valueText != null) reasonRow.valueText.text = (score >= 0 ? score.ToString() : score.ToString());
    }

    private void RevealMultiplierRow(int idx, int multiplier)
    {
        if (idx >= multiplierRows.Count) return;
        var row = multiplierRows[idx];
        if (row.rowRoot != null) row.rowRoot.SetActive(true);
        if (row.multiplierText != null) row.multiplierText.text = $"X{multiplier}!";
    }

    private void HideAllRows()
    {
        if (gameScoreRow?.rowRoot != null) gameScoreRow.rowRoot.SetActive(false);
        if (excitedLvRow?.rowRoot != null) excitedLvRow.rowRoot.SetActive(false);
        foreach (var row in bonusRows)
            if (row.rowRoot != null) row.rowRoot.SetActive(false);
        if (reasonRow?.rowRoot != null) reasonRow.rowRoot.SetActive(false);
        foreach (var row in multiplierRows)
            if (row.rowRoot != null) row.rowRoot.SetActive(false);
    }

    private void RevealExcitedLvRow(int lv, int value)
    {
        if (excitedLvRow == null) return;
        if (excitedLvRow.rowRoot != null) excitedLvRow.rowRoot.SetActive(true);
        if (excitedLvRow.lvText != null) excitedLvRow.lvText.text = $"Lv.{lv}";
        if (excitedLvRow.valueText != null) excitedLvRow.valueText.text = value.ToString();
    }

    private void SetTotalExpStyle(Color color, float scale)
    {
        if (totalExpText == null) return;
        totalExpText.color = color;
        totalExpText.transform.localScale = Vector3.one * scale;
    }

    private void UpdateTotalExpUI()
    {
        if (totalExpText != null)
            totalExpText.text = $"+{_totalFilledExp}";
    }

    private void UpdateExpProgressUI()
    {
        if (expProgressText == null) return;
        int threshold = GetThreshold(_currentLv);
        int displayExp = Mathf.RoundToInt(lewdSlider.value);
        expProgressText.text = $"({displayExp}/{threshold})";
    }

    private int GetThreshold(int level)
    {
        var config = GameStatusService.Instance?.HeroineConfig;
        if (config != null && config.lewdnessExpTable != null)
        {
            int idx = Mathf.Clamp(level, 0, config.lewdnessExpTable.Count - 1);
            return config.lewdnessExpTable[idx];
        }
        return 100;
    }
}