using UnityEngine;

/// <summary>
/// 簡易 API 觸發器：用於從 Inspector、UnityEvents 或外部系統呼叫 SaveLoadMenu 單例
/// </summary>
public class SaveLoadTrigger : MonoBehaviour
{
    

    /// <summary>
    /// 開啟存檔畫面 (供 Button OnClick 或 Event 使用)
    /// </summary>
    public void OpenSave()
    {
        if (SaveLoadMenu.Instance != null)
        {
            SaveLoadMenu.Instance.OpenSaveScreen();
        }
        else
        {
            Debug.LogWarning("SaveLoadTrigger: 找不到 SaveLoadMenu 實體！");
        }
    }

    /// <summary>
    /// 開啟讀檔畫面 (供 Button OnClick 或 Event 使用)
    /// </summary>
    public void OpenLoad()
    {
        if (SaveLoadMenu.Instance != null)
        {
            SaveLoadMenu.Instance.OpenLoadScreen();
        }
        else
        {
            Debug.LogWarning("SaveLoadTrigger: 找不到 SaveLoadMenu 實體！");
        }
    }

    /// <summary>
    /// 關閉選單
    /// </summary>
    public void Close()
    {
        if (SaveLoadMenu.Instance != null)
        {
            SaveLoadMenu.Instance.CloseMenu();
        }
    }

    /// <summary>
    /// 切換開關 (若開則關，若關則開存檔)
    /// </summary>
    public void ToggleSave()
    {
        // 這裡假設你有在 SaveLoadMenu 增加一個判斷是否開啟的屬性
        // 如果沒有，最簡單的方式是直接呼叫 OpenSave
        OpenSave();
    }

    // 直接開啟存檔所在的實體資料夾
    [ContextMenu("Debug/開啟存檔資料夾")]
    public void OpenSaveFolder()
    {
        if (SaveLoadMenu.Instance != null)
        {
            SaveLoadMenu.Instance.OpenSaveFolder();
        }
    }
}