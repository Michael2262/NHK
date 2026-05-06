using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// 條件式按鈕列表呼叫器（放在各場景）
/// 
/// 管理場景中的圓形按鈕，點擊後將對應的 ButtonData 列表
/// 傳送給不卸載場景上的 ResponseListService 單例來生成 UI。
/// 關閉時自動恢復圓形按鈕顯示。
/// </summary>
public class ConditionalButtonListCaller : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  資料結構
    // ══════════════════════════════════════════

    [Serializable]
    public class ButtonGroup
    {
        [Header("圓形按鈕")]
        public Button circleButton;

        [Header("列表出現位置")]
        [Tooltip("此圓按鈕開啟列表時的面板位置（預設中間）")]
        public ResponseListService.ListPosition listPosition = ResponseListService.ListPosition.Center;

        [Header("此圓按鈕對應的選項列表")]
        public List<ResponseListService.ButtonData> entries = new List<ResponseListService.ButtonData>();
    }

    // ══════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════

    [Header("按鈕群組（可自由增減）")]
    [SerializeField] private List<ButtonGroup> groups = new List<ButtonGroup>();

    [Header("UI 引用")]
    [Tooltip("放所有圓形按鈕的容器")]
    [SerializeField] private GameObject circleButtonContainer;

    // ══════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════

    private void Start()
    {
        for (int i = 0; i < groups.Count; i++)
        {
            int index = i;
            if (groups[i].circleButton != null)
            {
                groups[i].circleButton.onClick.AddListener(() => OnCircleButtonClicked(index));
            }
        }
        ShowCircleButtons();
    }

    private void OnDestroy()
    {
        foreach (var group in groups)
        {
            if (group.circleButton != null)
                group.circleButton.onClick.RemoveAllListeners();
        }
    }

    // ══════════════════════════════════════════
    //  核心邏輯
    // ══════════════════════════════════════════

    private void OnCircleButtonClicked(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count) return;
        if (ResponseListService.Instance == null)
        {
            Debug.LogWarning("ResponseListService 單例不存在，請確認不卸載場景已載入。");
            return;
        }

        HideCircleButtons();

        ResponseListService.Instance.Show(
            groups[groupIndex].entries,
            onClose: () => ShowCircleButtons(),
            position: groups[groupIndex].listPosition
        );
    }

    // ══════════════════════════════════════════
    //  圓按鈕顯示控制
    // ══════════════════════════════════════════

    private void ShowCircleButtons()
    {
        if (circleButtonContainer != null)
            circleButtonContainer.SetActive(true);
    }

    private void HideCircleButtons()
    {
        if (circleButtonContainer != null)
            circleButtonContainer.SetActive(false);
    }
}