/*
 * [EroMinigame UI 控制器] - 版本 3.9
 * 變更項目：
 * 1. 新增對 fsm_LocalExcitedThreshold 的監控。
 * 2. 動態更新 excitementSlider 的 maxValue，以符合變動門檻邏輯。
 * 3. 新增 excitedStateText：依興奮等級從 Text Table 查詢本地化字串顯示
 *    (預設 4 組：Neutral / Shy / Excited / Overload，可在 Inspector 擴增)。
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HutongGames.PlayMaker;
using DG.Tweening;
using PixelCrushers.DialogueSystem; // 為了使用 DialogueManager
using Tooltip = UnityEngine.TooltipAttribute;

public class EroMinigameUIController : MonoBehaviour
{
    [Header("FSM 來源 (唯一數據主體)")]
    public PlayMakerFSM eroMinigameFSM;

    [Header("UI 元件參考 - 基礎 Slider")]
    public Slider discomfortSlider;
    public Slider excitementSlider;
    public Slider suspicionSlider;

    [Header("UI 元件參考 - 狀態等級")]
    [UnityEngine.Tooltip("只顯示等級的文字 (格式: Lv.1)")]
    public TextMeshProUGUI lewdnessLevelText;
    private int lastLewdnessLevelTarget = -1;

    [Header("UI 元件參考 - Text")]
    public TextMeshProUGUI heroineIDText;
    [UnityEngine.Tooltip("顯示絕頂次數的文字 (格式: x8)")]
    public TextMeshProUGUI orgasmTimesText;
    [UnityEngine.Tooltip("顯示興奮等級的文字 (格式: Lv.x)")]
    public TextMeshProUGUI excitedLvText;

    [UnityEngine.Tooltip("依興奮等級顯示對應的狀態文字 (從 Text Table 查詢 Key)")]
    public TextMeshProUGUI excitedStateText;

    [UnityEngine.Tooltip("對應各興奮等級的 Text Table Key。索引 0 = 初始(Lv.0)，索引 1 = Lv.1，以此類推。最少 4 組。")]
    public string[] excitedStateKeys = new string[]
    {
        "Excitement.Neutral",
        "Excitement.Shy",
        "Excitement.Excited",
        "Excitement.Overload"
    };
    [Header("UI 元件參考 - 上限狀態")]
    [UnityEngine.Tooltip("當達到興奮度上限或鎖定時顯示的物件 (例如標記為 MAX 的圖片或特效)")]
    public GameObject maxExcitementObject;

    [Header("基礎動畫設定")]
    public float tweenDuration = 0.5f;

    // --- FSM 變數快取 ---
    private FsmInt fsmDiscomfortMax;
    private FsmInt fsmLocalDiscomfort;
    private FsmInt fsmLewdnessLevel;
    private FsmInt fsmLocalExcitedThreshold; // 對應門檻變數
    private FsmInt fsmLocalExcitement;
    private FsmString fsmHeroineID;
    private FsmInt fsmLocalOrgasmTimes;
    private FsmInt fsmLocalExcitedLv;
    private FsmBool fsmIsExcitementMaxLv; // 對應 FSM 中的 IsExcitementMaxLv
    private FsmInt fsmPersonalSuspicion;
    private FsmInt fsmPersonalSuspicionMax;

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

    // --- 內部狀態偵測 ---
    private int lastDiscomfortTarget = -1;
    private int lastExcitementTarget = -1;
    private int lastThresholdTarget = -1; //用於偵測門檻是否變化
    private int lastOrgasmTimesTarget = -1;
    private int lastExcitedLvTarget = -1;
    private int lastExcitedStateLvTarget = -1;
    private bool lastMaxLvState = false; // 用於偵測上限狀態切換
    private int lastSuspicionTarget = -1;
    private int lastSuspicionMaxTarget = -1;
    private bool uiInitialized = false;



    void Start()
    {
        if (eroMinigameFSM == null)
        {
            Debug.LogError("【錯誤】eroMinigameFSM 尚未指定！", this.gameObject);
            this.enabled = false;
            return;
        }

        CacheFSMVariables();

        if (fsmHeroineID != null && heroineIDText != null)
        {
            heroineIDText.text = fsmHeroineID.Value;
        }

        // 初始隱藏 MAX 物件
        if (maxExcitementObject != null) maxExcitementObject.SetActive(false);
    }

    void CacheFSMVariables()
    {
        fsmDiscomfortMax = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_DiscomfortMax");
        fsmLocalDiscomfort = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LocalDiscomfort");

        // 優先讀取新變數，若不存在則嘗試回頭找舊的 fsm_ExcitementMax
        fsmLocalExcitedThreshold = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LocalExcitedThreshold");
        if (fsmLocalExcitedThreshold == null)
            fsmLocalExcitedThreshold = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_ExcitementMax");

        fsmLewdnessLevel = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LewdnessLevel");

        fsmLocalExcitement = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LocalExcitement");
        fsmHeroineID = eroMinigameFSM.FsmVariables.FindFsmString("fsm_HeroineID");
        fsmLocalOrgasmTimes = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LocalOrgasmTimes");
        fsmLocalExcitedLv = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LocalExcitedLv");
        // 快取上限布林值
        fsmIsExcitementMaxLv = eroMinigameFSM.FsmVariables.FindFsmBool("IsExcitementMaxLv");

        // 快取個人可疑度
        fsmPersonalSuspicion = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_PersonalSuspicion");
        fsmPersonalSuspicionMax = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_PersonalSuspicionMax");
    }


    void Update()
    {
        if (!uiInitialized)
        {
            TryInitializeUI();
            if (!uiInitialized) return;
        }
        if (lewdnessLevelText != null && fsmLewdnessLevel != null)
        {
            lastLewdnessLevelTarget = fsmLewdnessLevel.Value;
            lewdnessLevelText.text = "Lv." + lastLewdnessLevelTarget;
        }

        UpdateDiscomfortSlider();
        UpdateExcitementSlider(); // 此處現在包含門檻更新邏輯
        UpdateOrgasmTimesText();
        UpdateExcitedLvText();
        UpdateExcitedStateText();
        UpdateLewdnessLevelText();
        UpdateSuspicionSlider();
    }

    void TryInitializeUI()
    {
        // 確保門檻與最大值都已從 FSM 讀取到有效數值
        if (fsmDiscomfortMax.Value <= 0 || fsmLocalExcitedThreshold.Value <= 0) return;

        if (discomfortSlider != null)
        {
            discomfortSlider.maxValue = fsmDiscomfortMax.Value;
            discomfortSlider.value = fsmLocalDiscomfort.Value;
            lastDiscomfortTarget = fsmLocalDiscomfort.Value;
        }

        if (lewdnessLevelText != null && fsmLewdnessLevel != null)
        {
            lastLewdnessLevelTarget = fsmLewdnessLevel.Value;
            lewdnessLevelText.text = "Lv." + lastLewdnessLevelTarget;
        }

        if (excitementSlider != null)
        {
            lastThresholdTarget = fsmLocalExcitedThreshold.Value;
            excitementSlider.maxValue = lastThresholdTarget;

            // 初始也要判斷是否為 Max
            bool isMax = (fsmIsExcitementMaxLv != null) ? fsmIsExcitementMaxLv.Value : false;
            excitementSlider.value = isMax ? 0 : fsmLocalExcitement.Value;
            lastExcitementTarget = (int)excitementSlider.value;

            if (maxExcitementObject != null) maxExcitementObject.SetActive(isMax);
            lastMaxLvState = isMax;
        }

        if (orgasmTimesText != null)
        {
            lastOrgasmTimesTarget = fsmLocalOrgasmTimes.Value;
            // 使用新邏輯初始化顯示
            UpdateOrgasmTimesDisplay(lastOrgasmTimesTarget);
        }

        if (excitedLvText != null && fsmLocalExcitedLv != null)
        {
            lastExcitedLvTarget = fsmLocalExcitedLv.Value;
            excitedLvText.text = "Lv." + lastExcitedLvTarget;
        }

        if (excitedStateText != null && fsmLocalExcitedLv != null)
        {
            lastExcitedStateLvTarget = fsmLocalExcitedLv.Value;
            excitedStateText.text = GetLocalizedExcitedStateText(lastExcitedStateLvTarget);
        }

        if (suspicionSlider != null && fsmPersonalSuspicion != null && fsmPersonalSuspicionMax != null)
        {
            lastSuspicionMaxTarget = fsmPersonalSuspicionMax.Value;
            suspicionSlider.maxValue = (lastSuspicionMaxTarget > 0) ? lastSuspicionMaxTarget : 1;
            suspicionSlider.value = fsmPersonalSuspicion.Value;
            lastSuspicionTarget = fsmPersonalSuspicion.Value;
        }

        uiInitialized = true;
    }

    void UpdateDiscomfortSlider()
    {
        if (discomfortSlider == null) return;
        int target = fsmLocalDiscomfort.Value;
        if (target != lastDiscomfortTarget)
        {
            lastDiscomfortTarget = target;
            discomfortSlider.DOValue(target, tweenDuration).SetEase(Ease.OutQuad);
        }
    }

    void UpdateExcitementSlider()
    {
        if (excitementSlider == null || fsmLocalExcitedThreshold == null) return;

        // 1. 檢查上限狀態 (IsExcitementMaxLv)
        bool isMax = (fsmIsExcitementMaxLv != null) ? fsmIsExcitementMaxLv.Value : false;

        // 當狀態發生切換時執行
        if (isMax != lastMaxLvState)
        {
            lastMaxLvState = isMax;
            if (maxExcitementObject != null) maxExcitementObject.SetActive(isMax);

            if (isMax)
            {
                // 如果進入 Max 狀態，立即將 Slider 歸零
                excitementSlider.DOValue(0, tweenDuration).SetEase(Ease.OutQuad);
                lastExcitementTarget = 0;
                Debug.Log("[UI] 興奮度已達當前上限，進度條重置為 0");
            }
        }

        // 2. 如果是上限狀態，強制維持在 0，不執行後續邏輯
        if (isMax)
        {
            return;
        }

        // 3. 檢查門檻變更
        int currentThreshold = fsmLocalExcitedThreshold.Value;
        if (currentThreshold != lastThresholdTarget)
        {
            lastThresholdTarget = currentThreshold;
            excitementSlider.maxValue = currentThreshold;
        }

        // 4. 正常經驗值動畫
        int targetExp = fsmLocalExcitement.Value;
        if (targetExp != lastExcitementTarget)
        {
            lastExcitementTarget = targetExp;
            excitementSlider.DOValue(targetExp, tweenDuration).SetEase(Ease.OutQuad);
        }
    }

    void UpdateOrgasmTimesText()
    {
        if (orgasmTimesText == null) return;
        int target = fsmLocalOrgasmTimes.Value;
        if (target != lastOrgasmTimesTarget)
        {
            lastOrgasmTimesTarget = target;
            UpdateOrgasmTimesDisplay(target);
        }
    }

    /// <summary>
    /// 處理絕頂次數的格式化顯示邏輯
    /// </summary>
    void UpdateOrgasmTimesDisplay(int count)
    {
        if (orgasmTimesText == null) return;

        // 1. 抓取本地化文字標籤
        string label = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText("System.Orgasm");

        // 如果 TextTable 找不到 Key，保險起見顯示 Key 名稱
        if (string.IsNullOrEmpty(label)) label = "System.Orgasm";

        // 2. 根據次數判斷顯示格式
        if (count <= 0)
        {
            // 次數為 0 時：只顯示標籤
            orgasmTimesText.text = label;
        }
        else
        {
            // 次數 > 0 時：顯示標籤 + xN
            orgasmTimesText.text = $"{label} x{count}";
        }
    }

    void UpdateExcitedLvText()
    {
        if (excitedLvText == null || fsmLocalExcitedLv == null) return;

        int target = fsmLocalExcitedLv.Value;
        if (target != lastExcitedLvTarget)
        {
            lastExcitedLvTarget = target;
            excitedLvText.text = "Lv." + target;
        }
    }

    void UpdateExcitedStateText()
    {
        if (excitedStateText == null || fsmLocalExcitedLv == null) return;

        int target = fsmLocalExcitedLv.Value;
        if (target != lastExcitedStateLvTarget)
        {
            lastExcitedStateLvTarget = target;
            excitedStateText.text = GetLocalizedExcitedStateText(target);
        }
    }

    /// <summary>
    /// 依興奮等級取得對應的本地化狀態字串。
    /// 若等級超出陣列範圍，則回落使用最後一組 Key。
    /// </summary>
    string GetLocalizedExcitedStateText(int level)
    {
        if (excitedStateKeys == null || excitedStateKeys.Length == 0)
            return string.Empty;

        // 等級夾在 [0, length-1] 範圍內，超出時使用最後一組
        int index = Mathf.Clamp(level, 0, excitedStateKeys.Length - 1);
        string key = excitedStateKeys[index];

        if (string.IsNullOrEmpty(key)) return string.Empty;

        string localized = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);

        // 找不到 Key 時保險顯示 Key 名稱本身（與 UpdateOrgasmTimesDisplay 行為一致）
        if (string.IsNullOrEmpty(localized)) localized = key;

        return localized;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 保證 excitedStateKeys 至少有 4 組（可增不可減）
        const int minSize = 4;
        if (excitedStateKeys == null)
        {
            excitedStateKeys = new string[]
            {
                "Excitement.Neutral",
                "Excitement.Shy",
                "Excitement.Excited",
                "Excitement.Overload"
            };
        }
        else if (excitedStateKeys.Length < minSize)
        {
            string[] defaults = new string[]
            {
                "Excitement.Neutral",
                "Excitement.Shy",
                "Excitement.Excited",
                "Excitement.Overload"
            };

            string[] resized = new string[minSize];
            for (int i = 0; i < minSize; i++)
            {
                if (i < excitedStateKeys.Length && !string.IsNullOrEmpty(excitedStateKeys[i]))
                    resized[i] = excitedStateKeys[i];
                else
                    resized[i] = defaults[i];
            }
            excitedStateKeys = resized;
        }
    }
#endif

    void UpdateLewdnessLevelText()
    {
        if (lewdnessLevelText == null || fsmLewdnessLevel == null) return;

        int target = fsmLewdnessLevel.Value;
        if (target != lastLewdnessLevelTarget)
        {
            lastLewdnessLevelTarget = target;
            lewdnessLevelText.text = "Lv." + target;
        }
    }

    void UpdateSuspicionSlider()
    {
        if (suspicionSlider == null || fsmPersonalSuspicion == null || fsmPersonalSuspicionMax == null) return;

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

        // 1. 檢查上限變更 (PersonalSuspicionMax 可能被運行時調整)
        int currentMax = fsmPersonalSuspicionMax.Value;
        if (currentMax != lastSuspicionMaxTarget)
        {
            lastSuspicionMaxTarget = currentMax;
            suspicionSlider.maxValue = (currentMax > 0) ? currentMax : 1;
        }

        // 2. 可疑度數值動畫
        int target = fsmPersonalSuspicion.Value;
        if (target != lastSuspicionTarget)
        {
            lastSuspicionTarget = target;
            suspicionSlider.DOValue(target, tweenDuration).SetEase(Ease.OutQuad);
        }

        // 3. 根據填充比例變更 Slider 顏色
        ApplySuspicionSliderColor(suspicionSlider);
    }

    /// <summary>
    /// 根據可疑度百分比變更 Slider Fill 顏色。
    /// ≥90% → #FF2121, ≥70% → #FF6464, 否則恢復預設。
    /// </summary>
    void ApplySuspicionSliderColor(Slider slider)
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
}