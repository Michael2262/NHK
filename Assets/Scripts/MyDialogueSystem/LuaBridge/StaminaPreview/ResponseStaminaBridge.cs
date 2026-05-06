using UnityEngine;
using PixelCrushers.DialogueSystem;

public class ResponseStaminaBridge : MonoBehaviour
{
    private StandardUIResponseButton _uiButton;
    private StaminaPreviewHoverTrigger _hoverTrigger;

    void Awake()
    {
        _uiButton = GetComponent<StandardUIResponseButton>();
        _hoverTrigger = GetComponent<StaminaPreviewHoverTrigger>();
    }

    // 當選單面板打開，按鈕被啟用時執行
    void OnEnable()
    {
        // 稍微延遲一下，確保 Dialogue System 已經把 Response 資料填入按鈕
        Invoke(nameof(UpdateStaminaValue), 0.05f);
    }

    void UpdateStaminaValue()
    {
        if (_uiButton == null || _uiButton.response == null || _hoverTrigger == null) return;

        // 從對話節點(destinationEntry)的欄位中抓取 "StaminaPreview"
        // 如果沒填，預設會是 0
        int delta = Field.LookupInt(_uiButton.response.destinationEntry.fields, "StaminaPreview");

        // 將數值設定給你的工具腳本
        _hoverTrigger.SetUseRouter(false); // 確保不使用 Router 模式
        _hoverTrigger.SetPreviewDelta(delta);
    }
}