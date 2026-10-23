using UnityEditor;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    [CustomEditor(typeof(NhkUIMenuPanel), true)]
    public class NhkUIMenuPanelEditor : StandardUIMenuPanelEditor
    {
        private const string CopyMenu = "CONTEXT/NhkUIMenuPanel/複製同物件 Standard UI Menu Panel 設定";

        [MenuItem(CopyMenu)]
        private static void CopyStandardSettings(MenuCommand command)
        {
            var destination = command.context as NhkUIMenuPanel;
            if (destination == null || EditorApplication.isPlayingOrWillChangePlaymode) return;

            StandardUIMenuPanel source = null;
            foreach (var candidate in destination.GetComponents<StandardUIMenuPanel>())
            {
                if (candidate is NhkUIMenuPanel) continue;
                if (source != null)
                {
                    Debug.LogWarning("[NhkUIMenuPanel] 同物件上有多個 Standard UI Menu Panel，無法確定來源，未複製。請只保留一個來源元件。", destination);
                    return;
                }
                source = candidate;
            }
            if (source == null)
            {
                Debug.LogWarning("[NhkUIMenuPanel] 同一個 GameObject 上找不到 Standard UI Menu Panel，未複製。", destination);
                return;
            }

            using (var sourceObject = new SerializedObject(source))
            using (var destinationObject = new SerializedObject(destination))
            {
                sourceObject.Update();
                destinationObject.Update();
                var property = sourceObject.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = false;
                    // 只複製原生面板／UIPanel 宣告的序列化欄位，不複製 Unity 元件身分或 NHK 欄位。
                    if (!IsPanelField(property.propertyPath)) continue;
                    if (destinationObject.FindProperty(property.propertyPath) == null) continue;
                    destinationObject.CopyFromSerializedProperty(property);
                }
                Undo.RecordObject(destination, "複製 Standard UI Menu Panel 設定");
                destinationObject.ApplyModifiedProperties();
                if (PrefabUtility.IsPartOfPrefabInstance(destination))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
            }
            Debug.Log("[NhkUIMenuPanel] 已複製同物件原生面板設定，保留 NHK 樣式欄位與 Prefab 對照。請將 Dialogue UI 的面板引用改指向 NHK 元件，再移除舊元件。", destination);
        }

        private static bool IsPanelField(string name)
        {
            for (var type = typeof(StandardUIMenuPanel);
                 type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                var field = type.GetField(name, System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);
                if (field != null) return true;
            }
            return false;
        }

        [MenuItem(CopyMenu, true)]
        private static bool CanCopyStandardSettings(MenuCommand command)
        {
            return command.context is NhkUIMenuPanel && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("完整按鈕樣式", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonStyleFieldName"), new GUIContent("樣式欄位名稱"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonPrefabStyles"), new GUIContent("ID 與 Button Prefab 對照"), true);
            EditorGUILayout.HelpBox("在選項節點填 Button Style，例如 Cancel。空白或無匹配時使用上方 Button Template。每個模板根物件放自己的 Button、NhkUIResponseButton 與 CursorArea；Button 的 On Click 留空可沿用原生自動綁定。", MessageType.Info);
            EditorGUILayout.HelpBox("此功能用於 Auto-Created Buttons：請指定 Button Template 與 Button Template Holder。使用 [position] 固定位置的選項維持原本 Buttons，不會更換模板。Button Style 選擇完整模板；Button Skin 則由 NhkUIResponseButton 切換內部皮膚，兩者使用不同欄位。", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
