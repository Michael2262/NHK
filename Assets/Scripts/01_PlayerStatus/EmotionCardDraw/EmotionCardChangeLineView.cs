using TMPro;
using UnityEngine;

/// <summary>
/// HeroineEmotionCardChangeView 用的一列變化顯示。
/// 建議做成 row prefab: 左邊 cardRoot 放 EmotionCard,右邊 deltaText 顯示 +1 / -1。
/// 本列不顯示情緒代表字,只顯示卡面與變化數值。
///
/// 注意: Instantiate 出來的 EmotionCard 會被強制重設 RectTransform,置中於 cardRoot,
/// 避免每張 prefab 自己的 anchor / anchoredPosition 設定造成位置跑掉。
/// </summary>
public class EmotionCardChangeLineView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform cardRoot;
    [SerializeField] private TextMeshProUGUI deltaText;

    private EmotionCard currentCard;

    private void Reset()
    {
        cardRoot = transform;
        deltaText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(EmotionCard cardPrefab, int delta)
    {
        ClearCard();

        if (cardRoot == null) cardRoot = transform;

        if (cardPrefab != null)
        {
            currentCard = Instantiate(cardPrefab, cardRoot);
            ResetTransform(currentCard.transform);
            currentCard.Setup(false);
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

    private static void ResetTransform(Transform t)
    {
        if (t is RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
        else
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
    }
}