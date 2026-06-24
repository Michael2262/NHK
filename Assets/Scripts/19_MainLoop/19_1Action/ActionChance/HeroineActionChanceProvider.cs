using UnityEngine;

/// <summary>
/// 女主角相關行動的成功率計算器。
///
/// 公式：
///   成功率 = 100 - 主導情緒基礎失敗率 - 操作獨立失敗率 - 重複失敗率
///            + min(性慾 Libido × 係數A, 性慾加成上限)
///            + min(信任 Trust × 係數B, 信任加成上限)
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

    [Header("數值加成係數")]
    [Tooltip("性慾 Libido 的加成係數 A。最終加成 = Libido × A。可為小數或 0，不可為負數。預設 1。")]
    [Min(0f)]
    public float libidoMultiplier = 1f;

    [Tooltip("信任 Trust 的加成係數 B。最終加成 = Trust × B。可為小數或 0，不可為負數。預設 1。")]
    [Min(0f)]
    public float trustMultiplier = 1f;

    [Tooltip("性慾加成的上限（百分比）。Libido×A 算出的加成最多只到此值，再高也不會超過。預設 10。")]
    [Min(0f)]
    public float libidoBonusMax = 10f;

    [Tooltip("信任加成的上限（百分比）。Trust×B 算出的加成最多只到此值，再高也不會超過。預設 10。")]
    [Min(0f)]
    public float trustBonusMax = 10f;

    [Header("重複點擊失敗率")]
    [Tooltip("每次操作成功後，額外累加的失敗率（百分比）。")]
    [Range(0f, 100f)]
    public float repeatPenaltyPerSuccess = 10f;

    [Tooltip("重複失敗率的上限（百分比）。")]
    [Range(0f, 100f)]
    public float repeatPenaltyMax = 50f;

    [SerializeField, Range(0f, 100f)]
    private float currentRepeatPenalty = 0f;

    [Header("造假重複失敗率（僅影響顯示）")]
    [Tooltip("顯示用的『假』重複失敗率：每次成功後額外累加的造假失敗率（百分比）。\n" +
             "只會影響 chanceDisplay / Dialogue 變數上看到的數字，實際擲骰仍用上面真實的 repeatPenaltyPerSuccess。")]
    [Range(0f, 100f)]
    public float fakeRepeatPenaltyPerSuccess = 10f;

    [Tooltip("造假重複失敗率的上限（百分比）。僅影響顯示。")]
    [Range(0f, 100f)]
    public float fakeRepeatPenaltyMax = 50f;

    [SerializeField, Range(0f, 100f)]
    private float currentFakeRepeatPenalty = 0f;

    [Header("首次必定成功")]
    [Tooltip("勾選後，此 Provider 的第一次成功率強制為 100%（略過 min/max 上下限）。" +
             "收到第一次 NotifySuccess() 後即恢復正常計算。預設關閉。")]
    public bool guaranteeFirstSuccess = false;

    [SerializeField]
    [Tooltip("執行期狀態：首次必成功是否仍待觸發。第一次 NotifySuccess() 後變 false。")]
    private bool firstSuccessPending = true;

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

        // 實際擲骰：使用「真實」的重複失敗率。
        return ComputeChance(heroine, currentRepeatPenalty);
    }

    /// <summary>
    /// 顯示用成功率：除了重複失敗率改用「造假值」外，其餘與真實計算完全相同。
    /// 讓玩家在 chanceDisplay 上看到的危險度與實際擲骰不一致（造假危險度）。
    /// </summary>
    protected override float CalculateDisplayChance(ProtagonistStatusModel protagonist)
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null)
            return GetFallbackChance();

        // 顯示：使用「造假」的重複失敗率。
        return ComputeChance(heroine, currentFakeRepeatPenalty);
    }

    /// <summary>
    /// 成功率公式的共用核心。差別只在傳入的重複失敗率（真實 or 造假）。
    /// 成功率 = 100 - 情緒失敗率 - 操作失敗率 - 重複失敗率 + 性慾加成(Libido×A) + 信任加成(Trust×B)
    /// </summary>
    private float ComputeChance(HeroineStatusModel heroine, float repeatPenalty)
    {
        float emotionFailure = emotionFailureRateConfig != null
            ? emotionFailureRateConfig.GetFailureRate(heroine.CurrentEmotion)
            : 0f;

        // 加成 = 數值 × 係數，但各自夾在上限內（再高的數值也不會超過上限）。
        float libidoBonus = Mathf.Min(heroine.Libido * libidoMultiplier, libidoBonusMax);
        float trustBonus = Mathf.Min(heroine.Trust * trustMultiplier, trustBonusMax);

        float successPercent = 100f - emotionFailure - operationFailureRate - repeatPenalty
                               + libidoBonus + trustBonus;

        // 轉為 0~1
        return successPercent / 100f;
    }

    protected override float GetFallbackChance() => 0.3f;

    /// <summary>
    /// 首次必定成功：在第一次 NotifySuccess() 之前，強制回傳 100% 並略過 min/max 夾制，
    /// 確保是真正的 100%（否則會被 maxChance 夾下來）。其餘情況沿用基底的上下限。
    /// </summary>
    protected override float ApplyChanceLimits(float chance)
    {
        if (guaranteeFirstSuccess && firstSuccessPending)
            return 1f;

        return base.ApplyChanceLimits(chance);
    }

    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null)
            return $"{GetType().Name}: chance={chance:P0}, HeroineID={heroineID} (NOT FOUND)";

        float emotionFailure = emotionFailureRateConfig != null
            ? emotionFailureRateConfig.GetFailureRate(heroine.CurrentEmotion)
            : 0f;

        string firstSuccessTag = (guaranteeFirstSuccess && firstSuccessPending) ? " [首次必成功]" : "";

        return $"{GetType().Name}: chance={chance:P0}{firstSuccessTag}, " +
               $"Heroine={heroineID}, Emotion={heroine.CurrentEmotion}, " +
               $"EmotionFail={emotionFailure}%, OpFail={operationFailureRate}%, " +
               $"RepeatPenalty(真實)={currentRepeatPenalty}%, RepeatPenalty(顯示造假)={currentFakeRepeatPenalty}%, " +
               $"Libido={heroine.Libido}×{libidoMultiplier}, Trust={heroine.Trust}×{trustMultiplier}";
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
        // 首次必定成功：消耗掉這次保證，之後恢復正常計算
        firstSuccessPending = false;

        currentRepeatPenalty = Mathf.Min(currentRepeatPenalty + repeatPenaltyPerSuccess, repeatPenaltyMax);
        currentFakeRepeatPenalty = Mathf.Min(currentFakeRepeatPenalty + fakeRepeatPenaltyPerSuccess, fakeRepeatPenaltyMax);

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
        currentFakeRepeatPenalty = 0f;

        if (chanceDisplay != null)
            CalculateSuccessChance();
    }

    private void SubscribeHeroineEvents()
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null) return;

        heroine.OnCurrentEmotionChanged += OnHeroineValueChanged;
        heroine.OnLibidoChanged += OnHeroineIntValueChanged;
        heroine.OnTrustChanged += OnHeroineIntValueChanged;
    }

    private void UnsubscribeHeroineEvents()
    {
        HeroineStatusModel heroine = GetHeroine();
        if (heroine == null) return;

        heroine.OnCurrentEmotionChanged -= OnHeroineValueChanged;
        heroine.OnLibidoChanged -= OnHeroineIntValueChanged;
        heroine.OnTrustChanged -= OnHeroineIntValueChanged;
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