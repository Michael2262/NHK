using System;
using UnityEngine;

/// <summary>TeaseGame 操作模式。真有需要再擴增。</summary>
public enum TeaseMode
{
    Hand,
    Mouth,
    Clothes,
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[TeaseModeController] 場上已有一個實例，銷毀重複的 {name}。", this);
            Destroy(this);
            return;
        }

        Instance = this;
        CurrentMode = initialMode;
    }

    private void Start()
    {
        // 訂閱旗標變動：flag 解鎖時自動刷新各 Zone
        var gss = GameStatusService.Instance;
        if (gss != null && gss.ProgressFlags != null)
        {
            _onFlagChanged = (flag, state) => RefreshZones();
            gss.ProgressFlags.OnFlagChanged += _onFlagChanged;
        }

        // 初始廣播：讓已在 OnEnable 訂閱的 Zone 依預設狀態初始化
        OnModeChanged?.Invoke(CurrentMode);
        OnHintPreviewChanged?.Invoke(HoveredMode);
    }

    private void OnDestroy()
    {
        var gss = GameStatusService.Instance;
        if (gss != null && gss.ProgressFlags != null && _onFlagChanged != null)
            gss.ProgressFlags.OnFlagChanged -= _onFlagChanged;
        _onFlagChanged = null;

        if (Instance == this) Instance = null;
    }

    // ───── 當前模式 ─────

    /// <summary>切換當前操作模式（給模式按鈕呼叫）。</summary>
    public void SetMode(TeaseMode mode)
    {
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

    // ───── 懸浮預覽 ─────

    /// <summary>設定懸浮預覽的模式（滑鼠移到/按住某模式按鈕時呼叫）。</summary>
    public void SetHoveredMode(TeaseMode mode)
    {
        HoveredMode = mode;
        OnHintPreviewChanged?.Invoke(HoveredMode);
    }

    /// <summary>清除懸浮預覽（滑鼠離開且放開模式按鈕時呼叫）。</summary>
    public void ClearHoveredMode()
    {
        HoveredMode = null;
        OnHintPreviewChanged?.Invoke(null);
    }

    /// <summary>目前是否正懸浮在指定模式上。</summary>
    public bool IsHovered(TeaseMode mode) => HoveredMode.HasValue && HoveredMode.Value == mode;

    // ───── 刷新 ─────

    /// <summary>強制各 Zone 重算啟用與提示（改 flag 後由旗標事件自動呼叫，也可手動呼叫）。</summary>
    public void RefreshZones()
    {
        OnModeChanged?.Invoke(CurrentMode);
        OnHintPreviewChanged?.Invoke(HoveredMode);
    }
}
