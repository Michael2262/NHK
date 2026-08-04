using UnityEngine;

/// <summary>
/// 依「本地點是否已解鎖 × 當前 MapScope」決定地點呈現「亮且可點」或「半透明且不可點」。
///
/// 職責邊界：
///   - 只管「亮 / 暗」。用 CanvasGroup（alpha + interactable + blocksRaycasts）。
///   - 「存在 / 不存在（未知地點）」不歸它管 —— 由 ProgressStateController 的 SetActive 處理。
///     未知地點的根物件被關掉時，掛在其上的本元件也一併停用，自然不參與亮暗運算。
///
/// 運作：
///   - scope 與解鎖狀態都是 ProgressFlagModel 的旗標，變動時透過既有的 OnFlagChanged 通知本元件重算。
///   - 進場鏈中 Task_InitMapScope 設定 scope 旗標，會觸發 OnFlagChanged → 本元件套用初次亮暗。
///
/// 亮暗規則（對每個地點都一樣，故不需逐地點手寫）：
///   拜訪模式：亮「已解鎖」的地點
///   挑戰模式：亮「未解鎖」的地點
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MapSpotView : MonoBehaviour
{
    [Header("此地點的『已解鎖』Flag")]
    [UnityEngine.Tooltip("拜訪模式：亮「已解鎖」的地點；挑戰模式：亮「未解鎖」的地點")]
    [SerializeField] private ProgressFlagDefinition unlockFlag;

    [Header("半透明（不可點）時的 alpha")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.4f;

    private CanvasGroup _cg;
    private ProgressFlagModel _flags;
    private string _unlockFlagId;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _flags = GameStatusService.Instance?.ProgressFlags;
        _unlockFlagId = unlockFlag != null ? unlockFlag.FlagID : null;
    }

    private void OnEnable()
    {
        if (_flags != null)
            _flags.OnFlagChanged += HandleFlagChanged;

        Apply(); // 啟用當下先套一次（若 scope 旗標已在進場鏈設好，這裡即為初次定稿）
    }

    private void OnDisable()
    {
        if (_flags != null)
            _flags.OnFlagChanged -= HandleFlagChanged;
    }

    /// <summary>只有跟本地點亮暗相關的旗標變動時才重算。</summary>
    private void HandleFlagChanged(string id, bool _)
    {
        if (id == _unlockFlagId || id == MapScopeFlags.Unlock || id == MapScopeFlags.Visit)
            Apply();
    }

    private void Apply()
    {
        if (_cg == null || _flags == null) return;

        bool unlockScope = _flags.Contains(MapScopeFlags.Unlock);
        bool unlocked = !string.IsNullOrEmpty(_unlockFlagId) && _flags.Contains(_unlockFlagId);

        // 挑戰模式亮未解鎖的；拜訪模式亮已解鎖的。
        bool lit = unlockScope ? !unlocked : unlocked;

        _cg.alpha = lit ? 1f : dimAlpha;
        _cg.interactable = lit;    // 暗的不可互動
        _cg.blocksRaycasts = lit;  // 暗的不吃射線 → MapSpotCaller 的點擊不會觸發
    }
}
