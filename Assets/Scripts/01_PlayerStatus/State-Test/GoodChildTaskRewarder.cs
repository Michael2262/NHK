using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NHK.StateTest
{
    /// <summary>依序領取獎勵，最後一筆可重複領取；進度僅保留於此元件的生命週期。</summary>
    [AddComponentMenu("Game/State Test/Good Child Task Rewarder")]
    public sealed class GoodChildTaskRewarder : MonoBehaviour
    {
        [Serializable]
        public sealed class RewardDefinition
        {
            public UnityEvent onReward = new UnityEvent();
        }

        [SerializeField] private List<RewardDefinition> rewards = new List<RewardDefinition>();
        private readonly RewardSequenceModel sequence = new RewardSequenceModel();
        private bool invokingEvents;

        /// <summary>每次呼叫領取一筆；全部領完後，每次都執行最後一筆。</summary>
        public void Reward()
        {
            if (!isActiveAndEnabled || invokingEvents || rewards == null || rewards.Count == 0) return;
            var reward = rewards[sequence.GetNextIndex(rewards.Count)];
            if (reward == null || reward.onReward == null)
            {
                Debug.LogWarning("[GoodChildTaskRewarder] 目前獎勵資料為空，未推進領取順序。", this);
                return;
            }

            // 先推進，再執行；事件內重複呼叫自身不會連續領取。
            sequence.Advance(rewards.Count);
            invokingEvents = true;
            try { reward.onReward.Invoke(); }
            finally { invokingEvents = false; }
        }
    }

    /// <summary>獎勵順序的純 C# 狀態；索引停在最後一筆，不隨重複領取累加。</summary>
    internal sealed class RewardSequenceModel
    {
        private int nextIndex;
        public int GetNextIndex(int count) => Math.Min(nextIndex, count - 1);
        public void Advance(int count)
        {
            int index = GetNextIndex(count);
            nextIndex = index < count - 1 ? index + 1 : index;
        }
    }
}
