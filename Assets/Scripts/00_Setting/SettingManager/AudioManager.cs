using UnityEngine;
using System.Collections.Generic;

// 定義音效的播放類型
public enum SoundType
{
    Fixed,      // 固定音效
    Conditional, // 條件音效
    Random      // 隨機音效
}

// 用一個類別來儲存單個音效的所有設定
// [System.Serializable] 讓它可以顯示在 Unity Inspector 中
[System.Serializable]
public class Sound
{
    [Tooltip("對應 Spine 事件中的字串 Key")]
    public string key;
    public SoundType type = SoundType.Fixed;

    [Header("Fixed Setting")]
    [Tooltip("【固定型】播放這一個音效")]
    public AudioClip clip;

    [Header("Random Settings")]
    [Tooltip("【隨機型】從這個列表中隨機抽一個播放")]
    public AudioClip[] clips;

    [Header("Conditional Settings")]
    [Tooltip("【條件型】當興奮度 < 門檻時播放")]
    public AudioClip lowExcitementClip;
    [Tooltip("【條件型】當興奮度 >= 門檻時播放")]
    public AudioClip highExcitementClip;
    [Tooltip("【條件型】用來判斷的興奮度等級門檻")]
    public int excitementThreshold = 3;
}


public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("在這裡設定您所有的音效")]
    public List<Sound> sounds;

    private AudioSource audioSource;
    private Dictionary<string, Sound> soundDictionary;

    void Awake()
    {
        // Singleton 初始化
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

        audioSource = gameObject.AddComponent<AudioSource>();

        float savedVolume = PlayerPrefs.GetFloat("SfxVolume", 0.4f); // <--- 將 1.0f 改為 0.4f
        audioSource.volume = savedVolume;

        // 將 List 轉換成 Dictionary，以加速查找
        soundDictionary = new Dictionary<string, Sound>();
        foreach (Sound sound in sounds)
        {
            soundDictionary[sound.key] = sound;
        }
    }
    /// <summary>
    /// 設定音效的音量
    /// </summary>
    /// <param name="volume">音量大小，範圍應為 0.0 到 1.0</param>
    public void SetVolume(float volume)
    {
        // 使用 Mathf.Clamp01 確保傳入的值永遠介於 0 和 1 之間，增加程式碼的穩健性
        audioSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 供外部呼叫的播放音效接口
    /// </summary>
    /// <param name="key">來自 Spine 事件的音效關鍵字</param>
    public void PlaySound(string key)
    {
        if (!soundDictionary.TryGetValue(key, out Sound soundToPlay))
        {
            Debug.LogWarning($"AudioManager: 找不到 Key 為 '{key}' 的音效設定。");
            return;
        }

        AudioClip clip = null;

        // 根據音效類型，執行不同的邏輯
        switch (soundToPlay.type)
        {
            case SoundType.Fixed:
                clip = soundToPlay.clip;
                break;

            case SoundType.Random:
                if (soundToPlay.clips.Length > 0)
                {
                    clip = soundToPlay.clips[Random.Range(0, soundToPlay.clips.Length)];
                }
                break;

            case SoundType.Conditional:
                
                break;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"播放音效: {clip.name}");
        }
    }
}