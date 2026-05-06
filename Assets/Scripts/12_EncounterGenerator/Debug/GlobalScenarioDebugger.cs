using UnityEngine;
using System.Text;

public class GlobalScenarioDebugger : MonoBehaviour
{
    [ContextMenu("🔍 執行：檢查全域邏輯狀態")]
    public void CheckGlobalState()
    {
        if (GameStatusService.Instance == null || GameStatusService.Instance.Scenario == null)
        {
            Debug.LogError("無法讀取：GameStatusService 尚未初始化。");
            return;
        }

        var service = GameStatusService.Instance;
        var model = service.Scenario;
        var time = service.Time; // 取得時間模型

        string playerLoc = model.LocationID; // 這是存放在 Model 裡的玩家位置

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=yellow>========== [全域數據檢查] ==========</color>");

        // --- 新增時間資訊 ---
        sb.AppendLine($"<color=white>【當前時間點】</color>");
        sb.AppendLine($"第 <b>{time.DayIndex}</b> 天 (Day)");
        sb.AppendLine($"時段索引 (Phase): <b>{time.CurrentPhaseIndex}</b>");
        sb.AppendLine($"細分槽位 (Slot): <b>{time.CurrentSlotInPhase}</b>");
        sb.AppendLine("------------------------------------");

        sb.AppendLine($"玩家邏輯位置 (LocationID): <b>{playerLoc}</b>");
        sb.AppendLine($"是否在情境中 (IsInScenario): {model.IsInScenario}");

        // 檢查該地點的詳細數據
        var state = model.GetState(playerLoc);
        if (state == null)
        {
            sb.AppendLine($"<color=orange>警告：數據層中找不到地點 '{playerLoc}' 的任何狀態資料。</color>");
        }
        else
        {
            sb.AppendLine($"\n<color=white>【當前地點角色清單】</color>");
            foreach (var h in state.Heroines)
            {
                sb.AppendLine($"- 👩 女主角: <b>{h.HeroineID}</b> | 動作: <b>{h.Activity}</b>");
            }
            foreach (var r in state.Risks)
            {
                sb.AppendLine($"- ⚡ 風險角色: <b>{r.inspectionTypeID}</b>");
            }
        }

        Debug.Log(sb.ToString());
    }
}