using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 【[Serializable]】
/// 負責：定義一個「條件物體」的顯示規則。
/// 這會顯示在 InteractableUIButton 的 Inspector 列表中。
/// </summary>
[System.Serializable]
public class ConditionalObject
{
    [Tooltip("要顯示/隱藏的 GameObject")]
    public GameObject targetObject;

    [Tooltip("是否預設顯示？" +
             "True = 永遠顯示。" +
             "False = 只有在滿足 Flag 條件時才顯示。")]
    public bool isDefault = true;

    [Tooltip("【需求 3】(當 isDefault=false 時) 需要檢查的 ProgressFlag SO。" +
             "會去檢查 ProgressFlagModel 中是否存在此 Flag，請拖曳對應的 Flag SO檔案到這裡。")]
    public ProgressFlagDefinition requiredFlag;
}

/// <summary>
/// 【UI 版本】
/// 可互動的 UI 按鈕，點擊後顯示指定物體
/// - 點擊顯示物體
/// - 「點擊外部區域」或「再次點擊按鈕」關閉物體
/// - 可設定按鈕在物體顯示時是否暫時消失
/// - 物體可設定 Flag 條件決定是否顯現
/// </summary>
public class InteractableUIButton : MonoBehaviour, IPointerClickHandler
{
    [Header("顯示物體設定")]
    [Tooltip("設定點擊後要顯示的物體及其條件")]
    public List<ConditionalObject> conditionalObjects;

    [Header("互動區域設定")]
    [Tooltip("互動區域的 RectTransform (包含按鈕和顯示物體的範圍)。\n" +
             "如果不設定，會使用此按鈕的 RectTransform。\n" +
             "建議設定為包含所有顯示物體的父物件。")]
    public RectTransform interactionArea;

    [Header("按鈕行為設定")]
    [Tooltip("物體顯示時，是否隱藏此按鈕？(使用 CanvasGroup 控制)")]
    public bool hideButtonWhenActive = false;

    [Tooltip("關閉方式：點擊互動區域外時關閉")]
    public bool closeOnClickOutside = true;

    [Tooltip("關閉方式：再次點擊按鈕時關閉")]
    public bool closeOnClickAgain = true;

    // --- 內部狀態 ---
    private CanvasGroup _buttonCanvasGroup;
    private RectTransform _myRectTransform;
    private Canvas _parentCanvas;
    private Camera _canvasCamera;
    private bool _isObjectsActive = false;

    // 你的 Flag 系統服務，請根據實際情況調整
    private GameStatusService _service;
    private ProgressFlagModel _flags;

    void Start()
    {
        // 取得自身的 RectTransform
        _myRectTransform = GetComponent<RectTransform>();

        // 如果沒有設定互動區域，使用自身
        if (interactionArea == null)
        {
            interactionArea = _myRectTransform;
        }

        // 取得 Canvas 和 Camera (用於座標轉換)
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
        {
            // World Space 或 Camera Space Canvas 需要相機
            if (_parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _canvasCamera = _parentCanvas.worldCamera;
                if (_canvasCamera == null)
                {
                    _canvasCamera = Camera.main;
                }
            }
        }

        // 取得或新增 CanvasGroup (用於控制按鈕顯示/隱藏)
        _buttonCanvasGroup = GetComponent<CanvasGroup>();
        if (_buttonCanvasGroup == null && hideButtonWhenActive)
        {
            _buttonCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 取得 Flag 系統
        _service = GameStatusService.Instance;
        if (_service != null)
        {
            _flags = _service.ProgressFlags;
        }
        else
        {
            Debug.LogWarning("InteractableUIButton 找不到 GameStatusService，Flag 功能將無法使用。", this);
        }

        // 初始化：隱藏所有物體
        SetAllObjectsActive(false);
    }

    /// <summary>
    /// 當點擊按鈕時觸發 (IPointerClickHandler)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isObjectsActive && closeOnClickAgain)
        {
            // 已開啟狀態，再次點擊則關閉
            CloseObjects();
        }
        else if (!_isObjectsActive)
        {
            // 關閉狀態，點擊則開啟
            OpenObjects();
        }
    }

    /// <summary>
    /// 每一幀檢查是否點擊了「外部」(參考你的 InteractableCharacter)
    /// </summary>
    void Update()
    {
        if (!_isObjectsActive || !closeOnClickOutside)
            return;

        // 安全檢查：確保滑鼠存在
        if (Mouse.current == null)
            return;

        // 使用 Mouse.current.leftButton.wasPressedThisFrame
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 檢查是否點擊在互動區域內
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            if (IsPointerInsideInteractionArea(mousePosition))
            {
                // 點擊在互動區域內，不關閉 (讓 OnPointerClick 或其他 UI 處理)
                return;
            }

            // 點擊在外部，關閉物體
            CloseObjects();
        }
    }

    /// <summary>
    /// 檢查滑鼠位置是否在互動區域內
    /// </summary>
    private bool IsPointerInsideInteractionArea(Vector2 screenPoint)
    {
        if (interactionArea == null) return false;

        // 使用 RectTransformUtility 來檢查點擊位置
        return RectTransformUtility.RectangleContainsScreenPoint(
            interactionArea,
            screenPoint,
            _canvasCamera
        );
    }

    /// <summary>
    /// 開啟物體顯示
    /// </summary>
    private void OpenObjects()
    {
        UpdateObjectConditions();
        _isObjectsActive = true;

        // 隱藏按鈕 (如果設定了的話)
        if (hideButtonWhenActive && _buttonCanvasGroup != null)
        {
            SetButtonVisible(false);
        }
    }

    /// <summary>
    /// 關閉物體顯示
    /// </summary>
    public void CloseObjects()
    {
        SetAllObjectsActive(false);
        _isObjectsActive = false;

        // 顯示按鈕
        if (hideButtonWhenActive && _buttonCanvasGroup != null)
        {
            SetButtonVisible(true);
        }
    }

    /// <summary>
    /// 遍歷所有條件物體，根據規則決定顯示或隱藏
    /// </summary>
    private void UpdateObjectConditions()
    {
        foreach (var obj in conditionalObjects)
        {
            if (obj.targetObject == null) continue;

            bool show = false;

            if (obj.isDefault)
            {
                // 預設顯示
                show = true;
            }
            else
            {
                // 需要檢查 Flag 條件
                if (obj.requiredFlag == null)
                {
                    show = false;
                }
                else if (_flags != null)
                {
                    show = _flags.Contains(obj.requiredFlag.FlagID);
                }
                else
                {
                    // Flag 系統不可用，預設不顯示
                    show = false;
                }
            }

            obj.targetObject.SetActive(show);
        }
    }

    /// <summary>
    /// 設定所有物體的顯示狀態
    /// </summary>
    private void SetAllObjectsActive(bool active)
    {
        foreach (var obj in conditionalObjects)
        {
            if (obj.targetObject != null)
            {
                obj.targetObject.SetActive(active);
            }
        }
    }

    /// <summary>
    /// 使用 CanvasGroup 控制按鈕的可見性
    /// </summary>
    private void SetButtonVisible(bool visible)
    {
        if (_buttonCanvasGroup == null) return;

        _buttonCanvasGroup.alpha = visible ? 1f : 0f;
        _buttonCanvasGroup.interactable = visible;
        _buttonCanvasGroup.blocksRaycasts = visible;
    }

    // ===== API for external call =====

    /// <summary>
    /// 主動關閉物體 (供外部呼叫)
    /// </summary>
    public void ForceCloseObjects()
    {
        CloseObjects();
    }

    /// <summary>
    /// 主動開啟物體 (供外部呼叫)
    /// </summary>
    public void ForceOpenObjects()
    {
        if (!_isObjectsActive)
        {
            OpenObjects();
        }
    }

    /// <summary>
    /// 取得目前物體是否為開啟狀態
    /// </summary>
    public bool IsObjectsActive => _isObjectsActive;
}