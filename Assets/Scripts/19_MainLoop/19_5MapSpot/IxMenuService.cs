using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using TMPro;
using PixelCrushers.DialogueSystem;

/// <summary>
/// IxMenuService（場景級單例，放在 Map 場景上）
/// 
/// 地圖上的 MapSpot 被點擊時，呼叫 Show() 來顯示互動選單。
/// 選單包含：地點名稱（多語系）、依時間相位切換的地點圖片、
/// 以及由 Flag + 優先次序篩選的互動按鈕（上限由 optionButtons 陣列長度決定）。
/// 
/// 特性：
///   - 點不同 MapSpot 會直接刷新 IxMenu 內容（不需先關閉）
///   - 點選項後 IxMenu 保持開啟，並自動重新篩選選項
///   - MapSpot 永遠可見，不做隱藏控制
///   - 無圖片時自動隱藏 SpotImage 元件
///   - Hide() 公開方法，供關閉按鈕綁定
/// </summary>
[DefaultExecutionOrder(-800)]
public class IxMenuService : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  場景級單例
    // ══════════════════════════════════════════

    public static IxMenuService Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[IxMenuService] 發現重複的 IxMenuService，已銷毀。");
            Destroy(gameObject);
            return;
        }

        // 啟動時預設關閉面板
        if (ixMenuPanel != null)
            ixMenuPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 場景卸載時清除靜態引用，避免殘留
        if (Instance == this)
            Instance = null;
    }

    // ══════════════════════════════════════════
    //  資料結構
    // ══════════════════════════════════════════

    /// <summary>此選項顯示於哪個地圖模式（MapScope）。用 enum 下拉，避免字串填錯。</summary>
    public enum IxScope
    {
        [InspectorName("兩者皆顯示")] Both,
        [InspectorName("僅拜訪模式")] VisitOnly,
        [InspectorName("僅挑戰模式")] UnlockOnly,
    }

    /// <summary>
    /// 單一互動選項的資料。
    /// 顯示由 IxOptionDrawer 控制，請勿在此加 Header / Tooltip。
    /// </summary>
    [Serializable]
    public class IxOption
    {
        public string labelKey;
        public IxScope scope = IxScope.Both;          // 地圖模式條件
        public ProgressFlagDefinition conditionFlag;  // 額外進度條件（未設=不限）
        public int priority;
        public int staminaDelta;

        public bool useResourceCheck;
        public ProtagonistValueRouter.CheckType checkType;
        public int amount;
        public bool checkTime;
        public int timeAmount;

        public UnityEvent onSelected;
        public UnityEvent onFailure;
        public UnityEvent onTimeFailure;
    }

    /// <summary>
    /// 依時間相位顯示不同圖片。
    /// </summary>
    [Serializable]
    public class SpotPhaseImage
    {
        public int phaseIndex;
        public Sprite image;
    }

    // ══════════════════════════════════════════
    //  Inspector：UI 引用
    // ══════════════════════════════════════════

    [Header("=== IxMenu UI 引用 ===")]
    [Tooltip("整個 IxMenu 面板的根物件")]
    [SerializeField] private GameObject ixMenuPanel;

    [Tooltip("顯示地點名稱的 TMP_Text（可掛 LocalizeUIText）")]
    [SerializeField] private TMP_Text spotNameText;

    [Tooltip("顯示地點圖片的 Image")]
    [SerializeField] private Image spotImage;

    [Header("選項按鈕（數量即為顯示上限，依序排列）")]
    [SerializeField] private Button[] optionButtons;

    // ══════════════════════════════════════════
    //  狀態：記住當前顯示的資料（供自動刷新用）
    // ══════════════════════════════════════════

    private string _currentSpotNameKey;
    private List<SpotPhaseImage> _currentPhaseImages;
    private List<IxOption> _currentOptions;

    /// <summary>
    /// IxMenu 是否正在顯示中。
    /// </summary>
    public bool IsShowing => ixMenuPanel != null && ixMenuPanel.activeSelf;

    // ══════════════════════════════════════════
    //  公開方法
    // ══════════════════════════════════════════

    /// <summary>
    /// 顯示或刷新 IxMenu。
    /// 若已開啟會直接刷新內容，不需先關閉。
    /// </summary>
    /// <param name="spotNameKey">地點名稱 key（供 LocalizeUIText 翻譯）</param>
    /// <param name="phaseImages">依時間相位切換的圖片列表</param>
    /// <param name="options">所有可能的互動選項（依 Flag + 優先次序篩選）</param>
    public void Show(string spotNameKey, List<SpotPhaseImage> phaseImages, List<IxOption> options)
    {
        if (ixMenuPanel == null) return;

        // 記住當前資料，供選項點擊後自動刷新用
        _currentSpotNameKey = spotNameKey;
        _currentPhaseImages = phaseImages;
        _currentOptions = options;

        Refresh();

        ixMenuPanel.SetActive(true);
    }

    /// <summary>
    /// 關閉 IxMenu。直接綁在關閉按鈕的 OnClick 上即可。
    /// </summary>
    public void Hide()
    {
        if (ixMenuPanel != null)
            ixMenuPanel.SetActive(false);

        ClearOptionButtons();

        _currentSpotNameKey = null;
        _currentPhaseImages = null;
        _currentOptions = null;
    }

    /// <summary>
    /// 以當前資料重新篩選並刷新顯示。
    /// 可在外部 Flag 變化後手動呼叫。
    /// </summary>
    public void Refresh()
    {
        if (_currentOptions == null) return;

        // --- 1. 地點名稱（多語系） ---
        ApplyLocalizedText(spotNameText, _currentSpotNameKey);

        // --- 2. 地點圖片（依當前 phase，找不到往前回退） ---
        UpdateSpotImage();

        // --- 3. 篩選選項並設定按鈕 ---
        List<IxOption> visible = FilterAndSortOptions(_currentOptions);
        SetupOptionButtons(visible);
    }

    // ══════════════════════════════════════════
    //  內部邏輯：時間相位圖片
    // ══════════════════════════════════════════

    private int GetCurrentPhaseIndex()
    {
        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
            return GameStatusService.Instance.Time.CurrentPhaseIndex;
        return 0;
    }

    private void UpdateSpotImage()
    {
        if (spotImage == null) return;

        if (_currentPhaseImages == null || _currentPhaseImages.Count == 0)
        {
            // 無圖片資料 → 隱藏 Image 元件
            spotImage.gameObject.SetActive(false);
            return;
        }

        Sprite resolved = ResolvePhaseImage(_currentPhaseImages, GetCurrentPhaseIndex());
        if (resolved != null)
        {
            spotImage.sprite = resolved;
            spotImage.gameObject.SetActive(true);
        }
        else
        {
            // 有列表但全部都沒匹配到 → 隱藏
            spotImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 精確匹配當前 phase；找不到則往前找最近的已定義 phase。
    /// </summary>
    private Sprite ResolvePhaseImage(List<SpotPhaseImage> phaseImages, int currentPhase)
    {
        Sprite best = null;
        int bestPhase = int.MinValue;

        foreach (var pi in phaseImages)
        {
            if (pi.image == null) continue;

            // 精確命中直接回傳
            if (pi.phaseIndex == currentPhase)
                return pi.image;

            // 記錄 <= currentPhase 中最近的
            if (pi.phaseIndex < currentPhase && pi.phaseIndex > bestPhase)
            {
                bestPhase = pi.phaseIndex;
                best = pi.image;
            }
        }

        return best;
    }

    // ══════════════════════════════════════════
    //  內部邏輯：Flag 條件篩選 + 優先次序
    // ══════════════════════════════════════════

    /// <summary>依當前 scope 場景旗標，判斷此選項是否該顯示。</summary>
    private bool IsScopeMatched(IxScope scope, ProgressFlagModel flags)
    {
        if (scope == IxScope.Both) return true;
        if (flags == null) return true; // 無 scope 資訊（非地圖情境）時不擋

        bool unlockScope = flags.Contains(MapScopeFlags.Unlock);
        return scope == IxScope.UnlockOnly ? unlockScope : !unlockScope;
    }

    private List<IxOption> FilterAndSortOptions(List<IxOption> allOptions)
    {
        if (allOptions == null) return new List<IxOption>();

        var flags = GameStatusService.Instance?.ProgressFlags;
        List<IxOption> eligible = new List<IxOption>();

        foreach (var opt in allOptions)
        {
            // 1) 地圖模式條件
            if (!IsScopeMatched(opt.scope, flags))
                continue;

            // 2) 額外進度條件（未設=不限）
            if (opt.conditionFlag != null
                && (flags == null || !flags.Contains(opt.conditionFlag.FlagID)))
                continue;

            eligible.Add(opt);
        }

        // 依優先次序排序（數字越小越優先）
        eligible.Sort((a, b) => a.priority.CompareTo(b.priority));

        // 上限由 Inspector 中拖入的按鈕數量決定
        int max = optionButtons != null ? optionButtons.Length : 0;
        if (eligible.Count > max)
            eligible = eligible.GetRange(0, max);

        return eligible;
    }

    // ══════════════════════════════════════════
    //  內部邏輯：按鈕設定
    // ══════════════════════════════════════════

    private void SetupOptionButtons(List<IxOption> visibleOptions)
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;

            if (i < visibleOptions.Count)
            {
                optionButtons[i].gameObject.SetActive(true);

                // 自動從按鈕子物件找 TMP_Text 並設定多語系文字
                var tmpText = optionButtons[i].GetComponentInChildren<TMP_Text>();
                ApplyLocalizedText(tmpText, visibleOptions[i].labelKey);

                // 同步壓力預覽數值到按鈕上的 StaminaPreviewHoverTrigger
                var hoverTrigger = optionButtons[i].GetComponent<StaminaPreviewHoverTrigger>();
                if (hoverTrigger != null)
                {
                    int previewDelta = visibleOptions[i].staminaDelta;

                    // 啟用資源檢查且類型是體力時，自動從 amount 取值
                    if (visibleOptions[i].useResourceCheck
                        && visibleOptions[i].checkType == ProtagonistValueRouter.CheckType.CheckStressAtMost
                        && previewDelta == 0)
                    {
                        // 新版 Router 的 amount 是壓力上限門檻，不是變化量；
                        // 因此不再自動推導 previewDelta。需要預覽時請在 IxOption.staminaDelta 明確設定。
                        previewDelta = 0;
                    }

                    hoverTrigger.SetUseRouter(false);
                    hoverTrigger.SetPreviewDelta(previewDelta);
                }

                // 如果啟用資源檢查，同步設定到按鈕上的 ProtagonistValueRouter
                var router = optionButtons[i].GetComponent<ProtagonistValueRouter>();
                var option = visibleOptions[i];

                if (option.useResourceCheck && router != null)
                {
                    router.checkType = option.checkType;
                    router.amount = option.amount;
                    router.checkTime = option.checkTime;
                    router.timeAmount = option.timeAmount;
                }

                // 綁定點擊事件
                optionButtons[i].onClick.RemoveAllListeners();
                int idx = i;
                optionButtons[i].onClick.AddListener(() =>
                {
                    var opt = visibleOptions[idx];

                    if (opt.useResourceCheck && router != null)
                    {
                        // 走 Router 檢查，根據結果觸發對應事件
                        var result = router.Check();
                        switch (result)
                        {
                            case ProtagonistValueRouter.CheckResult.Success:
                                opt.onSelected?.Invoke();
                                break;
                            case ProtagonistValueRouter.CheckResult.ValueFail:
                                opt.onFailure?.Invoke();
                                break;
                            case ProtagonistValueRouter.CheckResult.TimeFail:
                                opt.onTimeFailure?.Invoke();
                                break;
                        }
                    }
                    else
                    {
                        // 不檢查，直接觸發
                        opt.onSelected?.Invoke();
                    }

                    // 選項可能改變了 Flag 狀態，自動重新篩選
                    Refresh();
                });
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void ClearOptionButtons()
    {
        if (optionButtons == null) return;

        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
    }

    // ══════════════════════════════════════════
    //  內部邏輯：多語系文字工具
    // ══════════════════════════════════════════

    /// <summary>
    /// 用 DialogueManager.GetLocalizedText 查表翻譯，設定到 TMP_Text。
    /// 找不到翻譯時 fallback 顯示原始 key。
    /// </summary>
    private void ApplyLocalizedText(TMP_Text textComponent, string key)
    {
        if (textComponent == null || string.IsNullOrEmpty(key)) return;

        string localized = DialogueManager.GetLocalizedText(key);
        textComponent.text = string.IsNullOrEmpty(localized) ? key : localized;
    }
}