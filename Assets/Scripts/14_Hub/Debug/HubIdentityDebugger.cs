using UnityEngine;
using System.Reflection;

public class HubIdentityDebugger : MonoBehaviour
{
    [ContextMenu("🚩 執行：檢查 Hub 與全域位置是否匹配")]
    public void VerifyHubMatch()
    {
        var hub = GetComponent<HubController>();
        if (hub == null) return;

        // 利用反射抓取 HubController 內部的 myLocationID
        FieldInfo field = typeof(HubController).GetField("myLocationID", BindingFlags.NonPublic | BindingFlags.Instance);
        string hubID = field != null ? field.GetValue(hub).ToString() : "無法獲取";

        string globalID = GameStatusService.Instance.Scenario.LocationID;

        Debug.Log($"<color=cyan>========== [Hub 身份驗證] ==========</color>");
        Debug.Log($"本場景 Hub 標記為: <b>{hubID}</b>");
        Debug.Log($"全域 Model 標記玩家在: <b>{globalID}</b>");

        if (hubID != globalID)
        {
            Debug.LogError($"<color=red>[不一致!]</color> 此場景 Hub 的 ID ({hubID}) 與玩家所在的邏輯 ID ({globalID}) 不符！\n" +
                           $"這會導致 HubController.RefreshVisuals() 抓不到正確的角色資料。");
        }
        else
        {
            Debug.Log("<color=lime>[正確]</color> Hub ID 與全域玩家位置一致。");
        }
    }
}