using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 女主角狀態面板 UI（單例）。
/// 透過 CanvasGroup 控制顯示/隱藏，初始不可見。
/// 
/// 使用方式：
///   HeroineUI.Instance.Show("Heroine01");   // 指定 ID 開啟
///   HeroineUI.Instance.ShowByOrder(0);       // 指定順位開啟
///   HeroineUI.Instance.Hide();               // 關閉面板
/// </summary>
public class HeroineUI : MonoBehaviour
{
    // ==========================================================
    //  Singleton
    // ==========================================================
    public static HeroineUI Instance { get; private set; }

    // ==========================================================
    //  Inspector 設定
    // ==========================================================

    [Header("順位列表 (依序填入 HeroineID)")]
    [Tooltip("在 Inspector 中按照你想要的 Next 切換順序填入 HeroineID")]
    [SerializeField] private List<string> heroineOrder = new List<string>();

    [Header("UI 元件綁定")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("標題列的開發度等級文字 ( Lv.X)")]
    [SerializeField] private TextMeshProUGUI lewdnessLevelText;

    [Tooltip("開發度經驗 Slider")]
    [SerializeField] private Slider lewdnessExpSlider;

    [Tooltip("親密程度等級文字 ( Lv.X)")]
    [SerializeField] private TextMeshProUGUI affinityLevelText;

    [Tooltip("親密度經驗 Slider")]
    [SerializeField] private Slider affinityExpSlider;

    [Tooltip("興奮程度等級文字 ( Lv.X)")]
    [SerializeField] private TextMeshProUGUI excitementLevelText;

    [Tooltip("興奮程度經驗 Slider ")]
    [SerializeField] private Slider excitementExpSlider;

    [Tooltip("不快值 / 可疑度 Slider ")]
    [SerializeField] private Slider suspicionSlider;

    [Tooltip("可疑度數值文字 (顯示 PersonalSuspicion 數值)")]
    [SerializeField] private TextMeshProUGUI suspicionValueText;

    [Header("上限狀態")]
    [Tooltip("當達到興奮度等級上限或被鎖定時顯示的物件 (例如標記為 MAX 的圖片)")]
    [SerializeField] private GameObject maxExcitementObject;

    [Header("按鈕綁定")]
    [SerializeField] private Button detailButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    // ==========================================================
    //  內部狀態
    // ==========================================================
    private int currentOrderIndex = 0;
    private HeroineStatusModel currentModel;

    // --- 可疑度 Slider 變色 ---
    [Header("可疑度 Slider 變色設定")]
    [Tooltip("觸發警告色的填充百分比 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float suspicionWarningRatio = 0.7f;
    [Tooltip("警告色")]
    [SerializeField] private Color suspicionColorWarning = new Color(1f, 0.392f, 0.392f, 1f); // #FF6464
    [Tooltip("觸發危險色的填充百分比 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float suspicionDangerRatio = 0.9f;
    [Tooltip("危險色")]
    [SerializeField] private Color suspicionColorDanger = new Color(1f, 0.129f, 0.129f, 1f); // #FF2121

    private Color suspicionColorDefault;
    private bool suspicionDefaultColorCached = false;

    [Header("可疑度數值文字顏色")]
    [Tooltip("正常狀態的文字顏色")]
    [SerializeField] private Color suspicionTextColorNormal = Color.black;
    [Tooltip("文字變為警告色的可疑度門檻")]
    [SerializeField] private int suspicionTextWarningThreshold = 70;
    [Tooltip("文字變為危險色的可疑度門檻")]
    [SerializeField] private int suspicionTextDangerThreshold = 100;

    // ==========================================================
    //  生命週期
    // ==========================================================

    private void Awake()
    {
        // Singleton 保護
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始隱藏
        SetCanvasGroupVisible(false);

        // 初始隱藏 MAX 物件
        if (maxExcitementObject != null) maxExcitementObject.SetActive(false);

        // 綁定按鈕
        if (detailButton != null) detailButton.onClick.AddListener(OnDetailClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        // 清除事件訂閱
        UnsubscribeFromModel();

        if (Instance == this) Instance = null;
    }

    // ==========================================================
    //  公開方法：顯示 / 隱藏
    // ==========================================================

    /// <summary>
    /// 依「順位索引」開啟面板 (對應 heroineOrder 列表)。
    /// </summary>
    public void ShowByOrder(int orderIndex)
    {
        if (heroineOrder == null || heroineOrder.Count == 0)
        {
            Debug.LogWarning("[HeroineUI] heroineOrder 為空，無法顯示。");
            return;
        }

        currentOrderIndex = Mathf.Clamp(orderIndex, 0, heroineOrder.Count - 1);
        ApplyHeroineData(heroineOrder[currentOrderIndex]);
        SetCanvasGroupVisible(true);
    }

    /// <summary>
    /// 以 HeroineID 開啟面板。
    /// 若該 ID 存在於順位列表中，會同步更新 currentOrderIndex。
    /// </summary>
    public void Show(string heroineID)
    {
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning("[HeroineUI] heroineID 為空。");
            return;
        }

        // 嘗試同步順位索引
        int idx = heroineOrder.IndexOf(heroineID);
        if (idx >= 0) currentOrderIndex = idx;

        ApplyHeroineData(heroineID);
        SetCanvasGroupVisible(true);
    }

    /// <summary>
    /// 關閉面板。
    /// </summary>
    public void Hide()
    {
        SetCanvasGroupVisible(false);
        UnsubscribeFromModel();
        currentModel = null;
    }

    // ==========================================================
    //  按鈕回調
    // ==========================================================

    private void OnDetailClicked()
    {
        Debug.Log($"[HeroineUI] Detail 按鈕被按下。當前角色: {currentModel?.HeroineID ?? "null"}");
        // TODO: 開啟詳細資訊面板
    }

    private void OnNextClicked()
    {
        if (heroineOrder == null || heroineOrder.Count == 0) return;

        // 循環切換至下一順位
        currentOrderIndex = (currentOrderIndex + 1) % heroineOrder.Count;
        ApplyHeroineData(heroineOrder[currentOrderIndex]);
    }

    // ==========================================================
    //  資料綁定
    // ==========================================================

    /// <summary>
    /// 根據 HeroineID，從 GameStatusService 取得 Model 並更新 UI。
    /// </summary>
    private void ApplyHeroineData(string heroineID)
    {
        // 先退訂舊的
        UnsubscribeFromModel();

        // 從 Service 取得 Model
        var service = GameStatusService.Instance;
        if (service == null || service.Heroines == null)
        {
            Debug.LogWarning("[HeroineUI] GameStatusService 尚未就緒。");
            return;
        }

        if (!service.Heroines.TryGetValue(heroineID, out var model))
        {
            Debug.LogWarning($"[HeroineUI] 找不到 HeroineID: {heroineID}");
            return;
        }

        currentModel = model;

        // 訂閱事件
        SubscribeToModel();

        // 立即刷新一次 UI
        RefreshAllUI();
    }

    // ==========================================================
    //  事件訂閱 / 退訂
    // ==========================================================

    private void SubscribeToModel()
    {
        if (currentModel == null) return;

        currentModel.OnLewdnessChanged += HandleLewdnessChanged;
        currentModel.OnAffinityChanged += HandleAffinityChanged;
        currentModel.OnExcitementChanged += HandleExcitementChanged;
        currentModel.OnExcitementLevelChanged += HandleExcitementLevelChanged;
        currentModel.OnPersonalSuspicionChanged += HandleSuspicionChanged;
    }

    private void UnsubscribeFromModel()
    {
        if (currentModel == null) return;

        currentModel.OnLewdnessChanged -= HandleLewdnessChanged;
        currentModel.OnAffinityChanged -= HandleAffinityChanged;
        currentModel.OnExcitementChanged -= HandleExcitementChanged;
        currentModel.OnExcitementLevelChanged -= HandleExcitementLevelChanged;
        currentModel.OnPersonalSuspicionChanged -= HandleSuspicionChanged;
    }

    // ==========================================================
    //  事件處理 (Handler)
    // ==========================================================

    private void HandleLewdnessChanged(int _) => RefreshLewdnessUI();
    private void HandleAffinityChanged(int _) => RefreshAffinityUI();
    private void HandleExcitementChanged(int _) => RefreshExcitementExpUI();
    private void HandleExcitementLevelChanged(int _) => RefreshExcitementLevelUI();
    private void HandleSuspicionChanged(int _) => RefreshSuspicionUI();

    // ==========================================================
    //  UI 刷新
    // ==========================================================

    /// <summary>一次刷新面板上所有欄位。</summary>
    private void RefreshAllUI()
    {
        RefreshLewdnessUI();
        RefreshAffinityUI();
        RefreshExcitementLevelUI();
        RefreshExcitementExpUI();
        RefreshSuspicionUI();
    }

    /// <summary>刷新開發度等級文字與經驗 Slider</summary>
    private void RefreshLewdnessUI()
    {
        if (currentModel == null) return;

        // 等級文字
        if (lewdnessLevelText != null)
            lewdnessLevelText.text = $"Lv.{currentModel.LewdnessLevel}";

        // 經驗 Slider
        if (lewdnessExpSlider == null) return;

        int threshold = currentModel.GetCurrentLewdnessThreshold(currentModel.LewdnessLevel);
        if (threshold <= 0)
        {
            lewdnessExpSlider.value = 0f;
            return;
        }

        lewdnessExpSlider.maxValue = threshold;
        lewdnessExpSlider.value = currentModel.LewdnessExp;
    }

    /// <summary>刷新親密度等級文字與經驗 Slider</summary>
    private void RefreshAffinityUI()
    {
        if (currentModel == null) return;

        // 等級文字
        if (affinityLevelText != null)
            affinityLevelText.text = $"Lv.{currentModel.BaseAffinityLevel}";

        // 經驗 Slider
        if (affinityExpSlider == null) return;

        // 判斷是否處於鎖定狀態
        bool isLocked = currentModel.IsAffinityLevelLocked(currentModel.BaseAffinityLevel);
        if (isLocked)
        {
            affinityExpSlider.value = 0f;
            return;
        }

        int threshold = currentModel.GetCurrentAffinityThreshold(currentModel.BaseAffinityLevel);
        if (threshold <= 0)
        {
            affinityExpSlider.value = 0f;
            return;
        }

        affinityExpSlider.maxValue = threshold;
        affinityExpSlider.value = currentModel.BaseAffinityExp;
    }

    /// <summary>刷新興奮度等級 (藍框 Lv.X)</summary>
    private void RefreshExcitementLevelUI()
    {
        if (currentModel == null) return;
        if (excitementLevelText != null)
            excitementLevelText.text = $"Lv.{currentModel.BaseExcitementLevel}";
    }

    /// <summary>刷新興奮度經驗 Slider (綠箭頭)，並處理 MAX 狀態顯示。</summary>
    private void RefreshExcitementExpUI()
    {
        if (currentModel == null) return;

        // 判斷是否處於 MAX 狀態 (當前等級被鎖定，代表已達上限或被劇情鎖住)
        bool isMax = currentModel.IsExcitementLevelLocked(currentModel.BaseExcitementLevel);
        if (maxExcitementObject != null) maxExcitementObject.SetActive(isMax);

        if (excitementExpSlider == null) return;

        if (isMax)
        {
            // MAX 狀態下，Slider 歸零
            excitementExpSlider.value = 0f;
            return;
        }

        int threshold = currentModel.GetCurrentExcitementThreshold(currentModel.BaseExcitementLevel);
        if (threshold <= 0)
        {
            excitementExpSlider.value = 0f;
            return;
        }

        excitementExpSlider.maxValue = threshold;
        excitementExpSlider.value = currentModel.BaseExcitementExp;
    }

    /// <summary>刷新可疑度 Slider (紅箭頭)，並根據百分比變色。</summary>
    private void RefreshSuspicionUI()
    {
        if (currentModel == null) return;
        if (suspicionSlider == null) return;

        // 快取預設顏色 (只做一次)
        if (!suspicionDefaultColorCached && suspicionSlider.fillRect != null)
        {
            Image fillImg = suspicionSlider.fillRect.GetComponent<Image>();
            if (fillImg != null)
            {
                suspicionColorDefault = fillImg.color;
                suspicionDefaultColorCached = true;
            }
        }

        int max = currentModel.PersonalSuspicionMax;
        if (max <= 0)
        {
            suspicionSlider.value = 0f;
            return;
        }

        suspicionSlider.maxValue = max;
        suspicionSlider.value = currentModel.PersonalSuspicion;

        // 更新可疑度數值文字
        if (suspicionValueText != null)
        {
            int sus = currentModel.PersonalSuspicion;
            suspicionValueText.text = sus.ToString();

            if (sus >= suspicionTextDangerThreshold)
                suspicionValueText.color = suspicionColorDanger;
            else if (sus >= suspicionTextWarningThreshold)
                suspicionValueText.color = suspicionColorWarning;
            else
                suspicionValueText.color = suspicionTextColorNormal;
        }

        // 根據填充比例變更顏色
        ApplySuspicionSliderColor(suspicionSlider);
    }

    /// <summary>
    /// 根據可疑度百分比變更 Slider Fill 顏色。
    /// ≥90% → #FF2121, ≥70% → #FF6464, 否則恢復預設。
    /// </summary>
    private void ApplySuspicionSliderColor(Slider slider)
    {
        if (slider == null || slider.fillRect == null) return;

        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage == null) return;

        float ratio = (slider.maxValue > 0) ? slider.value / slider.maxValue : 0f;

        if (ratio >= suspicionDangerRatio)
            fillImage.color = suspicionColorDanger;
        else if (ratio >= suspicionWarningRatio)
            fillImage.color = suspicionColorWarning;
        else
            fillImage.color = suspicionColorDefault;
    }

    // ==========================================================
    //  CanvasGroup 開關
    // ==========================================================

    private void SetCanvasGroupVisible(bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}