using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 單按鈕式列表呼叫器
/// 
/// 掛在單顆按鈕上，點擊後呼叫 ResponseListService 展開 ButtonList，
/// 僅隱藏自己（不影響場景中其他按鈕）。
/// 關閉時只恢復自己的顯示。
/// </summary>
public class SingleButtonListCaller : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════

    [Header("按鈕（可自動抓同物件的 Button）")]
    [Tooltip("留空時自動取得同 GameObject 上的 Button")]
    [SerializeField] private Button targetButton;

    [Header("列表出現位置")]
    [SerializeField] private ResponseListService.ListPosition listPosition = ResponseListService.ListPosition.Center;

    [Header("選項列表")]
    [SerializeField] private List<ResponseListService.ButtonData> entries = new List<ResponseListService.ButtonData>();

    // ══════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════

    private void Start()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetButton != null)
            targetButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(OnButtonClicked);
    }

    // ══════════════════════════════════════════
    //  核心邏輯
    // ══════════════════════════════════════════

    private void OnButtonClicked()
    {
        if (ResponseListService.Instance == null)
        {
            Debug.LogWarning("[SingleButtonListCaller] ResponseListService 單例不存在，請確認不卸載場景已載入。");
            return;
        }

        // 只隱藏自己
        gameObject.SetActive(false);

        // 呼叫單例服務，關閉時恢復自己
        ResponseListService.Instance.Show(
            entries,
            onClose: () => gameObject.SetActive(true),
            position: listPosition
        );
    }
}
