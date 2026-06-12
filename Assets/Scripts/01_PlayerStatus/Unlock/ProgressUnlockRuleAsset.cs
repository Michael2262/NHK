using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 單條進度解鎖規則 (獨立 ScriptableObject)。
/// 每條規則建立一個 .asset，例如 Rule_Sister_UnlockFootJob.asset。
///
/// 條件可混搭女主角數值 (Libido / Trust / HCount，需填 heroineID)
/// 與主角數值 (Stress / LifePower / Sociality / Dependency)，
/// 全部達成 (All) 才算符合。
///
/// 設計原則：Rule 是「自足」的 — 任何引用它的地方 (ProgressUnlockConfig、
/// RequirementBlocker、甚至未來新系統) 都不需要額外上下文就能完整評估條件。
///
/// 建議在 Project 視窗 Create → Game → Progress → Progress Unlock Rule 建立，
/// 並依角色分資料夾管理 (例如 Assets/.../UnlockRules/Sister/)。
/// </summary>
[CreateAssetMenu(
    menuName = "Game/Progress/Progress Unlock Rule",
    fileName = "Rule_NewUnlockRule"
)]
public class ProgressUnlockRuleAsset : ScriptableObject
{
    [Tooltip("備註：方便在 Inspector 中識別這條規則的用途")]
    public string ruleName;

    // ───── 條件 ─────
    [Header("條件")]
    [Tooltip(
        "要檢查哪一位女主角 (HeroineID，需與 HeroineStatusConfig 中的 ID 一致)。\n" +
        "條件中含女主角數值 (Libido / Trust / HCount) 時必填；\n" +
        "純主角數值的規則可留空。"
    )]
    public string heroineID;

    [Tooltip(
        "解鎖條件列表，全部達成 (All) 才算符合。\n" +
        "Libido / Trust / HCount 查上方 heroineID 指定的女主角；\n" +
        "Stress / LifePower / Sociality / Dependency 查主角。"
    )]
    public List<UnlockStatCondition> conditions = new List<UnlockStatCondition>();

    // ───── 執行動作 ─────
    [Header("達成時的動作")]
    [Tooltip(
        "達成條件時，要對 Progress 做甚麼操作。\n" +
        "選 OnlyCondition 代表此規則只用來當 UI 阻擋條件，不會動 Progress。"
    )]
    public ProgressActionType action = ProgressActionType.SetFlagOn;

    [Tooltip(
        "目標：ProgressFlagDefinition 或 ProgressValueDefinition 擇一拖入。\n" +
        "action 為 OnlyCondition 時可留空。"
    )]
    public ProgressBaseDefinition target;

    [Tooltip("當 action 為 SetValue 時要寫入的數值；其他情況忽略")]
    public int valueToSet = 1;

    // ───── 失去條件時的行為 ─────
    [Header("失去條件時的行為")]
    [Tooltip(
        "勾選：條件不成立時會自動「撤銷」(SetFlagOn→RemoveFlag；SetValue→設回 0)，再次達成時會重新套用。\n" +
        "不勾：達成一次就永久啟用 (即使後來數值降低也不會撤銷)，這是預設行為。\n" +
        "OnlyCondition 類型不受此欄位影響 (本來就不會動 Progress)。"
    )]
    public bool revertWhenConditionFails = false;

    // ==========================================================
    // UI 提示資訊 (供 RequirementBlocker 使用)
    // ==========================================================

    [Header("UI 提示 (供 RequirementBlocker 顯示時使用)")]
    [Tooltip(
        "類型文字的 localization key，Blocker 會顯示在「類型名稱」位置。\n" +
        "留空則自動用第一個條件的數值名稱 (Libido / Trust / Stress …)。"
    )]
    public string uiHintTypeKeyOverride;

    [Tooltip(
        "自訂顯示等級，Blocker 會填入 LevelFormat。\n" +
        "留 0 則自動用第一個條件的閾值。"
    )]
    public int uiHintLevelOverride = 0;

    // ==========================================================
    // 條件組成查詢 (給 Manager / Blocker 決定訂閱對象)
    // ==========================================================

    /// <summary> 是否含有女主角數值條件 (Libido / Trust / HCount) </summary>
    public bool HasHeroineCondition
    {
        get
        {
            if (conditions == null) return false;
            foreach (var c in conditions)
                if (c != null && c.IsHeroineStat) return true;
            return false;
        }
    }

    /// <summary> 是否含有主角數值條件 (Stress / LifePower / Sociality / Dependency) </summary>
    public bool HasProtagonistCondition
    {
        get
        {
            if (conditions == null) return false;
            foreach (var c in conditions)
                if (c != null && !c.IsHeroineStat) return true;
            return false;
        }
    }

    // ==========================================================
    // 輔助方法 (給 RequirementBlocker / Manager 共用)
    // ==========================================================

    /// <summary>
    /// 取得此規則用於 UI 顯示的「所需等級」。
    /// 若 uiHintLevelOverride 有填值 (>0) 則用它；否則用第一個條件的閾值。
    /// </summary>
    public int GetUIDisplayLevel()
    {
        if (uiHintLevelOverride > 0) return uiHintLevelOverride;

        if (conditions != null)
        {
            foreach (var c in conditions)
                if (c != null) return c.threshold;
        }
        return 0;
    }

    /// <summary>
    /// 取得此規則用於 UI 顯示的「類型文字 key」(localization key)。
    /// 若 uiHintTypeKeyOverride 有填值則用它；否則用第一個條件的數值名稱。
    /// </summary>
    public string GetUIDisplayTypeKey()
    {
        if (!string.IsNullOrEmpty(uiHintTypeKeyOverride)) return uiHintTypeKeyOverride;

        if (conditions != null)
        {
            foreach (var c in conditions)
                if (c != null) return c.stat.ToString();
        }
        return null;
    }

    /// <summary>
    /// 檢查是否達成此規則的所有條件 (All 邏輯)。
    /// 沒有任何條件時視為「不達成」(空規則多半是設定遺漏)。
    /// heroine 可為 null (純主角條件的規則)；含女主角條件而 heroine 為 null 時不達成。
    /// </summary>
    public bool IsConditionMet(HeroineStatusModel heroine, ProtagonistStatusModel protagonist)
    {
        if (conditions == null || conditions.Count == 0) return false;

        foreach (var c in conditions)
        {
            if (c == null) continue;
            if (!c.IsMet(heroine, protagonist)) return false;
        }
        return true;
    }

    /// <summary>
    /// 所有條件的文字摘要，例如 "Libido≥3 & Stress<50" (Editor 視窗與 Debug 用)
    /// </summary>
    public string GetConditionsSummary()
    {
        if (conditions == null || conditions.Count == 0) return "(無條件)";

        var sb = new StringBuilder();
        foreach (var c in conditions)
        {
            if (c == null) continue;
            if (sb.Length > 0) sb.Append(" & ");
            sb.Append(c.ToSummary());
        }
        return sb.Length > 0 ? sb.ToString() : "(無條件)";
    }
}
