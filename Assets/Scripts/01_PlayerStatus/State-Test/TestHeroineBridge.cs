using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>正式女主角數值轉接（保留舊元件名稱）；掛上元件後可由 Button／UnityEvent 呼叫。</summary>
    [AddComponentMenu("Game/State Test/Test Heroine Bridge")]
    public class TestHeroineBridge : MonoBehaviour
    {
        [SerializeField, Tooltip("正式角色 ID；只有一位角色時可留空。") ] private string heroineID;
        public void SetHeroineID(string id) => heroineID = id;

        private global::HeroineStatusModel Model
        {
            get
            {
                var service = GameStatusService.Instance;
                var model = service != null ? service.GetHeroine(heroineID) : null;
                if (model == null)
                    Debug.LogWarning("[TestHeroineBridge] 找不到正式女主角，請確認 GameStatusService 與 Heroine ID；多角色時不可留空。", this);
                return model;
            }
        }

        public int GetTrust() => Model?.Trust ?? 0;
        public int GetMood() => Model?.Mood ?? 0;
        public int GetAffinity() => Model?.Affinity ?? 0;
        public int GetLibido() => Model?.Libido ?? 0;

        public void SetTrust(int value) => Model?.SetTrust(value);
        public void AddTrust(int amount) => Model?.AddTrust(amount);
        public void SetMood(int value) => Model?.SetMood(value);
        public void AddMood(int amount) => Model?.AddMood(amount);
        public void SetAffinity(int value) => Model?.SetAffinity(value);
        public void AddAffinity(int amount) => Model?.AddAffinity(amount);
        public void SetLibido(int value) => Model?.SetLibido(value);
        public void AddLibido(int amount) => Model?.AddLibido(amount);

    }
}

