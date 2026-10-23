using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.DialogueEditor;
using UnityEditor;
using UnityEngine;

/// <summary>管理按鈕／對白模板，並在原生節點 Inspector 提供分組補欄位與編輯。</summary>
public class NhkDialogueFieldTemplates : EditorWindow
{
    [SerializeField] private DialogueDatabase database;
    private Vector2 scroll;
    private bool basicOpen;
    private bool buttonsOpen = true;
    private bool dialogueOpen = true;
    private static bool nodeButtonsOpen = true;
    private static bool nodeDialogueOpen;
    private static Field clipboard;

    [InitializeOnLoadMethod]
    private static void Register()
    {
        DialogueEditorWindow.customDrawDialogueEntryInspector -= DrawEntry;
        DialogueEditorWindow.customDrawDialogueEntryInspector += DrawEntry;
        Undo.undoRedoPerformed -= PersistUndo;
        Undo.undoRedoPerformed += PersistUndo;
    }

    [MenuItem("Tools/NHK/Dialogue/欄位模板分組")]
    public static void Open()
    {
        OpenFor(DialogueEditorWindow.GetCurrentlyEditedDatabase());
    }

    private static void OpenFor(DialogueDatabase source)
    {
        var window = GetWindow<NhkDialogueFieldTemplates>("NHK 欄位模板");
        if (source != null) window.database = source;
        window.minSize = new Vector2(440, 350);
        window.Show();
    }

    private static void PersistUndo()
    {
        // 即使管理視窗已關閉，分組設定的 Undo／Redo 也需保存。
        NhkDialogueFieldTemplateSettings.instance.PersistIfCreated();
    }

    private static Template ReadBasic(DialogueDatabase source)
    {
        // 原生模板 JSON 是 Unity 定義的格式，使用其原生反序列化方式。
        return !string.IsNullOrEmpty(source.templateJson)
            ? JsonUtility.FromJson<Template>(source.templateJson)
            : TemplateTools.LoadFromEditorPrefs();
    }

    private void OnGUI()
    {
        database = (DialogueDatabase)EditorGUILayout.ObjectField("對話資料庫", database, typeof(DialogueDatabase), false);
        if (database == null) return;
        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(database)))
        {
            EditorGUILayout.HelpBox("請選取已儲存的 Dialogue Database 資產。", MessageType.Info);
            return;
        }
        EditorGUILayout.HelpBox("基本組沿用原生 Templates → Dialogue Entries。按鈕／對白組獨立保存名稱、型別及預設值；到節點按套用才補上。分組設定保存在 ProjectSettings，可隨專案提交。", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var settings = NhkDialogueFieldTemplateSettings.instance;
            var groups = settings.Find(database);
            if (groups == null)
            {
                if (GUILayout.Button("建立此資料庫的分組設定"))
                {
                    Undo.RecordObject(settings, "建立對話欄位分組");
                    groups = settings.Find(database, true);
                    var basic = ReadBasic(database);
                    foreach (string name in new[] { "Button Style", "Option Image", "NG Conditions" })
                    {
                        var field = Field.Lookup(basic.dialogueEntryFields, name);
                        groups.buttons.Add(field != null ? new Field(field) : new Field(name, "", FieldType.Text));
                    }
                    settings.Persist();
                }
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            basicOpen = EditorGUILayout.Foldout(basicOpen, "基本／不分組（目前原生模板）", true);
            if (basicOpen)
            {
                EditorGUILayout.HelpBox("此處唯讀。可將欄位複製到選用組；要移出基本組，請在原生 Templates 移除該列，不要使用 Enforce／Sync 去清除既有節點欄位。", MessageType.Info);
                foreach (var field in ReadBasic(database).dialogueEntryFields)
                {
                    if (field == null) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(field.title + "  (" + field.typeString + ")");
                        if (GUILayout.Button("→ 按鈕", GUILayout.Width(65))) CopyInto(groups.buttons, field);
                        if (GUILayout.Button("→ 對白", GUILayout.Width(65))) CopyInto(groups.dialogue, field);
                    }
                }
            }
            DrawTemplateGroup("按鈕組", groups.buttons, groups.dialogue, ref buttonsOpen);
            DrawTemplateGroup("對白組", groups.dialogue, groups.buttons, ref dialogueOpen);
            EditorGUILayout.EndScrollView();
        }
    }

    private static void CopyInto(List<Field> fields, Field source)
    {
        if (Field.Lookup(fields, source.title) != null)
        {
            EditorUtility.DisplayDialog("已有同名欄位", "目標組已有「" + source.title + "」，保留現有定義。", "確定");
            return;
        }
        var settings = NhkDialogueFieldTemplateSettings.instance;
        Undo.RecordObject(settings, "複製模板欄位");
        fields.Add(new Field(source));
        settings.Persist();
        GUIUtility.ExitGUI();
    }

    private static void DrawTemplateGroup(string title, List<Field> fields, List<Field> other, ref bool expanded)
    {
        expanded = EditorGUILayout.Foldout(expanded, title + "（" + fields.Count + "）", true);
        if (!expanded) return;
        var settings = NhkDialogueFieldTemplateSettings.instance;
        foreach (var field in fields.ToArray())
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.TextField("欄位名稱", field.title);
                var type = (FieldType)EditorGUILayout.EnumPopup("型別", field.type);
                string customType = EditorGUILayout.TextField(new GUIContent("型別字串", "一般型別會自動更新；自訂 Field Type 可保留自己的型別字串。"), field.typeString);
                string value = DrawValue("預設值", type, field.value);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(settings, "編輯模板欄位");
                    if (type != field.type) customType = new Field(name, value, type).typeString;
                    field.title = name;
                    field.type = type;
                    field.typeString = customType;
                    field.value = value;
                    settings.Persist();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("複製到另一組")) CopyInto(other, field);
                    if (GUILayout.Button("複製欄位")) clipboard = new Field(field);
                    if (GUILayout.Button("移除此組欄位"))
                    {
                        Undo.RecordObject(settings, "移除模板欄位");
                        fields.Remove(field);
                        settings.Persist();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新增欄位"))
            {
                Undo.RecordObject(settings, "新增模板欄位");
                fields.Add(new Field("", "", FieldType.Text));
                settings.Persist();
                GUIUtility.ExitGUI();
            }
            using (new EditorGUI.DisabledScope(clipboard == null))
                if (GUILayout.Button("貼上欄位")) CopyInto(fields, clipboard);
        }
        string error = Validate(fields);
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
    }

    private static string Validate(List<Field> fields)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.title)) return "有欄位名稱未填，請完成後再套用。";
            if (field.title != field.title.Trim()) return "欄位名稱前後不可有空白：" + field.title;
            if (!names.Add(field.title)) return "此組有重複欄位：" + field.title;
            if (field.type == FieldType.Number &&
                (!double.TryParse(field.value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double number) || double.IsNaN(number) || double.IsInfinity(number)))
                return field.title + " 的數值預設值無效，請輸入數字，例如 0。";
            if (field.type == FieldType.Boolean && !bool.TryParse(field.value, out _))
                return field.title + " 的布林預設值需為 True 或 False。";
        }
        return null;
    }

    private static string DrawValue(string label, FieldType type, string value)
    {
        if (type == FieldType.Boolean)
        {
            bool.TryParse(value, out bool flag);
            return EditorGUILayout.Toggle(label, flag) ? "True" : "False";
        }
        if (type == FieldType.Text || type == FieldType.Localization)
        {
            EditorGUILayout.LabelField(label);
            return EditorGUILayout.TextArea(value ?? "", GUILayout.MinHeight(36));
        }
        return EditorGUILayout.TextField(label, value);
    }

    private static void DrawEntry(DialogueDatabase source, DialogueEntry entry)
    {
        if (source == null || entry == null) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("NHK 欄位分組", EditorStyles.boldLabel);
        if (GUILayout.Button("管理欄位模板")) OpenFor(source);
        var groups = NhkDialogueFieldTemplateSettings.instance.Find(source);
        if (groups == null)
        {
            EditorGUILayout.HelpBox("先開啟管理視窗，建立此資料庫的分組設定。", MessageType.Info);
            return;
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            DrawEntryGroup(source, entry, "按鈕組", groups.buttons, ref nodeButtonsOpen);
            DrawEntryGroup(source, entry, "對白組", groups.dialogue, ref nodeDialogueOpen);
        }
    }

    private static void DrawEntryGroup(DialogueDatabase source, DialogueEntry entry, string title, List<Field> definitions, ref bool expanded)
    {
        expanded = EditorGUILayout.Foldout(expanded, title, true);
        if (!expanded) return;
        string error = Validate(definitions);
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
        using (new EditorGUI.DisabledScope(error != null || definitions.Count == 0))
        {
            if (GUILayout.Button("套用" + title + "（只補缺少欄位）"))
            {
                var missing = definitions.FindAll(field => Field.Lookup(entry.fields, field.title) == null);
                if (missing.Count > 0)
                {
                    Undo.RecordObject(source, "套用" + title);
                    foreach (var field in missing) entry.fields.Add(new Field(field));
                    EditorUtility.SetDirty(source);
                }
            }
        }
        foreach (var definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.title)) continue;
            var field = Field.Lookup(entry.fields, definition.title);
            if (field == null)
            {
                EditorGUILayout.LabelField(definition.title, "尚未加入");
                continue;
            }
            if (field.type != definition.type || field.typeString != definition.typeString)
                EditorGUILayout.HelpBox(field.title + " 的型別與模板不同，保留節點原值；請在 All Fields 確認。", MessageType.Warning);
            // 維持原生字串資料格式；自訂型別與物件 ID 的專用選擇器仍在 All Fields。
            EditorGUI.BeginChangeCheck();
            string value = DrawValue(field.title, field.type, field.value);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(source, "編輯 " + field.title);
                field.value = value;
                EditorUtility.SetDirty(source);
            }
        }
    }
}
