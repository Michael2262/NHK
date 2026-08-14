using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 全域鼠標管理者（軟體游標版）。
/// 內部不再使用系統硬體游標 <see cref="Cursor.SetCursor"/>，改為在一個常駐的
/// Screen Space - Overlay Canvas 上驅動一張跟隨滑鼠的 UI Image，
/// 這樣游標才能做縮放等補間動畫。
///
/// 對外 API（SetCursorArea / SetOverrideCursor / ResetToDefaultCursor …）維持不變，
/// 場景中 SceneCursorSettings、CursorArea 的既有設定不需更動。
/// </summary>
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

    [Header("點擊縮放 (全域)")]
    [Tooltip("按下滑鼠時，游標縮放到「當前靜止倍率 × 此值」。1 = 不縮小。")]
    public float clickScaleFactor = 0.85f;
    [Tooltip("點擊縮放的補間時間（秒）")]
    public float clickScaleDuration = 0.08f;
    public Ease clickScaleEase = Ease.OutQuad;

    [Header("Hover 縮放的補間設定 (倍率由各 CursorArea 決定)")]
    public Ease hoverScaleEase = Ease.OutBack;

    [Header("離開遊戲畫面時的行為")]
    [Tooltip("勾選後，滑鼠離開遊戲畫面(失焦或超出畫面範圍)會叫回系統游標並藏起軟體游標，方便在 Editor 操作。")]
    public bool showSystemCursorWhenOutside = true;
    private bool systemCursorShown = false;

    // --- 軟體游標載體 (執行期自動建立，不需在場景拉) ---
    private Canvas cursorCanvas;
    private Image cursorImage;
    private RectTransform cursorRect;
    private readonly Dictionary<Texture2D, Sprite> spriteCache = new Dictionary<Texture2D, Sprite>();

    // --- 縮放狀態 ---
    private float restingScale = 1f;   // 不點擊時游標應維持的倍率（hover 進區域會提高）
    private bool isClickHeld = false;
    private Tween scaleTween;

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

        BuildSoftwareCursor();

        // 關掉系統硬體游標，改由軟體游標接手
        Cursor.visible = false;

        // 斷開 PlayMaker 對游標的控制（否則它每幀把系統游標打開，造成閃爍）
        DisablePlayMakerCursorControl();

        clickAction = inputActions.FindActionMap("PlayerControls").FindAction("Click");
    }

    /// <summary>
    /// 找出場景中所有 PlayMakerGUI，關閉其「Control Mouse Cursor」，
    /// 避免它每幀把 Cursor.visible 打開跟軟體游標打架。
    /// 每次場景載入都要重跑一次（PlayMakerGUI 是逐場景存在的）。
    /// </summary>
    private void DisablePlayMakerCursorControl()
    {
        var guis = FindObjectsOfType<PlayMakerGUI>(true);
        foreach (var gui in guis)
        {
            if (gui != null) gui.controlMouseCursor = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisablePlayMakerCursorControl();
        Cursor.visible = false;
    }

    /// <summary>執行期建立常駐的 Overlay Canvas 與跟隨用的 Image。</summary>
    private void BuildSoftwareCursor()
    {
        var canvasGO = new GameObject("CursorCanvas");
        canvasGO.transform.SetParent(transform, false);

        cursorCanvas = canvasGO.AddComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.sortingOrder = short.MaxValue; // 永遠蓋在最上層
        canvasGO.AddComponent<GraphicRaycaster>().enabled = false; // 不需要，關掉以免干擾

        var imageGO = new GameObject("CursorImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        cursorImage = imageGO.AddComponent<Image>();
        cursorImage.raycastTarget = false; // 關鍵：不可擋住底下的點擊
        cursorImage.enabled = false;

        cursorRect = cursorImage.rectTransform;
        // 錨點固定在左下角，anchoredPosition 便可直接對應滑鼠螢幕座標
        cursorRect.anchorMin = Vector2.zero;
        cursorRect.anchorMax = Vector2.zero;
        cursorRect.localScale = Vector3.one;
    }

    void Start()
    {
        SetCursorArea(defaultNormalTexture, defaultNormalHotspot, defaultClickTexture, defaultClickHotspot);
    }

    void Update()
    {
        // 滑鼠離開遊戲畫面時，叫回系統游標、藏起軟體游標（方便在 Editor 操作）
        if (showSystemCursorWhenOutside && IsPointerOutsideGameView())
        {
            if (!systemCursorShown)
            {
                systemCursorShown = true;
                Cursor.visible = true;
                if (cursorImage != null) cursorImage.enabled = false;
            }
            return; // 停止跟隨與壓游標
        }

        // 剛回到畫面內：切回軟體游標
        if (systemCursorShown)
        {
            systemCursorShown = false;
            ApplyCurrentCursor(); // 依當前狀態重新套用圖案並重新啟用 Image
        }

        // 壓回系統游標：對話系統等第三方會偷偷把它打開，這裡每幀確保維持隱藏
        if (Cursor.visible) Cursor.visible = false;

        // 每幀讓游標 Image 跟隨滑鼠位置（以 hotspot 為對位點，見 SetCursor）
        if (cursorRect == null || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float scaleFactor = cursorCanvas != null ? cursorCanvas.scaleFactor : 1f;
        if (scaleFactor <= 0f) scaleFactor = 1f;
        cursorRect.anchoredPosition = mousePos / scaleFactor;
    }

    /// <summary>
    /// 滑鼠是否離開了遊戲畫面：視窗失焦，或座標超出畫面範圍。
    /// </summary>
    private bool IsPointerOutsideGameView()
    {
        if (!Application.isFocused) return true;
        if (Mouse.current == null) return false;

        Vector2 p = Mouse.current.position.ReadValue();
        return p.x < 0f || p.y < 0f || p.x >= Screen.width || p.y >= Screen.height;
    }

    /// <summary>依當前狀態(Override / Click held)重新把正確的游標圖套到 Image。</summary>
    private void ApplyCurrentCursor()
    {
        if (isCursorLocked)
            SetCursor(isClickHeld ? overrideClickTexture : overrideNormalTexture,
                      isClickHeld ? overrideClickHotspot : overrideNormalHotspot);
        else
            SetCursor(isClickHeld ? currentClickTexture : currentNormalTexture,
                      isClickHeld ? currentClickHotspot : currentNormalHotspot);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (clickAction != null)
        {
            clickAction.Enable();
            clickAction.started += OnClickStarted;
            clickAction.canceled += OnClickCanceled;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (clickAction != null)
        {
            clickAction.Disable();
            clickAction.started -= OnClickStarted;
            clickAction.canceled -= OnClickCanceled;
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        isClickHeld = true;

        if (isCursorLocked)
            SetCursor(overrideClickTexture, overrideClickHotspot);
        else
            SetCursor(currentClickTexture, currentClickHotspot);

        // 全域點擊縮放：相對「當前靜止倍率」再縮一下
        AnimateScale(restingScale * clickScaleFactor, clickScaleDuration, clickScaleEase);
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        isClickHeld = false;

        if (isCursorLocked)
            SetCursor(overrideNormalTexture, overrideNormalHotspot);
        else
            SetCursor(currentNormalTexture, currentNormalHotspot);

        // 放開後回到當前靜止倍率（可能是 1 或 hover 放大值）
        AnimateScale(restingScale, clickScaleDuration, clickScaleEase);
    }

    /// <summary>
    /// 實際把游標圖案套到 UI Image 上（取代舊的 Cursor.SetCursor）。
    /// hotspot 以 RectTransform 的 pivot 實現：
    /// 既讓尖端對準滑鼠點，也讓縮放以 hotspot 為中心，尖端不位移。
    /// </summary>
    private void SetCursor(Texture2D texture, Vector2 hotspot)
    {
        if (cursorImage == null) return;

        if (texture == null)
        {
            cursorImage.enabled = false;
            return;
        }

        cursorImage.enabled = true;
        cursorImage.sprite = GetSprite(texture);
        cursorRect.sizeDelta = new Vector2(texture.width, texture.height);

        // Cursor hotspot 以左上角為原點；RectTransform pivot 以左下角為原點且正規化
        cursorRect.pivot = new Vector2(
            texture.width > 0 ? hotspot.x / texture.width : 0f,
            texture.height > 0 ? 1f - hotspot.y / texture.height : 0f
        );
    }

    private Sprite GetSprite(Texture2D texture)
    {
        if (texture == null) return null;
        if (spriteCache.TryGetValue(texture, out var cached)) return cached;

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        spriteCache[texture] = sprite;
        return sprite;
    }

    // --- 縮放補間共用 ---

    private void AnimateScale(float target, float duration, Ease ease)
    {
        if (cursorRect == null) return;
        scaleTween?.Kill();
        scaleTween = cursorRect.DOScale(target, duration).SetEase(ease).SetUpdate(true);
    }

    /// <summary>
    /// (供 CursorArea 呼叫) 進入 hover 區域：提高靜止倍率並放大維持。
    /// </summary>
    public void ApplyHoverScale(float scale, float duration)
    {
        restingScale = scale;
        // 若正按著，維持縮小視覺，放開時 OnClickCanceled 會回到新的 restingScale
        if (!isClickHeld)
            AnimateScale(restingScale, duration, hoverScaleEase);
    }

    /// <summary>
    /// (供 CursorArea 呼叫) 離開 hover 區域：靜止倍率回到 1 並縮回。
    /// </summary>
    public void ClearHoverScale(float duration)
    {
        restingScale = 1f;
        if (!isClickHeld)
            AnimateScale(restingScale, duration, hoverScaleEase);
    }

    /// <summary>
    /// 只換圖、沿用「預設游標的 hotspot」。CursorArea 用這個版本，
    /// 各區域不必自己重填 hotspot。
    /// </summary>
    public void SetCursorArea(Texture2D normal, Texture2D click)
    {
        SetCursorArea(normal, defaultNormalHotspot, click, defaultClickHotspot);
    }

    public void SetCursorArea(Texture2D normal, Vector2 normalHotspot, Texture2D click, Vector2 clickHotspot)
    {
        currentNormalTexture = normal;
        currentNormalHotspot = normalHotspot;
        currentClickTexture = click;
        currentClickHotspot = clickHotspot;

        if (isCursorLocked) return;

        if (clickAction == null || !clickAction.IsPressed())
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

        // 進入鎖定狀態時離開任何 hover 放大狀態
        restingScale = 1f;
        if (!isClickHeld) AnimateScale(1f, clickScaleDuration, hoverScaleEase);

        if (clickAction != null && clickAction.IsPressed())
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

        if (clickAction != null && clickAction.IsPressed())
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
