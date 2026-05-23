using UnityEngine;
using System.Collections;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

/// <summary>
/// 對話期間自動隱藏指定的 CanvasGroup 與 InteractableCharacter，對話結束後恢復。
/// 掛在場景常駐物件上，Inspector 拖入要控制的對象即可。
/// </summary>
public class DialogueVisibilityHandler : MonoBehaviour
{
    [Header("對話期間要隱藏的 UI")]
    [Tooltip("對話開始時 alpha=0 且不可互動，結束後恢復")]
    public CanvasGroup[] canvasGroups;

    [Header("對話期間要隱藏的角色")]
    [Tooltip("會關閉角色的 characterSprite 和 Collider2D，並停止所有點擊/情緒動畫")]
    public InteractableCharacter[] charactersToHide;

    [Header("淡入設定")]
    [Tooltip("角色恢復顯示時的淡入時間（秒）")]
    public float fadeInDuration = 0.4f;
    [Tooltip("淡入的緩動曲線")]
    public Ease fadeInEase = Ease.OutQuad;

    private bool _subscribed = false;
    private bool _isHidden = false;
    private Coroutine _restoreCoroutine;

    IEnumerator Start()
    {
        while (DialogueManager.instance == null)
        {
            yield return null;
        }

        DialogueManager.instance.conversationStarted += OnConversationStarted;
        DialogueManager.instance.conversationEnded += OnConversationEnded;
        _subscribed = true;
    }

    void OnDestroy()
    {
        if (_subscribed && DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted -= OnConversationStarted;
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
        }
    }

    void OnConversationStarted(Transform actor)
    {
        if (_restoreCoroutine != null)
        {
            StopCoroutine(_restoreCoroutine);
            _restoreCoroutine = null;
        }

        if (_isHidden) return;

        SetHidden();
        _isHidden = true;
    }

    void OnConversationEnded(Transform actor)
    {
        if (_restoreCoroutine != null)
        {
            StopCoroutine(_restoreCoroutine);
        }
        _restoreCoroutine = StartCoroutine(DelayedRestore());
    }

    IEnumerator DelayedRestore()
    {
        yield return null;

        if (!DialogueManager.isConversationActive)
        {
            SetVisible();
            _isHidden = false;
        }

        _restoreCoroutine = null;
    }

    /// <summary>
    /// 對話開始：立即隱藏，不需要淡出
    /// </summary>
    void SetHidden()
    {
        // CanvasGroup 控制
        foreach (var cg in canvasGroups)
        {
            if (cg == null) continue;
            cg.DOKill();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // InteractableCharacter 控制
        foreach (var character in charactersToHide)
        {
            if (character == null) continue;

            // 停止該 Sprite 上可能殘留的 Tween
            if (character.characterSprite != null)
            {
                character.characterSprite.DOKill();
                character.characterSprite.color = new Color(
                    character.characterSprite.color.r,
                    character.characterSprite.color.g,
                    character.characterSprite.color.b,
                    0f
                );
            }

            var col = character.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            character.CloseInteractionUI();

            var aniEmotion = character.GetComponentInChildren<AniByEmotionOnTouch>();
            if (aniEmotion != null) aniEmotion.StopAndReset();

            var aniTouch = character.GetComponentInChildren<AniOnTouch>();
            if (aniTouch != null) aniTouch.StopAndReset();

            var emotionPlayer = character.GetComponentInChildren<EmotionAnimationPlayer>();
            if (emotionPlayer != null) emotionPlayer.StopAndReset();
        }
    }

    /// <summary>
    /// 對話結束：淡入恢復
    /// </summary>
    void SetVisible()
    {
        // CanvasGroup 淡入
        foreach (var cg in canvasGroups)
        {
            if (cg == null) continue;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.DOFade(1f, fadeInDuration).SetEase(fadeInEase);
        }

        // InteractableCharacter 淡入
        foreach (var character in charactersToHide)
        {
            if (character == null) continue;

            // 先恢復情緒動畫（這樣淡入過程中動畫就在跑了）
            var emotionPlayer = character.GetComponentInChildren<EmotionAnimationPlayer>();
            if (emotionPlayer != null) emotionPlayer.Resume();

            // Sprite 淡入
            if (character.characterSprite != null)
            {
                character.characterSprite.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
                    .OnComplete(() =>
                    {
                        // 淡入完成後才開啟碰撞，避免淡入中被點擊
                        var col = character.GetComponent<Collider2D>();
                        if (col != null) col.enabled = true;
                    });
            }
            else
            {
                // 沒有 Sprite 的情況，直接開碰撞
                var col = character.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
            }
        }
    }
}