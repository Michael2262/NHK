using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>正式主角數值轉接（保留舊元件名稱與事件綁定）；掛上元件後可由 Button／UnityEvent 呼叫。</summary>
    [AddComponentMenu("Game/State Test/Test Protagonist Bridge")]
    public class TestProtagonistBridge : MonoBehaviour
    {
        private global::ProtagonistStatusModel Model
        {
            get
            {
                var service = GameStatusService.Instance;
                var model = service != null ? service.Protagonist : null;
                if (model == null)
                    Debug.LogWarning("[TestProtagonistBridge] 正式主角 Model 尚未初始化，請確認場景中有 GameStatusService。", this);
                return model;
            }
        }

        public int GetStaminaMax() => Model?.StaminaMax ?? 0;
        public int GetStamina() => Model?.Stamina ?? 0;
        public int GetShootTimesMax() => Model?.ShootTimesMax ?? 0;
        public int GetShootTimes() => Model?.ShootTimes ?? 0;
        public bool GetIsOverShoot() => Model?.IsOverShoot ?? false;
        public int GetLust() => Model?.Lust ?? 0;
        public int GetStickerCount() => Model?.StickerCount ?? 0;
        public int GetPendingStickerCount() => Model?.PendingStickerCount ?? 0;
        public int GetMoney() => Model?.Money ?? 0;

        public void SetStaminaMax(int value) => Model?.SetStaminaMax(value);
        public void AddStaminaMax(int amount) => Model?.AddStaminaMax(amount);
        public void SetStamina(int value) => Model?.SetStamina(value);
        public void AddStamina(int amount) => Model?.AddStamina(amount);
        public void ForceAddStamina(int amount) => Model?.ForceAddStamina(amount);
        public void SetShootTimesMax(int value) => Model?.SetShootTimesMax(value);
        public void AddShootTimesMax(int amount) => Model?.AddShootTimesMax(amount);
        public void SetShootTimes(int value) => Model?.SetShootTimes(value);
        public void AddShootTimes(int amount) => Model?.AddShootTimes(amount);
        public void ForceAddShootTimes(int amount) => Model?.ForceAddShootTimes(amount);
        public void SetLust(int value) => Model?.SetLust(value);
        public void AddLust(int amount) => Model?.AddLust(amount);
        public void SetStickerCount(int value) => Model?.SetStickerCount(value);
        public void AddStickerCount(int amount) => Model?.AddStickerCount(amount);
        public void SetPendingStickerCount(int value) => Model?.SetPendingStickerCount(value);
        public void AddPendingStickerCount(int amount) => Model?.AddPendingStickerCount(amount);
        public void SetMoney(int value) => Model?.SetMoney(value);
        public void AddMoney(int amount) => Model?.AddMoney(amount);

        // void 入口可綁定 Button／UnityEvent；是否有可領數量由 Model 判斷。
        public void RedeemPendingStickers() => Model?.RedeemPendingStickers();

    }
}

