using UnityEngine;

/// <summary>
/// 一個簡單的輔助元件，提供一個公開方法來播放音樂。
/// 可被用於 UI Button 的 OnClick() 事件、動畫事件、Timeline Signal 等。
/// </summary>
public class MusicEventCaller : MonoBehaviour
{
    [Header("音樂設定")]
    [Tooltip("要播放的音樂 Key")]
    [SerializeField] private string musicKey;

    [Tooltip("音樂交叉淡化的時間")]
    [SerializeField] private float fadeDuration = 0.5f;

    /// <summary>
    /// 這個公開方法是用來給其他系統呼叫的
    /// </summary>
    public void PlayMusic()
    {
        Debug.Log($"事件觸發，播放音樂: {musicKey}");
        MusicManager.Instance.PlayMusic(musicKey, fadeDuration);
    }
}