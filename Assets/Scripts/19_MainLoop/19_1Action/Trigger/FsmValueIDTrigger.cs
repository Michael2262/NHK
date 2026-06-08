using UnityEngine;

/// <summary>
/// 透過 FsmValueManager 註冊的 ID 來跨 GameObject 觸發 FSM 事件。
/// 與 FsmTrigge 不同，它不限定同一個 GameObject，而是經由
/// FsmValueManager.Instance.SendEventById 找到對應的 FSM 並發送事件。
/// </summary>
[AddComponentMenu("PlayMaker/FSM Value ID Trigger")]
public class FsmValueIDTrigger : MonoBehaviour
{
    [Header("Settings")]
    [UnityEngine.Tooltip("FsmValueManager 中註冊的目標 ID")]
    [SerializeField] private string targetId = "";

    [UnityEngine.Tooltip("要發送給目標 FSM 的事件名稱")]
    [SerializeField] private string eventName = "TRIGGER";

    public void Trigger()
    {
        if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(eventName))
            return;

        if (FsmValueManager.Instance == null)
        {
            Debug.LogError($"[FsmValueIDTrigger] 場景中找不到 FsmValueManager（{gameObject.name}）");
            return;
        }

        FsmValueManager.Instance.SendEventById(targetId, eventName);
    }
}
