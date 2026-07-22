using UnityEngine;
using DG.Tweening;
using PixelCrushers;                       // UITextField
using PixelCrushers.DialogueSystem;        // FormattedText

/// <summary>
/// 單一系統公告（Toast）的視圖元件，掛在 toast prefab 上。
///
/// 由 <see cref="AlertToastManager"/> Instantiate 後呼叫 <see cref="Init"/>：
///   1. 用 FormattedText.Parse 解析 [em2] 等 emphasis 標籤（沿用原生 Alert 的著色）。
///   2. 淡入 → 停留 duration 秒 → 向管理器「請求消失」。
///   3. 由管理器依 staggerInterval 錯開呼叫 <see cref="PlayFadeOut"/>，淡出完成後標記 <see cref="IsFinished"/>。
///   4. 淡出後「不」立即銷毀，保留 clone 佔住排版位置；等整批公告都淡出完成，
///      再由管理器 <see cref="DestroySelf"/> 一次清除 —— 這樣公告從出現到消失都不會跑位置。
///
/// 之所以「到期不自己淡出、而是請求管理器」，是為了讓「消失」跟「出現」共用同一個時間差
/// （多則同時到期時逐一消失，而非一起消失）。
///
/// 只做「顯示 / 動畫」，不含任何遊戲邏輯（符合 Bridge/UI 只轉接顯示的慣例）。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AlertToast : MonoBehaviour
{
    [Header("參照")]
    [Tooltip("公告文字（可指向 uGUI Text 或 TMP，沿用 Pixel Crushers UITextField）")]
    [SerializeField] private UITextField label;

    [Tooltip("控制淡入淡出的 CanvasGroup（留空會自動抓同物件上的）")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("動畫")]
    [Tooltip("淡入時間（秒）")]
    [SerializeField] private float fadeInTime = 0.2f;

    [Tooltip("淡出時間（秒）")]
    [SerializeField] private float fadeOutTime = 0.3f;

    [Tooltip("冒出時的起始縮放（1 = 不縮放）。localScale 動畫不會與 LayoutGroup 打架")]
    [SerializeField] private float popFromScale = 0.92f;

    [Tooltip("duration <= 0（無限）時的保底停留秒數")]
    [SerializeField] private float fallbackDuration = 3f;

    private AlertToastManager _manager;
    private Sequence _sequence;
    private bool _dismissing; // 已進入淡出流程，避免重複觸發
    private bool _finished;   // 淡出已完成（但尚未銷毀，等整批一起清）

    /// <summary>是否已進入淡出流程（含淡出中與淡出完成）。供上限管理排除「正在離場」的公告。</summary>
    public bool IsDismissing => _dismissing;

    /// <summary>淡出是否已完成（alpha 已為 0，等待整批銷毀）。</summary>
    public bool IsFinished => _finished;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>由管理器呼叫：設定文字並開始生命週期動畫。</summary>
    public void Init(AlertToastManager manager, string message, float duration)
    {
        _manager = manager;

        if (label != null)
            label.text = FormattedText.Parse(message).text;

        float life = (duration >= 0f) ? duration : fallbackDuration;

        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * popFromScale;

        // 用 SetUpdate(true) 走 unscaled time，遊戲暫停（timeScale=0）時公告仍會動
        _sequence = DOTween.Sequence().SetUpdate(true);
        _sequence.Append(canvasGroup.DOFade(1f, fadeInTime));
        _sequence.Join(transform.DOScale(1f, fadeInTime).SetEase(Ease.OutBack));
        _sequence.AppendInterval(life);
        _sequence.OnComplete(RequestDismiss); // 停留結束 → 請求管理器安排錯開淡出
    }

    /// <summary>停留時間到：不自己淡出，回報管理器由它錯開安排。</summary>
    private void RequestDismiss()
    {
        if (_dismissing || _finished) return;
        if (_manager != null) _manager.RequestDismiss(this);
        else PlayFadeOut(); // 沒有管理器（保底）就自己淡出
    }

    /// <summary>
    /// 實際淡出。由管理器呼叫：
    ///   - 到期時：依 staggerInterval 錯開逐一呼叫（消失也有時間差）
    ///   - 超過上限被擠掉時：立即呼叫
    /// 淡出完成後只標記 IsFinished，「不」銷毀（保留位置），等整批一起清。
    /// </summary>
    public void PlayFadeOut()
    {
        if (_dismissing || _finished) return;
        _dismissing = true;
        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetUpdate(true);
        _sequence.Append(canvasGroup.DOFade(0f, fadeOutTime));
        _sequence.OnComplete(OnFadeOutComplete);
    }

    private void OnFadeOutComplete()
    {
        if (_finished) return;
        _finished = true;
        if (canvasGroup != null) canvasGroup.alpha = 0f; // 確保完全透明
        // 不銷毀：保留 clone 佔住排版位置，避免其它公告因 LayoutGroup 重排而跑位。
        // 由管理器在「整批都淡出完成」時呼叫 DestroySelf 一次清除。
        if (_manager != null) _manager.NotifyFinished(this);
    }

    /// <summary>由管理器在整批公告都結束後呼叫，實際銷毀 clone。</summary>
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
    }
}
