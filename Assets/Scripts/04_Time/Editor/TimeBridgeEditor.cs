using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimeBridge))]
public class TimeBridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying) return;
        var model = GameStatusService.Instance?.Time;
        if (model == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("執行中時間", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Day {model.DayIndex} / Phase {model.CurrentPhaseIndex} / Slot {model.CurrentSlotInPhase}");
        EditorGUILayout.LabelField("Mini 已推進／總量", $"{model.CurrentMiniInSlot} / {model.MinisInCurrentSlot}");
        EditorGUILayout.LabelField("今日剩餘 Mini（不跨日）", model.GetRemainingMinisInDay().ToString());
        var bridge = (TimeBridge)target;
        EditorGUILayout.LabelField("時間流逝中", bridge.IsTimeFlowing ? "是" : "否");
        if (GUILayout.Button("測試：播放一小段時間")) bridge.PlayAdvanceShortTime();
        if (GUILayout.Button("測試：播放中段時間")) bridge.PlayAdvanceMediumTime();
        if (GUILayout.Button("測試：播放長段時間")) bridge.PlayAdvanceLongTime();
        if (GUILayout.Button("測試：直達中段時間")) bridge.AdvanceMediumTime();
        if (GUILayout.Button("測試：直達長段時間")) bridge.AdvanceLongTime();
        if (GUILayout.Button("取消時間流逝")) bridge.CancelTimeFlow();
        if (GUILayout.Button("測試：推進 1 Mini")) bridge.AdvanceOneMini();
        if (GUILayout.Button("測試：推進 1 Mini（不跨日）")) bridge.AdvanceOneMiniWithinDay();
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
