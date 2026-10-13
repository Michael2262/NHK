using System;
using UnityEngine;

/// <summary>TeaseGame 操作模式。真有需要再擴增。</summary>
public enum TeaseMode
{
    Hand,
    Mouth,
    Clothes,
    GG,
}

/// <summary>
/// TeaseGame 操作模式控制器（一個場景一個）。
///
/// 管兩件事：
///   - CurrentMode：當前操作模式，決定哪些 TeaseZone 可被觸碰。
///   - HoveredMode：滑鼠正懸浮/按住在哪顆模式按鈕上，決定要預覽哪個模式的提示（null = 沒有）。
///
/// 另會訂閱進度旗標變動，flag 一解鎖就自動叫各 Zone 重算啟用/提示。
/// </summary>
[DefaultExecutionOrder(-100)] // 比一般 TeaseZone 早 Awake，確保 Zone 訂閱時 Instance 已就緒
public class TeaseModeController : MonoBehaviour
{
    /// <summary>場景內單例。小遊戲場景卸載時自動清空。</summary>
    public static TeaseModeController Instance { get; private set; }

    [Header("模式")]
    [Tooltip("進場時的預設模式（也代表一開始啟動的模式按鈕）。")]
    [SerializeField] private TeaseMode initialMode = TeaseMode.Hand;

    /// <summary>目前的操作模式（控制 Zone 能不能被碰）。</summary>
    public TeaseMode CurrentMode { get; private set; }

    /// <summary>目前懸浮預覽的模式；沒有時為 null（控制提示顯示）。</summary>
    public TeaseMode? HoveredMode { get; private set; }

    /// <summary>當前模式改變時觸發，參數為新模式。</summary>
    public event Action<TeaseMode> OnModeChanged;

    /// <summary>懸浮預覽模式改變時觸發，參數為懸浮模式（無則 null）。</summary>
    public event Action<TeaseMode?> OnHintPreviewChanged;

    private Action<string, bool> _onFlagChanged;
    private ProgressFlagModel _flags;

    /// <summary>只取得指定場景目前可用的控制器，避免轉場期間跨場景操作。</summary>
    public static TeaseModeController GetForScene(UnityEngine.SceneManagement.Scene scene)
    {
        var controller = Instance;
        return controller != null && controller.isActiveAndEnabled
            && controller.gameObject.scene == scene ? controller : null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.gameObject.scene == gameObject.scene)
            {
                Debug.LogWarning($"[TeaseModeController] 同一場景已有一個實例，銷毀重複的 {name}。", this);
                enabled = false;
                Destroy(this);
                return;
            }

            // Additive 轉場先停用舊控制器，解除旗標訂閱與預覽，再由新場景接管。
            Instance.enabled = false;
        }

        Instance = this;
        CurrentMode = initialMode;
        HoveredMode = null;
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        SubscribeFlags();
        RefreshZones();
    }

    private void Start()
    {
        if (Instance != this || !isActiveAndEnabled) return;
        // 補上 OnEnable 時服務尚未就緒的訂閱，並刷新已完成初始化的 Zone。
        SubscribeFlags();
        RefreshZones();
    }

    private void SubscribeFlags()
    {
        if (_flags != null) return;
        // 訂閱旗標變動：flag 解鎖時自動刷新各 Zone
        var gss = GameStatusService.Instance;
        if (gss != null && gss.ProgressFlags != null)
        {
            _onFlagChanged = (flag, state) => RefreshZones();
            _flags = gss.ProgressFlags;
            _flags.OnFlagChanged += _onFlagChanged;
        }
    }

    private void OnDisable()
    {
        UnsubscribeFlags();
        HoveredMode = null;
        // 此時 GetForScene 已不會回傳本控制器，讓舊 Zone 關閉觸碰與提示。
        OnModeChanged?.Invoke(CurrentMode);
        OnHintPreviewChanged?.Invoke(HoveredMode);
    }

    private void UnsubscribeFlags()
    {
        if (_flags != null && _onFlagChanged != null)
            _flags.OnFlagChanged -= _onFlagChanged;
        _flags = null;
        _onFlagChanged = null;
    }

    private void OnDestroy()
    {
        UnsubscribeFlags();
        if (Instance == this) Instance = null;
    }

    // ───── 當前模式 ─────

    /// <summary>切換當前操作模式（給模式按鈕呼叫）。</summary>
    public void SetMode(TeaseMode mode)
    {
        if (Instance != this || !isActiveAndEnabled) return;
        if (CurrentMode == mode) return;

        CurrentMode = mode;
        OnModeChanged?.Invoke(CurrentMode);
    }

    /// <summary>目前是否為指定模式。</summary>
    public bool IsMode(TeaseMode mode) => CurrentMode == mode;

    // 給 Button.onClick 用的無參數捷徑（onClick 無法直接傳 enum 參數）
    public void SetHandMode() => SetMode(TeaseMode.Hand);
    public void SetMouthMode() => SetMode(TeaseMode.Mouth);
    public void SetClothesMode() => SetMode(TeaseMode.Clothes);
    public void SetGGMode() => SetMode(TeaseMode.GG);

    // ───── 懸浮預覽 ─────

    /// <summary>設定懸浮預覽的模式（滑鼠移到/按住某模式按鈕時呼叫）。</summary>
    public void SetHoveredMode(TeaseMode mode)
    {
        if (Instance != this || !isActiveAndEnabled) return;
        HoveredMode = mode;
        OnHintPreviewChanged?.Invoke(HoveredMode);
    }

    /// <summary>清除懸浮預覽（滑鼠離開且放開模式按鈕時呼叫）。</summary>
    public void ClearHoveredMode()
    {
        if (Instance != this || !isActiveAndEnabled) return;
        HoveredMode = null;
        OnHintPreviewChanged?.Invoke(null);
    }

    /// <summary>目前是否正懸浮在指定模式上。</summary>
    public bool IsHovered(TeaseMode mode) => HoveredMode.HasValue && HoveredMode.Value == mode;

    // ───── 刷新 ─────

    /// <summary>強制各 Zone 重算啟用與提示（改 flag 後由旗標事件自動呼叫，也可手動呼叫）。</summary>
    public void RefreshZones()
    {
        if (Instance != this || !isActiveAndEnabled) return;
        OnModeChanged?.Invoke(CurrentMode);
        OnHintPreviewChanged?.Invoke(HoveredMode);
    }
}
