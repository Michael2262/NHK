using System.Collections;
using UnityEngine;

/// <summary>
/// 進場時依 SceneEntryID 決定地圖 scope，寫成 scene-lifetime 旗標。
///
/// 對照 Task_InitProgressState：
///   - Task_InitProgressState 觸發 ProgressStateController → 決定地點「存在 / 不存在」。
///   - Task_InitMapScope       設定 scope 旗標           → 決定地點「亮 / 暗」與選單內容。
///
/// 設計要點：
///   - 只設旗標，不做廣播。AddSceneFlag 會觸發 ProgressFlagModel.OnFlagChanged，
///     MapSpotView 與（下次開啟的）IxMenu 走這條既有事件通道自動反應，不需另立事件。
///   - scope 旗標是 Scene 生命週期，離開地圖場景時由 ProgressFlagModel 自動清除。
///
/// 擺放：放進 SceneReadyCoordinator 的 sceneTasks，排在 Task_InitProgressState 之後
///       （先決定誰存在，再決定存在者的亮暗）。
/// </summary>
public class Task_InitMapScope : SceneReadyTaskBase
{
    public override IEnumerator ExecuteTask(string entryID)
    {
        var flags = GameStatusService.Instance?.ProgressFlags;
        if (flags == null)
        {
            Debug.LogWarning("[Task_InitMapScope] ProgressFlags 尚未就緒，跳過。");
            yield break;
        }

        // entryID 含 "Challenge" → 挑戰模式；其餘（含 "Unknown" 直接 Play）一律拜訪模式。
        bool challenge = !string.IsNullOrEmpty(entryID) && entryID.Contains("Challenge");

        flags.AddSceneFlag(challenge ? MapScopeFlags.Unlock : MapScopeFlags.Visit);
        flags.RemoveFlag(challenge ? MapScopeFlags.Visit : MapScopeFlags.Unlock);

        Debug.Log($"[Task_InitMapScope] entryID=[{entryID}] → scope=[{(challenge ? "Challenge" : "Visit")}]");
        yield return null;
    }
}
