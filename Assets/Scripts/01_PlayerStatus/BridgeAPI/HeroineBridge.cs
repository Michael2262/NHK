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
/// - Libido 性慾值
/// - HCount H 次數
/// - Emotion Card 情緒卡池
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
    [Tooltip("給無參數按鈕方法使用的預設性慾變化量。")]
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
    // Libido / 性慾
    // ==========================================================

    public void AddLibido(int amount) => H?.AddLibido(amount);
    public void ReduceLibido(int amount) => H?.AddLibido(-Mathf.Abs(amount));
    public void SetLibido(int value) => H?.SetLibido(value);

    public void AddLibidoDefault() => AddLibido(defaultLibidoAmount);
    public void ReduceLibidoDefault() => ReduceLibido(defaultLibidoAmount);
    public void SetLibidoZero() => SetLibido(0);
    public void SetLibidoMax() => SetLibido(HeroineStatusModel.LibidoMax);

    // ==========================================================
    // H Count
    // ==========================================================

    public void AddHCount(int amount) => H?.AddHCount(amount);
    public void ReduceHCount(int amount) => H?.AddHCount(-Mathf.Abs(amount));
    public void SetHCount(int value) => H?.SetHCount(value);

    public void AddHCountDefault() => AddHCount(defaultHCountAmount);
    public void ReduceHCountDefault() => ReduceHCount(defaultHCountAmount);
    public void SetHCountZero() => SetHCount(0);

    // ==========================================================
    // Emotion Card - Generic
    // ==========================================================

    /// <summary>
    /// 新增一張情緒卡。HeroineStatusModel 內部會依規則替換卡池中的卡。
    /// 若 Unity Inspector 無法選 enum 參數，請改用下方 AddAngryCard / AddShyCard 等明確方法。
    /// </summary>
    public void AddEmotionCard(HeroineEmotionCardType type) => H?.AddEmotionCard(type);

    public void ReplaceEmotionCard(HeroineEmotionCardType type) => H?.ReplaceEmotionCard(type);

    public void RemoveOneCardOfType(HeroineEmotionCardType type)
    {
        var heroine = H;
        if (heroine == null) return;

        bool removed = heroine.RemoveOneCardOfType(type);
        if (!removed)
        {
            Debug.Log($"[HeroineBridge] {heroine.HeroineID} 沒有可移除的 {type} 情緒卡。", this);
        }
    }

    public void RemoveOldestCard() => H?.RemoveOldestCard();

    // ==========================================================
    // Emotion Card - Explicit Button Methods
    // ==========================================================

    public void AddAngryCard() => AddEmotionCard(HeroineEmotionCardType.Angry);
    public void AddShyCard() => AddEmotionCard(HeroineEmotionCardType.Shy);
    public void AddWorriedCard() => AddEmotionCard(HeroineEmotionCardType.Worried);
    public void AddMaternalCard() => AddEmotionCard(HeroineEmotionCardType.Maternal);
    public void AddRelaxedCard() => AddEmotionCard(HeroineEmotionCardType.Relaxed);
    public void AddDisappointedCard() => AddEmotionCard(HeroineEmotionCardType.Disappointed);

    public void RemoveAngryCard() => RemoveOneCardOfType(HeroineEmotionCardType.Angry);
    public void RemoveShyCard() => RemoveOneCardOfType(HeroineEmotionCardType.Shy);
    public void RemoveWorriedCard() => RemoveOneCardOfType(HeroineEmotionCardType.Worried);
    public void RemoveMaternalCard() => RemoveOneCardOfType(HeroineEmotionCardType.Maternal);
    public void RemoveRelaxedCard() => RemoveOneCardOfType(HeroineEmotionCardType.Relaxed);
    public void RemoveDisappointedCard() => RemoveOneCardOfType(HeroineEmotionCardType.Disappointed);

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
