using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 場景遮幕的顯示元件。掛在獨立 Canvas 下的全畫面 Image 上。
/// Canvas 排序需介於立繪與常駐 UI 之間；生命週期由常駐場景管理。
/// 共用 GameStatusService 的 TimeConfig 配色，不參與 SceneController 的全畫面轉場。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class SceneCurtain : MonoBehaviour
{
    [Header("遮幕動畫")]
    [Tooltip("Curtain 指令省略秒數、傳入 0 或負數時使用；獨立於全畫面 Fade。設為 0 可立即完成。")]
    [Min(0f)]
    [SerializeField] private float defaultFadeDuration = 0.15f;

    // 只保存視覺操作的完成狀態，不屬於遊戲資料或存檔。
    public sealed class FadeOperation
    {
        public bool IsDone { get; internal set; }
        internal bool Cover;
        internal float Duration;
        internal int PhaseIndex;
    }

    private Image _image;
    private CanvasGroup _group;
    private readonly Queue<FadeOperation> _pending = new Queue<FadeOperation>();
    private FadeOperation _current;
    private bool _isProcessing;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _group = GetComponent<CanvasGroup>();
        _image.raycastTarget = false;
        _group.interactable = false;
        _group.blocksRaycasts = false;
        _group.alpha = 0f;
    }

    /// <summary>依目前或指定 Phase 配色壓幕；完成後保持遮蔽，直到 Reveal。</summary>
    public FadeOperation Cover(float duration = -1f, int phaseIndex = -1)
    {
        return Enqueue(true, duration, phaseIndex);
    }

    /// <summary>保留壓幕時的 RGB，將遮幕淡化至完全透明。</summary>
    public FadeOperation Reveal(float duration = -1f)
    {
        return Enqueue(false, duration, -1);
    }

    private FadeOperation Enqueue(bool cover, float duration, int phaseIndex)
    {
        var operation = new FadeOperation
        {
            Cover = cover,
            Duration = ResolveDuration(duration),
            PhaseIndex = phaseIndex
        };

        if (!isActiveAndEnabled || _image == null || _group == null)
        {
            Debug.LogWarning("[SceneCurtain] 遮幕未啟用或缺少 Image／CanvasGroup，略過動畫。", this);
            operation.IsDone = true;
            return operation;
        }

        _pending.Enqueue(operation);
        if (!_isProcessing)
        {
            _isProcessing = true;
            StartCoroutine(ProcessQueue());
        }
        return operation;
    }

    private float ResolveDuration(float duration)
    {
        float result = duration > 0f ? duration : defaultFadeDuration;
        return float.IsNaN(result) || float.IsInfinity(result) ? 0.15f : Mathf.Max(0f, result);
    }

    private IEnumerator ProcessQueue()
    {
        // 動畫由遮幕持有，對話 Sequence 結束也會繼續；重疊指令依收到的順序播放。
        while (_pending.Count > 0)
        {
            _current = _pending.Dequeue();
            if (_current.Cover)
            {
                Color color = ResolveColor(_current.PhaseIndex);
                color.a = 1f; // 透明度統一交給 CanvasGroup，確保壓幕能完全遮蔽。
                _image.color = color;
            }

            float target = _current.Cover ? 1f : 0f;
            float from = _group.alpha;
            float elapsed = 0f;
            while (elapsed < _current.Duration && from != target)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / _current.Duration));
                yield return null;
            }
            _group.alpha = target;
            _current.IsDone = true;
            _current = null;
        }
        _isProcessing = false;
    }

    private Color ResolveColor(int phaseIndex)
    {
        var service = GameStatusService.Instance;
        var config = service != null ? service.TimeConfig : null;
        if (config == null || (phaseIndex < 0 && service.Time == null))
        {
            Debug.LogWarning("[SceneCurtain] 無法取得 TimeConfig 或目前時段，使用白色遮幕。", this);
            return Color.white;
        }

        int index = phaseIndex < 0 ? service.Time.CurrentPhaseIndex : phaseIndex;
        if (config.PhaseFadeColors == null || index < 0 || index >= config.PhaseFadeColors.Count)
        {
            Debug.LogWarning($"[SceneCurtain] Phase {index} 沒有對應的轉場顏色，使用白色遮幕。", this);
            return Color.white;
        }
        return config.GetPhaseFadeColor(index);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        // 停用或卸載後解除指令等待，重新啟用時不留下半透明遮幕。
        if (_current != null) _current.IsDone = true;
        _current = null;
        while (_pending.Count > 0) _pending.Dequeue().IsDone = true;
        _isProcessing = false;
        if (_group != null) _group.alpha = 0f;
    }
}
