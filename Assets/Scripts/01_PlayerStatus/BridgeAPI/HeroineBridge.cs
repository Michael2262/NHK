using UnityEngine;

/// <summary>
/// 女主角數值變化 UnityEvent / Button.onClick 橋接工具。
///
/// 用法：
/// 1. 掛在任一 GameObject 上。
/// 2. 在 heroineID 填入目標女主角 ID。
/// 3. Button.onClick() 直接綁定本元件的 public void 方法。
///
/// 目前對應 NHK 版 HeroineStatusModel：
/// - Interest 性興味 / Libido 性欲值
/// - Excitement 獨立興奮度（無性欲加成）
/// - Orgasm 獨立高潮度
/// - Trust 信賴值 / Affinity 好感度
/// - IsInHeat 發情開關
/// - HCount H 次數
/// - CurrentEmotion 主導情緒
///
/// 注意：
/// - 如果 heroineID 留空，會嘗試使用 GameStatusService.Heroines 內的第一位女主角。
/// - 建議正式專案仍填 heroineID，避免多女主角時打錯對象。
/// </summary>
[AddComponentMenu("Game/API/Heroine Bridge")]
public class HeroineBridge : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("目標女主角 ID。留空時會使用 Heroines 字典中的第一位女主角。")]
    [SerializeField] private string heroineID;

    [Header("Default Button Amount")]
    [Tooltip("給無參數按鈕方法使用的預設性欲變化量。")]
    [SerializeField] private int defaultLibidoAmount = 10;

    [Tooltip("給無參數按鈕方法使用的預設 H 次數變化量。")]
    [SerializeField] private int defaultHCountAmount = 1;

    private HeroineStatusModel H
    {
        get
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogWarning("[HeroineBridge] GameStatusService.Instance 尚未初始化。", this);
                return null;
            }

            if (service.Heroines == null || service.Heroines.Count == 0)
            {
                Debug.LogWarning("[HeroineBridge] GameStatusService.Heroines 為空，找不到女主角 Model。", this);
                return null;
            }

            if (!string.IsNullOrEmpty(heroineID))
            {
                if (service.Heroines.TryGetValue(heroineID, out var heroine))
                {
                    return heroine;
                }

                Debug.LogWarning($"[HeroineBridge] 找不到 heroineID = {heroineID} 的女主角。", this);
                return null;
            }

            foreach (var pair in service.Heroines)
            {
                return pair.Value;
            }

            return null;
        }
    }

    // ==========================================================
    // Target Helper
    // ==========================================================

    /// <summary>讓按鈕或事件可以動態切換目標女主角。</summary>
    public void SetHeroineID(string id)
    {
        heroineID = id;
    }

    // ==========================================================
    // Awake
    // ==========================================================

    public float GetAwake() => H?.GetAwake() ?? 0f;
    public void SetAwake(float value) => H?.SetAwake(value);
    public void AddAwake(float amount) => H?.AddAwake(amount);

    // 性興味：獨立數值，範圍與防溢位由 Model 處理。
    public int GetInterest() => H?.GetInterest() ?? 0;
    public void SetInterest(int value) => H?.SetInterest(value);
    public void AddInterest(int amount) => H?.AddInterest(amount);

    // ==========================================================
    // Libido / 性欲
    // ==========================================================

    public void AddLibido(int amount) => H?.AddLibido(amount);
    public void ReduceLibido(int amount) => H?.AddLibido(ToReductionDelta(amount));
    public void SetLibido(int value) => H?.SetLibido(value);

    public void AddLibidoDefault() => AddLibido(defaultLibidoAmount);
    public void ReduceLibidoDefault() => ReduceLibido(defaultLibidoAmount);
    public void SetLibidoZero() => SetLibido(0);
    public void SetLibidoMax() => SetLibido(HeroineStatusModel.LibidoMax);

    // 獨立興奮度：範圍由 Model 處理。
    public int GetExcitement() => H?.GetExcitement() ?? 0;
    public void SetExcitement(int value) => H?.SetExcitement(value);
    public void AddExcitement(int amount) => H?.AddExcitement(amount);
    /// <summary>依輸入的絕對值減少興奮度，不套用性欲倍率。</summary>
    public void ReduceExcitement(int amount) => H?.AddExcitement(ToReductionDelta(amount));
    public void SetExcitementZero() => SetExcitement(0);
    /// <summary>舊綁定相容入口，已移除性欲加成，等同 AddExcitement。</summary>
    public void WeightedAddExcitement(int amount) => H?.AddExcitement(amount);

    // 獨立高潮度
    public int GetOrgasm() => H?.GetOrgasm() ?? 0;
    public void SetOrgasm(int value) => H?.SetOrgasm(value);
    public void AddOrgasm(int amount) => H?.AddOrgasm(amount);
    /// <summary>依輸入的絕對值減少高潮度。</summary>
    public void ReduceOrgasm(int amount) => H?.AddOrgasm(ToReductionDelta(amount));
    public void SetOrgasmZero() => SetOrgasm(0);

    // ==========================================================
    // 信賴 / 好感度：範圍限制由 Model 處理。
    // ==========================================================

    public int GetTrust() => H?.GetTrust() ?? 0;
    public int GetAffinity() => H?.GetAffinity() ?? 0;
    public int GetMood() => H?.GetMood() ?? 0;
    public void SetMood(int value) => H?.SetMood(value);
    public void AddMood(int amount) => H?.AddMood(amount);

    public void AddTrust(int amount) => H?.AddTrust(amount);
    /// <summary>依輸入的絕對值減少信賴。</summary>
    public void ReduceTrust(int amount) => H?.AddTrust(ToReductionDelta(amount));
    public void SetTrust(int value) => H?.SetTrust(value);

    public void AddAffinity(int amount) => H?.AddAffinity(amount);
    /// <summary>依輸入的絕對值減少好感度。</summary>
    public void ReduceAffinity(int amount) => H?.AddAffinity(ToReductionDelta(amount));
    public void SetAffinity(int value) => H?.SetAffinity(value);

    // ==========================================================
    // 發情開關
    // ==========================================================

    public void SetInHeat(bool value) => H?.SetInHeat(value);
    public void EnableInHeat() => SetInHeat(true);
    public void DisableInHeat() => SetInHeat(false);

    // 生理週期：日數與機率由正式 Model 管理，設定機率使用 0～1。
    public string GetCyclePhase() => H?.CurrentCyclePhase.ToString() ?? string.Empty;
    public int GetCyclePhaseDay() => H?.CurrentPhaseDay ?? 0;
    /// <summary>設定當日機率（0～1），例如 0.8 代表 80%；換日後依階段重設。</summary>
    public void SetPregnancyProbability(float value) => H?.SetPregnancyProbability(value);
    public void SetCyclePhase(MenstrualCyclePhase phase) => H?.SetCyclePhase(phase);
    public void BeginBeforePregnant() => H?.SetPregnancyState(PregnancyState.EarlyPregnancy);
    public void SetPregnant() => H?.SetPregnancyState(PregnancyState.Pregnant);
    public void ClearPregnancy() => H?.ClearPregnancy();
    public void TryConceive() => H?.TryConceive();

    // 正數轉為負數，負數維持原值，避免 int.MinValue 取絕對值時溢位。
    private static int ToReductionDelta(int amount) => amount > 0 ? -amount : amount;

    // ==========================================================
    // H Count
    // ==========================================================

    public void AddHCount(int amount) => H?.AddHCount(amount);
    public void ReduceHCount(int amount) => H?.AddHCount(ToReductionDelta(amount));
    public void SetHCount(int value) => H?.SetHCount(value);

    public void AddHCountDefault() => AddHCount(defaultHCountAmount);
    public void ReduceHCountDefault() => ReduceHCount(defaultHCountAmount);
    public void SetHCountZero() => SetHCount(0);

    // ==========================================================
    // Current Emotion / 主導情緒
    // ==========================================================

    public void SetCurrentEmotion(HeroineEmotionCardType emotion) => H?.SetCurrentEmotion(emotion);

    public void SetCurrentEmotionNormal() => SetCurrentEmotion(HeroineEmotionCardType.Normal);
    public void SetCurrentEmotionAngry() => SetCurrentEmotion(HeroineEmotionCardType.Angry);
    public void SetCurrentEmotionShy() => SetCurrentEmotion(HeroineEmotionCardType.Shy);
    public void SetCurrentEmotionWorried() => SetCurrentEmotion(HeroineEmotionCardType.Worried);
    public void SetCurrentEmotionMaternal() => SetCurrentEmotion(HeroineEmotionCardType.Maternal);
    public void SetCurrentEmotionRelaxed() => SetCurrentEmotion(HeroineEmotionCardType.Relaxed);
    public void SetCurrentEmotionDisappointed() => SetCurrentEmotion(HeroineEmotionCardType.Disappointed);
}
