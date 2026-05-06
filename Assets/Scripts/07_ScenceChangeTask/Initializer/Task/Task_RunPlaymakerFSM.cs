using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker; // Playmaker 命名空間
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 將 Playmaker FSM 整合進 SceneReadyTask 管線的橋接任務。
/// 
/// 【運作方式】
/// 1. 啟動指定的 FSM
/// 2. 透過 FSM 的全域變數傳入 entryID
/// 3. 等待 FSM 發送指定的「完成事件」
/// 4. 完成後讓 Coordinator 繼續往下走
/// 
/// 【Playmaker 端設定】
/// - 在 FSM 中建立一個 String 變數（預設名稱 "EntryID"），此任務會自動寫入值
/// - FSM 做完所有工作後，發送一個全域事件（預設名稱 "SCENE_READY_COMPLETE"）
/// - 如果 FSM 不需要等待（例如純粹設定狀態），可以打勾 fireAndForget
/// 
/// 【使用範例】
/// 場景中某些複雜的初始化邏輯（多階段動畫、條件式物件啟用等）
/// 如果用純 Task 寫太複雜，可以用 Playmaker FSM 視覺化編輯，
/// 然後透過此 Task 掛進 Coordinator 的管線中。
/// </summary>
public class Task_RunPlaymakerFSM : SceneReadyTaskBase
{
    [Header("Playmaker FSM 設定")]
    [Tooltip("要執行的 PlayMakerFSM 元件。會在任務開始時啟用它。")]
    [SerializeField] private PlayMakerFSM targetFSM;

    [Header("EntryID 傳遞")]
    [Tooltip("FSM 中用來接收 entryID 的變數名稱")]
    [SerializeField] private string entryIDVariableName = "EntryID";

    [Header("完成信號")]
    [Tooltip("FSM 完成後會發送的全域事件名稱。此任務會等待此事件。")]
    [SerializeField] private string completionEventName = "SCENE_READY_COMPLETE";

    [Tooltip("打勾 = 啟動 FSM 後立即完成，不等待完成事件（Fire and Forget 模式）")]
    [SerializeField] private bool fireAndForget = false;

    private bool _isComplete = false;

    public override IEnumerator ExecuteTask(string entryID)
    {
        if (targetFSM == null)
        {
            Debug.LogWarning($"[Task_RunPlaymakerFSM] {gameObject.name}: targetFSM 未設定！");
            yield break;
        }

        // 1. 傳入 entryID 到 FSM 變數
        var entryVar = targetFSM.FsmVariables.FindFsmString(entryIDVariableName);
        if (entryVar != null)
        {
            entryVar.Value = entryID;
        }
        else
        {
            Debug.LogWarning($"[Task_RunPlaymakerFSM] FSM 中找不到 String 變數 '{entryIDVariableName}'");
        }

        // 2. 啟用並啟動 FSM
        targetFSM.enabled = true;
        targetFSM.SendEvent("START"); // 可自訂啟動事件名稱
        Debug.Log($"[Task_RunPlaymakerFSM] 已啟動 FSM: {targetFSM.FsmName}, entryID: {entryID}");

        // 3. Fire and Forget 模式：不等待
        if (fireAndForget)
        {
            yield break;
        }

        // 4. 等待 FSM 發送完成事件
        _isComplete = false;

        // 訂閱全域事件
        // 注意：這裡使用輪詢方式檢查，因為 Playmaker 的全域事件機制
        // 不容易直接轉成 C# callback。另一種做法是在 FSM 最後一個 State
        // 加一個 SendMessage Action 來呼叫此物件的 OnFSMComplete()。
        
        // 方案 A：使用 SendMessage（推薦，更可靠）
        // 在 FSM 的最後一個 State 加入 Send Message Action，
        // 目標設為此 GameObject，方法名稱填 "OnFSMComplete"
        
        float timeout = 30f; // 安全超時
        float timer = 0f;

        while (!_isComplete && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_isComplete)
        {
            Debug.LogWarning($"[Task_RunPlaymakerFSM] FSM 等待超時（{timeout}s），強制繼續！");
        }
        else
        {
            Debug.Log($"[Task_RunPlaymakerFSM] FSM 完成: {targetFSM.FsmName}");
        }
    }

    /// <summary>
    /// 由 Playmaker FSM 透過 SendMessage 呼叫，通知此任務已完成。
    /// 
    /// 【Playmaker 設定方式】
    /// 在 FSM 的最後一個 State 加入 "Send Message" Action：
    /// - Game Object: 使用 Specify Game Object，拖入此 Task 所在的 GameObject
    /// - Method Name: "OnFSMComplete"
    /// - Delivery: SendToAll (或 DontRequireReceiver)
    /// </summary>
    public void OnFSMComplete()
    {
        _isComplete = true;
    }
}
