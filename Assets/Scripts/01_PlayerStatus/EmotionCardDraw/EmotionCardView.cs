using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 情緒卡 prefab 的最小控制腳本。
///
/// 一張 EmotionCard 建議由「圖像 + 情緒代表字」組成，並把本腳本掛在 prefab 根物件上。
/// - EmotionCardDrawView：會顯示卡圖，但關閉情緒代表字。
/// - HeroineEmotionCardChangeView：會顯示卡圖，也顯示情緒代表字。
/// </summary>
public class EmotionCardView : MonoBehaviour
{
    [Header("Card Data")]
    [SerializeField] private HeroineEmotionCardType cardType = HeroineEmotionCardType.Angry;
    [SerializeField] private string displayNameOverride;

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image cardImage;
    [SerializeField] private Text legacyRepresentativeText;
    [SerializeField] private TextMeshProUGUI representativeText;

    public HeroineEmotionCardType CardType => cardType;

    private void Reset()
    {
        root = gameObject;
        cardImage = GetComponentInChildren<Image>(true);
        representativeText = GetComponentInChildren<TextMeshProUGUI>(true);
        legacyRepresentativeText = GetComponentInChildren<Text>(true);
    }

    private void Awake()
    {
        ApplyDisplayName();
    }

    private void OnValidate()
    {
        ApplyDisplayName();
    }

    public void Setup(HeroineEmotionCardType type, bool showRepresentativeText)
    {
        cardType = type;
        ApplyDisplayName();
        SetRepresentativeTextVisible(showRepresentativeText);
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
        else gameObject.SetActive(visible);
    }

    public void SetRepresentativeTextVisible(bool visible)
    {
        if (representativeText != null) representativeText.enabled = visible;
        if (legacyRepresentativeText != null) legacyRepresentativeText.enabled = visible;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayNameOverride)) return displayNameOverride;
        return GetDefaultDisplayName(cardType);
    }

    private void ApplyDisplayName()
    {
        string displayName = GetDisplayName();

        if (representativeText != null)
            representativeText.text = displayName;

        if (legacyRepresentativeText != null)
            legacyRepresentativeText.text = displayName;
    }

    public static string GetDefaultDisplayName(HeroineEmotionCardType type)
    {
        switch (type)
        {
            case HeroineEmotionCardType.Angry: return "生氣";
            case HeroineEmotionCardType.Shy: return "害羞";
            case HeroineEmotionCardType.Worried: return "擔心";
            case HeroineEmotionCardType.Maternal: return "母性";
            case HeroineEmotionCardType.Relaxed: return "放鬆";
            case HeroineEmotionCardType.Disappointed: return "失望";
            default: return type.ToString();
        }
    }
}
