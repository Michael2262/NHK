using UnityEngine;

/// <summary>
/// 試玩版時間限制器
/// 當遊戲時間推進到指定的 Day / Phase / Slot 時，觸發試玩結束。
/// 
/// 偵測方式：
///   - 訂閱 OnTimeSlotAdvanced（一般時間推進）
///   - 訂閱 OnDayPassed（跳日推進，因為 SkipToNextDay 不經過 OnTimeSlotAdvanced）
/// 
/// 使用方式：掛在不卸載的場景物件上（例如跟 GameStatusService 同場景），
///           在 Inspector 設定結束時刻即可。
/// </summary>
public class DemoTimeLimit : MonoBehaviour
{
    [Header("試玩結束時刻")]
    [Tooltip("第幾天（DayIndex，從 0 開始）")]
    [SerializeField] private int endDayIndex = 2;

    [Tooltip("第幾個 Phase（從 0 開始：0=白天, 1=黃昏, 2=晚上, 3=深夜）")]
    [SerializeField] private int endPhaseIndex = 0;

    [Tooltip("第幾個 Slot（從 1 開始）")]
    [SerializeField] private int endSlotInPhase = 1;

    [Header("結束方式")]
    [SerializeField] private EndAction endAction = EndAction.LoadScene;

    [Tooltip("當結束方式為 LoadScene 時，要載入的場景名稱（例如試玩結束畫面）")]
    [SerializeField] private string endSceneName = "DemoEndScene";

    private bool _hasTriggered = false;

    public enum EndAction
    {
        LoadScene,      // 切到試玩結束場景
        PauseGame,      // 暫停遊戲（Time.timeScale = 0）
        QuitApplication // 直接關閉遊戲
    }

    private void Start()
    {
        var service = GameStatusService.Instance;
        if (service == null) return;

        // 訂閱一般時間推進（AdvanceTime 逐格推進時觸發）
        service.Time.OnTimeSlotAdvanced += OnSlotAdvanced;

        // 訂閱跳日事件（SkipToNextDay 不走 OnTimeSlotAdvanced，要額外捕捉）
        service.Time.OnDayPassed += OnDayPassed;

        // 訂閱遊戲重開事件，重設 _hasTriggered
        service.OnGameStatusLoaded += OnGameReloaded;
    }

    private void OnDestroy()
    {
        var service = GameStatusService.Instance;
        if (service == null) return;

        service.Time.OnTimeSlotAdvanced -= OnSlotAdvanced;
        service.Time.OnDayPassed -= OnDayPassed;
        service.OnGameStatusLoaded -= OnGameReloaded;
    }

    // ── 事件回呼 ──

    private void OnSlotAdvanced(int slotsAdvanced)
    {
        CheckTimeLimit();
    }

    private void OnDayPassed()
    {
        // 跳日後時間已經是新的一天 D(n+1) P0 S1
        // 延遲一幀檢查，確保所有 HandleDayPassed 邏輯執行完畢
        StartCoroutine(CheckNextFrame());
    }

    private System.Collections.IEnumerator CheckNextFrame()
    {
        yield return null;
        CheckTimeLimit();
    }

    private void OnGameReloaded()
    {
        // NewGame 或 LoadGame 時重設，讓新一輪遊戲能再次觸發
        _hasTriggered = false;
        Debug.Log("[DemoTimeLimit] 遊戲重載，重設試玩限制器。");
    }

    // ── 核心檢查 ──

    private void CheckTimeLimit()
    {
        if (_hasTriggered) return;

        var time = GameStatusService.Instance?.Time;
        if (time == null) return;

        if (IsTimeReached(time))
        {
            _hasTriggered = true;
            Debug.Log($"[DemoTimeLimit] 試玩時間到達 D{time.DayIndex}P{time.CurrentPhaseIndex}S{time.CurrentSlotInPhase}，觸發結束。");
            ExecuteEndAction();
        }
    }

    private bool IsTimeReached(TimeSystemModel time)
    {
        // 先比 Day
        if (time.DayIndex > endDayIndex) return true;
        if (time.DayIndex < endDayIndex) return false;

        // Day 相同，比 Phase
        if (time.CurrentPhaseIndex > endPhaseIndex) return true;
        if (time.CurrentPhaseIndex < endPhaseIndex) return false;

        // Phase 也相同，比 Slot
        return time.CurrentSlotInPhase >= endSlotInPhase;
    }

    // ── 結束動作 ──

    private void ExecuteEndAction()
    {
        switch (endAction)
        {
            case EndAction.LoadScene:
                if (!string.IsNullOrEmpty(endSceneName))
                {
                    SceneController.ChangeScene(endSceneName);
                }
                break;

            case EndAction.PauseGame:
                Time.timeScale = 0f;
                Debug.Log("[DemoTimeLimit] 遊戲已暫停。");
                break;

            case EndAction.QuitApplication:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}