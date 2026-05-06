// SaveSystemListener.cs 

using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers; // 確保有這一行

// 這個組件的職責是作為 Dialogue System 的 SaveSystem 和我們自己的 GameStatusService 之間的橋樑。
public class SaveSystemListener : MonoBehaviour
{
    // 在 Unity Inspector 中，我們需要手動將 GameStatusService 物件拖曳到這裡。
    public GameStatusService gameStatusService;

    // OnEnable 會在物件啟用時自動被呼叫。
    void OnEnable()
    {
        // ★ 核心修正：監聽 saveDataApplied 事件，而不是 gameLoaded 事件。
        // saveDataApplied 事件在您提供的 SaveSystem.cs 檔案中是確實存在的。
        SaveSystem.saveDataApplied += OnSaveDataApplied;
    }

    // OnDisable 會在物件停用時自動被呼叫。
    void OnDisable()
    {
        // ★ 同樣，取消對 saveDataApplied 事件的監聽。
        SaveSystem.saveDataApplied -= OnSaveDataApplied;
    }

    // 當 saveDataApplied 事件被觸發時，這個方法就會被執行。
    // 為了清晰起見，我們將方法名稱也改掉。
    private void OnSaveDataApplied()
    {
        Debug.Log("<color=purple>[SaveSystemListener] 接收到 Dialogue System 的 OnSaveDataApplied 事件！</color>");

        if (gameStatusService != null)
        {
            gameStatusService.ApplyDataAfterSceneLoad();   
        }
        else
        {
            Debug.LogError("[SaveSystemListener] 尚未連結 GameStatusService 實例！請在 Inspector 中拖曳物件。");
        }


        if (SceneController.Instance != null)
        {
            SceneController.Instance.ValidatePersistentUI();
        }
        else
        {
            Debug.LogError("[SaveSystemListener] 找不到 SceneController 實例！無法驗證常駐 UI。");
        }
    }
}