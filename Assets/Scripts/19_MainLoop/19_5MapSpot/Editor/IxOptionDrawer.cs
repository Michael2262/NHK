#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// IxOption 的自訂 PropertyDrawer。
/// 依據開關狀態動態顯示/隱藏欄位，保持 Inspector 整潔。
/// 標題簡短，詳細說明放在 Tooltip（滑鼠懸停顯示）。
/// </summary>
[CustomPropertyDrawer(typeof(IxMenuService.IxOption))]
public class IxOptionDrawer : PropertyDrawer
{
    private const float Spacing = 2f;
    private const float SeparatorHeight = 5f;

    // ══════════════════════════════════════════
    //  欄位定義（標題, tooltip, 欄位名）
    // ══════════════════════════════════════════

    private static readonly (string label, string tooltip, string field)[] BasicFields =
    {
        ("文字 Key",     "多語系翻譯用的 key",                              "labelKey"),
        ("進度條件旗標", "拖入 ProgressFlagDefinition，留空=不受進度限制",   "conditionFlag"),
        ("優先次序",     "數字越小越優先",                                   "priority"),
        ("體力預覽",     "負數=消耗, 正數=回復, 0=不預覽",                  "staminaDelta"),
    };

    private static readonly (string label, string tooltip, string field)[] CheckFields =
    {
        ("檢查類型",     "要檢查的主角資源類型",                   "checkType"),
        ("數量",         "檢查的資源數量",                         "amount"),
        ("檢查時間",     "是否同時檢查剩餘時段",                   "checkTime"),
    };

    // ══════════════════════════════════════════
    //  高度計算
    // ══════════════════════════════════════════

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float h = EditorGUIUtility.singleLineHeight + Spacing; // Foldout

        // 基本欄位
        foreach (var f in BasicFields)
            h += FieldH(property, f.field);

        h += SeparatorHeight; // 分隔線

        // 資源檢查開關
        h += FieldH(property, "useResourceCheck");

        bool useCheck = property.FindPropertyRelative("useResourceCheck").boolValue;
        if (useCheck)
        {
            foreach (var f in CheckFields)
                h += FieldH(property, f.field);

            if (property.FindPropertyRelative("checkTime").boolValue)
                h += FieldH(property, "timeAmount");
        }

        h += SeparatorHeight; // 分隔線

        // 事件
        h += FieldH(property, "onSelected");
        if (useCheck)
        {
            h += FieldH(property, "onFailure");
            h += FieldH(property, "onTimeFailure");
        }

        return h;
    }

    // ══════════════════════════════════════════
    //  繪製
    // ══════════════════════════════════════════

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // Foldout
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
        if (!property.isExpanded) { EditorGUI.EndProperty(); return; }

        EditorGUI.indentLevel++;
        rect.y += EditorGUIUtility.singleLineHeight + Spacing;

        // === 基本欄位 ===
        foreach (var f in BasicFields)
            rect = Draw(rect, property, f.field, f.label, f.tooltip);

        // --- 分隔線 ---
        rect = DrawSep(rect);

        // === 資源檢查 ===
        rect = Draw(rect, property, "useResourceCheck", "資源檢查", "不勾選則直接觸發 onSelected");

        bool useCheck = property.FindPropertyRelative("useResourceCheck").boolValue;
        if (useCheck)
        {
            EditorGUI.indentLevel++;

            foreach (var f in CheckFields)
                rect = Draw(rect, property, f.field, f.label, f.tooltip);

            if (property.FindPropertyRelative("checkTime").boolValue)
                rect = Draw(rect, property, "timeAmount", "時段數量", "需要消耗的時段數量");

            EditorGUI.indentLevel--;
        }

        // --- 分隔線 ---
        rect = DrawSep(rect);

        // === 事件 ===
        string selLabel = useCheck ? "onSelected（通過）" : "onSelected";
        string selTip = useCheck ? "資源與時間皆通過時觸發" : "點擊時觸發";
        rect = Draw(rect, property, "onSelected", selLabel, selTip);

        if (useCheck)
        {
            rect = Draw(rect, property, "onFailure", "onFailure", "資源不足時觸發");
            rect = Draw(rect, property, "onTimeFailure", "onTimeFailure", "資源足夠但時間不足時觸發");
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    // ══════════════════════════════════════════
    //  工具
    // ══════════════════════════════════════════

    private float FieldH(SerializedProperty parent, string name)
    {
        var p = parent.FindPropertyRelative(name);
        return p != null ? EditorGUI.GetPropertyHeight(p, true) + Spacing : 0f;
    }

    private Rect Draw(Rect rect, SerializedProperty parent, string name, string label, string tooltip = "")
    {
        var p = parent.FindPropertyRelative(name);
        if (p == null) return rect;

        float h = EditorGUI.GetPropertyHeight(p, true);
        rect.height = h;
        EditorGUI.PropertyField(rect, p, new GUIContent(label, tooltip), true);
        rect.y += h + Spacing;
        return rect;
    }

    private Rect DrawSep(Rect rect)
    {
        rect.y += SeparatorHeight * 0.5f;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(0.35f, 0.35f, 0.35f));
        rect.y += SeparatorHeight * 0.5f;
        return rect;
    }
}
#endif