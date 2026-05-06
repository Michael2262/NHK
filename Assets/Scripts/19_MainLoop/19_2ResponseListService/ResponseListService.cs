using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 公版按鈕列表服務（單例）
/// 放在不卸載的場景上，接收外部呼叫後動態生成 ResponseButton 列表。
/// 
/// 支援三種按鈕類型：
///   Simple  → 點擊即執行 onClicked + 關閉面板
///   Complex → 帶 ProtagonistValueRouter 資源檢查 + StaminaPreviewHoverTrigger
///   Rest    → 點擊即執行 onRestClicked + 關閉面板，Hover 時顯示休息體力預覽
///             （不可用時直接不生成按鈕）
///
/// 支援兩種顯示條件模式：
///   Value → ProgressValue: 0=不生成, ≥1=正常, &lt;0=半透明
///   Flag  → 多個 ProgressFlag, Any/All 邏輯, 滿足=顯示, 不滿足=不生成（無半透明）
///   未配任何條件 → 永遠正常顯示
/// </summary>
public class ResponseListService : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════

    public static ResponseListService Instance { get; private set; }

    // ══════════════════════════════════════════
    //  列舉定義
    // ══════════════════════════════════════════

    public enum ListPosition
    {
        Center,
        Right,
        Left
    }

    public enum ButtonType
    {
        Simple,
        Complex,
        Rest       // 休息按鈕
    }

    public enum ConditionMode
    {
        Value, // ProgressValue: 0=隱藏, ≥1=正常, <0=半透明
        Flag   // ProgressFlag: 滿足=顯示, 不滿足=隱藏（無半透明）
    }

    public enum FlagLogic
    {
        All, // 所有 Flag 都要為 true
        Any  // 任一 Flag 為 true 即可
    }

    // ══════════════════════════════════════════
    //  請求用資料結構（外部傳入）
    // ══════════════════════════════════════════

    [Serializable]
    public class ButtonData
    {
        [Header("按鈕類型")]
        public ButtonType buttonType = ButtonType.Simple;

        [Header("按鈕文字（多語系 Key）")]
        [Tooltip("Dialogue System 的 Localization Field Name")]
        public string localizationKey;

        // ── 條件模式 ──
        [Header("顯示條件")]
        public ConditionMode conditionMode = ConditionMode.Value;

        // ── Value 條件 ──
        [Tooltip("ProgressValue：0=不生成, ≥1=正常, <0=半透明。留空=永遠顯示")]
        public ProgressValueDefinition conditionValue;

        // ── Flag 條件 ──
        [Tooltip("需要檢查的 Flag 列表")]
        public List<ProgressFlagDefinition> conditionFlags = new List<ProgressFlagDefinition>();
        [Tooltip("All = 全部為 true 才顯示；Any = 任一為 true 就顯示")]
        public FlagLogic flagLogic = FlagLogic.All;

        // ── Simple 專用 ──
        [Header("Simple：點擊事件")]
        public UnityEvent onClicked;

        // ── Complex 專用 ──
        [Header("Complex：數值檢查設定")]
        public ProtagonistValueRouter.CheckType checkType = ProtagonistValueRouter.CheckType.CheckStressAtMost;
        public int resourceAmount = 1;
        public bool checkTime = false;
        public int timeAmount = 1;

        [Header("Complex：結果事件")]
        public UnityEvent onSuccess;
        public UnityEvent onFailure;
        public UnityEvent onTimeFailure;

        // ── Rest 專用 ──
        [Header("Rest：休息預覽模式")]
        [Tooltip("選擇對應的休息模式，Hover 時會在體力條上顯示回復預覽")]
        public RestPreviewHoverTrigger.RestMode restPreviewMode = RestPreviewHoverTrigger.RestMode.RestOneSlot;

        [Header("Rest：點擊事件")]
        public UnityEvent onRestClicked;
    }

    // ══════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════

    [Header("Center 面板（Center 專用）")]
    [SerializeField] private GameObject centerPanel;
    [SerializeField] private Transform centerContentParent;
    [SerializeField] private Button centerCloseButton;

    [Header("Side 面板（Left / Right 共用）")]
    [SerializeField] private GameObject sidePanel;
    [SerializeField] private Transform sideContentParent;
    [SerializeField] private Button sideCloseButton;

    [Header("Side 位置設定")]
    [Tooltip("Left/Right 偏移的 X 值（Right = +此值, Left = -此值）")]
    [SerializeField] private float sideOffsetX = 638f;

    [Header("Prefab")]
    [SerializeField] private GameObject simpleButtonPrefab;
    [SerializeField] private GameObject complexButtonPrefab;

    [Header("半透明設定")]
    [SerializeField, Range(0f, 1f)] private float exhaustedAlpha = 0.4f;

    // ══════════════════════════════════════════
    //  內部狀態
    // ══════════════════════════════════════════

    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
    private List<ButtonData> _currentDataList;
    private Action _onCloseCallback;

    // 當前使用的面板組
    private GameObject _activePanel;
    private Transform _activeContentParent;

    private Action<string, int> _onVariableChangedHandler;
    private Action<string, bool> _onFlagChangedHandler;
    private bool _isListening;

    // ══════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("發現重複的 ResponseListService，已銷毀。");
            Destroy(gameObject);
            return;
        }

        // 預設隱藏兩組面板
        if (centerPanel != null) centerPanel.SetActive(false);
        if (sidePanel != null) sidePanel.SetActive(false);

        // 綁定兩個關閉按鈕
        if (centerCloseButton != null) centerCloseButton.onClick.AddListener(Close);
        if (sideCloseButton != null) sideCloseButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        StopListening();
        if (centerCloseButton != null) centerCloseButton.onClick.RemoveAllListeners();
        if (sideCloseButton != null) sideCloseButton.onClick.RemoveAllListeners();
        if (Instance == this) Instance = null;
    }

    // ══════════════════════════════════════════
    //  公開 API
    // ══════════════════════════════════════════

    public void Show(List<ButtonData> dataList, Action onClose = null, ListPosition position = ListPosition.Center)
    {
        if (dataList == null || dataList.Count == 0) return;

        _currentDataList = dataList;
        _onCloseCallback = onClose;

        ClearSpawnedButtons();

        // 選擇面板組
        if (position == ListPosition.Center)
        {
            _activePanel = centerPanel;
            _activeContentParent = centerContentParent;
        }
        else
        {
            _activePanel = sidePanel;
            _activeContentParent = sideContentParent;
            ApplySidePosition(position);
        }

        BuildButtons();

        if (_activePanel != null) _activePanel.SetActive(true);
        StartListening();
    }

    public void Close()
    {
        StopListening();
        ClearSpawnedButtons();

        // 關閉當前活動的面板
        if (_activePanel != null) _activePanel.SetActive(false);
        _activePanel = null;
        _activeContentParent = null;
        _currentDataList = null;

        _onCloseCallback?.Invoke();
        _onCloseCallback = null;
    }

    public bool IsOpen =>
        (centerPanel != null && centerPanel.activeSelf) ||
        (sidePanel != null && sidePanel.activeSelf);

    // ══════════════════════════════════════════
    //  按鈕生成
    // ══════════════════════════════════════════

    private void BuildButtons()
    {
        if (_activeContentParent == null || GameStatusService.Instance == null) return;

        var progressFlags = GameStatusService.Instance.ProgressFlags;

        foreach (var data in _currentDataList)
        {
            // ── 條件判定（Flag / Value） ──
            bool shouldShow;
            bool isExhausted = false;

            if (data.conditionMode == ConditionMode.Flag)
            {
                shouldShow = EvaluateFlagCondition(data, progressFlags);
            }
            else
            {
                int value = GetConditionValue(data, progressFlags);
                shouldShow = value != 0;
                isExhausted = value < 0;
            }

            if (!shouldShow) continue;

            // ── Rest 類型額外檢查：時間不足則不生成 ──
            if (data.buttonType == ButtonType.Rest)
            {
                var restCtrl = RestButtonController.Instance;
                if (restCtrl == null || !restCtrl.IsRestAvailable(data.restPreviewMode))
                    continue;
            }

            // ── 選擇 Prefab（Rest 共用 simpleButtonPrefab） ──
            GameObject prefab;
            if (data.buttonType == ButtonType.Complex)
                prefab = complexButtonPrefab;
            else
                prefab = simpleButtonPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"[ResponseListService] {data.buttonType} Prefab 未指定，跳過: {data.localizationKey}");
                continue;
            }

            // ── 生成 ──
            var btnObj = Instantiate(prefab, _activeContentParent);
            btnObj.SetActive(true);
            _spawnedButtons.Add(btnObj);

            SetupLocalization(btnObj, data.localizationKey);

            // ── 依類型接線 ──
            switch (data.buttonType)
            {
                case ButtonType.Simple:
                    SetupSimpleButton(btnObj, data);
                    break;
                case ButtonType.Complex:
                    SetupComplexButton(btnObj, data);
                    break;
                case ButtonType.Rest:
                    SetupRestButton(btnObj, data);
                    break;
            }

            ApplyVisualState(btnObj, isExhausted);
        }
    }

    // ══════════════════════════════════════════
    //  條件判定
    // ══════════════════════════════════════════

    private bool EvaluateFlagCondition(ButtonData data, ProgressFlagModel progressFlags)
    {
        if (data.conditionFlags == null || data.conditionFlags.Count == 0)
            return true;

        var validFlags = data.conditionFlags.Where(f => f != null).ToList();
        if (validFlags.Count == 0) return true;

        if (data.flagLogic == FlagLogic.All)
            return validFlags.All(f => progressFlags.Contains(f.name));
        else
            return validFlags.Any(f => progressFlags.Contains(f.name));
    }

    private int GetConditionValue(ButtonData data, ProgressFlagModel progressFlags)
    {
        if (data.conditionValue != null)
            return progressFlags.GetValue(data.conditionValue.name);
        return 1;
    }

    // ══════════════════════════════════════════
    //  Simple 按鈕接線
    // ══════════════════════════════════════════

    private void SetupSimpleButton(GameObject btnObj, ButtonData data)
    {
        var btn = btnObj.GetComponent<Button>();
        if (btn == null) return;

        var capturedData = data;
        btn.onClick.AddListener(() =>
        {
            capturedData.onClicked?.Invoke();
            Close();
        });
    }

    // ══════════════════════════════════════════
    //  Complex 按鈕接線
    // ══════════════════════════════════════════

    private void SetupComplexButton(GameObject btnObj, ButtonData data)
    {
        var router = btnObj.GetComponent<ProtagonistValueRouter>();
        if (router != null)
        {
            router.checkType = data.checkType;
            router.amount = data.resourceAmount;
            router.checkTime = data.checkTime;
            router.timeAmount = data.timeAmount;

            router.onSuccess = new UnityEvent();
            router.onFailure = new UnityEvent();
            router.onTimeFailure = new UnityEvent();

            var capturedData = data;
            router.onSuccess.AddListener(() =>
            {
                capturedData.onSuccess?.Invoke();
                Close();
            });
            router.onFailure.AddListener(() => capturedData.onFailure?.Invoke());
            router.onTimeFailure.AddListener(() => capturedData.onTimeFailure?.Invoke());
        }
        else
        {
            Debug.LogWarning($"[ResponseListService] Complex Prefab 缺少 ProtagonistValueRouter: {data.localizationKey}");
        }

        var btn = btnObj.GetComponent<Button>();
        if (btn != null && router != null)
            btn.onClick.AddListener(() => router.Trigger());
    }

    // ══════════════════════════════════════════
    //  Rest 按鈕接線
    // ══════════════════════════════════════════

    private void SetupRestButton(GameObject btnObj, ButtonData data)
    {
        // 1. 點擊事件
        var btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            var capturedData = data;
            btn.onClick.AddListener(() =>
            {
                capturedData.onRestClicked?.Invoke();
                Close();
            });
        }

        // 2. 附加休息預覽觸發器
        var trigger = btnObj.AddComponent<RestPreviewHoverTrigger>();
        trigger.SetRestMode(data.restPreviewMode);
    }

    // ══════════════════════════════════════════
    //  刷新邏輯
    // ══════════════════════════════════════════

    private void RefreshList()
    {
        if (_currentDataList == null || !IsOpen) return;
        ClearSpawnedButtons();
        BuildButtons();
    }

    // ══════════════════════════════════════════
    //  事件監聽
    // ══════════════════════════════════════════

    private void StartListening()
    {
        if (_isListening) return;
        if (GameStatusService.Instance == null || GameStatusService.Instance.ProgressFlags == null) return;

        var flags = GameStatusService.Instance.ProgressFlags;

        _onVariableChangedHandler = (key, value) => RefreshList();
        _onFlagChangedHandler = (flag, state) => RefreshList();

        flags.OnVariableChanged += _onVariableChangedHandler;
        flags.OnFlagChanged += _onFlagChangedHandler;
        _isListening = true;
    }

    private void StopListening()
    {
        if (!_isListening) return;
        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            var flags = GameStatusService.Instance.ProgressFlags;
            if (_onVariableChangedHandler != null) flags.OnVariableChanged -= _onVariableChangedHandler;
            if (_onFlagChangedHandler != null) flags.OnFlagChanged -= _onFlagChangedHandler;
        }
        _onVariableChangedHandler = null;
        _onFlagChangedHandler = null;
        _isListening = false;
    }

    // ══════════════════════════════════════════
    //  工具方法
    // ══════════════════════════════════════════

    private void ApplySidePosition(ListPosition position)
    {
        if (sidePanel == null) return;
        var rt = sidePanel.GetComponent<RectTransform>();
        if (rt == null) return;

        var pos = rt.anchoredPosition;
        pos.x = position == ListPosition.Left ? -sideOffsetX : sideOffsetX;
        rt.anchoredPosition = pos;
    }

    private void SetupLocalization(GameObject btnObj, string localizationKey)
    {
        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;

        string localizedText = DialogueManager.GetLocalizedText(localizationKey);
        if (string.IsNullOrEmpty(localizedText))
        {
            Debug.LogWarning($"[ResponseListService] Text Table 找不到 Key: {localizationKey}");
            localizedText = localizationKey;
        }
        tmp.text = localizedText;
    }

    private void ApplyVisualState(GameObject btnObj, bool isExhausted)
    {
        var cg = btnObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnObj.AddComponent<CanvasGroup>();

        cg.alpha = isExhausted ? exhaustedAlpha : 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void ClearSpawnedButtons()
    {
        foreach (var obj in _spawnedButtons)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedButtons.Clear();
    }
}