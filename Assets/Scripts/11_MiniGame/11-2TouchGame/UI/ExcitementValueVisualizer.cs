using UnityEngine;
using TMPro;
using HutongGames.PlayMaker;
using DG.Tweening;
using PixelCrushers;
using System.Linq;
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 職責：監控興奮度數值，並根據 UIStageSettings 執行文字地道化、顏色變換與彈跳動畫。
/// </summary>
public class ExcitementValueVisualizer : MonoBehaviour
{
    [Header("1. 數據與配置來源")]
    public PlayMakerFSM eroMinigameFSM;
    [Tooltip("拖入 UIStageSettings 資產 (例如：Excitement_PinkTheme)")]
    public UIStageSettings settings;

    [Header("2. Dialogue System 地道化")]
    public TextTable textTable;
    [Tooltip("Text Table 中的 Key，例如 'System.Excitement'")]
    public string excitementField = "System.Excitement";

    [Header("3. UI 元件參考")]
    [SerializeField] private TextMeshProUGUI excitementValueText;

    [Header("4. 動畫參數")]
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float colorFadeDuration = 0.20f;

    // --- 內部變數 ---
    private FsmInt fsmLocalExcitement;
    private string excitementFormat = "興奮度：{0}";
    private int lastValue = -1;
    private RectTransform textRect;

    void Start()
    {
        if (excitementValueText != null)
            textRect = excitementValueText.GetComponent<RectTransform>();

        if (eroMinigameFSM == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 尚未指定 eroMinigameFSM。");
            return;
        }

        RefreshLocalization();

        // 取得 PlayMaker 區域變數
        fsmLocalExcitement = eroMinigameFSM.FsmVariables.FindFsmInt("fsm_LocalExcitement");

        if (fsmLocalExcitement != null)
        {
            lastValue = fsmLocalExcitement.Value;
            UpdateVisuals(false); // 初始刷新，不播放動畫
        }
    }

    public void RefreshLocalization()
    {
        if (textTable != null && textTable.HasField(excitementField))
        {
            excitementFormat = textTable.GetFieldText(excitementField);
        }
    }

    void Update()
    {
        if (fsmLocalExcitement == null) return;

        // 偵測 FSM 數值變化
        if (fsmLocalExcitement.Value != lastValue)
        {
            lastValue = fsmLocalExcitement.Value;
            UpdateVisuals(true); // 觸發動畫
        }
    }

    private void UpdateVisuals(bool playAnimation)
    {
        if (excitementValueText == null || settings == null || settings.stages.Count == 0) return;

        // 1. 查找目前最符合的階段 (從高門檻往低找)
        var activeStage = settings.stages
            .OrderByDescending(s => s.threshold)
            .FirstOrDefault(s => lastValue >= s.threshold) ?? settings.stages[0];

        // 2. 更新地道化文字
        excitementValueText.text = string.Format(excitementFormat, lastValue);

        // 3. 執行視覺效果
        if (playAnimation && gameObject.activeInHierarchy)
        {
            // 顏色淡入動畫
            excitementValueText.DOColor(activeStage.stageColor, colorFadeDuration);

            // 彈跳動畫 (參考 MinigameUI 邏輯)
            if (textRect != null)
            {
                textRect.DOKill();
                float halfDuration = punchDuration / 2f;

                DOTween.Sequence()
                    .Append(textRect.DOScale(Vector3.one * activeStage.punchScale, halfDuration).SetEase(Ease.OutQuad))
                    .Append(textRect.DOScale(Vector3.one * activeStage.baseScale, halfDuration).SetEase(Ease.InQuad));
            }
        }
        else
        {
            // 直接設定最終狀態 (用於初始化或不可見時)
            excitementValueText.color = activeStage.stageColor;
            if (textRect != null) textRect.localScale = Vector3.one * activeStage.baseScale;
        }
    }

    private void OnDisable()
    {
        // 清理動畫，避免內存洩漏或報錯
        excitementValueText?.DOKill();
        textRect?.DOKill();
    }
}