using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TeaseGame 指令觸發器。
///
/// 接收 InputSensor 發來的指令 ID，查表觸發對應的 UnityEvent 與 FSM 事件。
/// 每個指令 ID 對應一組綁定；沒綁定的指令 ID 會被靜默忽略。
/// 本身不含遊戲邏輯——要做什麼全部掛在 UnityEvent / FSM 上。
/// </summary>
public class SensorLogicTrigger : MonoBehaviour
{
    [System.Serializable]
    public class CommandBinding
    {
        [Tooltip("指令 ID，例如 PRESS / RELEASE / HOLD / TRIGGER / TRIGGER2 / TRIGGER3。")]
        public string commandId = InputSensor.CMD_PRESS;

        [Tooltip("收到此指令時觸發的 UnityEvent。")]
        public UnityEvent onCommand;

        [Tooltip("收到此指令時要發送的 FSM 事件。fsmOwner 留空則不發送。")]
        public FsmEventSender fsm;
    }

    [Header("指令綁定")]
    [Tooltip("每個指令 ID 對應一組 UnityEvent + FSM 事件。")]
    public List<CommandBinding> bindings = new List<CommandBinding>();

    private Dictionary<string, CommandBinding> _lookup;

    private void Awake()
    {
        _lookup = new Dictionary<string, CommandBinding>();

        foreach (var binding in bindings)
        {
            if (binding == null || string.IsNullOrEmpty(binding.commandId)) continue;

            if (_lookup.ContainsKey(binding.commandId))
            {
                Debug.LogWarning(
                    $"[SensorLogicTrigger] {name} 有重複的指令 ID '{binding.commandId}'，後面的綁定將被忽略。", this);
                continue;
            }

            _lookup.Add(binding.commandId, binding);
        }
    }

    /// <summary>
    /// 接收指令 ID 並觸發對應綁定。由 InputSensor 呼叫，也可從 UnityEvent / FSM 直接呼叫。
    /// </summary>
    public void ReceiveCommand(string commandId)
    {
        if (string.IsNullOrEmpty(commandId) || _lookup == null) return;

        if (_lookup.TryGetValue(commandId, out var binding))
        {
            binding.onCommand?.Invoke();
            binding.fsm?.Send();
        }
    }
}
