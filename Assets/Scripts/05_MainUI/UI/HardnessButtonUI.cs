using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 依主角勃起度控制本物件的 CanvasGroup；外層 CanvasGroup 交由 UIVisibilityManager 管理。
/// 掛在 Button 上，Button 本身的 Interactable 須保持開啟。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
[AddComponentMenu("Game/UI/Hardness Button UI")]
public class HardnessButtonUI : MonoBehaviour
{
    [Tooltip("顯示目前勃起度，例如 65%。請指定按鈕底下的 TMP 文字元件。")]
    [SerializeField] private TMP_Text hardnessText;

    [Header("縮放脈動（大於 50 時循環播放）")]
    [Tooltip("勃起度實際增加時立即脈動一次；播放中再次增加會重新觸發。讀檔與重新啟用不算增加。")]
    [SerializeField] private bool pulseOnIncrease = true;
    [Tooltip("相對目前平常大小的放大倍率；1～49 時以原始大小的 80% 為基準。")]
    [Min(1f)]
    [SerializeField] private float pulseScale = 1.05f;
    [Tooltip("放大或縮回各自所需的秒數。")]
    [Min(0.01f)]
    [SerializeField] private float pulseDuration = 0.3f;
    [Tooltip("每次放大縮回後，停留在原始大小的秒數。")]
    [Min(0f)]
    [SerializeField] private float pulseInterval = 1f;

    private CanvasGroup canvasGroup;
    private GameStatusService service;
    private ProtagonistStatusModel model;
    private Sequence pulseSequence;
    private Vector3 originalScale;
    private Vector3 restingScale;
    private int lastHardness;
    private bool pulseIsLooping;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
        restingScale = originalScale;
        // 保留外層對互動與射線的限制，讓場景顯示規則仍能關閉按鈕。
        canvasGroup.ignoreParentGroups = false;
    }

    private void OnEnable() => RefreshUI();

    private void Update()
    {
        // 只檢查資料來源；數值變更由事件刷新，不每幀重寫 CanvasGroup。
        // 支援 UI 先啟用，以及 Service 或 Model 在執行期間重建。
        var current = GameStatusService.Instance;
        var currentModel = current != null ? current.Protagonist : null;
        if (!ReferenceEquals(service, current) || !ReferenceEquals(model, currentModel))
            RefreshUI();
    }

    private void OnDisable()
    {
        Unbind();
        StopPulse();
        transform.localScale = originalScale;
    }

    private void Bind()
    {
        var current = GameStatusService.Instance;
        var currentModel = current != null ? current.Protagonist : null;
        if (ReferenceEquals(service, current) && ReferenceEquals(model, currentModel)) return;

        Unbind();
        service = current;
        model = currentModel;
        lastHardness = model != null ? model.Hardness : 0;
        if (service != null) service.OnGameStatusLoaded += HandleGameStatusLoaded;
        if (model != null) model.OnChanged += HandleModelChanged;
    }

    private void Unbind()
    {
        if (model != null) model.OnChanged -= HandleModelChanged;
        if (!ReferenceEquals(service, null)) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        model = null;
        service = null;
    }

    private void HandleModelChanged()
    {
        if (!isActiveAndEnabled) return;
        int hardness = model != null ? model.Hardness : 0;
        bool increased = hardness > lastHardness;
        lastHardness = hardness;
        ApplyPresentation(hardness, increased);
    }

    private void HandleGameStatusLoaded()
    {
        // 清除上一個進度的動畫，讀檔只同步顯示，不觸發增加脈動。
        StopPulse();
        RefreshUI();
    }

    /// <summary>啟用、數值變更及新遊戲／讀檔完成時刷新，也可由 UnityEvent 手動呼叫。</summary>
    public void RefreshUI()
    {
        if (!isActiveAndEnabled) return;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        Bind();

        // 資料尚未就緒時，先隱藏並停用互動。
        lastHardness = model != null ? model.Hardness : 0;
        ApplyPresentation(lastHardness, false);
    }

    private void ApplyPresentation(int hardness, bool increased)
    {
        bool canInteract = hardness > 0;
        bool isLow = canInteract && hardness < 50;
        canvasGroup.alpha = !canInteract ? 0f : isLow ? 0.3f : 1f;
        canvasGroup.interactable = canInteract;
        canvasGroup.blocksRaycasts = canInteract;

        if (hardnessText != null) hardnessText.text = $"{hardness}%";

        Vector3 nextRestingScale = originalScale * (isLow ? 0.8f : 1f);
        if (restingScale != nextRestingScale || !canInteract)
        {
            StopPulse();
            restingScale = nextRestingScale;
            transform.localScale = restingScale;
        }

        bool shouldLoop = hardness > 50;
        if (increased && pulseOnIncrease)
        {
            // 增加時立即重新脈動；高於門檻則接著維持間歇循環。
            StopPulse();
            StartPulse(shouldLoop);
        }
        else if (shouldLoop)
        {
            StartPulse(true);
        }
        else if (pulseIsLooping)
        {
            StopPulse();
        }
        else if (pulseSequence == null || !pulseSequence.IsActive())
        {
            // 低數值的單次脈動仍在播放時，不被其他數值的刷新打斷。
            transform.localScale = restingScale;
        }
    }

    private void StartPulse(bool loop)
    {
        // 其他主角數值變更也會刷新 UI，不重啟正在播放的動畫。
        if (pulseSequence != null && pulseSequence.IsActive())
        {
            if (pulseIsLooping == loop) return;
            StopPulse();
        }

        pulseIsLooping = loop;
        float duration = Mathf.Max(0.01f, pulseDuration);
        pulseSequence = DOTween.Sequence();
        pulseSequence.SetRecyclable(false);
        pulseSequence.Append(transform.DOScale(restingScale * Mathf.Max(1f, pulseScale), duration)
            .SetEase(Ease.InOutSine));
        pulseSequence.Append(transform.DOScale(restingScale, duration).SetEase(Ease.InOutSine));
        if (loop)
        {
            pulseSequence.AppendInterval(Mathf.Max(0f, pulseInterval));
            pulseSequence.SetLoops(-1, LoopType.Restart);
        }
        pulseSequence.SetUpdate(true);
    }

    private void StopPulse()
    {
        // 只停止自己建立的動畫，避免影響外層 UIVisibilityManager 的淡入淡出。
        if (pulseSequence != null) pulseSequence.Kill();
        pulseSequence = null;
        pulseIsLooping = false;
        transform.localScale = restingScale;
    }
}
