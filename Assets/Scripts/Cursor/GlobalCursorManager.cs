using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-800)]
public class GlobalCursorManager : MonoBehaviour
{
    public static GlobalCursorManager Instance { get; private set; }

    [Header("預設鼠標 (Default)")]
    public Texture2D defaultNormalTexture;
    public Vector2 defaultNormalHotspot = Vector2.zero;
    public Texture2D defaultClickTexture;
    public Vector2 defaultClickHotspot = Vector2.zero;

    [Header("Input Action 參照")]
    public InputActionAsset inputActions;
    private InputAction clickAction;

    private Texture2D currentNormalTexture;
    private Vector2 currentNormalHotspot;
    private Texture2D currentClickTexture;
    private Vector2 currentClickHotspot;

    // --- 最高優先級 (Override) 狀態 ---
    private bool isCursorLocked = false;
    private Texture2D overrideNormalTexture;
    private Vector2 overrideNormalHotspot;
    private Texture2D overrideClickTexture;
    private Vector2 overrideClickHotspot;

    // --- 2D Collider 點擊控制 ---
    private Physics2DRaycaster currentPhysicsRaycaster;
    private bool areColliderClicksDisabled = false;

    // --- 新增 UI 控制變數 ---
    private EventSystem currentEventSystem;
    private bool areUIClicksDisabled = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Cursor.visible = true;
        clickAction = inputActions.FindActionMap("PlayerControls").FindAction("Click");
    }

    void Start()
    {
        SetCursorArea(defaultNormalTexture, defaultNormalHotspot, defaultClickTexture, defaultClickHotspot);
    }

    private void OnEnable()
    {
        if (clickAction != null)
        {
            clickAction.Enable();
            clickAction.started += OnClickStarted;
            clickAction.canceled += OnClickCanceled;
        }
    }

    private void OnDisable()
    {
        if (clickAction != null)
        {
            clickAction.Disable();
            clickAction.started -= OnClickStarted;
            clickAction.canceled -= OnClickCanceled;
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (isCursorLocked)
        {
            SetCursor(overrideClickTexture, overrideClickHotspot);
            return;
        }
        SetCursor(currentClickTexture, currentClickHotspot);
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (isCursorLocked)
        {
            SetCursor(overrideNormalTexture, overrideNormalHotspot);
            return;
        }
        SetCursor(currentNormalTexture, currentNormalHotspot);
    }

    private void SetCursor(Texture2D texture, Vector2 hotspot)
    {
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    public void SetCursorArea(Texture2D normal, Vector2 normalHotspot, Texture2D click, Vector2 clickHotspot)
    {
        currentNormalTexture = normal;
        currentNormalHotspot = normalHotspot;
        currentClickTexture = click;
        currentClickHotspot = clickHotspot;

        if (isCursorLocked) return;

        if (!clickAction.IsPressed())
        {
            SetCursor(currentNormalTexture, currentNormalHotspot);
        }
    }

    public void ResetToDefaultCursor()
    {
        SetCursorArea(defaultNormalTexture, defaultNormalHotspot, defaultClickTexture, defaultClickHotspot);
    }

    public void SetDefaultCursors(Texture2D normal, Vector2 normalHotspot, Texture2D click, Vector2 clickHotspot)
    {
        defaultNormalTexture = normal;
        defaultNormalHotspot = normalHotspot;
        defaultClickTexture = click;
        defaultClickHotspot = clickHotspot;
        ResetToDefaultCursor();
    }

    public void SetOverrideCursor(Texture2D normal, Vector2 normalHotspot, Texture2D click, Vector2 clickHotspot)
    {
        isCursorLocked = true;
        overrideNormalTexture = normal;
        overrideNormalHotspot = normalHotspot;
        overrideClickTexture = click;
        overrideClickHotspot = clickHotspot;

        if (clickAction.IsPressed())
            SetCursor(overrideClickTexture, overrideClickHotspot);
        else
            SetCursor(overrideNormalTexture, overrideNormalHotspot);
    }

    public void SetOverrideCursor(Texture2D texture, Vector2 hotspot)
    {
        SetOverrideCursor(texture, hotspot, texture, hotspot);
    }

    public void ReleaseOverrideCursor()
    {
        if (!isCursorLocked) return;

        isCursorLocked = false;
        overrideNormalTexture = null;
        overrideClickTexture = null;

        if (clickAction.IsPressed())
            SetCursor(currentClickTexture, currentClickHotspot);
        else
            SetCursor(currentNormalTexture, currentNormalHotspot);
    }

    // --- 註冊 Physics Raycaster (舊有) ---
    public void RegisterPhysicsRaycaster(Physics2DRaycaster raycaster)
    {
        currentPhysicsRaycaster = raycaster;
        if (currentPhysicsRaycaster != null)
        {
            areColliderClicksDisabled = !currentPhysicsRaycaster.enabled;
        }
        else
        {
            Debug.LogError("GlobalCursorManager: 註冊的 Physics2DRaycaster 為 null！");
            areColliderClicksDisabled = false;
        }
    }

    // --- 新增：註冊 UI EventSystem ---
    public void RegisterEventSystem(EventSystem eventSystem)
    {
        currentEventSystem = eventSystem;
        if (currentEventSystem != null)
        {
            // 同步狀態：以 EventSystem 當前的啟用狀態為準
            areUIClicksDisabled = !currentEventSystem.enabled;
        }
        else
        {
            // 雖然少見，但有些場景可能真的沒有 UI
            // Debug.LogWarning("GlobalCursorManager: 註冊的 EventSystem 為 null！");
            areUIClicksDisabled = false;
        }
    }

    // --- 2D Collider 控制 (舊有) ---
    public void DisableColliderClicks()
    {
        if (currentPhysicsRaycaster != null && !areColliderClicksDisabled)
        {
            currentPhysicsRaycaster.enabled = false;
            areColliderClicksDisabled = true;
            ResetToDefaultCursor(); // 避免鼠標卡在"可點擊"圖示
        }
    }

    public void EnableColliderClicks()
    {
        if (currentPhysicsRaycaster != null && areColliderClicksDisabled)
        {
            currentPhysicsRaycaster.enabled = true;
            areColliderClicksDisabled = false;
        }
    }

    // --- 新增：UI 點擊控制 ---

    /// <summary>
    /// 暫時禁用所有 UI 點擊事件 (按鈕、Hover 等都會失效)。
    /// </summary>
    public void DisableUIClicks()
    {
        if (currentEventSystem != null && !areUIClicksDisabled)
        {
            // 改為停用 InputModule，而非整個 EventSystem
            var inputModule = currentEventSystem.currentInputModule;
            if (inputModule != null)
                inputModule.enabled = false;

            areUIClicksDisabled = true;
            ResetToDefaultCursor();

            Debug.Log($"[GCM] DisableUIClicks. Frame={Time.frameCount}");
        }
    }
    /// <summary>
    /// 重新啟用 UI 點擊事件。
    /// </summary>
    public void EnableUIClicks()
    {
        if (currentEventSystem != null && areUIClicksDisabled)
        {
            var inputModule = currentEventSystem.currentInputModule;
            if (inputModule != null)
                inputModule.enabled = true;

            areUIClicksDisabled = false;

            Debug.Log($"[GCM] EnableUIClicks. Frame={Time.frameCount}, mousePressed={Mouse.current?.leftButton?.isPressed}");
        }
    }

}