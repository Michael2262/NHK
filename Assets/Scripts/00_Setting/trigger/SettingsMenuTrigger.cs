using UnityEngine;

/// <summary>
/// 簡易 API 觸發器：用於從 Inspector、UnityEvents 或外部系統呼叫 SettingsMenu 單例
/// </summary>
public class SettingsMenuTrigger : MonoBehaviour
{
    /// <summary>
    /// 開啟設定畫面 (供 Button OnClick 或 Event 使用)
    /// </summary>
    public void Open()
    {
        if (SettingsMenu.Instance != null)
        {
            SettingsMenu.Instance.Open();
        }
        else
        {
            Debug.LogWarning("SettingsMenuTrigger: 找不到 SettingsMenu 實體！");
        }
    }

    /// <summary>
    /// 關閉設定畫面
    /// </summary>
    public void Close()
    {
        if (SettingsMenu.Instance != null)
        {
            SettingsMenu.Instance.Close();
        }
        else
        {
            Debug.LogWarning("SettingsMenuTrigger: 找不到 SettingsMenu 實體！");
        }
    }

    /// <summary>
    /// 切換設定畫面狀態 (開變關，關變開)
    /// </summary>
    public void Toggle()
    {
        if (SettingsMenu.Instance != null)
        {
            SettingsMenu.Instance.Toggle();
        }
        else
        {
            Debug.LogWarning("SettingsMenuTrigger: 找不到 SettingsMenu 實體！");
        }
    }
}