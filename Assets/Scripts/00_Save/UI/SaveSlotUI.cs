// 檔案:SaveSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI 文本元素")]
    [SerializeField] private TextMeshProUGUI text_Title;
    [SerializeField] private TextMeshProUGUI text_GameDay;
    [SerializeField] private TextMeshProUGUI text_Timestamp;

    [Header("UI 狀態物件")]
    [SerializeField] private GameObject occupiedSlotInfo;
    [SerializeField] private GameObject emptySlotInfo;

    [Header("功能")]
    [SerializeField] private Button mainButton;

    [Header("時段顯示 (v3.1 新增)")]
    [Tooltip("時段對應表,用於顯示「DAY X 早上/中午/晚上/深夜」")]
    [SerializeField] private TimeMappingSO timeMapping;
    [Tooltip("顯示時段的多語系 LocalizeUI 元件(Pixel Crushers)。掛在要顯示時段文字的 TMP 上。")]
    [SerializeField] private LocalizeUI phaseLocalizeUI;
    [Tooltip("若沒有 LocalizeUI,也可指定一個 TMP 直接顯示 phaseName 作為 fallback")]
    [SerializeField] private TextMeshProUGUI text_Phase;

    private int _slotIndex;
    private SaveLoadMenu _menuController;

    public void Populate(int slotIndex, SaveSlotMetaData metaData, SaveLoadMenu menuController)
    {
        _slotIndex = slotIndex;
        _menuController = menuController;
        bool isEmpty = (metaData == null || metaData.IsEmpty);

        if (_slotIndex == SaveGameManager.AUTOSAVE_SLOT_INDEX)
        {
            text_Title.text = "AUTO SAVE";
        }
        else
        {
            text_Title.text = $"SAVE {_slotIndex:D2}";
        }

        emptySlotInfo.SetActive(isEmpty);
        occupiedSlotInfo.SetActive(!isEmpty);

        if (!isEmpty)
        {
            // ★ v3.1 改動:顯示「DAY X」+ 時段
            int displayDay = CalculateDisplayDay(metaData);
            text_GameDay.text = $"DAY {displayDay}";

            // 時段顯示(多語系)
            UpdatePhaseDisplay(metaData);

            text_Timestamp.text = metaData.SaveTimestamp;
        }

        mainButton.onClick.RemoveAllListeners();
        mainButton.onClick.AddListener(OnSlotClicked);
    }

    /// <summary>
    /// 計算要顯示的天數,考慮 TimeMappingSO 的視覺換日設定
    /// </summary>
    private int CalculateDisplayDay(SaveSlotMetaData metaData)
    {
        // 舊存檔相容:如果沒有存 PhaseIndex(預設 -1),直接用 GameDay
        if (metaData.PhaseIndex < 0 || timeMapping == null)
        {
            return metaData.GameDay;
        }

        // 如果已達視覺換日門檻,顯示天數 + 1
        if (timeMapping.ShouldShowNextDay(metaData.PhaseIndex, metaData.SlotInPhase))
        {
            return metaData.GameDay + 1;
        }

        return metaData.GameDay;
    }

    /// <summary>
    /// 更新時段顯示,優先使用 LocalizeUI 做多語系
    /// </summary>
    private void UpdatePhaseDisplay(SaveSlotMetaData metaData)
    {
        // 舊存檔相容:若沒存 PhaseIndex,清空時段顯示
        if (metaData.PhaseIndex < 0 || timeMapping == null)
        {
            if (phaseLocalizeUI != null) phaseLocalizeUI.fieldName = string.Empty;
            if (text_Phase != null) text_Phase.text = string.Empty;
            return;
        }

        // 取得多語系 Key
        string locKey = timeMapping.GetPhaseLocalizationKey(metaData.PhaseIndex);

        if (phaseLocalizeUI != null && !string.IsNullOrEmpty(locKey))
        {
            // 使用 Pixel Crushers LocalizeUI
            phaseLocalizeUI.fieldName = locKey;
            phaseLocalizeUI.UpdateText();
        }
        else if (text_Phase != null)
        {
            // Fallback:直接顯示 Key(通常是 phaseName)
            text_Phase.text = locKey;
        }
    }

    private void OnSlotClicked()
    {
        Debug.Log($"<color=yellow>Debug:</color> 槽位 {_slotIndex} 被點擊了!");

        if (_menuController != null)
        {
            _menuController.OnSlotSelected(_slotIndex);
        }
    }
}