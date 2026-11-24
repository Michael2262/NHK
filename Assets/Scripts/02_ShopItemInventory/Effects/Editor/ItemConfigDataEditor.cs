#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

/// <summary>僅編輯購得效果；舊自用、送禮與目標條件不再提供設定入口。</summary>
[CustomEditor(typeof(ItemConfigData))]
public class ItemConfigDataEditor : Editor
{
    private static readonly Type[] EffectTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
        .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ItemEffect)) &&
                    !Attribute.IsDefined(t, typeof(ObsoleteAttribute)))
        .Where(t =>
        {
            var effect = (ItemEffect)Activator.CreateInstance(t);
            return !effect.IsRetired && effect.Target != EffectTarget.Heroine;
        })
        .OrderBy(t => t.Name)
        .ToArray();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        DrawPropertiesExcluding(serializedObject, "m_Script", "AcquireEffects");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("購得效果", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("現購立即執行，網購到貨才執行。每份一次，不乘以每份數量。\n" +
            "給予額外道具不會觸發其購得效果；留空時只取得商品本身。", MessageType.Info);
        DrawEffects(serializedObject.FindProperty("AcquireEffects"));
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEffects(SerializedProperty list)
    {
        int removeIndex = -1;
        for (int i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            var effect = element.managedReferenceValue as ItemEffect;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i + 1}] " +
                (effect == null ? "（空）" : EffectName(effect.GetType())), EditorStyles.boldLabel);
            if (GUILayout.Button("移除", GUILayout.Width(50))) removeIndex = i;
            EditorGUILayout.EndHorizontal();

            if (effect != null)
            {
                if (effect.IsRetired || effect.Target == EffectTarget.Heroine)
                    EditorGUILayout.HelpBox("此效果已退役，請移除並重新選擇購得效果。", MessageType.Error);
                DrawChildProperties(element);
            }
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0) list.DeleteArrayElementAtIndex(removeIndex);
        if (GUILayout.Button("＋ 新增購得效果"))
        {
            var menu = new GenericMenu();
            foreach (var type in EffectTypes)
            {
                var selectedType = type;
                menu.AddItem(new GUIContent(EffectName(type)), false, () =>
                {
                    if (target == null) return;
                    serializedObject.Update();
                    var currentList = serializedObject.FindProperty("AcquireEffects");
                    int index = currentList.arraySize;
                    currentList.InsertArrayElementAtIndex(index);
                    currentList.GetArrayElementAtIndex(index).managedReferenceValue =
                        Activator.CreateInstance(selectedType);
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }

    private static string EffectName(Type type)
    {
        if (type == typeof(GrantItemEffect)) return "額外獲得道具";
        if (type == typeof(AddMoneyEffect)) return "增減金錢";
        if (type == typeof(AddSkillPointsEffect)) return "增減技能點";
        if (type == typeof(ActivateFlagEffect)) return "啟動進度旗標";
        if (type == typeof(PermanentStatBoostEffect)) return "調整資源";
        return ObjectNames.NicifyVariableName(type.Name);
    }

    private static void DrawChildProperties(SerializedProperty parent)
    {
        var iterator = parent.Copy();
        var end = parent.GetEndProperty();
        if (!iterator.NextVisible(true)) return;
        do
        {
            if (SerializedProperty.EqualContents(iterator, end)) break;
            EditorGUILayout.PropertyField(iterator, true);
        } while (iterator.NextVisible(false));
    }
}
#endif
