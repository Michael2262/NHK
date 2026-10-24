using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>共同編輯時間結構與 Mini 顯示對照；兩份資產仍各自保存。</summary>
public class TimeSettingsWindow : EditorWindow
{
    [SerializeField] private TimeConfig timeConfig;
    [SerializeField] private TimeMappingSO timeMapping;
    private Vector2 scroll;
    private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    [MenuItem("Tools/NHK/時間設定（Slot・Mini）")]
    public static void OpenWindow()
    {
        GetWindow<TimeSettingsWindow>("時間設定");
    }

    private void OnEnable()
    {
        minSize = new Vector2(420, 360);
        if (timeConfig == null)
            timeConfig = AssetDatabase.LoadAssetAtPath<TimeConfig>("Assets/Resources/Time/Time Config.asset");
        if (timeMapping == null)
            timeMapping = AssetDatabase.LoadAssetAtPath<TimeMappingSO>("Assets/Resources/LobbyUI/TimeMapping.asset");
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable() => Undo.undoRedoPerformed -= Repaint;

    private void OnGUI()
    {
        timeConfig = (TimeConfig)EditorGUILayout.ObjectField("時間規則 Config", timeConfig, typeof(TimeConfig), false);
        timeMapping = (TimeMappingSO)EditorGUILayout.ObjectField("時間顯示 Mapping", timeMapping, typeof(TimeMappingSO), false);
        if (timeConfig == null || timeMapping == null)
        {
            EditorGUILayout.HelpBox("請指定兩份設定資產。時間結構以 TimeConfig 為準。", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox("Mini 0 是剛進入 Slot 的時間。請填 HH:mm（例如 08:05）；空白或無效時間顯示 --:--。減少 Slot／Mini 數量只隱藏資料，加回數量即可恢復。支援 Undo／Redo。", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("儲存這兩份設定"))
            {
                AssetDatabase.SaveAssetIfDirty(timeConfig);
                AssetDatabase.SaveAssetIfDirty(timeMapping);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            int shortTime = Mathf.Max(1, EditorGUILayout.DelayedIntField("一小段時間的 Mini 數量", timeConfig.ShortTimeMinis));
            if (EditorGUI.EndChangeCheck())
                Edit(timeConfig, () => timeConfig.ShortTimeMinis = shortTime);

            EditorGUI.BeginChangeCheck();
            int mediumTime = Mathf.Max(1, EditorGUILayout.DelayedIntField("中段時間的 Mini 數量", timeConfig.MediumTimeMinis));
            if (EditorGUI.EndChangeCheck())
                Edit(timeConfig, () => timeConfig.MediumTimeMinis = mediumTime);

            EditorGUI.BeginChangeCheck();
            int longTime = Mathf.Max(1, EditorGUILayout.DelayedIntField("長段時間的 Mini 數量", timeConfig.LongTimeMinis));
            if (EditorGUI.EndChangeCheck())
                Edit(timeConfig, () => timeConfig.LongTimeMinis = longTime);

            EditorGUI.BeginChangeCheck();
            float seconds = EditorGUILayout.DelayedFloatField("每個 Mini 停留秒數", timeConfig.MiniPlaybackSeconds);
            if (EditorGUI.EndChangeCheck() && !float.IsNaN(seconds) && !float.IsInfinity(seconds))
                Edit(timeConfig, () => timeConfig.MiniPlaybackSeconds = Mathf.Max(0, seconds));

            if (Foldout("rollover", "視覺換日設定（只影響 UI 日期）", false))
            {
                EditorGUI.BeginChangeCheck();
                int phase = Mathf.Max(0, EditorGUILayout.DelayedIntField("換日 Phase（從 0 開始）", timeMapping.dayRolloverPhase));
                int slot = Mathf.Max(1, EditorGUILayout.DelayedIntField("換日 Slot（從 1 開始）", timeMapping.dayRolloverSlot));
                if (EditorGUI.EndChangeCheck())
                    Edit(timeMapping, () => { timeMapping.dayRolloverPhase = phase; timeMapping.dayRolloverSlot = slot; });
            }

            int phaseCount = timeConfig.PhaseNames?.Count ?? 0;
            if (phaseCount == 0)
                EditorGUILayout.HelpBox("請先在 TimeConfig Inspector 建立 Phase。", MessageType.Warning);
            for (int p = 0; p < phaseCount; p++) DrawPhase(p);
            EditorGUILayout.EndScrollView();
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("請離開 Play Mode 後編輯時間設定。", MessageType.Info);
    }

    private void DrawPhase(int p)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (Foldout($"phase/{p}", $"Phase {p}：{timeConfig.PhaseNames[p]}", true))
        {
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.DelayedTextField("階段名稱", timeConfig.PhaseNames[p]);
            if (EditorGUI.EndChangeCheck()) Edit(timeConfig, () => timeConfig.PhaseNames[p] = name);

            var mapping = GetPhase(p);
            string label = mapping?.phaseName ?? "";
            string key = mapping?.localizationKey ?? "";
            string newLabel = EditorGUILayout.DelayedTextField("顯示名稱（備用）", label);
            string newKey = EditorGUILayout.DelayedTextField("時段名稱多語系 Key", key);
            if (label != newLabel || key != newKey)
                Edit(timeMapping, () => { var entry = EnsurePhase(p); entry.phaseName = newLabel; entry.localizationKey = newKey; });

            int count = timeConfig.GetTotalSlots(p);
            EditorGUI.BeginChangeCheck();
            int newCount = Mathf.Max(1, EditorGUILayout.DelayedIntField("Slot 數量", count));
            if (EditorGUI.EndChangeCheck())
            {
                Edit(timeConfig, () =>
                {
                    if (timeConfig.SlotsPerPhase == null) timeConfig.SlotsPerPhase = new List<int>();
                    while (timeConfig.SlotsPerPhase.Count <= p) timeConfig.SlotsPerPhase.Add(1);
                    timeConfig.SlotsPerPhase[p] = newCount;
                });
                count = newCount;
            }
            for (int s = 0; s < count; s++) DrawSlot(p, s);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSlot(int p, int s)
    {
        if (!Foldout($"slot/{p}/{s}", $"Slot {s + 1}", false)) return;
        EditorGUI.indentLevel++;
        int count = timeConfig.GetMinisInSlot(p, s + 1);
        int newCount = Mathf.Max(1, EditorGUILayout.DelayedIntField("Mini 數量", count));
        if (newCount != count)
        {
            Edit(timeConfig, () =>
            {
                if (timeConfig.MiniSettings == null) timeConfig.MiniSettings = new List<TimeConfig.PhaseMiniConfig>();
                while (timeConfig.MiniSettings.Count <= p) timeConfig.MiniSettings.Add(new TimeConfig.PhaseMiniConfig());
                if (timeConfig.MiniSettings[p] == null) timeConfig.MiniSettings[p] = new TimeConfig.PhaseMiniConfig();
                var phase = timeConfig.MiniSettings[p];
                if (phase.MinisPerSlot == null) phase.MinisPerSlot = new List<int>();
                while (phase.MinisPerSlot.Count <= s) phase.MinisPerSlot.Add(1);
                phase.MinisPerSlot[s] = newCount;
            });
            count = newCount;
        }

        var phaseMapping = GetPhase(p);
        var times = phaseMapping?.slots != null && s < phaseMapping.slots.Count
            ? phaseMapping.slots[s]?.miniTimes : null;
        for (int m = 0; m < count; m++)
        {
            int miniIndex = m;
            string value = times != null && m < times.Count ? times[m] ?? "" : "";
            string edited = EditorGUILayout.DelayedTextField($"Mini {m} 顯示時間", value);
            if (edited != value)
            {
                edited = edited.Trim();
                Edit(timeMapping, () =>
                {
                    var phase = EnsurePhase(p);
                    if (phase.slots == null) phase.slots = new List<TimeMappingSO.SlotTimeInfo>();
                    while (phase.slots.Count <= s) phase.slots.Add(new TimeMappingSO.SlotTimeInfo());
                    if (phase.slots[s] == null) phase.slots[s] = new TimeMappingSO.SlotTimeInfo();
                    if (phase.slots[s].miniTimes == null) phase.slots[s].miniTimes = new List<string>();
                    var entries = phase.slots[s].miniTimes;
                    while (entries.Count <= miniIndex) entries.Add("");
                    entries[miniIndex] = edited;
                });
                value = edited;
                times = GetPhase(p).slots[s].miniTimes;
            }
            if (!string.IsNullOrEmpty(value) && !TimeMappingSO.TryParseClockTime(value, out _, out _))
                EditorGUILayout.HelpBox($"Mini {m}：請填 00:00～23:59，例如 08:05。", MessageType.Warning);
        }
        if (times != null && times.Count > count)
            EditorGUILayout.LabelField($"另保留 {times.Count - count} 筆隱藏的 Mini 時間。", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;
    }

    private TimeMappingSO.PhaseTimeInfo GetPhase(int p)
        => timeMapping.phases != null && p < timeMapping.phases.Count ? timeMapping.phases[p] : null;

    private TimeMappingSO.PhaseTimeInfo EnsurePhase(int p)
    {
        if (timeMapping.phases == null) timeMapping.phases = new List<TimeMappingSO.PhaseTimeInfo>();
        while (timeMapping.phases.Count <= p) timeMapping.phases.Add(new TimeMappingSO.PhaseTimeInfo());
        if (timeMapping.phases[p] == null) timeMapping.phases[p] = new TimeMappingSO.PhaseTimeInfo();
        return timeMapping.phases[p];
    }

    private bool Foldout(string key, string label, bool defaultValue)
    {
        if (!foldouts.TryGetValue(key, out bool expanded)) expanded = defaultValue;
        expanded = EditorGUILayout.Foldout(expanded, label, true);
        foldouts[key] = expanded;
        return expanded;
    }

    private static void Edit(UnityEngine.Object asset, Action change)
    {
        Undo.RecordObject(asset, "編輯時間設定");
        change();
        EditorUtility.SetDirty(asset);
    }
}
