using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Linq;
using HutongGames.PlayMaker; // 必須引用以存取全域變數

/// <summary>
/// 職責：在小遊戲中顯示並監控 PlayMaker 全域變數 "global_Suspicion" 的 UI 表現。
/// 視覺規則繼承自 SuspicionSettings 資產。
/// </summary>
public class MinigameUI : MonoBehaviour
{
    [Header("Suspicion FX (Referencing Asset)")]
    [UnityEngine.Tooltip("拖入已建立好的 SuspicionSettings Asset 檔案")]
    [SerializeField] private SuspicionSettings suspicionSettings;

    [Space(5)]
    [SerializeField] private TextMeshProUGUI textSuspicion;
    [SerializeField] private TextMeshProUGUI textSuspicionTitle;

    [Header("Animation Settings")]
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float colorFadeDuration = 0.20f;

    private RectTransform _textSuspicionRect;
    private FsmInt _globalSuspicion;
    private int _lastValue;

    private void Start()
    {
        // 1. 初始化元件與全域變數引用
        if (textSuspicion != null)
            _textSuspicionRect = textSuspicion.GetComponent<RectTransform>();

        // 取得 PlayMaker 全域變數
        _globalSuspicion = PlayMakerGlobals.Instance.Variables.FindFsmInt("global_Suspicion");

        if (_globalSuspicion != null)
        {
            _lastValue = _globalSuspicion.Value;
            UpdateSuspicionUI(false); // 初始刷新，不撥放動畫
        }
        else
        {
            Debug.LogWarning("[MinigameUI] 找不到 PlayMaker 全域變數 'global_Suspicion'。");
        }
    }

    private void Update()
    {
        // 2. 每幀監控數值變化 (由於全域變數沒有 C# 事件，使用 Polling 方式)
        if (_globalSuspicion == null) return;

        if (_globalSuspicion.Value != _lastValue)
        {
            _lastValue = _globalSuspicion.Value;
            UpdateSuspicionUI(true); // 數值改變，觸發動畫
        }
    }

    private void UpdateSuspicionUI(bool playAnimation)
    {
        if (textSuspicion == null || _textSuspicionRect == null || suspicionSettings == null || suspicionSettings.stages.Count == 0)
            return;

        float currentValue = _lastValue;

        // 3. 繼承自 LobbyUI 的階段查找邏輯
        var activeStage = suspicionSettings.stages
            .OrderByDescending(s => s.threshold)
            .FirstOrDefault(s => currentValue >= s.threshold) ?? suspicionSettings.stages[0];

        textSuspicion.text = $"{currentValue}%";

        if (playAnimation && gameObject.activeInHierarchy)
        {
            // 4. 執行顏色與縮放動畫
            textSuspicion.DOColor(activeStage.stageColor, colorFadeDuration);
            if (textSuspicionTitle != null)
                textSuspicionTitle.DOColor(activeStage.stageColor, colorFadeDuration);

            _textSuspicionRect.DOKill();
            float halfDuration = punchDuration / 2f;

            DOTween.Sequence()
                .Append(_textSuspicionRect.DOScale(Vector3.one * activeStage.punchScale, halfDuration).SetEase(Ease.OutQuad))
                .Append(_textSuspicionRect.DOScale(Vector3.one * activeStage.baseScale, halfDuration).SetEase(Ease.InQuad));
        }
        else
        {
            // 直接設定狀態
            textSuspicion.color = activeStage.stageColor;
            if (textSuspicionTitle != null) textSuspicionTitle.color = activeStage.stageColor;
            _textSuspicionRect.localScale = Vector3.one * activeStage.baseScale;
        }
    }

    private void OnDisable()
    {
        // 安全清理動畫
        textSuspicion?.DOKill();
        textSuspicionTitle?.DOKill();
        _textSuspicionRect?.DOKill();
    }
}