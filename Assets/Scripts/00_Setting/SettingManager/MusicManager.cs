using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 用於在 Inspector 中設定的音樂資料結構
/// </summary>
[System.Serializable]
public class MusicTrack
{
    [Tooltip("用於程式碼中呼叫的唯一 Key")]
    public string key;
    [Tooltip("對應的音樂檔案")]
    public AudioClip clip;
}

/// <summary>
/// 管理遊戲背景音樂(BGM)的播放、切換與音量
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Tooltip("在這裡設定您所有的背景音樂")]
    [SerializeField] private List<MusicTrack> musicTracks;

    private AudioSource _audioSource1;
    private AudioSource _audioSource2;
    private bool _isSource1Active;

    private Dictionary<string, AudioClip> _musicDictionary;
    private Coroutine _activeFadeCoroutine;

    // 公開的 Master Volume，UI 和淡化效果都將參考此值
    public float MasterVolume { get; private set; }

    void Awake()
    {
        // --- Singleton 初始化 ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- 動態創建兩個 AudioSource 元件 ---
        _audioSource1 = gameObject.AddComponent<AudioSource>();
        _audioSource2 = gameObject.AddComponent<AudioSource>();

        // 設定 AudioSource 預設值
        _audioSource1.loop = true;
        _audioSource2.loop = true;
        _audioSource1.playOnAwake = false;
        _audioSource2.playOnAwake = false;

        // --- 初始化音樂字典 ---
        _musicDictionary = new Dictionary<string, AudioClip>();
        foreach (var track in musicTracks)
        {
            _musicDictionary[track.key] = track.clip;
        }

        // --- 載入儲存的音量 ---
        MasterVolume = PlayerPrefs.GetFloat("BgmVolume", 0.4f);
        _audioSource1.volume = 0;
        _audioSource2.volume = 0;
    }

    /// <summary>
    /// 以交叉淡化效果播放指定的背景音樂
    /// </summary>
    /// <param name="key">音樂的 Key</param>
    /// <param name="fadeDuration">淡化時間(秒)</param>
    public void PlayMusic(string key, float fadeDuration = 1.0f)
    {
        if (!_musicDictionary.TryGetValue(key, out AudioClip clipToPlay))
        {
            Debug.LogWarning($"MusicManager: 找不到 Key 為 '{key}' 的音樂。");
            return;
        }

        // 如果正在播放同一首音樂，則不執行任何操作
        AudioSource activeSource = _isSource1Active ? _audioSource1 : _audioSource2;
        if (activeSource.isPlaying && activeSource.clip == clipToPlay)
        {
            return;
        }

        // 如果有正在進行的淡化，先停止它
        if (_activeFadeCoroutine != null)
        {
            StopCoroutine(_activeFadeCoroutine);
        }

        // 開始新的淡化協程
        _activeFadeCoroutine = StartCoroutine(CrossfadeCoroutine(clipToPlay, fadeDuration));
    }

    /// <summary>
    /// 以淡出效果停止目前的音樂
    /// </summary>
    /// <param name="fadeDuration">淡出時間(秒)</param>
    public void StopMusic(float fadeDuration = 1.0f)
    {
        if (_activeFadeCoroutine != null)
        {
            StopCoroutine(_activeFadeCoroutine);
        }
        _activeFadeCoroutine = StartCoroutine(FadeOutCoroutine(fadeDuration));
    }

    /// <summary>
    /// 設定 BGM 的主音量
    /// </summary>
    /// <param name="volume">音量大小 (0.0 到 1.0)</param>
    public void SetVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);

        // 即時更新當前播放中的 AudioSource 音量
        // 淡化協程也會自動參考這個新的 MasterVolume
        AudioSource activeSource = _isSource1Active ? _audioSource1 : _audioSource2;
        if (activeSource.isPlaying)
        {
            activeSource.volume = MasterVolume;
        }

        // 注意：MusicSettingsUI 腳本會負責儲存 PlayerPrefs
    }

    // 交叉淡化的協程
    private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
    {
        // 決定哪個是作用中(要淡出)，哪個是閒置(要淡入)
        _isSource1Active = !_isSource1Active; // 切換作用中的 Source
        AudioSource oldSource = _isSource1Active ? _audioSource2 : _audioSource1;
        AudioSource newSource = _isSource1Active ? _audioSource1 : _audioSource2;

        // 設定新音樂並開始播放
        newSource.clip = newClip;
        newSource.Play();
        newSource.volume = 0;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // 根據 MasterVolume 來計算當前的音量
            newSource.volume = Mathf.Lerp(0, MasterVolume, progress);
            oldSource.volume = Mathf.Lerp(oldSource.volume, 0, progress); // 從當前音量開始淡出

            yield return null;
        }

        // 確保最終狀態正確
        newSource.volume = MasterVolume;
        oldSource.Stop();
        oldSource.clip = null; // 清除舊的 clip
    }

    // 淡出至靜音的協程
    private IEnumerator FadeOutCoroutine(float duration)
    {
        AudioSource activeSource = _isSource1Active ? _audioSource1 : _audioSource2;
        float startVolume = activeSource.volume;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            activeSource.volume = Mathf.Lerp(startVolume, 0, progress);
            yield return null;
        }

        activeSource.Stop();
        activeSource.clip = null;
        activeSource.volume = 0;
    }
}