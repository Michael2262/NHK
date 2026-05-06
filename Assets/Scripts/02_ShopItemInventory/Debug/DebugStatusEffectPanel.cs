using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 職責：監聽狀態效果模型的變化，並動態更新 UI 顯示。
/// </summary>
public class DebugStatusEffectPanel : MonoBehaviour
{
    [Header("UI 預製件與容器")]
    [SerializeField] private GameObject statusEffectTextPrefab; // 拖曳你的 StatusEffectText_Prefab
    [SerializeField] private Transform container;               // 拖曳 StatusEffectPanel_Container

    private ProtagonistStatusEffectModel _statusEffectModel;

    void Start()
    {
        Invoke(nameof(Initialize), 0.1f);
    }

    private void Initialize()
    {
        if (GameStatusService.Instance == null) return;

        _statusEffectModel = GameStatusService.Instance.StatusEffectModel;

        // 訂閱狀態效果列表變化的事件
        _statusEffectModel.OnEffectsChanged += HandleEffectsChanged;

        // 第一次手動刷新，顯示初始狀態
        HandleEffectsChanged();
    }

    private void OnDestroy()
    {
        if (_statusEffectModel != null)
        {
            // 取消訂閱
            _statusEffectModel.OnEffectsChanged -= HandleEffectsChanged;
        }
    }

    /// <summary>
    /// 當效果列表發生變化時，此方法會被觸發。
    /// </summary>
    private void HandleEffectsChanged()
    {
        // 1. 清空所有舊的 UI 物件
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. 獲取最新的效果列表資訊
        var effectInfos = _statusEffectModel.GetActiveEffectInfoForUI();

        // 3. 根據新列表，生成新的 UI 物件
        if (effectInfos.Count == 0)
        {
            // 如果沒有效果，可以顯示一個提示
            var textObj = Instantiate(statusEffectTextPrefab, container);
            textObj.GetComponent<TextMeshProUGUI>().text = "- 無狀態效果 -";
        }
        else
        {
            foreach (var info in effectInfos)
            {
                var textObj = Instantiate(statusEffectTextPrefab, container);
                textObj.GetComponent<TextMeshProUGUI>().text = $"{info.name} (剩餘 {info.days} 天)";
            }
        }
    }
}