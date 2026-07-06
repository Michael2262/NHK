using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// InputSensor 變體：時長分流。
///
/// 放開時依「這次按住多久」發送不同指令：
///   - 按住時長 ≥ splitThreshold → 發送長按指令（預設 "TRIGGER"）
///   - 按住時長 <  splitThreshold → 發送短按指令（預設 "TRIGGER2"）
///
/// 基礎的 PRESS / RELEASE / HOLD 仍照常發送（分流指令在 RELEASE 之後發出），
/// 不需要的話在 SensorLogicTrigger 上不綁定即可。
/// </summary>
public class HoldDurationSensor : InputSensor
{
    [Header("時長分流設定")]
    [Tooltip("放開時，按住時長達到此秒數 → 發送長按指令，否則發送短按指令。")]
    [SerializeField] private float splitThreshold = 0.5f;

    [Tooltip("長按放開時發送的指令 ID。")]
    [SerializeField] private string longReleaseCommand = CMD_TRIGGER;

    [Tooltip("短按放開時發送的指令 ID。")]
    [SerializeField] private string shortReleaseCommand = CMD_TRIGGER2;

    protected override void OnReleased(float heldDuration, PointerEventData eventData)
    {
        Emit(heldDuration >= splitThreshold ? longReleaseCommand : shortReleaseCommand);
    }
}
