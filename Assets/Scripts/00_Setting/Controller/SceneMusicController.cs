using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneMusicController : MonoBehaviour
{
    public static SceneMusicController Instance { get; private set; }

    [System.Serializable]
    public class FlagMusicMap
    {
        public ProgressFlagDefinition flag;
        public string musicKey;
    }

    /// <summary>
    /// 單一時段的條件式音樂規則組：符合的 Flag 會覆蓋該時段的預設音樂。
    /// 與 phaseMusicKeys 一樣「依 Phase index 對齊」放在 List 裡。
    /// </summary>
    [System.Serializable]
    public class PhaseFlagRules
    {
        [Tooltip("此時段的條件式音樂。符合的 Flag 會覆蓋該時段預設音樂；清單越下方優先級越高。")]
        public List<FlagMusicMap> rules = new List<FlagMusicMap>();
    }

    [Header("基礎設定")]
    [Tooltip("是否啟用此場景的自動音樂邏輯")]
    public bool playSceneMusic = true;

    [Tooltip("若沒有任何 Flag 符合,預設播放的音樂")]
    public string defaultMusicKey = "Normal_BGM";

    [Tooltip("此場景沒有經過 SceneReadyCoordinator / Task_InitSceneMusic 時(例如第一幕 TitleScene)," +
             "勾選後會在 Start() 自己觸發一次播放。走正規轉場的場景請保持不勾,改由 Task 觸發。")]
    [SerializeField] private bool initOnStart = false;

    [Header("時段音樂 (優先級:中)")]
    [Tooltip("請依照 TimeConfig 的 Phase 順序填入 Music Key。若該時段為空則使用預設音樂。")]
    [SerializeField] private List<string> phaseMusicKeys = new List<string>();

    [Tooltip("各時段的條件式音樂(依 Phase 順序對齊)。讓每個時段可以吃不同 Flag 換成不同音樂,會覆蓋該時段的預設 Key。")]
    [SerializeField] private List<PhaseFlagRules> phaseFlagMusic = new List<PhaseFlagRules>();

    [Header("周末時段音樂 (優先級:中,疊在平日之上)")]
    [Tooltip("啟用後,週末(星期六/日)會切換成另一組時段音樂。未填寫的時段會自動沿用平日設定。")]
    [SerializeField] private bool enableWeekendMusic = false;

    [Tooltip("週末的時段預設音樂(依 Phase 順序對齊)。留空的時段沿用平日 phaseMusicKeys。")]
    [SerializeField] private List<string> weekendPhaseMusicKeys = new List<string>();

    [Tooltip("週末各時段的條件式音樂(依 Phase 順序對齊)。會覆蓋週末的時段預設 Key。")]
    [SerializeField] private List<PhaseFlagRules> weekendPhaseFlagMusic = new List<PhaseFlagRules>();

    [Header("條件式音樂 (優先級:高-清單越下方優先級越高)")]
    [SerializeField] private List<FlagMusicMap> flagMusicPriorities;

    private bool _isOverridden = false;
    private string _currentSceneBgmKey = "";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // ★ v4 改動:只訂閱事件,不再立刻 RefreshSceneMusic
        // 初始播放改由 Task_InitSceneMusic 在正確時機(ApplySaveData 之後)觸發
        // 否則讀檔時會在 Time 還是預設值(phase 0)時就播錯誤的 BGM

        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            GameStatusService.Instance.ProgressFlags.OnFlagChanged += HandleFlagChanged;
        }

        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
        {
            GameStatusService.Instance.Time.OnPhaseChanged += RefreshSceneMusic;
            // 跨午夜(AdvanceTime 跨日分支)只觸發 OnDayPassed 而不觸發 OnPhaseChanged,
            // 週末狀態又是跨日才改變,故需額外訂閱 OnDayPassed 以正確切換週末音樂。
            GameStatusService.Instance.Time.OnDayPassed += RefreshSceneMusic;
        }

        // 沒有 SceneReadyCoordinator 的場景(例如第一幕 TitleScene)沒有 Task 來觸發初始播放,
        // 在此自己補一次。這類場景通常沒有讀檔/時間狀態,故無「播到錯誤 phase」的時機問題。
        if (initOnStart)
        {
            RefreshSceneMusic();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            GameStatusService.Instance.ProgressFlags.OnFlagChanged -= HandleFlagChanged;
        }
        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
        {
            GameStatusService.Instance.Time.OnPhaseChanged -= RefreshSceneMusic;
            GameStatusService.Instance.Time.OnDayPassed -= RefreshSceneMusic;
        }
    }

    /// <summary>
    /// 【v4 新增】公開初始化方法,由 Task_InitSceneMusic 在 ReadyHandlers 管線中呼叫。
    /// 確保此時 CurrentPhaseIndex 已是讀檔/新遊戲的正確值。
    /// </summary>
    public IEnumerator Initialize()
    {
        Debug.Log($"[SceneMusicController] Initialize 執行,CurrentPhaseIndex = {GameStatusService.Instance?.Time?.CurrentPhaseIndex}");
        RefreshSceneMusic();
        yield return null;
    }

    /// <summary>
    /// 根據當前 Flag 狀態評估應播放的場景音樂
    /// </summary>
    public void RefreshSceneMusic()
    {
        if (!playSceneMusic) return;

        // --- 優先級 3: 預設音樂 ---
        string targetKey = defaultMusicKey;

        // --- 優先級 2: 時段音樂 (含時段 Flag 覆蓋 + 週末切換) ---
        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
        {
            int currentPhase = GameStatusService.Instance.Time.CurrentPhaseIndex;

            // 2a. 先算出平日基準音樂(時段預設 → 時段 Flag 覆蓋)
            string phaseKey = ResolvePhaseKey(phaseMusicKeys, phaseFlagMusic, currentPhase, targetKey);

            // 2b. 若啟用週末音樂且當天為週末,週末設定疊在平日基準之上;
            //     週末未填寫的時段會自動沿用平日結果。
            if (enableWeekendMusic && GameStatusService.Instance.Time.IsWeekend)
            {
                phaseKey = ResolvePhaseKey(weekendPhaseMusicKeys, weekendPhaseFlagMusic, currentPhase, phaseKey);
            }

            targetKey = phaseKey;
        }

        // --- 優先級 1: 條件式 Flag 音樂 (最高優先,會覆蓋前面的結果) ---
        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            foreach (var map in flagMusicPriorities)
            {
                if (map.flag != null && GameStatusService.Instance.ProgressFlags.Contains(map.flag.FlagID))
                {
                    targetKey = map.musicKey;
                }
            }
        }

        _currentSceneBgmKey = targetKey;

        // 3. 執行播放或停止動作
        if (!_isOverridden && MusicManager.Instance != null)
        {
            if (string.IsNullOrEmpty(_currentSceneBgmKey))
            {
                MusicManager.Instance.StopMusic();
            }
            else
            {
                MusicManager.Instance.PlayMusic(_currentSceneBgmKey);
            }
        }
    }

    /// <summary>
    /// 計算指定時段(phase)的音樂 Key:先套用該時段的預設 Key,再依序套用該時段的 Flag 覆蓋
    /// (清單越下方優先級越高)。若該時段沒有任何設定,回傳傳入的 fallback。
    /// </summary>
    private string ResolvePhaseKey(List<string> keys, List<PhaseFlagRules> flagRules, int phase, string fallback)
    {
        string result = fallback;
        if (phase < 0) return result;

        // 時段預設 Key
        if (keys != null && phase < keys.Count && !string.IsNullOrEmpty(keys[phase]))
        {
            result = keys[phase];
        }

        // 時段 Flag 覆蓋
        if (flagRules != null && phase < flagRules.Count && flagRules[phase] != null
            && GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            foreach (var map in flagRules[phase].rules)
            {
                if (map != null && map.flag != null
                    && GameStatusService.Instance.ProgressFlags.Contains(map.flag.FlagID))
                {
                    result = map.musicKey;
                }
            }
        }

        return result;
    }

    private void HandleFlagChanged(string flagID, bool isAdded)
    {
        RefreshSceneMusic();
    }

    // ==========================================================
    // 公開 API 供外部調用
    // ==========================================================

    public void PlayOverride(string key, float fadeDuration = 1.0f)
    {
        _isOverridden = true;
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic(key, fadeDuration);
        }
    }

    public void StopOverride(bool resumeSceneMusic, float fadeDuration = 1.0f)
    {
        if (resumeSceneMusic)
        {
            _isOverridden = false;
            RefreshSceneMusic();
        }
        else
        {
            _isOverridden = true;
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StopMusic(fadeDuration);
            }
        }
    }

    public void SetPlaySceneMusic(bool enable)
    {
        playSceneMusic = enable;
        if (enable)
        {
            RefreshSceneMusic();
        }
        else if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }
    }
}
