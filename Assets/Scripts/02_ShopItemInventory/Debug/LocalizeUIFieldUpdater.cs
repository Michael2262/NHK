using UnityEngine;
using PixelCrushers.Wrappers; // 引用 LocalizeUI 的命名空間

/// <summary>
/// 【修正版 2.0 - 允許自訂測試欄位】
/// 提供 API (UpdateLocalizationField) 來動態更新 LocalizeUI 的 Field Name。
/// 您可以在 Inspector 的 "Test Field Name" 欄位輸入文字，
/// 然後用右鍵點擊組件標題來執行測試。
/// </summary>
[RequireComponent(typeof(LocalizeUI))] // 確保此腳本與 LocalizeUI 在同一個物件上
public class LocalizeUIFieldUpdater : MonoBehaviour
{
    [Header("測試用的欄位")]
    [Tooltip("請在這裡輸入您想在 Play Mode 中測試的 Field Name")]
    public string testFieldName = "YOUR_FIELD_NAME_HERE"; // 允許您在 Inspector 填寫

    private LocalizeUI m_localizeUI;

    void Awake()
    {
        // 啟動時，自動抓取 LocalizeUI 組件
        m_localizeUI = GetComponent<LocalizeUI>();
    }

    /// <summary>
    /// 【這就是您要的 API 方法】
    /// 更新 LocalizeUI 的 Field Name，並立即刷新顯示的文字。
    /// </summary>
    /// <param name="newFieldName">要顯示的新欄位名稱 (必須存在於您的 Text Table 中)</param>
    public void UpdateLocalizationField(string newFieldName)
    {
        if (m_localizeUI == null)
        {
            Debug.LogWarning("找不到 LocalizeUI 組件。", this);
            return;
        }

        // 1. 【修正】直接設定 'fieldName' 公開屬性
        // ( 'fieldName' 是 LocalizeUI 基礎類別的屬性 )
        m_localizeUI.fieldName = newFieldName;

        // 2. 呼叫 UpdateText() 來強制立即刷新 UI 上的文字
        // ( 'UpdateText' 是 LocalizeUI 基礎類別的方法 )
        m_localizeUI.UpdateText();
    }


    // --- 以下為 Play Mode 測試用的方法 ---

    [ContextMenu("測試：使用上方 'Test Field Name' 的值來更新文字")]
    private void TestSetFieldFromInspector()
    {
        if (string.IsNullOrEmpty(testFieldName))
        {
            Debug.LogWarning("Test Field Name 欄位是空的，請先填寫要測試的 Field Name。", this);
            return;
        }

        Debug.Log($"[測試] 正在設定 Field Name 為: {testFieldName}");
        UpdateLocalizationField(testFieldName);
    }
}