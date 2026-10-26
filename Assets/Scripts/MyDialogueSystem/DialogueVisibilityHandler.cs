using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

/// <summary>
/// 對話期間自動隱藏指定的 CanvasGroup 與一般互動物件，對話結束後恢復。
/// 掛在場景常駐物件上，Inspector 拖入要控制的對象即可。
/// </summary>
public class DialogueVisibilityHandler : MonoBehaviour
{
    // 保存簡化元件的執行期狀態，不在 Inspector 重複設定。
    private class InteractionTarget
    {
        [Tooltip("對話期間隱藏，結束後淡入的 SpriteRenderer。")]
        public SpriteRenderer sprite;
        [Tooltip("對話期間停用的點擊碰撞區域，請加入所有相關 Collider2D。")]
        public Collider2D[] colliders = new Collider2D[0];

        [System.NonSerialized] internal bool captured;
        [System.NonSerialized] internal float originalAlpha;
        [System.NonSerialized] internal Collider2D[] savedColliders;
        [System.NonSerialized] internal bool[] colliderStates;
        [System.NonSerialized] internal Tween fadeTween;

        internal void RestoreColliders()
        {
            if (!captured) return;
            for (int i = 0; i < savedColliders.Length; i++)
                if (savedColliders[i] != null) savedColliders[i].enabled = colliderStates[i];
            captured = false;
        }
    }

    [Header("對話期間要隱藏的 UI")]
    [Tooltip("對話開始時 alpha=0 且不可互動，結束後恢復")]
    public CanvasGroup[] canvasGroups;

    [Header("一般互動物件（簡化版）")]
    [Tooltip("拖入共同父物件，再用元件右鍵選單「從父物件收集 DialogueVisibilityTarget」填入下方清單。包含未啟用的子物件。")]
    public GameObject targetsRoot;

    [Tooltip("拖入已設定圖片與碰撞清單的 DialogueVisibilityTarget，或透過父物件右鍵收集。")]
    public DialogueVisibilityTarget[] targetsToHide = new DialogueVisibilityTarget[0];

#if UNITY_EDITOR
    [ContextMenu("從父物件收集 DialogueVisibilityTarget")]
    private void CollectTargetsFromRoot()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (targetsRoot == null)
        {
            Debug.LogWarning("[DialogueVisibilityHandler] 請先指定 Targets Root 父物件，原清單保持不變。", this);
            return;
        }

        var collected = targetsRoot.GetComponentsInChildren<DialogueVisibilityTarget>(true);
        // 使用序列化介面，支援 Undo、Prefab Override 與場景修改狀態。
        var serialized = new UnityEditor.SerializedObject(this);
        serialized.Update();
        var list = serialized.FindProperty(nameof(targetsToHide));
        list.arraySize = collected.Length;
        for (int i = 0; i < collected.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = collected[i];
        serialized.ApplyModifiedProperties();
        Debug.Log($"[DialogueVisibilityHandler] 已從「{targetsRoot.name}」收集 {collected.Length} 個目標並更新清單。", this);
    }
#endif

    private readonly List<InteractionTarget> interactionTargets = new List<InteractionTarget>();

    private void PrepareTargets()
    {
        // 若上一段對話的淡入尚未結束，必須保留最初的透明度與碰撞狀態。
        foreach (var target in interactionTargets)
            if (target.captured) return;

        interactionTargets.Clear();
        var roots = new HashSet<GameObject>();
        foreach (var target in targetsToHide ?? new DialogueVisibilityTarget[0])
        {
            if (target == null || !roots.Add(target.gameObject)) continue;
            interactionTargets.Add(new InteractionTarget
            {
                sprite = target.sprite,
                colliders = target.colliders
            });
        }
    }

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
        if (DialogueManager.isConversationActive) OnConversationStarted(null);
    }

    void OnDestroy()
    {
        foreach (var target in interactionTargets)
        {
            if (target == null) continue;
            target.fadeTween?.Kill();
            if (target.captured && target.sprite != null)
            {
                Color color = target.sprite.color;
                color.a = target.originalAlpha;
                target.sprite.color = color;
            }
            target.RestoreColliders();
        }
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
        if (!_isHidden) return;
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
        PrepareTargets();

        // CanvasGroup 控制
        foreach (var cg in canvasGroups ?? new CanvasGroup[0])
        {
            if (cg == null) continue;
            cg.DOKill();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        foreach (var target in interactionTargets)
        {
            if (target == null) continue;
            target.fadeTween?.Kill();
            target.fadeTween = null;
            // 淡入尚未完成就開始下一段對話時，保留最初狀態，不記錄中途的透明度。
            if (!target.captured)
            {
                target.originalAlpha = target.sprite != null ? target.sprite.color.a : 1f;
                target.savedColliders = target.colliders != null
                    ? (Collider2D[])target.colliders.Clone() : new Collider2D[0];
                target.colliderStates = new bool[target.savedColliders.Length];
                for (int i = 0; i < target.savedColliders.Length; i++)
                    target.colliderStates[i] = target.savedColliders[i] != null && target.savedColliders[i].enabled;
                target.captured = true;
            }
            foreach (var col in target.savedColliders)
                if (col != null) col.enabled = false;

            if (target.sprite != null)
            {
                target.sprite.DOKill();
                Color color = target.sprite.color;
                color.a = 0f;
                target.sprite.color = color;
            }
        }
    }

    /// <summary>
    /// 對話結束：淡入恢復
    /// </summary>
    void SetVisible()
    {
        // CanvasGroup 淡入
        foreach (var cg in canvasGroups ?? new CanvasGroup[0])
        {
            if (cg == null) continue;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.DOFade(1f, fadeInDuration).SetEase(fadeInEase);
        }

        foreach (var target in interactionTargets)
        {
            if (target == null || !target.captured) continue;

            // Sprite 淡入
            if (target.sprite != null)
            {
                target.fadeTween = target.sprite.DOFade(target.originalAlpha, Mathf.Max(0f, fadeInDuration))
                    .SetEase(fadeInEase)
                    .OnComplete(() =>
                    {
                        // 淡入完成才恢復原本的碰撞狀態，不誤開原本停用的區域。
                        target.fadeTween = null;
                        target.RestoreColliders();
                    });
            }
            else
            {
                target.RestoreColliders();
            }
        }
    }
}
