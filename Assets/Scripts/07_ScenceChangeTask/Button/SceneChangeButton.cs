using UnityEngine;

/// <summary>
/// SceneChangeButton
/// 負責處理場景切換前的資料登記與跳轉。
/// 現在不強制要求物件上必須有 Button 組件，可透過 Unity Event 或代碼調用 SwitchScene()。
/// </summary>
public class SceneChangeButton : MonoBehaviour
{
    [Header("場景設定")]
    [SerializeField] private string targetScene = "Stage2";

    [Header("是否更新 Scenario?")]
    [Tooltip("是否要將邏輯地點寫入 Model?如果不是要在遊戲內location移動可關。預設開啟")]
    [SerializeField] private bool updateScenario = true;

    [Header("入口識別")]
    [Tooltip("定義從這個按鈕進入下個場景的『入口ID』，下個場景的 Initializer 會根據此ID執行不同邏輯。")]
    [SerializeField] private string entryID = "DefaultEntry";

    

    /// <summary>
    /// 公用 API：執行場景切換
    /// 可以在 Button 的 OnClick() 中拖入此物件並選擇此方法。
    /// </summary>
    public void SwitchScene()
    {
        // 1. 安全檢查：確保 GameDataManager 存在
        if (GameDataManager.Instance == null)
        {
            Debug.LogError($"[SceneChangeButton] 切換失敗：場景中找不到 GameDataManager 實例！物件：{gameObject.name}", this);
            return;
        }

        // 2. 更新 Scenario 位置狀態
        // 在執行物理跳轉前，先將邏輯地點寫入 Model
        if (ScenarioManager.Instance != null && !string.IsNullOrEmpty(targetScene) && updateScenario == true)
        {
            ScenarioManager.Instance.ChangeLocation(targetScene); // 調用您要求的新方法
        }

        // 3. 登記入口 ID
        GameDataManager.Instance.SetNextSceneEntry(entryID);
        Debug.Log($"[SceneChangeButton] 已登記入口 ID: {entryID}，準備前往場景: {targetScene}");

        // 4. 執行跳轉
        // 注意：請確保 SceneController 類與 ChangeScene 方法是靜態的或已正確實作
        SceneController.ChangeScene(targetScene);
    }

    // 提供一個右鍵選單，方便你在編輯器 Play Mode 測試，不用真的去點按鈕
    [ContextMenu("Execute Switch Scene")]
    private void TestSwitch()
    {
        if (Application.isPlaying)
            SwitchScene();
        else
            Debug.LogWarning("請在 Play Mode 執行測試。");
    }
}