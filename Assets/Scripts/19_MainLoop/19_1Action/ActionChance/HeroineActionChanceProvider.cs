using UnityEngine;

/// <summary>
/// 女主角相關行動的成功率計算器。
///
/// 公式：
///   成功率 = 100 - 主導情緒基礎失敗率 - 操作獨立失敗率 + 女主角性慾隱值
///   最終結果轉為 0~1 並由 minChance / maxChance 夾住。
///
/// 設定方式：
///   1. 拖入 EmotionFailureRateConfig（ScriptableObject），裡面填每種情緒的基礎失敗率。
///   2. 在 Inspector 填入 operationFailureRate（操作獨立失敗率）。
///   3. 指定 heroineID，Provider 會自動從 GameStatusService 取得對應女主角。
/// </summary>
public class HeroineActionChanceProvider : ActionChanceProvider
{
    [Header("女主角設定")]
    [Tooltip("目標女主角的 ID。用於從 GameStatusService 取得 HeroineStatusModel。")]
    public string heroineID;

    [Header("情緒失敗率設定")]
    [Tooltip("各主導情緒對應的基礎失敗率 config。右鍵 Create → ActionChance → EmotionFailureRateConfig 建立。")]
    public EmotionFailureRateConfig emotionFailureRateConfig;

    [Header("操作獨立失敗率")]
    [Tooltip("此操作本身固有的失敗率（百分比，例如 20 = 20%）。每個 Provider 可以填不同值。")]
    [Range(0f, 100f)]
    public float operationFailureRate = 20f;

    [Header("重複點擊失敗率")]
    [Tooltip("每次操作成功後，額外累加的失敗率（百分比）。")]
    [Range(0f, 100f)]
    public float repeatPenaltyPerSuccess = 10f;

    [Tooltip("重複失敗率的上限（百分比）。")]
    [Range(0f, 100f)]
    public float repeatPenaltyMax = 50f;

    [SerializeField, Range(0f, 100f)]
    private float currentRepeatPenalty = 0f;

    /// <summary>
    /// 取得指定的女主角 Model。子類別也能用。
    /// </summary>
    protected HeroineStatusModel GetHeroine()
    {
        var heroines = GameStatusService.Instance?.Heroines;
        if (heroines == null) return null;
        heroines.TryGetValue(heroineID, out var heroine);
        return heroine;
    }

    protected override float CalculateChance(ProtagonistStatusModel protagonist)
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null)
        {
            Debug.LogWarning($"{GetType().Name}: 找不到 HeroineID=\"{heroineID}\" 的女主角，使用 fallback。", this);
            return GetFallbackChance();
        }

        if (emotionFailureRateConfig == null)
        {
            Debug.LogWarning($"{GetType().Name}: 未指定 EmotionFailureRateConfig，情緒失敗率視為 0。", this);
        }

        float emotionFailure = emotionFailureRateConfig != null
            ? emotionFailureRateConfig.GetFailureRate(heroine.CurrentEmotion)
            : 0f;

        float libidoBonus = heroine.Libido;

        // 成功率 = 100 - 情緒失敗率 - 操作失敗率 - 重複失敗率 + 性慾隱值
        float successPercent = 100f - emotionFailure - operationFailureRate - currentRepeatPenalty + libidoBonus;

        // 轉為 0~1
        return successPercent / 100f;
    }

    protected override float GetFallbackChance() => 0.3f;

    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null)
            return $"{GetType().Name}: chance={chance:P0}, HeroineID={heroineID} (NOT FOUND)";

        float emotionFailure = emotionFailureRateConfig != null
            ? emotionFailureRateConfig.GetFailureRate(heroine.CurrentEmotion)
            : 0f;

        return $"{GetType().Name}: chance={chance:P0}, " +
               $"Heroine={heroineID}, Emotion={heroine.CurrentEmotion}, " +
               $"EmotionFail={emotionFailure}%, OpFail={operationFailureRate}%, " +
               $"RepeatPenalty={currentRepeatPenalty}%, Libido={heroine.Libido}";
    }

    // ─────────────────────────────────────────────────────────────
    // 事件訂閱：女主角情緒或性慾變動時自動刷新顯示
    // ─────────────────────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeHeroineEvents();
        SceneController.OnSceneChanged += ResetRepeatPenalty;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeHeroineEvents();
        SceneController.OnSceneChanged -= ResetRepeatPenalty;
    }

    // ─────────────────────────────────────────────────────────────
    // 重複點擊失敗率
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 操作成功後呼叫此方法，累加重複失敗率。
    /// 用法：將此方法掛到 ActionOverlayTrigger 的 onSuccessEvents。
    /// </summary>
    public void NotifySuccess()
    {
        currentRepeatPenalty = Mathf.Min(currentRepeatPenalty + repeatPenaltyPerSuccess, repeatPenaltyMax);

        // 成功後立即刷新顯示，讓玩家看到成功率已下降
        if (chanceDisplay != null)
            CalculateSuccessChance();
    }

    /// <summary>
    /// 重置重複失敗率。場景切換時自動呼叫。
    /// </summary>
    public void ResetRepeatPenalty()
    {
        currentRepeatPenalty = 0f;

        if (chanceDisplay != null)
            CalculateSuccessChance();
    }

    private void SubscribeHeroineEvents()
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null) return;

        heroine.OnCurrentEmotionChanged += OnHeroineValueChanged;
        heroine.OnLibidoChanged += OnHeroineIntValueChanged;
    }

    private void UnsubscribeHeroineEvents()
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null) return;

        heroine.OnCurrentEmotionChanged -= OnHeroineValueChanged;
        heroine.OnLibidoChanged -= OnHeroineIntValueChanged;
    }

    private void OnHeroineValueChanged(HeroineEmotionCardType _)
    {
        if (chanceDisplay == null) return;
        CalculateSuccessChance();
    }

    private void OnHeroineIntValueChanged(int _)
    {
        if (chanceDisplay == null) return;
        CalculateSuccessChance();
    }
}