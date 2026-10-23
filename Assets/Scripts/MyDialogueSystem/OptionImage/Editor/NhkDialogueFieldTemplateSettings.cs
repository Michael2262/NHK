using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;

// 僅供 Editor 使用；分組設定依資料庫 GUID 保存，不掃描或回填歷史節點。
[FilePath("ProjectSettings/NhkDialogueFieldTemplates.asset", FilePathAttribute.Location.ProjectFolder)]
internal class NhkDialogueFieldTemplateSettings : ScriptableSingleton<NhkDialogueFieldTemplateSettings>
{
    [Serializable]
    internal class Groups
    {
        public string databaseGuid;
        public List<Field> buttons = new List<Field>();
        public List<Field> dialogue = new List<Field>();
        public bool dialogueDefaultsInitialized;
        public bool buttonSkinDefaultInitialized;
    }

    [SerializeField] private List<Groups> databases = new List<Groups>();

    internal Groups Find(DialogueDatabase database, bool create = false)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(database));
        if (string.IsNullOrEmpty(guid)) return null;
        var result = databases.Find(item => item.databaseGuid == guid);
        if (result == null && create)
        {
            result = new Groups { databaseGuid = guid };
            databases.Add(result);
        }
        if (result != null && !result.dialogueDefaultsInitialized && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // 新舊分組都只補一次。之後使用者自行移除或搬移欄位，不再自動加回。
            var basic = !string.IsNullOrEmpty(database.templateJson)
                ? JsonUtility.FromJson<Template>(database.templateJson)
                : TemplateTools.LoadFromEditorPrefs();
            Undo.RecordObject(this, "補入對白組預設欄位");
            foreach (string name in new[] { "Narration", "NarrationFlag" })
            {
                if (Field.Lookup(result.dialogue, name) != null) continue;
                var source = basic != null ? Field.Lookup(basic.dialogueEntryFields, name) : null;
                result.dialogue.Add(source != null ? new Field(source) : new Field(name, "", FieldType.Text));
            }
            result.dialogueDefaultsInitialized = true;
            Persist();
        }
        if (result != null && !result.buttonSkinDefaultInitialized && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Undo.RecordObject(this, "補入按鈕組 Button Skin 欄位");
            if (Field.Lookup(result.buttons, "Button Skin") == null)
            {
                var basic = !string.IsNullOrEmpty(database.templateJson)
                    ? JsonUtility.FromJson<Template>(database.templateJson)
                    : TemplateTools.LoadFromEditorPrefs();
                var source = basic != null ? Field.Lookup(basic.dialogueEntryFields, "Button Skin") : null;
                result.buttons.Add(source != null ? new Field(source) : new Field("Button Skin", "", FieldType.Text));
            }
            result.buttonSkinDefaultInitialized = true;
            Persist();
        }
        return result;
    }

    internal void Persist() => Save(true);
    internal void PersistIfCreated()
    {
        // 撤銷第一次建立時，空清單也必須寫回既有檔案。
        if (databases.Count > 0 || System.IO.File.Exists("ProjectSettings/NhkDialogueFieldTemplates.asset")) Persist();
    }
}
