using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// 注意：不要 using PixelCrushers.DialogueSystem，它有自己的 Toggle 型別，會跟 UnityEngine.UI.Toggle 撞名
using DialogueManager = PixelCrushers.DialogueSystem.DialogueManager;

/// <summary>
/// 任務目標清單 UI。
/// - Toggle 切換顯示「未完成（已顯示）」或「已完成」兩類。
/// - 項目生成在 contentRoot 下（建議掛在 ScrollRect 的 Content 上，滑桿由場景自行配置）。
/// - 文字用 DialogueManager.GetLocalizedText(TextTableKey) 查多語系表。
///
/// 場景掛法：
///   1. Panel 上掛本腳本，指定 contentRoot / itemPrefab。
///   2. showCompletedToggle 指到切換用的 Toggle（isOn = 顯示已完成清單）；
///      不指定也可以，改用 SetShowCompleted(bool) 綁按鈕。
/// </summary>
public class QuestObjectiveListUI : MonoBehaviour
{
    [Header("必要參照")]
    [Tooltip("項目生成的父物件（通常是 ScrollRect 的 Content）")]
    [SerializeField] private Transform contentRoot;

    [Tooltip("單一目標的 prefab（需掛 QuestObjectiveItemUI）")]
    [SerializeField] private QuestObjectiveItemUI itemPrefab;

    [Header("切換")]
    [Tooltip("切換用 Toggle：isOn = 顯示已完成清單、isOff = 顯示未完成清單。可留空改用 SetShowCompleted。")]
    [SerializeField] private Toggle showCompletedToggle;

    [Tooltip("目前是否顯示「已完成」清單（無 Toggle 時的初始狀態）")]
    [SerializeField] private bool showCompleted = false;

    private readonly List<QuestObjectiveItemUI> _spawnedItems = new List<QuestObjectiveItemUI>();

    // ==========================================================
    // 生命週期
    // ==========================================================

    private void OnEnable()
    {
        var service = GameStatusService.Instance;
        if (service != null)
        {
            service.QuestObjectives.OnObjectiveChanged += HandleObjectiveChanged;
            service.OnGameStatusLoaded += Refresh;
        }

        if (showCompletedToggle != null)
        {
            showCompleted = showCompletedToggle.isOn;
            showCompletedToggle.onValueChanged.AddListener(HandleToggleChanged);
        }

        Refresh();
    }

    private void OnDisable()
    {

        var service = GameStatusService.Instance;
        if (service != null)
        {
            service.QuestObjectives.OnObjectiveChanged -= HandleObjectiveChanged;
            service.OnGameStatusLoaded -= Refresh;
        }

        if (showCompletedToggle != null)
            showCompletedToggle.onValueChanged.RemoveListener(HandleToggleChanged);
    }

    // ==========================================================
    // 對外 API（可綁 Button / Toggle / FSM）
    // ==========================================================

    /// <summary> 切換顯示類別：true = 已完成、false = 未完成（已顯示）。 </summary>
    public void SetShowCompleted(bool value)
    {
        if (showCompleted == value) return;
        showCompleted = value;
        Refresh();
    }

    // ==========================================================
    // 內部邏輯
    // ==========================================================

    private void HandleToggleChanged(bool isOn) => SetShowCompleted(isOn);

    private void HandleObjectiveChanged(string id, QuestObjectiveState state)
    {
        // 任一目標變動都整批重建（清單量級小，不需要做差異更新）
        Refresh();
    }

    private void Refresh()
    {
        ClearItems();

        var service = GameStatusService.Instance;
        if (service == null || contentRoot == null || itemPrefab == null) return;

        var targetState = showCompleted ? QuestObjectiveState.Completed : QuestObjectiveState.Revealed;
        var defs = service.QuestObjectives.GetObjectives(targetState);

        foreach (var def in defs)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(GetLocalizedText(def), targetState == QuestObjectiveState.Completed);
            _spawnedItems.Add(item);
        }
    }

    private void ClearItems()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    /// <summary> 查 TextTable 取多語系文字；查不到時退回 Key 本身，方便在畫面上直接看出漏填。 </summary>
    private string GetLocalizedText(QuestObjectiveDefinition def)
    {
        if (string.IsNullOrEmpty(def.TextTableKey))
        {
            Debug.LogWarning($"[QuestObjectiveListUI] 目標「{def.ObjectiveID}」沒有設定 TextTableKey。");
            return def.ObjectiveID;
        }

        string localized = DialogueManager.GetLocalizedText(def.TextTableKey);
        return string.IsNullOrEmpty(localized) ? def.TextTableKey : localized;
    }
}
