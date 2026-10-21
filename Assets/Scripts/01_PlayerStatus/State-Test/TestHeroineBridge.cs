using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>測試版數值轉接；掛上元件後可由 Button／UnityEvent 呼叫。</summary>
    [AddComponentMenu("Game/State Test/Test Heroine Bridge")]
    public class TestHeroineBridge : MonoBehaviour
    {
        private HeroineStatusModel Model
        {
            get
            {
                var service = GameStatusService.Instance;
                var model = service != null ? service.TestHeroine : null;
                if (model == null)
                    Debug.LogWarning("[TestHeroineBridge] 測試 Model 尚未初始化，請確認場景中有 GameStatusService。", this);
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

