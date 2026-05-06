using UnityEngine;
using UnityEngine.UI;

public enum MenuMode
{
    Save,
    Load
}

public class SaveLoadMenu : MonoBehaviour
{
    // ★ 單例實體
    public static SaveLoadMenu Instance { get; private set; }

    [Header("核心 UI 控制")]
    [Tooltip("請將包含所有背景、按鈕的父物件拖到這裡")]
    [SerializeField] private GameObject mainPanel;

    [Header("列表配置")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private int totalSlots = 30;

    [Header("UI 標題物件")]
    [SerializeField] private GameObject saveTitleUI;
    [SerializeField] private GameObject loadTitleUI;

    [Header("Popup 多語系 Key")]
    [Tooltip("覆蓋存檔確認訊息的 Localization Key")]
    [SerializeField] private string overwriteMessageKey = "System.ConfirmOverwrite";
    [Tooltip("讀檔確認訊息的 Localization Key")]
    [SerializeField] private string loadMessageKey = "System.ConfirmLoad";

    [Header("存檔通知")]
    [SerializeField] private SaveNotification saveNotification;

    [Header("Debug 工具")]
    [SerializeField] private bool enableDebugButtons = true;

    // 當前模式：存檔或讀取
    public MenuMode CurrentMode { get; set; } = MenuMode.Load;

    private CoreSystemBridge _bridge;

    void Awake()
    {
        // ★ 單例初始化邏輯
        if (Instance == null)
        {
            Instance = this;
            // 如果希望存檔選單跨場景存在，可取消註解下一行
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 提前尋找 Bridge
        _bridge = FindAnyObjectByType<CoreSystemBridge>();

        if (_bridge == null)
        {
            Debug.LogError("SaveLoadMenu 找不到 CoreSystemBridge！");
        }

        // 初始化時確保 UI 是關閉的，但腳本是運作的
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    // ==========================================================
    // 開啟與關閉介面
    // ==========================================================

    public void OpenSaveScreen()
    {
        CurrentMode = MenuMode.Save;
        UpdateTitleAndShow();
    }

    public void OpenLoadScreen()
    {
        CurrentMode = MenuMode.Load;
        UpdateTitleAndShow();
    }

    private void UpdateTitleAndShow()
    {
        if (saveTitleUI != null) saveTitleUI.SetActive(CurrentMode == MenuMode.Save);
        if (loadTitleUI != null) loadTitleUI.SetActive(CurrentMode == MenuMode.Load);

        if (mainPanel != null) mainPanel.SetActive(true);
        RefreshUI();
    }

    public void CloseMenu()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    // ==========================================================
    // 列表處理邏輯
    // ==========================================================

    public void RefreshUI()
    {
        // 清除舊有的 Slot
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        var saveManager = GameStatusService.Instance?.SaveManager;
        if (saveManager == null) return;

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, contentParent);
            SaveSlotUI slotUI = slotGO.GetComponent<SaveSlotUI>();
            SaveSlotMetaData metaData = saveManager.GetMetaDataForSlot(i);

            // 傳遞自身引用給子物件，以便觸發 OnSlotSelected
            slotUI.Populate(i, metaData, this);
        }
    }

    public void OnSlotSelected(int slotIndex)
    {
        Debug.Log($"[SaveLoadMenu] 點擊槽位: {slotIndex}, 當前模式: {CurrentMode}");

        // 如果發現 Bridge 是空的，嘗試重新尋找一次
        if (_bridge == null)
        {
            _bridge = FindAnyObjectByType<CoreSystemBridge>();
        }

        if (_bridge == null)
        {
            Debug.LogError("[SaveLoadMenu] 嚴重錯誤：場景中依舊找不到 CoreSystemBridge！請檢查物件是否存在。");
            return;
        }

        if (PopupController.Instance == null)
        {
            Debug.LogError("[SaveLoadMenu] 找不到 PopupController！請確認不卸載場景已載入。");
            return;
        }

        var metaData = GameStatusService.Instance.SaveManager.GetMetaDataForSlot(slotIndex);
        bool isEmpty = (metaData == null || metaData.IsEmpty);
        Debug.Log($"[SaveLoadMenu] 該槽位是否為空: {isEmpty}");

        if (CurrentMode == MenuMode.Save)
        {
            if (!isEmpty)
            {
                // 覆蓋確認：透過 PopupController 顯示多語系彈窗
                Debug.Log("[SaveLoadMenu] 彈出覆蓋確認視窗");
                int pendingSlot = slotIndex; // 捕獲到 lambda 閉包中
                PopupController.Instance.ShowConfirmCancel(
                    overwriteMessageKey,
                    onConfirm: () =>
                    {
                        _bridge.SaveGameToSlot(pendingSlot);
                        NotifyManualSave();
                        RefreshUI();
                    },
                    onCancel: null
                );
            }
            else
            {
                Debug.Log("[SaveLoadMenu] 直接執行存檔");
                _bridge.SaveGameToSlot(slotIndex);
                NotifyManualSave();
                RefreshUI();
            }
        }
        else // Load 模式
        {
            if (!isEmpty)
            {
                // 讀檔確認：透過 PopupController 顯示多語系彈窗
                Debug.Log("[SaveLoadMenu] 彈出讀檔確認視窗");
                int pendingSlot = slotIndex;
                PopupController.Instance.ShowConfirmCancel(
                    loadMessageKey,
                    onConfirm: () =>
                    {
                        _bridge.LoadGameFromSlot(pendingSlot);
                        CloseMenu();
                    },
                    onCancel: null
                );
            }
            else
            {
                Debug.Log("[SaveLoadMenu] 點擊了空存檔，讀檔模式下無反應");
            }
        }
    }

    // ==========================================================
    // 存檔通知
    // ==========================================================

    private void NotifyManualSave()
    {
        if (saveNotification != null)
            saveNotification.ShowManualSave();
    }

    // ==========================================================
    //直接開啟存檔所在的實體資料夾 (Debug 用)
    // ==========================================================


    public void OpenSaveFolder()
    {
        // Application.persistentDataPath 是 Unity 預設的存檔路徑
        string path = Application.persistentDataPath;

        // 根據平台開啟資料夾
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            // Windows 使用 explorer
            System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
        }
        else
        {
            // 其他平台 (Mac/Linux) 使用 OpenURL
            Application.OpenURL("file://" + path);
        }

        Debug.Log($"<color=cyan>Debug:</color> 已開啟存檔資料夾：{path}");
    }
}