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

    [Header("基礎設定")]
    [Tooltip("是否啟用此場景的自動音樂邏輯")]
    public bool playSceneMusic = true;

    [Tooltip("若沒有任何 Flag 符合,預設播放的音樂")]
    public string defaultMusicKey = "Normal_BGM";

    [Header("時段音樂 (優先級:中)")]
    [Tooltip("請依照 TimeConfig 的 Phase 順序填入 Music Key。若該時段為空則使用預設音樂。")]
    [SerializeField] private List<string> phaseMusicKeys = new List<string>();

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

        // --- 優先級 2: 時段音樂 ---
        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
        {
            int currentPhase = GameStatusService.Instance.Time.CurrentPhaseIndex;

            // 檢查索引是否在清單範圍內,且該索引有填寫 Key
            if (currentPhase >= 0 && currentPhase < phaseMusicKeys.Count)
            {
                if (!string.IsNullOrEmpty(phaseMusicKeys[currentPhase]))
                {
                    targetKey = phaseMusicKeys[currentPhase];
                }
            }
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