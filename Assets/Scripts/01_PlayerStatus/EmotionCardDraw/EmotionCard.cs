using TMPro;
using UnityEngine;

/// <summary>
/// 情緒卡 prefab 的最小控制腳本。
///
/// 一張 EmotionCard 就是一個已經做好的 prefab / GameObject:
/// - 圖片請直接在 prefab 裡設定。
/// - 情緒代表字請直接在 prefab 裡設定。
/// - 本腳本不會依照 HeroineEmotionCardType 改文字,也不會改圖片。
///
/// 使用情境:
/// - EmotionCardDrawView (抽選表演): 關閉情緒代表字。
/// - EmotionCardChangeLineView (情緒卡變化提示一列): 關閉情緒代表字,只顯示卡面與數值。
///
/// 註: 本類別是單張情緒卡 prefab 的最小控制腳本。
/// </summary>
public class EmotionCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI representativeText;

    private void Reset()
    {
        root = gameObject;
        representativeText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(bool showRepresentativeText)
    {
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
        if (representativeText != null)
            representativeText.enabled = visible;
    }
}
