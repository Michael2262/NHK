using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    // ★ 單例實體
    public static SettingsMenu Instance { get; private set; }

    [Header("核心 UI 控制")]
    [Tooltip("請將包含所有設定選項、背景的父物件拖到這裡")]
    [SerializeField] private GameObject mainPanel;

    void Awake()
    {
        // ★ 單例初始化邏輯
        if (Instance == null)
        {
            Instance = this;
            // 如果希望設定選單跨場景存在，可取消註解下一行
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化時確保 UI 是關閉的
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    // ==========================================================
    // 開啟與關閉介面
    // ==========================================================

    public void Open()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            // 這裡可以加入初始化設定值的邏輯，例如：RefreshSettings();
        }
    }

    public void Close()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    /// <summary>
    /// 切換開關狀態 (常用於按下 Esc 鍵時)
    /// </summary>
    public void Toggle()
    {
        if (mainPanel != null)
        {
            if (mainPanel.activeSelf) Close();
            else Open();
        }
    }

    // 你可以在這裡繼續保留或撰寫原本已寫好的設定邏輯 (音量、解析度等)
}