/// <summary>
/// 一個通用的角色狀態契約。
/// 無論是主角還是女主角，只要實作了這個介面，
/// 就代表他們是可以被道具效果影響的目標。
/// </summary>
public interface ICharacterStatus
{
    // 合約規定：所有角色都必須有一個可以增加攻擊力的方法
    void AddAttack(int amount);

    // 合約規定：所有角色都必須有一個可以增加防禦力的方法
    void AddDefense(int amount);

    // ★ 新增契約：任何角色都必須有體力
    int Stamina { get; }
    int StaminaMax { get; }
    void AddStamina(int amount);

    // ★ 新增契約：任何角色都必須有精神力

    int Spirit { get; }
    int SpiritMax { get; }

    void AddSpirit(int amount);

}