using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Trigger/Heroine Trigger Container", fileName = "Trigger_HeroineName")]
public class HeroineTriggerContainer : ScriptableObject
{
    [Tooltip("對應 HeroineStatusModel 的 ID")]
    public string HeroineID;

    [Header("--- 自動同步數值 (Sync Values) ---")]
    [Tooltip("將此女角的開發度等級同步到此 Value SO")]
    public ProgressValueDefinition LewdnessSyncSO;

    [Tooltip("將此女角的興奮度等級同步到此 Value SO")]
    public ProgressValueDefinition ExcitementSyncSO;

    [Header("--- 開發度觸發規則 (Lewdness) ---")]
    public List<StatTriggerRule> LewdnessRules = new List<StatTriggerRule>();

    [Header("--- 興奮度觸發規則 (Excitement) ---")]
    public List<StatTriggerRule> ExcitementRules = new List<StatTriggerRule>();
}