using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NHK.StateTest
{
    /// <summary>依序領取獎勵，最後一筆可重複領取；進度僅保留於此元件的生命週期。</summary>
    [AddComponentMenu("Game/State Test/Good Child Task Rewarder")]
    public sealed class GoodChildTaskRewarder : MonoBehaviour
    {
        [Serializable]
        public sealed class RewardDefinition
        {
            [Min(1)] public int stickerThreshold = 1;
            public UnityEvent onReward = new UnityEvent();
        }

        [SerializeField] private Button rewardButton;
        [SerializeField, Min(1)] private int repeatStickerInterval = 5;
        [SerializeField] private List<RewardDefinition> rewards = new List<RewardDefinition>();
        private readonly RewardSequenceModel sequence = new RewardSequenceModel();
        private bool invokingEvents;
        private Button boundButton;
        private int lastRewardFrame = -1;

        private void OnEnable() => RefreshButton();
        private void Update() => RefreshButton();
        private void OnDisable() => UnbindButton();

        private void UnbindButton()
        {
            if (boundButton == null) return;
            boundButton.onClick.RemoveListener(Reward);
            boundButton.interactable = false;
            boundButton = null;
        }

        private void RefreshButton()
        {
            if (boundButton != rewardButton)
            {
                UnbindButton();
                boundButton = rewardButton;
                if (boundButton != null)
                {
                    boundButton.interactable = false;
                    // 舊場景若已手動綁定 Reward，就不再自動加入同一入口。
                    bool alreadyBound = false;
                    for (int i = 0; i < boundButton.onClick.GetPersistentEventCount(); i++)
                        if (boundButton.onClick.GetPersistentTarget(i) == this
                            && boundButton.onClick.GetPersistentMethodName(i) == nameof(Reward)
                            && boundButton.onClick.GetPersistentListenerState(i) != UnityEventCallState.Off)
                            alreadyBound = true;
                    if (!alreadyBound) boundButton.onClick.AddListener(Reward);
                }
            }
            if (boundButton != null)
                boundButton.interactable = isActiveAndEnabled && !invokingEvents
                    && lastRewardFrame != Time.frameCount && CanReward();
        }

        private bool CanReward()
        {
            var service = GameStatusService.Instance;
            return service != null && service.TestProtagonist != null
                && GetConfigurationError() == null
                && service.TestProtagonist.StickerCount >= sequence.GetThreshold(rewards);
        }

        /// <summary>提供 Inspector 與執行期相同的設定檢查，不修改既有事件或門檻。</summary>
        public string GetConfigurationError()
        {
            if (rewards == null || rewards.Count == 0) return "獎勵清單為空，領獎按鈕會保持禁用。";
            if (repeatStickerInterval < 1) return "最後獎勵重複間隔必須至少為 1。";
            int previous = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                var entry = rewards[i];
                if (entry == null || entry.onReward == null) return $"獎勵 {i + 1} 的資料為空。";
                if (entry.stickerThreshold <= previous) return $"獎勵 {i + 1} 的貼紙門檻必須大於 {previous}，請依序遞增。";
                if (entry.stickerThreshold > ProtagonistStatusModel.StickerCountMax)
                    return $"獎勵 {i + 1} 的門檻超過貼紙上限 {ProtagonistStatusModel.StickerCountMax}。";
                previous = entry.stickerThreshold;
            }
            return null;
        }

        /// <summary>每次呼叫領取一筆；全部領完後，每次都執行最後一筆。</summary>
        public void Reward()
        {
            if (!isActiveAndEnabled || invokingEvents || lastRewardFrame == Time.frameCount || !CanReward()) return;
            var reward = rewards[sequence.GetNextIndex(rewards.Count)];
            if (reward == null || reward.onReward == null)
            {
                Debug.LogWarning("[GoodChildTaskRewarder] 目前獎勵資料為空，未推進領取順序。", this);
                return;
            }

            // 先推進，再執行；事件內重複呼叫自身不會連續領取。
            sequence.Advance(rewards, repeatStickerInterval);
            lastRewardFrame = Time.frameCount;
            invokingEvents = true;
            if (boundButton != null) boundButton.interactable = false;
            try { reward.onReward.Invoke(); }
            finally
            {
                invokingEvents = false;
                if (isActiveAndEnabled) RefreshButton();
            }
        }
    }

    /// <summary>獎勵順序的純 C# 狀態；索引停在最後一筆，不隨重複領取累加。</summary>
    internal sealed class RewardSequenceModel
    {
        private int nextIndex;
        private long repeatThreshold;
        public int GetNextIndex(int count) => Math.Min(nextIndex, count - 1);
        public long GetThreshold(List<GoodChildTaskRewarder.RewardDefinition> rewards)
            => repeatThreshold > 0 ? repeatThreshold : rewards[GetNextIndex(rewards.Count)].stickerThreshold;

        public void Advance(List<GoodChildTaskRewarder.RewardDefinition> rewards, int repeatInterval)
        {
            int index = GetNextIndex(rewards.Count);
            if (index < rewards.Count - 1) nextIndex = index + 1;
            else
            {
                // 超出 int 可持有數量後維持不可達，避免溢位後變成負門檻。
                repeatThreshold = Math.Min((long)int.MaxValue + 1, GetThreshold(rewards) + repeatInterval);
                nextIndex = index;
            }
        }
    }
}
