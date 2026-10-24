using UnityEngine;

/// <summary>正式主角 UnityEvent 接口；範圍、結算與存檔全部由 Model 負責。</summary>
[AddComponentMenu("Game/API/Protagonist Bridge API")]
public class ProtagonistBridgeAPI : MonoBehaviour
{
    private ProtagonistStatusModel P => GameStatusService.Instance?.Protagonist;

    // ───── 體力上限 ─────
    public int GetStaminaMax() => P?.StaminaMax ?? 0;
    public void SetStaminaMax(int value) => P?.SetStaminaMax(value);
    public void AddStaminaMax(int amount) => P?.AddStaminaMax(amount);

    // ───── 體力 ─────
    public int GetStamina() => P?.Stamina ?? 0;
    public void SetStamina(int value) => P?.SetStamina(value);
    public void AddStamina(int amount) => P?.AddStamina(amount);
    public void ForceAddStamina(int amount) => P?.ForceAddStamina(amount);

    // ───── 射精次數上限 ─────
    public int GetShootTimesMax() => P?.ShootTimesMax ?? 0;
    public void SetShootTimesMax(int value) => P?.SetShootTimesMax(value);
    public void AddShootTimesMax(int amount) => P?.AddShootTimesMax(amount);

    // ───── 射精次數 ─────
    public int GetShootTimes() => P?.ShootTimes ?? 0;
    public void SetShootTimes(int value) => P?.SetShootTimes(value);
    public void AddShootTimes(int amount) => P?.AddShootTimes(amount);
    public void ForceAddShootTimes(int amount) => P?.ForceAddShootTimes(amount);

    // ───── 邪念 ─────
    public int GetLust() => P?.Lust ?? 0;
    public void SetLust(int value) => P?.SetLust(value);
    public void AddLust(int amount) => P?.AddLust(amount);

    // ───── 持有貼紙 ─────
    public int GetStickerCount() => P?.StickerCount ?? 0;
    public void SetStickerCount(int value) => P?.SetStickerCount(value);
    public void AddStickerCount(int amount) => P?.AddStickerCount(amount);

    // ───── 待領貼紙 ─────
    public int GetPendingStickerCount() => P?.PendingStickerCount ?? 0;
    public void SetPendingStickerCount(int value) => P?.SetPendingStickerCount(value);
    public void AddPendingStickerCount(int amount) => P?.AddPendingStickerCount(amount);
    public void RedeemPendingStickers() => P?.RedeemPendingStickers();

    // ───── 金錢 ─────
    public int GetMoney() => P?.Money ?? 0;
    public void SetMoney(int value) => P?.SetMoney(value);
    public void AddMoney(int amount) => P?.AddMoney(amount);
    public void ReduceMoney(int amount)
    {
        if (P?.TryReduceMoney(amount) != true)
            Debug.LogWarning("[ProtagonistBridgeAPI] 金錢不足、數量無效或 Model 尚未初始化。", this);
    }

    // ───── 技能點 ─────
    public int GetSkillPoints() => P?.SkillPoints ?? 0;
    public void SetSkillPoints(int value) => P?.SetSkillPoints(value);
    public void AddSkillPoints(int amount) => P?.AddSkillPoints(amount);
    public void ReduceSkillPoints(int amount)
    {
        if (P?.TryReduceSkillPoints(amount) != true)
            Debug.LogWarning("[ProtagonistBridgeAPI] 技能點不足、數量無效或 Model 尚未初始化。", this);
    }

    // ───── 射精感 ─────
    public int GetSemen() => P?.Semen ?? 0;
    public void SetSemen(int value) => P?.SetSemen(value);
    public void AddSemen(int amount) => P?.AddSemen(amount);

    // ───── 房間髒亂 ─────
    public int GetRoomMessLevel() => P?.RoomMessLevel ?? 0;
    public void SetRoomMessLevel(int value) => P?.SetRoomMessLevel(value);
    public void AddRoomMessLevel(int amount) => P?.AddRoomMessLevel(amount);
    public void ReduceRoomMessLevel(int amount) => P?.ReduceRoomMessLevel(amount);

    public bool GetIsOverShoot() => P?.IsOverShoot ?? false;

    public void AdvanceTime(int slots)
    {
        var t = GameStatusService.Instance?.Time;
        if (t == null)
        {
            Debug.LogWarning("[ProtagonistBridgeAPI] TimeSystemModel 尚未初始化，無法推進時間。");
            return;
        }
        t.AdvanceTime(slots);
    }
}
