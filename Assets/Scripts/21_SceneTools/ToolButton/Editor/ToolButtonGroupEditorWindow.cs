using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>左側群組／按鈕導覽，右側編輯；Play Mode 僅查看設定與導覽狀態。</summary>
public sealed class ToolButtonGroupEditorWindow : EditorWindow
{
    private const string WindowTitle = "ToolButtonGroup編輯器";
    [SerializeField] private ToolButtonGroupDisplayControl controller;
    [SerializeField] private ToolButtonGroupDisplayControl editController;
    [SerializeField] private string editControllerId;
    private bool changingPlayMode;
    [SerializeField] private int selectedGroup = -1, selectedButton = -1;
    [SerializeField] private bool commonSettings;
    [SerializeField] private bool showUnityFunctions;
    private readonly Dictionary<string, FilteredUnityEventDrawer> eventDrawers = new Dictionary<string, FilteredUnityEventDrawer>();
    [SerializeField] private string search = string.Empty;
    [SerializeField] private Vector2 treeScroll, detailScroll;
    private readonly HashSet<int> collapsed = new HashSet<int>();
    private SerializedObject data;
    private int currentIndex = -1, backIndex = -1;
    private bool hasSnapshot;
    private static readonly Color CurrentColor = new Color(0.16f, 0.65f, 0.35f, 0.35f);
    private static readonly Color BackColor = new Color(1f, 0.55f, 0.12f, 0.35f);
    private static readonly Color SelectedColor = new Color(0.2f, 0.6f, 1f);
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/NHK/ToolButtonGroup編輯器")]
    private static void OpenMenu() => Open(SelectedController());

    public static void Open(ToolButtonGroupDisplayControl target)
    {
        var window = GetWindow<ToolButtonGroupEditorWindow>(WindowTitle);
        if (target != null) window.SetController(target);
        window.Show();
    }

    [MenuItem("CONTEXT/ToolButtonGroupDisplayControl/開啟 ToolButtonGroup編輯器")]
    private static void OpenContext(MenuCommand command) => Open(command.context as ToolButtonGroupDisplayControl);

    private static ToolButtonGroupDisplayControl SelectedController() => Selection.activeGameObject != null
        ? Selection.activeGameObject.GetComponent<ToolButtonGroupDisplayControl>() : null;

    private void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle);
        minSize = new Vector2(920, 560);
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        changingPlayMode = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        // 相容原本已開啟的視窗，首次更新時記住既有編輯目標。
        if (!Locked && editController == null && controller != null) RememberEditController(controller);
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        ReleaseData();
    }

    private void OnInspectorUpdate()
    {
        ResolveController();
        Repaint();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            data?.ApplyModifiedProperties();
            if (controller != null) RememberEditController(controller);
        }
        changingPlayMode = state == PlayModeStateChange.ExitingEditMode
            || state == PlayModeStateChange.ExitingPlayMode;
        ReleaseData();
        if (state == PlayModeStateChange.EnteredEditMode) controller = null;
        ResolveController();
        Repaint();
    }

    private void RememberEditController(ToolButtonGroupDisplayControl target)
    {
        editController = target;
        editControllerId = target != null
            ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : string.Empty;
    }

    private void ResolveController()
    {
        if (changingPlayMode) return;
        if (Application.isPlaying)
        {
            // 執行期目標獨立追蹤，不覆寫試玩前的編輯目標。
            BindController(ToolButtonGroupDisplayControl.Instance);
            return;
        }
        if (Locked) return;
        if (editController == null && !string.IsNullOrEmpty(editControllerId)
            && GlobalObjectId.TryParse(editControllerId, out var id))
            editController = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as ToolButtonGroupDisplayControl;

        if (editController == null)
        {
            // 包含未啟用物件；只在已載入的一般場景中存在唯一候選時自動選取。
            ToolButtonGroupDisplayControl candidate = null;
            foreach (var item in Resources.FindObjectsOfTypeAll<ToolButtonGroupDisplayControl>())
            {
                if (EditorUtility.IsPersistent(item) || !item.gameObject.scene.IsValid()
                    || !item.gameObject.scene.isLoaded
                    || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(item.gameObject)) continue;
                if (candidate != null) { candidate = null; break; }
                candidate = item;
            }
            if (candidate != null) RememberEditController(candidate);
        }
        BindController(editController);
    }

    private void BindController(ToolButtonGroupDisplayControl target)
    {
        if (ReferenceEquals(controller, target)) return;
        ReleaseData();
        controller = target;
    }
    private void OnUndo()
    {
        selectedGroup = selectedButton = -1;
        collapsed.Clear();
        ReleaseData();
        Repaint();
    }
    private void ReleaseData() { eventDrawers.Clear(); data?.Dispose(); data = null; }

    private void SetController(ToolButtonGroupDisplayControl target)
    {
        if (Locked) { ResolveController(); return; }
        RememberEditController(target);
        if (controller == target) return;
        data?.ApplyModifiedProperties();
        ReleaseData();
        controller = target;
        selectedGroup = selectedButton = -1;
        commonSettings = false;
        search = string.Empty;
        collapsed.Clear();
        treeScroll = detailScroll = Vector2.zero;
    }

    private void OnGUI()
    {
        if (changingPlayMode)
        {
            EditorGUILayout.HelpBox("正在切換試玩模式，完成後會自動連接控制器。", MessageType.Info);
            return;
        }
        DrawTargetToolbar();
        if (controller == null)
        {
            ReleaseData();
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "等待場景的 ToolButtonGroupDisplayControl 單例初始化，視窗會自動連接。"
                : "場景只有一個控制器時會自動連接。若有多個控制器或要編輯 Prefab，請指定目標或按「使用選取物件」。", MessageType.Info);
            return;
        }
        if (data == null || data.targetObject != controller)
        { ReleaseData(); data = new SerializedObject(controller); }
        data.Update();
        var groups = data.FindProperty("groups");
        hasSnapshot = controller.TryGetNavigationSnapshot(out currentIndex, out backIndex);
        DrawRuntimeStatus(groups);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * 0.31f, 270, 400))))
                DrawTree(groups);
            GUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
            {
                detailScroll = scroll.scrollPosition;
                float oldWidth = EditorGUIUtility.labelWidth;
                bool oldWide = EditorGUIUtility.wideMode;
                try
                {
                    EditorGUIUtility.labelWidth = 135;
                    EditorGUIUtility.wideMode = true;
                    if (commonSettings) DrawCommonSettings();
                    else DrawDetails(groups);
                }
                finally { EditorGUIUtility.labelWidth = oldWidth; EditorGUIUtility.wideMode = oldWide; }
            }
        }
        if (data.ApplyModifiedProperties()) Repaint();
    }

    private void DrawTargetToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(Application.isPlaying ? "執行中單例" : "編輯目標", GUILayout.Width(75));
            using (new EditorGUI.DisabledScope(Locked))
            {
                var target = (ToolButtonGroupDisplayControl)EditorGUILayout.ObjectField(
                    controller, typeof(ToolButtonGroupDisplayControl), true);
                if (target != controller) { SetController(target); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("使用選取物件", EditorStyles.toolbarButton, GUILayout.Width(100)))
                { SetController(SelectedController()); GUIUtility.ExitGUI(); }
            }
            using (new EditorGUI.DisabledScope(controller == null))
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    EditorGUIUtility.PingObject(controller.gameObject);
        }
    }

    private void DrawRuntimeStatus(SerializedProperty groups)
    {
        EditorGUILayout.LabelField("綠底：當前組　｜　橘底：BackGroup 目的地　｜　藍框：正在編輯／查看", EditorStyles.miniLabel);
        if (!Locked) return;
        string status = hasSnapshot
            ? $"當前組：{GroupName(groups, currentIndex)}　　返回組：{(backIndex >= 0 ? GroupName(groups, backIndex) : "無可返回組")}"
            : "正在等待場景單例完成初始化。";
        EditorGUILayout.HelpBox(status + "\n試玩期間設定唯讀，顏色自動更新；複製的試玩事件不可貼回編輯模式。", MessageType.Info);
    }

    private void DrawTree(SerializedProperty groups)
    {
        if (GUILayout.Button(commonSettings ? "● 共用設定" : "共用設定", GUILayout.Height(28)))
        { commonSettings = true; detailScroll = Vector2.zero; Commit(); }
        using (new EditorGUILayout.HorizontalScope())
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("清除", GUILayout.Width(45))) search = string.Empty;
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(Locked))
                if (GUILayout.Button("＋ Group")) AddGroup(groups);
            if (GUILayout.Button("全部展開")) { collapsed.Clear(); Commit(); }
            if (GUILayout.Button("收合"))
            {
                for (int i = 0; i < groups.arraySize; i++) collapsed.Add(i);
                Commit();
            }
        }
        using (var scroll = new EditorGUILayout.ScrollViewScope(treeScroll))
        {
            treeScroll = scroll.scrollPosition;
            bool any = false;
            for (int g = 0; g < groups.arraySize; g++)
            {
                var group = groups.GetArrayElementAtIndex(g);
                var options = group.FindPropertyRelative("options");
                string name = GroupName(groups, g);
                bool groupMatches = Matches(name);
                bool buttonMatches = false;
                for (int b = 0; b < options.arraySize; b++)
                    if (MatchesButton(options.GetArrayElementAtIndex(b), b)) { buttonMatches = true; break; }
                if (!groupMatches && !buttonMatches) continue;
                any = true;
                bool current = hasSnapshot && currentIndex == g;
                bool back = hasSnapshot && backIndex == g;
                string badge = (current ? " [當前組]" : "") + (back ? " [返回組]" : "");
                Rect row = EditorGUILayout.GetControlRect(false, 32);
                PaintRow(row, !commonSettings && selectedGroup == g && selectedButton < 0,
                    current ? CurrentColor : back ? BackColor : new Color(0.5f, 0.5f, 0.5f, 0.12f));
                if (current && back) EditorGUI.DrawRect(new Rect(row.xMax - 5, row.y, 5, row.height), BackColor);
                bool expanded = !collapsed.Contains(g) || !string.IsNullOrEmpty(search);
                bool nextExpanded = EditorGUI.Foldout(new Rect(row.x + 4, row.y + 7, 16, 18), expanded, GUIContent.none);
                if (nextExpanded != expanded)
                {
                    if (nextExpanded) collapsed.Remove(g); else collapsed.Add(g);
                    Commit();
                }
                if (GUI.Button(new Rect(row.x + 24, row.y, row.width - 28, row.height),
                    new GUIContent($"{name}  ({options.arraySize}){badge}", name + badge), EditorStyles.boldLabel)) Select(g, -1);
                if (!expanded) continue;
                for (int b = 0; b < options.arraySize; b++)
                {
                    var option = options.GetArrayElementAtIndex(b);
                    if (!groupMatches && !MatchesButton(option, b)) continue;
                    Rect item = EditorGUILayout.GetControlRect(false, 46);
                    item.xMin += 18;
                    PaintRow(item, !commonSettings && selectedGroup == g && selectedButton == b,
                        new Color(0.4f, 0.5f, 0.6f, 0.09f));
                    GUI.Label(new Rect(item.x + 8, item.y + 3, item.width - 16, 20),
                        new GUIContent($"{b + 1}. {ButtonName(option, b)}", ButtonName(option, b)), EditorStyles.label);
                    string ngSummary = option.FindPropertyRelative(nameof(ToolButtonGroupDisplayControl.GroupOption.useNgClickEvent)).boolValue
                        ? $" / NG {CallCount(option, "onNgClick")}" : " / NG 分流關閉";
                    GUI.Label(new Rect(item.x + 8, item.y + 24, item.width - 16, 18),
                        $"Icon: {option.FindPropertyRelative("iconName").stringValue}  ·  一般 {CallCount(option, "onClick")}{ngSummary}", EditorStyles.miniLabel);
                    if (GUI.Button(item, GUIContent.none, GUIStyle.none)) Select(g, b);
                }
                using (new EditorGUI.DisabledScope(Locked))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("＋ 此組按鈕", EditorStyles.miniButton)) AddButton(options, g);
                    var source = GetSelectedButton();
                    using (new EditorGUI.DisabledScope(source == null))
                        if (GUILayout.Button(new GUIContent("＋ 從選中按鈕複製", source == null
                            ? "請先選中一個來源按鈕。"
                            : "將「" + ButtonName(source, selectedButton) + "」的完整設定與事件複製到此組末尾。"), EditorStyles.miniButton))
                            CopySelectedButton(options, g);
                }
                GUILayout.Space(8);
            }
            if (!any) EditorGUILayout.HelpBox("尚無群組或沒有符合搜尋的項目。", MessageType.None);
        }
    }

    private static void PaintRow(Rect rect, bool selected, Color color)
    {
        EditorGUI.DrawRect(rect, color);
        if (!selected) return;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), SelectedColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), SelectedColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), SelectedColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), SelectedColor);
    }

    private bool Matches(string value) => string.IsNullOrEmpty(search)
        || (value ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    private bool MatchesButton(SerializedProperty option, int index) => Matches(ButtonName(option, index))
        || Matches(option.FindPropertyRelative("textKey").stringValue);
    private static string GroupName(SerializedProperty groups, int index)
    {
        if (index < 0 || index >= groups.arraySize) return "無";
        string name = groups.GetArrayElementAtIndex(index).FindPropertyRelative("groupName").stringValue;
        return string.IsNullOrWhiteSpace(name) ? $"未命名組 {index + 1}" : name;
    }
    private static string ButtonName(SerializedProperty option, int index)
    {
        string label = option.FindPropertyRelative("label").stringValue;
        if (!string.IsNullOrWhiteSpace(label)) return label;
        string key = option.FindPropertyRelative("textKey").stringValue;
        return string.IsNullOrWhiteSpace(key) ? $"未命名按鈕 {index + 1}" : key;
    }
    private static int CallCount(SerializedProperty option, string field) =>
        option.FindPropertyRelative(field + ".m_PersistentCalls.m_Calls").arraySize;

    private void Select(int group, int button)
    {
        selectedGroup = group;
        selectedButton = button;
        commonSettings = false;
        detailScroll = Vector2.zero;
        GUI.FocusControl(null);
        Commit();
    }

    private void DrawDetails(SerializedProperty groups)
    {
        if (selectedGroup < 0 || selectedGroup >= groups.arraySize)
        { EditorGUILayout.HelpBox("從左側選擇 Group 或按鈕，右側即顯示詳細設定。", MessageType.Info); return; }
        var group = groups.GetArrayElementAtIndex(selectedGroup);
        var options = group.FindPropertyRelative("options");
        string name = GroupName(groups, selectedGroup);
        EditorGUILayout.LabelField("Group / " + name, EditorStyles.boldLabel);
        if (selectedButton < 0 || selectedButton >= options.arraySize)
        {
            using (new EditorGUI.DisabledScope(Locked))
            {
                Field(group, "groupName", "Group 名稱");
                Field(group, "backGroupName", "返回 Group 名稱");
                Field(group, "showBackButton", "第一格顯示返回");
                DrawManagement(groups, selectedGroup, true);
                if (GUILayout.Button("＋ 新增按鈕")) AddButton(options, selectedGroup);
            }
            DrawGroupWarnings(groups, group);
            EditorGUILayout.HelpBox($"共 {options.arraySize} 個按鈕。左側由上到下為順位；開啟返回按鈕會占用第一個 Slot。\n返回名稱留空時使用 LastGroup 歷史。", MessageType.None);
            return;
        }
        var option = options.GetArrayElementAtIndex(selectedButton);
        EditorGUILayout.LabelField("按鈕 / " + ButtonName(option, selectedButton), EditorStyles.largeLabel);
        using (new EditorGUI.DisabledScope(Locked))
        {
            DrawManagement(options, selectedButton, false);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("名稱與 Icon", EditorStyles.boldLabel);
                Field(option, "label", "按鈕名稱／備註");
                Field(option, "textKey", "多語系 Text Key");
                Field(option, "iconName", "Icon");
            }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("顯示與可點條件", EditorStyles.boldLabel);
                Field(option, "visibilityFlag", "顯示 Flag");
                Field(option, "invertVisibility", "反轉顯示條件");
                Field(option, "interactableFlag", "可點 Flag");
                Field(option, "invertInteractable", "反轉可點條件");
            }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("NG 條件", EditorStyles.boldLabel);
                Field(option, "ngFlag", "NG Flag");
                Field(option, "invertNgFlag", "反轉 NG 條件");
                EditorGUILayout.HelpBox("未指定 NG Flag 永遠正常。勾選反轉時，Flag 為 false 才進入 NG。圖示與紅字照此條件顯示；點擊是否分流由下方開關決定，仍須符合可點條件。啟用分流後，NG 事件留空時不會回退至一般事件。", MessageType.None);
            }
        }
        DrawEvents(option, name + " / " + ButtonName(option, selectedButton));
    }

    private static void Field(SerializedProperty parent, string name, string label) =>
        EditorGUILayout.PropertyField(parent.FindPropertyRelative(name), new GUIContent(label), true);

    private void DrawGroupWarnings(SerializedProperty groups, SerializedProperty group)
    {
        string name = group.FindPropertyRelative("groupName").stringValue;
        if (string.IsNullOrWhiteSpace(name)) EditorGUILayout.HelpBox("Group 名稱不可空白。", MessageType.Warning);
        int count = 0;
        string back = group.FindPropertyRelative("backGroupName").stringValue;
        bool backExists = string.IsNullOrWhiteSpace(back);
        for (int i = 0; i < groups.arraySize; i++)
        {
            string other = groups.GetArrayElementAtIndex(i).FindPropertyRelative("groupName").stringValue;
            if (name == other) count++;
            if (back == other) backExists = true;
        }
        if (count > 1) EditorGUILayout.HelpBox("Group 名稱重複；依名稱導覽只會使用第一組。", MessageType.Warning);
        if (!backExists) EditorGUILayout.HelpBox("指定的返回 Group 不存在，BackGroup 將保持原組。", MessageType.Warning);
        if (!string.IsNullOrWhiteSpace(back) && back == name)
            EditorGUILayout.HelpBox("返回名稱指向本組；若名稱唯一，按下返回不會切換。", MessageType.Info);
    }

    private void DrawCommonSettings()
    {
        EditorGUILayout.LabelField("共用設定", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("事件選單顯示", EditorStyles.boldLabel);
        showUnityFunctions = EditorGUILayout.ToggleLeft(new GUIContent("顯示 Unity 內建功能", "統一套用此視窗所有一般與 NG 點擊事件。預設關閉；既有綁定不受影響。"), showUnityFunctions);
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(Locked))
        {
            foreach (string field in new[] { "slots", "icons", "backTextKey", "backBackgroundColor", "submenuIndent",
                "canvasGroup", "fadeInDuration", "fadeOutDuration" })
                EditorGUILayout.PropertyField(data.FindProperty(field), true);
        }
        EditorGUILayout.HelpBox("進場固定隱藏。Slots 順序決定位置；返回占第一格，其餘格只向右縮排。Icon 名稱 Ng／Back 為固定用途。", MessageType.None);
    }

    private void DrawEvents(SerializedProperty option, string source)
    {
        var normal = option.FindPropertyRelative("onClick");
        var ng = option.FindPropertyRelative("onNgClick");
        EditorGUILayout.Space();
        var useNg = option.FindPropertyRelative(nameof(ToolButtonGroupDisplayControl.GroupOption.useNgClickEvent));
        using (new EditorGUI.DisabledScope(Locked))
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useNg, new GUIContent("啟用 NG 點擊事件", "關閉時隱藏 NG 事件設定並走一般點擊事件；NG 圖示與紅字不受影響，既有 NG 事件保留。"));
            if (EditorGUI.EndChangeCheck()) Commit();
        }
        if (useNg.boolValue)
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("複製兩組事件")) CopyEvents(source + "（一般＋NG）", normal, ng);
            using (new EditorGUI.DisabledScope(!ToolButtonEventClipboard.CanPaste(2)))
                if (GUILayout.Button("貼上兩組事件（覆蓋）")) PasteEvents(normal, ng);
        }
        if (!string.IsNullOrEmpty(ToolButtonEventClipboard.Label))
            EditorGUILayout.LabelField("事件剪貼簿：" + ToolButtonEventClipboard.Label, EditorStyles.wordWrappedMiniLabel);
        if (!string.IsNullOrEmpty(ToolButtonEventClipboard.SingleLabel))
            EditorGUILayout.LabelField("單項剪貼簿：" + ToolButtonEventClipboard.SingleLabel, EditorStyles.wordWrappedMiniLabel);
        DrawEvent(normal, "一般點擊事件", source);
        if (useNg.boolValue) DrawEvent(ng, "NG 點擊事件", source);
    }

    private void DrawEvent(SerializedProperty property, string title, string source)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (!eventDrawers.TryGetValue(property.propertyPath, out var drawer))
            {
                drawer = new FilteredUnityEventDrawer(this, () => controller, () => showUnityFunctions,
                    () => !Locked && !changingPlayMode, item => eventDrawers.ContainsValue(item));
                eventDrawers.Add(property.propertyPath, drawer);
            }
            var calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            int selected = drawer.SelectedIndex;
            bool hasSelected = calls != null && selected >= 0 && selected < calls.arraySize;
            GUILayout.Label(title, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Locked || changingPlayMode || !hasSelected))
                    if (GUILayout.Button("刪除所選", GUILayout.Width(75)))
                    {
                        calls.DeleteArrayElementAtIndex(selected);
                        Commit();
                    }
                using (new EditorGUI.DisabledScope(!hasSelected))
                    if (GUILayout.Button("複製所選", GUILayout.Width(75)))
                    {
                        ToolButtonEventClipboard.CopySingle(controller.name + " / " + source + " / " + title
                            + " / 第 " + (selected + 1) + " 項", calls.GetArrayElementAtIndex(selected));
                        // 複製不清除選取，方便接著選擇貼上目標。
                        Repaint();
                    }
                using (new EditorGUI.DisabledScope(Locked || changingPlayMode || !hasSelected
                    || !ToolButtonEventClipboard.CanPasteSingle))
                    if (GUILayout.Button(new GUIContent("貼上所選", "以單項剪貼簿覆蓋目前選中的事件，不新增或變更其他項目。"), GUILayout.Width(75)))
                    {
                        if (ToolButtonEventClipboard.PasteSingle(controller, calls.GetArrayElementAtIndex(selected), out string error))
                            Commit();
                        else ShowNotification(new GUIContent(error));
                    }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("複製全部", GUILayout.Width(75))) CopyEvents(source + " / " + title, property);
                using (new EditorGUI.DisabledScope(!ToolButtonEventClipboard.CanPaste(1)))
                    if (GUILayout.Button("貼上覆蓋", GUILayout.Width(75))) PasteEvents(property);
            }
            var label = new GUIContent(title);
            Rect eventRect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
            using (new EditorGUI.DisabledScope(Locked)) drawer.OnGUI(eventRect, property, label);
            EditorGUILayout.LabelField("點選事件列後，可刪除、複製或貼上覆蓋單一項目；修改可使用 Undo 復原。", EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void CopyEvents(string source, params SerializedProperty[] properties)
    {
        ToolButtonEventClipboard.Copy(controller.name + " / " + source, properties);
        Commit();
    }

    private void PasteEvents(params SerializedProperty[] properties)
    {
        if (!ToolButtonEventClipboard.Paste(controller, out string error, properties))
        { ShowNotification(new GUIContent(error)); return; }
        Commit();
    }

    private void DrawManagement(SerializedProperty array, int index, bool isGroup)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isGroup ? "複製此組" : "複製此按鈕", GUILayout.Width(100)))
                Duplicate(array, index, isGroup);
            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("上移", GUILayout.Width(60))) Move(array, index, index - 1, isGroup);
            using (new EditorGUI.DisabledScope(index >= array.arraySize - 1))
                if (GUILayout.Button("下移", GUILayout.Width(60))) Move(array, index, index + 1, isGroup);
            if (GUILayout.Button(isGroup ? "刪除整組" : "刪除按鈕", GUILayout.Width(85)))
            {
                array.DeleteArrayElementAtIndex(index);
                if (isGroup) { selectedGroup = -1; collapsed.Clear(); }
                selectedButton = -1;
                Commit();
            }
        }
    }

    private void Duplicate(SerializedProperty array, int index, bool isGroup)
    {
        if (Locked || index < 0 || index >= array.arraySize) return;

        string copyName = null;
        if (isGroup)
        {
            var names = new HashSet<string>();
            for (int i = 0; i < array.arraySize; i++)
                names.Add(array.GetArrayElementAtIndex(i).FindPropertyRelative("groupName").stringValue);
            string originalName = array.GetArrayElementAtIndex(index).FindPropertyRelative("groupName").stringValue;
            string baseName = (string.IsNullOrWhiteSpace(originalName) ? "Group" : originalName) + "_副本";
            copyName = baseName;
            int suffix = 2;
            while (names.Contains(copyName)) copyName = baseName + suffix++;
        }

        // 使用 Unity 的序列化複製，保留巢狀按鈕、完整事件參數及物件參照。
        // 副本插在原項目後方，並由 Commit 統一處理 Undo 與 Prefab override。
        if (!array.GetArrayElementAtIndex(index).DuplicateCommand())
        {
            ShowNotification(new GUIContent("無法複製此項目。"));
            return;
        }
        int copyIndex = index + 1;
        if (isGroup)
        {
            array.GetArrayElementAtIndex(copyIndex).FindPropertyRelative("groupName").stringValue = copyName;
            collapsed.Clear();
        }
        else collapsed.Remove(selectedGroup);
        search = string.Empty;
        Select(isGroup ? copyIndex : selectedGroup, isGroup ? -1 : copyIndex);
    }

    private void Move(SerializedProperty array, int from, int to, bool isGroup)
    {
        array.MoveArrayElement(from, to);
        if (isGroup) { selectedGroup = to; collapsed.Clear(); }
        else selectedButton = to;
        Commit();
    }

    private void AddGroup(SerializedProperty groups)
    {
        int index = groups.arraySize++;
        var group = groups.GetArrayElementAtIndex(index);
        string name = index == 0 ? "Main" : "Group" + (index + 1);
        var names = new HashSet<string>();
        for (int i = 0; i < index; i++) names.Add(groups.GetArrayElementAtIndex(i).FindPropertyRelative("groupName").stringValue);
        while (names.Contains(name)) name += "_新";
        group.FindPropertyRelative("groupName").stringValue = name;
        group.FindPropertyRelative("backGroupName").stringValue = string.Empty;
        group.FindPropertyRelative("showBackButton").boolValue = false;
        group.FindPropertyRelative("options").ClearArray();
        collapsed.Remove(index);
        search = string.Empty;
        Select(index, -1);
    }

    private SerializedProperty GetSelectedButton()
    {
        if (commonSettings || selectedGroup < 0 || selectedButton < 0) return null;
        var groups = data.FindProperty("groups");
        if (selectedGroup >= groups.arraySize) return null;
        var options = groups.GetArrayElementAtIndex(selectedGroup).FindPropertyRelative("options");
        return selectedButton < options.arraySize ? options.GetArrayElementAtIndex(selectedButton) : null;
    }

    private struct ButtonFieldValue
    {
        public string path;
        public SerializedPropertyType type;
        public bool isArray;
        public object value;
    }

    private void CopySelectedButton(SerializedProperty options, int groupIndex)
    {
        var source = GetSelectedButton();
        if (Locked || source == null) return;

        // 先保存欄位快照再擴充陣列，同組複製也不會使用失效的來源屬性。
        // 保留 Unity 物件參照與完整 UnityEvent，不使用 JSON 或共用按鈕實例。
        var values = new List<ButtonFieldValue>();
        var iterator = source.Copy();
        var end = source.GetEndProperty();
        bool children = true;
        while (iterator.Next(children) && !SerializedProperty.EqualContents(iterator, end))
        {
            children = iterator.propertyType == SerializedPropertyType.Generic;
            var field = new ButtonFieldValue
            {
                path = iterator.propertyPath.Substring(source.propertyPath.Length + 1),
                type = iterator.propertyType,
                isArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String
            };
            if (field.isArray) field.value = iterator.arraySize;
            else switch (field.type)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ArraySize: continue;
                case SerializedPropertyType.Integer: field.value = iterator.longValue; break;
                case SerializedPropertyType.Boolean: field.value = iterator.boolValue; break;
                case SerializedPropertyType.Float: field.value = iterator.doubleValue; break;
                case SerializedPropertyType.String: field.value = iterator.stringValue; break;
                case SerializedPropertyType.Enum: field.value = iterator.intValue; break;
                case SerializedPropertyType.ObjectReference: field.value = iterator.objectReferenceValue; break;
                default:
                    ShowNotification(new GUIContent("無法複製此欄位類型：" + field.type));
                    return;
            }
            values.Add(field);
        }

        int index = options.arraySize++;
        var copy = options.GetArrayElementAtIndex(index);
        foreach (var field in values)
        {
            var destination = copy.FindPropertyRelative(field.path);
            if (field.isArray) destination.arraySize = (int)field.value;
            else switch (field.type)
            {
                case SerializedPropertyType.Integer: destination.longValue = (long)field.value; break;
                case SerializedPropertyType.Boolean: destination.boolValue = (bool)field.value; break;
                case SerializedPropertyType.Float: destination.doubleValue = (double)field.value; break;
                case SerializedPropertyType.String: destination.stringValue = (string)field.value; break;
                case SerializedPropertyType.Enum: destination.intValue = (int)field.value; break;
                case SerializedPropertyType.ObjectReference: destination.objectReferenceValue = (UnityEngine.Object)field.value; break;
            }
        }
        collapsed.Remove(groupIndex);
        search = string.Empty;
        Select(groupIndex, index);
    }

    private void AddButton(SerializedProperty options, int groupIndex)
    {
        int index = options.arraySize++;
        var option = options.GetArrayElementAtIndex(index);
        // 插入元素可能複製上一顆；明確清空所有條件與事件，避免新按鈕沿用舊觸發。
        option.FindPropertyRelative("label").stringValue = "新按鈕 " + (index + 1);
        option.FindPropertyRelative("textKey").stringValue = string.Empty;
        option.FindPropertyRelative("iconName").stringValue = "Heart";
        foreach (string field in new[] { "visibilityFlag", "interactableFlag", "ngFlag" })
            option.FindPropertyRelative(field).objectReferenceValue = null;
        option.FindPropertyRelative("invertVisibility").boolValue = false;
        option.FindPropertyRelative("invertInteractable").boolValue = false;
        option.FindPropertyRelative("invertNgFlag").boolValue = true;
        option.FindPropertyRelative(nameof(ToolButtonGroupDisplayControl.GroupOption.useNgClickEvent)).boolValue = false;
        option.FindPropertyRelative("onClick.m_PersistentCalls.m_Calls").ClearArray();
        option.FindPropertyRelative("onNgClick.m_PersistentCalls.m_Calls").ClearArray();
        collapsed.Remove(groupIndex);
        search = string.Empty;
        Select(groupIndex, index);
    }

    private void Commit()
    {
        // Unity 的 SerializedObject 處理 Undo、場景 dirty 與 Prefab override。
        data.ApplyModifiedProperties();
        eventDrawers.Clear();
        Repaint();
        GUIUtility.ExitGUI();
    }
}
