using UnityEngine;

/// <summary>
/// 效果：提供臨時的基礎屬性 Buff/Debuff (攻擊、防禦、興奮度)。
/// </summary>
[CreateAssetMenu(menuName = "Game/Status Effects/Temporary Stat Buff")]
public class StatusEffect_TemporaryStatBuff : StatusEffect
{
    // ▼▼▼【★ 修改點 1：加入 Excitement ★】▼▼▼
    public enum StatType { Attack, Defense, Excitement } // <-- 加入 Excitement
    // ▲▲▲【修改結束】▲▲▲

    public StatType StatToModify; //
    public int Amount; //

    // OnApply, OnDayPassed, OnRemove 保持不變
    // (它們只負責 Log 或觸發事件，實際數值由 ProtagonistStatusEffectModel 計算)
    public override void OnApply(ProtagonistStatusModel target)
    {
        Debug.Log($"獲得了臨時 Buff/Debuff：{DisplayName}！({StatToModify} {(Amount >= 0 ? "+" : "")}{Amount})"); // (可加入數值顯示)
        // 觸發總值重新計算會在 ProtagonistStatusEffectModel.AddEffect 中完成
    }

    public override void OnDayPassed(ProtagonistStatusModel target) { } //

    public override void OnRemove(ProtagonistStatusModel target)
    {
        Debug.Log($"臨時 Buff/Debuff：{DisplayName} 已失效。"); //
        // 觸發總值重新計算會在 ProtagonistStatusEffectModel.HandleDayPassed 中完成
    }

    // --- ▼▼▼【★ 修改點 2：覆寫 Modifier 方法 ★】▼▼▼ ---
    public override int GetAttackModifier()
    {
        // 如果 StatToModify 是 Attack，返回 Amount；否則返回 0
        return (StatToModify == StatType.Attack) ? Amount : 0;
    }

    public override int GetDefenseModifier()
    {
        // 如果 StatToModify 是 Defense，返回 Amount；否則返回 0
        return (StatToModify == StatType.Defense) ? Amount : 0;
    }

    public override int GetExcitementModifier()
    {
        // 如果 StatToModify 是 Excitement，返回 Amount；否則返回 0
        return (StatToModify == StatType.Excitement) ? Amount : 0;
    }
    // --- ▲▲▲【修改結束】▲▲▲ ---
}