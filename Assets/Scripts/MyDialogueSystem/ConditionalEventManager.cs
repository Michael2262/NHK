using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// 繼承自 DialogueVariableController 的靜態方法，用於方便存取。
/// 為了程式碼的自給自足，這裡再次引入了其依賴。
/// 實際應用中，您可能已經將 DialogueVariableController 作為獨立檔案。
/// </summary>
using PixelCrushers.DialogueSystem;

// --- 輔助結構和類別定義 ---

[Serializable]
public enum LogicOperator
{
    AND, // 所有的條件必須為真 (預設)
    OR   // 至少有一個條件必須為真
}

[Serializable]
public enum CheckType
{
    BoolCheck,    // 檢查布林變數是否為 True
    NumberCheck   // 檢查數字變數是否符合某個比較條件
}

[Serializable]
public enum ComparisonOperator
{
    Equal,        // =
    NotEqual,     // !=
    GreaterThan,  // >
    LessThan,     // <
    GreaterOrEqual, // >=
    LessOrEqual     // <=
}

[Serializable]
public class Condition
{
    [Tooltip("要檢查的對話系統變數名稱。")]
    public string VariableName;

    [Tooltip("要執行的檢查類型：布林檢查或數字檢查。")]
    public CheckType Type = CheckType.BoolCheck;

    // --- BoolCheck 專屬欄位 ---
    [Tooltip("BoolCheck：布林變數預期應為的值 (True)。")]
    [HideInInspector] // BoolCheck 預設檢查 True，不需要在 Inspector 中顯示
    public bool ExpectedBoolValue = true;

    // --- NumberCheck 專屬欄位 ---
    [Tooltip("NumberCheck：用於比較的運算符號。")]
    public ComparisonOperator Comparison = ComparisonOperator.Equal;

    [Tooltip("NumberCheck：用於比較的數值。")]
    public float CompareValue;

    /// <summary>
    /// 執行單個條件檢查，並透過 DialogueVariableController 取得結果。
    /// </summary>
    public bool Evaluate()
    {
        switch (Type)
        {
            case CheckType.BoolCheck:
                // 接口 3: 查找 bool 是否為 true
                bool currentBool = DialogueVariableController.GetBoolVariable(VariableName);
                return currentBool == ExpectedBoolValue;

            case CheckType.NumberCheck:
                // 接口 4: 查找 number 是否符合條件
                string opString = GetOperatorString(Comparison);
                return DialogueVariableController.CheckNumberCondition(VariableName, opString, CompareValue);

            default:
                Debug.LogError($"條件 '{VariableName}' 檢查類型無效: {Type}");
                return false;
        }
    }

    private string GetOperatorString(ComparisonOperator op)
    {
        switch (op)
        {
            case ComparisonOperator.Equal: return "==";
            case ComparisonOperator.NotEqual: return "!=";
            case ComparisonOperator.GreaterThan: return ">";
            case ComparisonOperator.LessThan: return "<";
            case ComparisonOperator.GreaterOrEqual: return ">=";
            case ComparisonOperator.LessOrEqual: return "<=";
            default: return "==";
        }
    }
}

[Serializable]
public class ConditionGroup
{
    [Tooltip("此組的名稱，僅供編輯器識別。")]
    public string GroupName;

    [Tooltip("多個條件間的邏輯關係 (AND/OR)。")]
    public LogicOperator Logic = LogicOperator.AND;

    [Tooltip("要檢查的條件列表。")]
    public List<Condition> Conditions = new List<Condition>();

    [Tooltip("當條件達成時要觸發的 Unity 事件。")]
    public UnityEvent OnConditionsMet;

    [Tooltip("如果條件達成，這個組是否應該被禁用 (只觸發一次)。")]
    public bool DisableAfterTrigger = true;

    [NonSerialized]
    public bool HasBeenTriggered = false;


    /// <summary>
    /// 評估此條件組的整體條件是否達成。
    /// </summary>
    public bool Evaluate()
    {
        if (Conditions == null || Conditions.Count == 0)
        {
            return false; // 沒有條件就無法評估
        }

        if (Logic == LogicOperator.AND)
        {
            // AND 邏輯：所有條件必須為真
            foreach (var condition in Conditions)
            {
                if (!condition.Evaluate())
                {
                    return false; // 只要有一個條件不符合，整個組就不成立
                }
            }
            return true; // 所有條件都符合
        }
        else // LogicOperator.OR
        {
            // OR 邏輯：至少一個條件必須為真
            foreach (var condition in Conditions)
            {
                if (condition.Evaluate())
                {
                    return true; // 只要有一個條件符合，整個組就成立
                }
            }
            return false; // 所有條件都不符合
        }
    }
}

/// <summary>
/// 核心管理腳本。在 OnEnable 時檢查條件，並提供一個公共方法來在變數改變時觸發檢查。
/// </summary>
public class ConditionalEventManager : MonoBehaviour
{
    [Tooltip("定義一組組條件及其事件。")]
    public List<ConditionGroup> EventGroups = new List<ConditionGroup>();

    private void OnEnable()
    {
        // 3. OnEnable 時判斷一次事件觸發
        // 遊戲載入或物件啟用時，執行初始檢查
        CheckAllConditions();

        // --- 變數監聽備註 ---
        // 由於 Pixel Crushers Dialogue System 沒有內建的「通用變數變動事件」，
        // 最有效率的監聽方法通常是在您呼叫 DialogueVariableController.Set... 的
        // 地方（例如：當玩家拾取物品、擊殺敵人時）主動呼叫此 Manager 的 CheckAllConditions()。
        //
        // 另一種方法是在 Update/Coroutine 中定期執行 CheckAllConditions()，但這效率較低。
        // 以下提供一個公用方法供外部程式碼呼叫。
        // ---
    }

    /// <summary>
    /// 在遊戲變數（例如：玩家狀態、任務進度）發生變化時，從外部呼叫此方法。
    /// 這是效率最高的事件監聽方式。
    /// </summary>
    public void CheckAllConditions()
    {
        for (int i = 0; i < EventGroups.Count; i++)
        {
            ConditionGroup group = EventGroups[i];

            // 檢查：如果組被設定為只觸發一次且已經觸發過，則跳過。
            if (group.DisableAfterTrigger && group.HasBeenTriggered)
            {
                continue;
            }

            // 1. 檢查條件是否達成
            if (group.Evaluate())
            {
                // 2. 達成條件，觸發事件
                Debug.Log($"條件組 '{group.GroupName}' 達成！觸發事件。");
                group.OnConditionsMet.Invoke();

                // 標記為已觸發
                group.HasBeenTriggered = true;
            }
        }
    }
}

// 為了讓 ConditionalEventManager 在編譯上獨立，我們將 DialogueVariableController 
// 的程式碼放在這裡，假設它與其他 C# 檔案中的實作是相同的。
// 如果您已經有 DialogueVariableController.cs，請刪除下面的程式碼，並確保它在專案中可見。

#region DialogueVariableController 副本
public static class DialogueVariableController
{
    public static void SetBoolVariable(string variableName, bool value)
    {
        DialogueLua.SetVariable(variableName, value);
    }

    public static void SetNumberVariable(string variableName, float value)
    {
        DialogueLua.SetVariable(variableName, value);
    }

    public static bool GetBoolVariable(string variableName)
    {
        return DialogueLua.GetVariable(variableName).asBool;
    }

    public static float GetNumberVariable(string variableName)
    {
        return DialogueLua.GetVariable(variableName).asFloat;
    }

    public static bool CheckNumberCondition(string variableName, string op, float compareValue)
    {
        float currentValue = GetNumberVariable(variableName);

        switch (op)
        {
            case "=":
            case "==":
                return Mathf.Approximately(currentValue, compareValue);
            case ">":
                return currentValue > compareValue;
            case "<":
                return currentValue < compareValue;
            case ">=":
                return currentValue >= compareValue;
            case "<=":
                return currentValue <= compareValue;
            case "!=":
                return !Mathf.Approximately(currentValue, compareValue);
            default:
                return false;
        }
    }
}
#endregion
