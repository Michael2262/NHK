using TMPro;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>掛在單筆任務 Prefab 上，只負責呈現 QuestTrackerUI 傳入的資料。</summary>
[DisallowMultipleComponent]
[AddComponentMenu("NHK/Quest/Quest Tracker Item UI")]
public sealed class QuestTrackerItemUI : MonoBehaviour
{
    [Header("貼紙獎勵")]
    [SerializeField] private TMP_Text stickerCountTMP;
    [SerializeField] private GameObject stickerIllustration;

    [Header("任務內容")]
    [SerializeField] private TMP_Text infoTMP;
    [SerializeField] private TMP_Text progressTMP;

    [Header("任務狀態文字")]
    [Tooltip("顯示明日開始／未完成／今日未完成／今日已完成／已完成。請使用常駐的 TMP 物件。")]
    [SerializeField] private TMP_Text statusTMP;

    private TMP_Text _progressColorTarget;
    private Color _normalProgressColor;

    public void Render(QuestTrackerItemState item, string completedTextKey = null,
        string completedTodayTextKey = null, string incompleteTextKey = null, string incompleteTodayTextKey = null,
        bool showTomorrow = false, string tomorrowTextKey = "Quest.Tomorrow")
    {
        if (item == null) return;
        if (stickerCountTMP != null)
        {
            stickerCountTMP.text = $"x{item.StickerCount}";
            SetVisible(stickerCountTMP.gameObject, item.ShowStickerCount);
        }
        SetVisible(stickerIllustration, item.ShowSticker);
        if (infoTMP != null) infoTMP.text = Localize(item.TitleTextKey);
        bool completedToday = item.ShowDailyProgress && item.PerformedToday;
        if (statusTMP != null)
        {
            string key = showTomorrow ? tomorrowTextKey
                : item.IsCompleted ? completedTextKey
                : !item.ShowDailyProgress ? incompleteTextKey
                : completedToday ? completedTodayTextKey : incompleteTodayTextKey;
            statusTMP.text = Localize(key);
            statusTMP.color = showTomorrow ? UIColorPalette.Gray
                : item.IsCompleted ? UIColorPalette.Green
                : completedToday ? UIColorPalette.Orange : UIColorPalette.Gray;
        }
        if (progressTMP != null)
        {
            // 記住此 TMP 的原色，扣回次數或重用項目後能恢復，不能沿用完成時的綠色。
            if (_progressColorTarget != progressTMP)
            {
                _progressColorTarget = progressTMP;
                _normalProgressColor = progressTMP.color;
            }
            progressTMP.text = $"{item.CurrentCount}/{item.RequiredCount}";
            progressTMP.color = item.IsCompleted ? UIColorPalette.Green : _normalProgressColor;
        }
    }

    private static void SetVisible(GameObject target, bool visible)
    {
        if (target != null && target.activeSelf != visible) target.SetActive(visible);
    }

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        string text = DialogueManager.GetLocalizedText(key);
        if (!string.IsNullOrEmpty(text)) return text;
        Debug.LogWarning($"[QuestTrackerItemUI] Text Table 找不到 Key：{key}", this);
        return key;
    }
}
