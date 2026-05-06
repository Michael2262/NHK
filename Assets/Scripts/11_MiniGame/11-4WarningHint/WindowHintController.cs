using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class WindowHintController : MonoBehaviour
{
    public static WindowHintController Instance;

    [Header("UI Components")]
    [Tooltip("帶有 Image 和 CanvasGroup 的物件")]
    public GameObject windowHintObject;

    [Header("Canvas Group")]
    [Tooltip("可自行指定 CanvasGroup，若未指定則自動從 windowHintObject 取得")]
    public CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [Tooltip("淡入淡出時間（秒），0 為立即切換")]
    public float fadeDuration = 0f;

    [Header("Audio Settings")]
    [Tooltip("針對特定圖片 ID 設定對應的音效")]
    public List<WindowHintAudio> audioMappings = new List<WindowHintAudio>();

    private Image _image;
    private string _currentId;
    private bool _isVisible;

    // 快取已載入的 Sprite
    private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
    // 快取音效對應
    private Dictionary<string, WindowHintAudio> _audioDictionary = new Dictionary<string, WindowHintAudio>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _image = windowHintObject.GetComponent<Image>();
        if (canvasGroup == null)
            canvasGroup = windowHintObject.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        _isVisible = false;

        // 建立音效查找字典，Key 從 Sprite 名稱自動抓取
        foreach (var mapping in audioMappings)
        {
            if (mapping.sprite != null)
            {
                string id = ExtractIdFromSpriteName(mapping.sprite.name);
                if (!string.IsNullOrEmpty(id))
                    _audioDictionary[id] = mapping;
            }
        }
    }

    /// <summary>
    /// 顯示指定 ID 的圖片，淡入出現
    /// </summary>
    /// <param name="id">圖片 ID</param>
    /// <param name="overrideDuration">覆蓋淡入時間，傳入負值則使用 Inspector 預設值</param>
    public void Show(string id, float overrideDuration = -1f)
    {
        canvasGroup.DOKill();

        _currentId = id;
        Sprite sprite = LoadSprite(id);
        if (sprite != null)
            _image.sprite = sprite;
        else
            Debug.LogWarning($"WindowHintController: 找不到 ID 為 '{id}' 的圖片 (WindowHint_{id})");

        _isVisible = true;

        float duration = overrideDuration >= 0f ? overrideDuration : fadeDuration;
        if (duration > 0f)
            canvasGroup.DOFade(1f, duration);
        else
            canvasGroup.alpha = 1f;

        TryPlayAudio(id, AudioAction.Show);
    }

    /// <summary>
    /// 淡出後隱藏
    /// </summary>
    /// <param name="overrideDuration">覆蓋淡出時間，傳入負值則使用 Inspector 預設值</param>
    public void Hide(float overrideDuration = -1f)
    {
        if (!_isVisible) return;

        canvasGroup.DOKill();

        float duration = overrideDuration >= 0f ? overrideDuration : fadeDuration;
        if (duration > 0f)
        {
            canvasGroup.DOFade(0f, duration)
                .OnComplete(() => _isVisible = false);
        }
        else
        {
            canvasGroup.alpha = 0f;
            _isVisible = false;
        }

        TryPlayAudio(_currentId, AudioAction.Hide);
    }

    private Sprite LoadSprite(string id)
    {
        if (_spriteCache.TryGetValue(id, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>($"WindowHint/WindowHint_{id}");
        if (sprite != null)
            _spriteCache[id] = sprite;

        return sprite;
    }

    #region Audio

    private enum AudioAction { Show, Hide }

    private void TryPlayAudio(string id, AudioAction action)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!_audioDictionary.TryGetValue(id, out WindowHintAudio mapping)) return;
        if (AudioManager.Instance == null) return;

        string audioKey = action switch
        {
            AudioAction.Show => mapping.showAudioKey,
            AudioAction.Hide => mapping.hideAudioKey,
            _ => null
        };

        if (!string.IsNullOrEmpty(audioKey))
            AudioManager.Instance.PlaySound(audioKey);
    }

    private string ExtractIdFromSpriteName(string spriteName)
    {
        if (spriteName.StartsWith("WindowHint_"))
            return spriteName.Substring("WindowHint_".Length);
        return spriteName;
    }

    #endregion
}

/// <summary>
/// 在 Inspector 中設定特定圖片 ID 對應的音效
/// </summary>
[System.Serializable]
public class WindowHintAudio
{
    [Tooltip("拉入對應的 Sprite（WindowHint_XXX），ID 會自動從名稱抓取")]
    public Sprite sprite;

    [Tooltip("Show() 時播放的音效 Key（留空則不播放）")]
    public string showAudioKey;

    [Tooltip("Hide() 時播放的音效 Key（留空則不播放）")]
    public string hideAudioKey;
}