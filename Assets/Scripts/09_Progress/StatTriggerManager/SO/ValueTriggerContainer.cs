using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用的數值觸發容器 SO
/// 用於定義「單一數值來源 → 多個 Flag/Value」的映射規則
/// 適用於：Phase、DayOfWeek、Weather、或任何其他數值來源
/// </summary>
[CreateAssetMenu(menuName = "Game/Trigger/Value Trigger Container", fileName = "Trigger_NewSource")]
public class ValueTriggerContainer : ScriptableObject
{
    [Header("--- 來源識別 ---")]
    [Tooltip("識別這個來源的 ID（如 Phase、DayOfWeek、Weather 等）")]
    public string SourceID;

    [Header("--- 備註 ---")]
    [Tooltip("記錄定義此 ID 的程式碼位置，方便日後維護")]
    [TextArea(2, 4)]
    public string Note;

    [Header("--- 自動同步數值 (可選) ---")]
    [Tooltip("將來源數值直接同步到此 Value SO（留空則不同步）")]
    public ProgressValueDefinition SyncValueSO;

    [Header("--- 觸發規則 ---")]
    [Tooltip("根據來源數值觸發的規則列表")]
    public List<StatTriggerRule> Rules = new List<StatTriggerRule>();
}