using UnityEngine;

/// <summary>
/// 所有「持續性狀態效果」的抽象基礎類別 (藍圖)。
/// (已加入 Modifier 虛擬方法)
/// </summary>
public abstract class StatusEffect : ScriptableObject
{
    public string EffectID => name; //
    public string DisplayName; //
    [TextArea] public string Description; //
    public Sprite Icon; //

    [Tooltip("效果持續的天數。0 代表永久，-1 代表直到被手動移除。")]
    public int DurationInDays; //

    // --- 生命週期方法 (保持 abstract) ---
    public abstract void OnApply(ProtagonistStatusModel target); //
    public abstract void OnDayPassed(ProtagonistStatusModel target); //
    public abstract void OnRemove(ProtagonistStatusModel target); //

    // --- ▼▼▼【★ 新增 Modifier 虛擬方法 ★】▼▼▼ ---
    // 讓子類別可以覆寫這些方法來提供對應的數值修正

    /// <summary>
    /// (虛擬方法) 返回此效果提供的攻擊力修正值。預設為 0。
    /// </summary>
    public virtual int GetAttackModifier() { return 0; }

    /// <summary>
    /// (虛擬方法) 返回此效果提供的防禦力修正值。預設為 0。
    /// </summary>
    public virtual int GetDefenseModifier() { return 0; }

    /// <summary>
    /// (虛擬方法) 返回此效果提供的興奮度等級修正值。預設為 0。
    /// </summary>
    public virtual int GetExcitementModifier() { return 0; }

    // 您未來可以為其他屬性 (體力回復、精神力回復等) 加入更多類似的虛擬方法
    // public virtual int GetStaminaRegenModifier() { return 0; }
    // --- ▲▲▲【新增結束】▲▲▲ ---
}