using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 數值變動 Preview 的「序列播放器」。
/// 各 UI 收到 Model 變動事件時，不再自行立即顯示，而是把「更新數字本體 + 顯示飄字」入列；
/// 本類別在同一幀收齊所有變動，依 order 排序後，每隔 stepInterval 逐一播放。
///
/// 設計重點：
///   - 同幀收集：第一筆入列時排程一個「等一幀」的 flush，把同幀的變動全收齊再排序。
///   - 全局序列化：單一 worker coroutine 逐一處理 queue，後到的批次接在後面，不互相覆蓋。
///   - 數字本體與飄字一起跳：applyValue 與飄字在同一個 step 觸發。
///
/// 取得方式：StatusPreviewSequencer.Instance（場景沒有就自動建立一個）。
/// 若想在 Inspector 調整間隔/淡入淡出，可手動把本元件掛到 MainUI 的某個 GameObject 上。
/// </summary>
[DefaultExecutionOrder(-100)]
public class StatusPreviewSequencer : MonoBehaviour
{
    // ===== 優先序常數（數字越小越先播） =====
    public const int OrderStress = 0;
    public const int OrderLifePower = 1;
    public const int OrderSociality = 2;
    public const int OrderDependency = 3;
    // 主角四項之後、女主角之前；先 Room 再 Body。
    public const int OrderRoomClean = 4;
    public const int OrderBodyClean = 5;
    public const int OrderHeroineTrust = 10;
    public const int OrderHeroineLibido = 11;
    public const int OrderHeroineEmotionAdded = 12;
    public const int OrderHeroineCurrentEmotion = 13;

    [Header("=== 播放設定 ===")]
    [Tooltip("每個 Preview 之間的間隔秒數。")]
    [SerializeField] private float stepInterval = 0.3f;
    [Tooltip("飄字停留秒數。")]
    [SerializeField] private float holdSeconds = 1.0f;
    [Tooltip("飄字淡出秒數。")]
    [SerializeField] private float fadeSeconds = 0.35f;

    [Header("=== 音效 ===")]
    [Tooltip("每次飄字彈出時播放的音效 Key（對應 AudioManager）。留空則不播。")]
    [SerializeField] private string previewSoundKey = "preview";

    private struct Req
    {
        public int order;
        public Action applyValue;        // 更新本體（數字 / 標籤），在該 step 觸發
        public TextMeshProUGUI floaty;   // 飄字目標
        public string floatyText;        // 要顯示的字；null = 不改字（純開關模式，保留原文字）
        public bool hasFloaty;           // 是否要播放飄字（false = 只跳本體）
    }

    private readonly List<Req> _pending = new List<Req>();   // 本幀收集中
    private readonly Queue<Req> _queue = new Queue<Req>();    // 待播放
    private readonly Dictionary<TextMeshProUGUI, Tween> _tweens = new Dictionary<TextMeshProUGUI, Tween>();
    private bool _flushScheduled;
    private Coroutine _worker;

    private static StatusPreviewSequencer _instance;
    public static StatusPreviewSequencer Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(StatusPreviewSequencer));
                _instance = go.AddComponent<StatusPreviewSequencer>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// 數字 delta 飄字（壓力 / 信任 / 性慾…）。delta == 0 視為無變動、直接忽略。
    /// preview 為 null（例如被正/負顯示設定擋掉）時，仍會跳數字本體、只是不冒飄字。
    /// </summary>
    /// <param name="order">優先序，越小越先播。</param>
    /// <param name="applyValue">在該筆播放時呼叫，用來更新數字本體（本體與飄字同時跳）。</param>
    /// <param name="preview">飄字目標；傳 null 表示只跳數字、不顯示飄字。</param>
    /// <param name="delta">變動量（決定飄字 +/- 與文字）。</param>
    public void Enqueue(int order, Action applyValue, TextMeshProUGUI preview, int delta)
    {
        if (delta == 0) return;
        bool show = preview != null;
        string text = show ? (delta > 0 ? $"+{delta}" : delta.ToString()) : null;
        EnqueueCore(order, applyValue, preview, text, show);
    }

    /// <summary>自訂文字飄字（例如情緒名稱 + ↑）。</summary>
    public void EnqueueText(int order, TextMeshProUGUI floaty, string text, Action applyValue = null)
    {
        if (floaty == null || string.IsNullOrEmpty(text))
        {
            if (applyValue != null) EnqueueCore(order, applyValue, null, null, false);
            return;
        }
        EnqueueCore(order, applyValue, floaty, text, true);
    }

    /// <summary>純開關飄字：不改字、只閃一下（例如 CurrentEmotion 變化）。floatyText 傳 null 即保留原文字。</summary>
    public void EnqueueToggle(int order, TextMeshProUGUI floaty, Action applyValue = null)
    {
        if (floaty == null)
        {
            if (applyValue != null) EnqueueCore(order, applyValue, null, null, false);
            return;
        }
        EnqueueCore(order, applyValue, floaty, null, true);
    }

    private void EnqueueCore(int order, Action applyValue, TextMeshProUGUI floaty, string floatyText, bool hasFloaty)
    {
        _pending.Add(new Req
        {
            order = order,
            applyValue = applyValue,
            floaty = floaty,
            floatyText = floatyText,
            hasFloaty = hasFloaty
        });

        if (!_flushScheduled)
        {
            _flushScheduled = true;
            StartCoroutine(FlushNextFrame());
        }
    }

    /// <summary>停止所有播放、清空 queue 並 kill 所有 tween。場景卸載 / 面板關閉時呼叫。</summary>
    public static void CancelAllIfExists()
    {
        if (_instance != null) _instance.CancelAll();
    }

    public void CancelAll()
    {
        _pending.Clear();
        _queue.Clear();
        _flushScheduled = false;

        if (_worker != null)
        {
            StopCoroutine(_worker);
            _worker = null;
        }

        foreach (var kv in _tweens) kv.Value?.Kill();
        _tweens.Clear();
    }

    private IEnumerator FlushNextFrame()
    {
        yield return null; // 等一幀，把同幀的變動全收齊
        _flushScheduled = false;

        _pending.Sort((a, b) => a.order.CompareTo(b.order));
        foreach (var r in _pending) _queue.Enqueue(r);
        _pending.Clear();

        if (_worker == null) _worker = StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        while (_queue.Count > 0)
        {
            PlayStep(_queue.Dequeue());
            yield return new WaitForSeconds(stepInterval);
        }
        _worker = null;
    }

    private void PlayStep(Req r)
    {
        r.applyValue?.Invoke();              // 本體（數字 / 標籤）在此刻跳
        if (r.hasFloaty) PlayFloaty(r.floaty, r.floatyText);
    }

    // floatyText 為 null = 不改字（純開關，保留 Inspector 設定的固定文字）；非 null = 設定該字。
    private void PlayFloaty(TextMeshProUGUI target, string floatyText)
    {
        if (target == null) return;

        // 每次飄字彈出時播放音效（純跳數字、無飄字的 step 不會進到這裡）。
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(previewSoundKey))
            AudioManager.Instance.PlaySound(previewSoundKey);

        if (_tweens.TryGetValue(target, out var old) && old != null) old.Kill();

        if (floatyText != null) target.text = floatyText;
        var c = target.color;
        c.a = 1f;
        target.color = c;
        target.gameObject.SetActive(true);

        Tween tween = DOTween.Sequence()
            .AppendInterval(holdSeconds)
            .Append(target.DOFade(0f, fadeSeconds))
            .OnComplete(() => { if (target != null && floatyText != null) target.text = ""; });

        _tweens[target] = tween;
    }
}
