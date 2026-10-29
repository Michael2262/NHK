using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>單一女主角的互斥行程；每條行程以專用 Flag 記錄啟用狀態。</summary>
[CreateAssetMenu(menuName = "Game/Config/Heroine Flag Schedule")]
public class HeroineFlagScheduleConfig : ScriptableObject
{
    public string heroineID;
    [Tooltip("只用於新遊戲。讀檔永遠依狀態 Flag 還原，不以預設行程補救異常存檔。")]
    public string defaultRuleID;
    public List<HeroineFlagScheduleRule> rules = new List<HeroineFlagScheduleRule>();
}

[Serializable]
public class HeroineFlagScheduleRule
{
    [Tooltip("呼叫行程時的識別碼，不作為存檔狀態來源。")]
    public string ruleID = Guid.NewGuid().ToString("N");
    public string ruleName;
    [Tooltip("必填且不得與其他行程共用。ON 表示此行程啟用，正式切換會自動關閉同角色其餘行程。")]
    public ProgressFlagDefinition stateFlag;
    public string locationID;
    [Tooltip("至少填一個活動 ID。同一角色不可在多個行程使用相同活動 ID。依 Flag 還原時等機率抽選，指定活動時則使用指定值。")]
    public List<string> activityIDs = new List<string>();
    // 舊資產的單一活動仍可讀取；編輯器轉成清單後清除此欄位。
    [HideInInspector] public string activityState;

    public string[] GetActivityIDs()
    {
        if (activityIDs != null && activityIDs.Count > 0) return activityIDs.ToArray();
        return string.IsNullOrWhiteSpace(activityState) ? Array.Empty<string>() : new[] { activityState };
    }

    // 保留第一版的序列化資料供人工對照，不再參與運算，也不自動推測要轉成哪個狀態 Flag。
    [HideInInspector] public bool enabled = true;
    [HideInInspector] public int priority;
    [HideInInspector] public int weight = 100;
    [HideInInspector] public ScheduleFlagMatchMode flagMatchMode;
    [HideInInspector] public List<ScheduleFlagCondition> conditions = new List<ScheduleFlagCondition>();
}
