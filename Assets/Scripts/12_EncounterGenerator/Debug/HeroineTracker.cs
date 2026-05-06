using UnityEngine;
using System.Linq;

public class HeroineTracker : MonoBehaviour
{
    [Header("輸入要查詢的女主角 ID")]
    public string targetHeroineID;

    [ContextMenu("🔍 執行：追蹤女主角位置")]
    public void TrackHeroine()
    {
        if (GameStatusService.Instance == null || GameStatusService.Instance.Scenario == null)
        {
            Debug.LogError("無法追蹤：GameStatusService 未啟動或 ScenarioModel 為空。");
            return;
        }

        var model = GameStatusService.Instance.Scenario;
        bool found = false;

        Debug.Log($"<color=white>========== [Debug] 追蹤角色: {targetHeroineID} ==========</color>");

        foreach (var kvp in model.AllLocationStates)
        {
            string locID = kvp.Key;
            var heroineData = kvp.Value.Heroines.Find(h =>
                h.HeroineID.Equals(targetHeroineID, System.StringComparison.OrdinalIgnoreCase));

            if (heroineData != null)
            {
                Debug.Log($"<color=lime>【發現位置】</color>\n" +
                          $"📍 地點 ID: <b>{locID}</b>\n" +
                          $"🎭 執行動作 (Activity): <b>{heroineData.Activity}</b>");
                found = true;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"<color=orange>[警告]</color> 在當前地圖的所有地點中，都找不到 ID 為 '{targetHeroineID}' 的角色。");
        }

        Debug.Log("==================================================");
    }
}