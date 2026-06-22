using UnityEngine;
using System.Collections;

/// <summary>
/// 標題畫面專用的獨立 BGM 播放器。
///
/// 【為什麼獨立於 MusicManager / SceneMusicController】
/// TitleScene 是 build 的第一個場景,且 MusicManager / GameStatusService / SceneController
/// 都常駐在 GlobalStatusUI,而該場景在停留於標題時並未載入。因此標題畫面沒有那套全域音樂
/// 基礎設施可用。標題音樂本來就與時段 / 旗標 / 存檔無關,故改用自帶 AudioSource 的獨立播放器,
/// 零依賴、零時機問題。
///
/// 【用法】掛在 TitleScene 的一個 GameObject 上,指定 bgmClip 即可。離開標題前若想淡出,
/// 可由「開始遊戲」按鈕呼叫 FadeOutAndStop()。
/// </summary>
public class TitleMusicController : MonoBehaviour
{
    public static TitleMusicController Instance { get; private set; }

    [Header("音樂")]
    [Tooltip("標題畫面要播放的 BGM")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("是否在 Start() 自動播放")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("是否循環播放")]
    [SerializeField] private bool loop = true;

    [Tooltip("淡入時間(秒)。設 0 則立即達到目標音量")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("啟動時先停掉常駐 MusicManager 正在播的 BGM。" +
             "用於『從遊戲場景返回標題』時:MusicManager(常駐於 GlobalStatusUI)仍在播遊戲音樂," +
             "需先讓它淡出,避免與標題曲同時播放。冷啟動時 MusicManager 不存在會自動略過。")]
    [SerializeField] private bool silenceGlobalMusicOnStart = true;

    [Header("音量")]
    [Tooltip("是否沿用遊戲設定的 BGM 音量(PlayerPrefs \"BgmVolume\",與 MusicManager 一致)")]
    [SerializeField] private bool useSavedBgmVolume = true;

    [Tooltip("找不到已存音量時使用的預設音量")]
    [Range(0f, 1f)]
    [SerializeField] private float fallbackVolume = 0.4f;

    private AudioSource _audioSource;
    private Coroutine _activeFade;

    private void Awake()
    {
        Instance = this;

        // 自帶 AudioSource,沒有就動態補上,行為與 MusicManager 一致。
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.loop = loop;
        _audioSource.clip = bgmClip;
        _audioSource.volume = 0f;
    }

    private void Start()
    {
        // 返回標題時,常駐 MusicManager 仍在播遊戲 BGM,先讓它淡出避免兩首重疊。
        // 冷啟動時 MusicManager.Instance 為 null,自動略過。
        if (silenceGlobalMusicOnStart && MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic(fadeInDuration);
        }

        if (playOnStart)
        {
            Play(fadeInDuration);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>目標音量:沿用遊戲 BGM 音量設定或退回預設值。</summary>
    private float TargetVolume =>
        useSavedBgmVolume ? PlayerPrefs.GetFloat("BgmVolume", fallbackVolume) : fallbackVolume;

    /// <summary>播放標題 BGM(可淡入)。</summary>
    public void Play(float fadeDuration = 1.0f)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[TitleMusicController] 未指定 bgmClip,無法播放。");
            return;
        }

        _audioSource.clip = bgmClip;
        _audioSource.loop = loop;
        if (!_audioSource.isPlaying) _audioSource.Play();

        StartFade(TargetVolume, fadeDuration);
    }

    /// <summary>淡出並停止標題 BGM(例如離開標題前呼叫)。</summary>
    public void FadeOutAndStop(float fadeDuration = 1.0f)
    {
        StartFade(0f, fadeDuration, stopWhenDone: true);
    }

    /// <summary>即時更新音量(若設定 UI 在標題就能調)。</summary>
    public void SetVolume(float volume)
    {
        if (_activeFade != null)
        {
            StopCoroutine(_activeFade);
            _activeFade = null;
        }
        _audioSource.volume = Mathf.Clamp01(volume);
    }

    private void StartFade(float targetVolume, float duration, bool stopWhenDone = false)
    {
        if (_activeFade != null) StopCoroutine(_activeFade);
        _activeFade = StartCoroutine(FadeRoutine(targetVolume, duration, stopWhenDone));
    }

    private IEnumerator FadeRoutine(float targetVolume, float duration, bool stopWhenDone)
    {
        float startVolume = _audioSource.volume;

        if (duration > 0f)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime; // 標題畫面 timeScale 可能為 0,用 unscaled 確保淡化照跑
                _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
        }

        _audioSource.volume = targetVolume;

        if (stopWhenDone)
        {
            _audioSource.Stop();
        }

        _activeFade = null;
    }
}
