using UnityEngine;

/// <summary>
/// 單條女主角解鎖規則 (獨立 ScriptableObject)。
/// 每條規則建立一個 .asset，例如 Rule_Alice_UnlockNight.asset。
///
/// 設計原則：Rule 是「自足」的 — 任何引用它的地方 (HeroineUnlockConfig、
/// RequirementBlocker、甚至未來新系統) 都不需要額外上下文就能完整評估條件。
///
/// 建議在 Project 視窗 Create → Game → Progress → Heroine Unlock Rule 建立，
/// 並依女主角分資料夾管理 (例如 Assets/.../Rules/Alice/、Assets/.../Rules/Bella/)。
/// </summary>
[CreateAssetMenu(
    menuName = "Game/Progress/Heroine Unlock Rule",
    fileName = "Rule_NewHeroineRule"
)]
public class HeroineUnlockRuleAsset : ScriptableObject
{
    [Tooltip("備註：方便在 Inspector 中識別這條規則的用途")]
    public string ruleName;

    // ───── 條件 ─────
    [Header("條件")]
    [Tooltip(
        "要檢查哪一位女主角 (HeroineID，需與 HeroineStatusConfig 中的 ID 一致)。\n" +
        "這是權威來源：無論 Rule 被誰引用 (Config 或 Blocker)，都以此 ID 為準。"
    )]
    public string heroineID;

    [Tooltip("要檢查哪些數值：只看 Lewd / 只看 Affinity / 兩者都要")]
    public HeroineUnlockConditionType conditionType = HeroineUnlockConditionType.LewdnessOnly;

    [Tooltip("LewdnessLevel 要達到多少 (>=) 才算達成 (當 conditionType 包含 Lewd 時生效)")]
    public int requiredLewdnessLevel = 0;

    [Tooltip("BaseAffinityLevel 要達到多少 (>=) 才算達成 (當 conditionType 包含 Affinity 時生效)")]
    public int requiredAffinityLevel = 0;

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
        "留空則 Blocker 會自動依 conditionType 推斷 (Lewdness / Affinity)。"
    )]
    public string uiHintTypeKeyOverride;

    [Tooltip(
        "自訂顯示等級，Blocker 會填入 LevelFormat。\n" +
        "留 0 則 Blocker 會自動依 conditionType 取值：\n" +
        "  LewdnessOnly → requiredLewdnessLevel\n" +
        "  AffinityOnly → requiredAffinityLevel\n" +
        "  BothRequired → 取兩者較大者 (通常用來提示「主要門檻」)"
    )]
    public int uiHintLevelOverride = 0;

    // ==========================================================
    // 輔助方法 (給 RequirementBlocker / Manager 共用)
    // ==========================================================

    /// <summary>
    /// 取得此規則用於 UI 顯示的「所需等級」。
    /// 若 uiHintLevelOverride 有填值 (>0) 則用它；否則依 conditionType 自動推斷。
    /// </summary>
    public int GetUIDisplayLevel()
    {
        if (uiHintLevelOverride > 0) return uiHintLevelOverride;

        switch (conditionType)
        {
            case HeroineUnlockConditionType.LewdnessOnly:
                return requiredLewdnessLevel;
            case HeroineUnlockConditionType.AffinityOnly:
                return requiredAffinityLevel;
            case HeroineUnlockConditionType.BothRequired:
                return Mathf.Max(requiredLewdnessLevel, requiredAffinityLevel);
            default:
                return 0;
        }
    }

    /// <summary>
    /// 取得此規則用於 UI 顯示的「類型文字 key」(localization key)。
    /// 若 uiHintTypeKeyOverride 有填值則用它；否則依 conditionType 自動推斷。
    /// </summary>
    public string GetUIDisplayTypeKey()
    {
        if (!string.IsNullOrEmpty(uiHintTypeKeyOverride)) return uiHintTypeKeyOverride;

        switch (conditionType)
        {
            case HeroineUnlockConditionType.LewdnessOnly:
                return "Lewdness";
            case HeroineUnlockConditionType.AffinityOnly:
                return "Affinity";
            case HeroineUnlockConditionType.BothRequired:
                return "Lewdness";
            default:
                return null;
        }
    }

    /// <summary>
    /// 檢查指定女主角是否達成此規則的條件。
    /// </summary>
    public bool IsConditionMet(HeroineStatusModel heroine)
    {
        if (heroine == null) return false;

        switch (conditionType)
        {
            case HeroineUnlockConditionType.LewdnessOnly:
                return heroine.LewdnessLevel >= requiredLewdnessLevel;
            case HeroineUnlockConditionType.AffinityOnly:
                return heroine.BaseAffinityLevel >= requiredAffinityLevel;
            case HeroineUnlockConditionType.BothRequired:
                return heroine.LewdnessLevel >= requiredLewdnessLevel
                    && heroine.BaseAffinityLevel >= requiredAffinityLevel;
            default:
                return false;
        }
    }
}
