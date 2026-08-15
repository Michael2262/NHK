using UnityEngine;
using TMPro;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 提示字顯示器（固定位置，一個場景一個）。
///
/// 掛在一個固定位置的 TMP 文字上。TeaseHoverTip 在 hover 時呼叫 Show(key)，
/// 這裡用 Text Table 查表後把字顯示在這個固定位置；離開時 Hide 隱藏。
///
/// 文字用 Pixel Crushers Text Table 動態查表（DialogueManager.GetLocalizedText），
/// 每次顯示都重新查，切語言後再次 hover 就會是新語言。
/// ⚠️ 這個 label 物件不要再掛 LocalizeUI（會和程式塞字互相覆蓋）。
/// </summary>
public class TeaseHoverTipDisplay : MonoBehaviour
{
    /// <summary>場景內單例。小遊戲場景卸載時自動清空。</summary>
    public static TeaseHoverTipDisplay Instance { get; private set; }

    [Tooltip("顯示提示字的 TMP 文字（固定位置）。")]
    [SerializeField] private TMP_Text label;

    [Tooltip("沒有提示時要隱藏的根物件；留空則隱藏 label 自己。")]
    [SerializeField] private GameObject tipRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[TeaseHoverTipDisplay] 場上已有一個實例，銷毀重複的 {name}。", this);
            Destroy(this);
            return;
        }

        Instance = this;
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>用 Text Table key 顯示提示。</summary>
    public void Show(string key)
    {
        if (label == null || string.IsNullOrEmpty(key)) return;

        label.text = Localize(key);
        SetVisible(true);
    }

    /// <summary>隱藏提示。</summary>
    public void Hide() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (tipRoot != null) tipRoot.SetActive(visible);
        else if (label != null) label.gameObject.SetActive(visible);
    }

    private string Localize(string key)
    {
        string text = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[TeaseHoverTipDisplay] Text Table 找不到 Key: {key}");
            return key; // 查不到時 fallback 顯示 key 本身
        }
        return text;
    }
}
