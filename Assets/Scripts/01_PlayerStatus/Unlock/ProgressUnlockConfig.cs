using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 解鎖設定表。每列規則直接存在此 Config，不需建立獨立 Rule 資產。
/// 規則、數值映射與布林映射可集中在同一份設定表，由 GameStatusService 交給 Manager。
/// </summary>
[CreateAssetMenu(
    menuName = "Game/Progress/Progress Unlock Config",
    fileName = "ProgressUnlock_NewGroup"
)]
public class ProgressUnlockConfig : ScriptableObject
{
    [Tooltip("設定表內的規則。每列可以有多個條件，全部成立才執行指定動作。")]
    public List<ProgressUnlockRule> entries = new List<ProgressUnlockRule>();

    [Header("數值映射 (Sync / 鏡像)")]
    [Tooltip(
        "把某個數值忠實同步到 Progress Value 變數 (數值一變就跟著變)。\n" +
        "女主角數值 (Libido / Trust / HCount) 需填 heroineID；主角數值留空。\n" +
        "與門檻規則不同：這裡不判斷大小，只是讓 Value 永遠等於來源的當下值。"
    )]
    public List<StatMirror> mirrors = new List<StatMirror>();

    [Header("布林 Flag 映射 (Sync / 鏡像)")]
    [Tooltip(
        "把主角布林狀態忠實同步到 Progress Flag（狀態一變就跟著開關）。\n" +
        "來源支援 BadHealthy / BadDependency；true 時加入 Persistent Flag，false 時移除 Flag。\n" +
        "此映射為單向：修改 Progress Flag 不會反向修改主角狀態。"
    )]
    public List<BoolFlagMirror> flagMirrors = new List<BoolFlagMirror>();

}

/// <summary>設定表中的一列；純資料與條件查詢，不是 Unity 資產。</summary>
[System.Serializable]
public class ProgressUnlockRule
{
    public string name = "新規則";
    public string ruleName;
    public string heroineID;
    public List<UnlockStatCondition> conditions = new List<UnlockStatCondition>();
    public ProgressActionType action = ProgressActionType.SetFlagOn;
    public ProgressBaseDefinition target;
    public int valueToSet = 1;
    public bool revertWhenConditionFails;
    public string unlockAlertKey;

    public bool HasHeroineCondition => conditions != null && conditions.Exists(c => c != null && c.IsHeroineStat);
    public bool HasProtagonistCondition => conditions != null && conditions.Exists(c => c != null && !c.IsHeroineStat);

    public bool IsConditionMet(HeroineStatusModel heroine, ProtagonistStatusModel protagonist)
    {
        if (conditions == null) return false;
        bool hasCondition = false;
        foreach (var condition in conditions)
        {
            if (condition == null) continue;
            hasCondition = true;
            if (!condition.IsMet(heroine, protagonist)) return false;
        }
        return hasCondition;
    }

    public string GetConditionsSummary()
    {
        var parts = new List<string>();
        if (conditions != null)
            foreach (var condition in conditions)
                if (condition != null) parts.Add(condition.ToSummary());
        return parts.Count == 0 ? "(無條件)" : string.Join(" & ", parts);
    }

    /// <summary>複製條件內容，避免表內編輯連帶改到其他列。</summary>
    public ProgressUnlockRule Copy()
    {
        var copy = (ProgressUnlockRule)MemberwiseClone();
        copy.conditions = new List<UnlockStatCondition>();
        if (conditions != null)
            foreach (var condition in conditions)
                if (condition != null)
                    copy.conditions.Add(new UnlockStatCondition
                    {
                        stat = condition.stat, op = condition.op, threshold = condition.threshold
                    });
        return copy;
    }
}
