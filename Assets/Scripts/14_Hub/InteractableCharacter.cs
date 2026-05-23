using UnityEngine;
using UnityEngine.EventSystems; // 這是偵測「是否點在 UI 上」所必需的
using System.Collections.Generic; // 為了使用 List
using UnityEngine.InputSystem; // ★ 1. 導入新的輸入系統

/// <summary>
/// 【[Serializable]】
/// 職責：定義一個「條件按鈕」的顯示規則。
/// 這會顯示在 InteractableCharacter 的 Inspector 列表中。
/// </summary>
[System.Serializable]
public class ConditionalButton
{
    [Tooltip("要顯示/隱藏的按鈕 GameObject")]
    public GameObject buttonObject;

    [Tooltip("是否預設顯示？" +
             "True = 永遠顯示。" +
             "False = 只有在滿足 Flag 條件時才顯示。")]
    public bool isDefault = true;

    [Tooltip("【請求 2.2】(當 isDefault=false 時) 需要檢查的 ProgressFlag SO。" +
             "會去檢查 ProgressFlagModel 中是否存在此 Flag，請拖曳對應的 Flag SO檔案到這裡。")]
    public ProgressFlagDefinition requiredFlag; // (你說的 FlagName)
}


/// <summary>
/// 【Prefab 腳本】
/// (★ 已修改：現在包含一個「條件按鈕」列表)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class InteractableCharacter : ConditionalTouchReactionBase
{
    [Header("角色顯示")]
    [Tooltip("角色實際顯示用的 SpriteRenderer（子物件上的那張圖）")]
    public SpriteRenderer characterSprite;

    [Header("UI 連結")]
    [Tooltip("子層級的 World Space Canvas 物件 (所有按鈕的總面板)")]
    public GameObject interactionUIPanel;

    [Header("按鈕顯示條件")]
    [Tooltip("設定此面板中的所有按鈕及其顯示條件")]
    public List<ConditionalButton> conditionalButtons;

    // --- 內部狀態 ---
    private Collider2D _myCollider;
    private Camera _mainCamera;

    private GameStatusService _service;
    private ProgressFlagModel _flags;

    void Start()
    {


        _myCollider = GetComponent<Collider2D>();
        _mainCamera = Camera.main;
        _service = GameStatusService.Instance;

        if (_service != null)
        {
            _flags = _service.ProgressFlags;
        }
        else
        {
            Debug.LogError("InteractableCharacter 找不到 GameStatusService！");
            this.enabled = false;
            return;
        }

        if (interactionUIPanel != null)
            interactionUIPanel.SetActive(false);
        else
            Debug.LogError("interactionUIPanel 沒有在 Inspector 中設定！", this);

        swipeConds = new SwipeDir[0]; // [cite: ConditionalTouchReactionBase.cs]
    }

    /// <summary>
    /// 當「點擊」手勢 (Click) 成功匹配時，由基底類別呼叫。
    /// (★ 邏輯不變)
    /// </summary>
    public override void OnTouched()
    {
        if (interactionUIPanel == null) return;

        bool shouldOpen = !interactionUIPanel.activeSelf;

        if (shouldOpen)
        {
            UpdateButtonConditions();
            interactionUIPanel.SetActive(true);
        }
        else
        {
            interactionUIPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 遍歷所有條件按鈕，根據規則決定顯示或隱藏
    /// (★ 邏輯不變)
    /// </summary>
    private void UpdateButtonConditions()
    {
        if (_flags == null)
        {
            Debug.LogError("ProgressFlagModel 為 null！無法檢查按鈕條件。");
            return;
        }

        foreach (var button in conditionalButtons)
        {
            if (button.buttonObject == null) continue;

            bool show = false;

            if (button.isDefault)
            {
                show = true;
            }
            else
            {
                if (button.requiredFlag == null)
                {

                    show = false;
                }
                else
                {
                    show = _flags.Contains(button.requiredFlag.FlagID);
                }
            }

            button.buttonObject.SetActive(show);
        }
    }

    /// <summary>
    /// 每一幀檢查是否點擊了「外部」
    /// (使用新的 Input System API)
    /// </summary>
    void Update()
    {
        if (interactionUIPanel == null || !interactionUIPanel.activeSelf)
            return;

        // 安全檢查：確保滑鼠存在
        if (Mouse.current == null)
            return;

        // 使用 Mouse.current.leftButton.wasPressedThisFrame
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())

            {
                // 點在 UI 上，不關閉
                return;
            }

            // 修正：使用 Mouse.current.position.ReadValue()
            Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider == _myCollider)

            {
                // 點在自己身上，OnTouched 會處理，這裡不關閉
                return;
            }

            // 點在外部，關閉面板
            interactionUIPanel.SetActive(false);
        }
    }

    //API for external call
    /// <summary>
    /// 主動關閉互動 UI 面板
    /// </summary>
    public void CloseInteractionUI()
    {
        if (interactionUIPanel != null)
        {
            interactionUIPanel.SetActive(false);
        }
    }
}