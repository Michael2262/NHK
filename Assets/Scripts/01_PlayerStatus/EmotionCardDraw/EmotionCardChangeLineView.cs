using TMPro;
using UnityEngine;

/// <summary>
/// HeroineEmotionCardChangeView 用的一列變化顯示。
/// 建議做成 row prefab：左邊 cardRoot 放 EmotionCard，右邊 deltaText 顯示 +1 / -1。
/// </summary>
public class EmotionCardChangeLineView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform cardRoot;
    [SerializeField] private TextMeshProUGUI deltaText;

    private EmotionCardView currentCard;

    private void Reset()
    {
        cardRoot = transform;
        deltaText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(EmotionCardView cardPrefab, HeroineEmotionCardType type, int delta, bool showRepresentativeText)
    {
        ClearCard();

        if (cardRoot == null) cardRoot = transform;

        if (cardPrefab != null)
        {
            currentCard = Instantiate(cardPrefab, cardRoot);
            currentCard.Setup(type, showRepresentativeText);
        }

        if (deltaText != null)
            deltaText.text = delta >= 0 ? $"+{delta}" : delta.ToString();
    }

    public void ClearCard()
    {
        if (currentCard != null)
        {
            Destroy(currentCard.gameObject);
            currentCard = null;
        }
    }
}
