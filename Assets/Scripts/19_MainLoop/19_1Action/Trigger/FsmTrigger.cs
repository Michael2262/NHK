using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = UnityEngine.TooltipAttribute;

[AddComponentMenu("PlayMaker/FSM Trigger Bridge")]
public class FsmTrigge : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("目標 FSM 的名稱(在 PlayMaker 編輯器 FSM 頁籤最上面可以看到)")]
    [SerializeField] private string fsmName = "";

    [Tooltip("要發送給 FSM 的事件名稱")]
    [SerializeField] private string eventName = "TRIGGER";

    private PlayMakerFSM _fsm;

    void Awake()
    {
        // 找到指定名稱的 FSM
        var fsms = GetComponents<PlayMakerFSM>();
        foreach (var f in fsms)
        {
            if (f.FsmName == fsmName)
            {
                _fsm = f;
                break;
            }
        }

        if (_fsm == null)
        {
            Debug.LogError($"[FsmTrigger] 在 {gameObject.name} 上找不到名稱為 '{fsmName}' 的 FSM!");
        }
    }

    public void Trigger()
    {
        if (_fsm != null && !string.IsNullOrEmpty(eventName))
        {
            _fsm.SendEvent(eventName);
        }
    }
}