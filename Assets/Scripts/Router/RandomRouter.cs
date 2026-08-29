using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

/// <summary>
/// 機率路由器。
/// 用途：呼叫 Trigger() 時，依各分支設定的「權重」做加權隨機，命中哪個分支就觸發它的 UnityEvent。
/// 適用情境：Button.onClick / Sequencer / FSM 想要「X% 走 A，其餘走 B」這種機率分歧。
///
/// 權重說明：
///   - weight 為相對值，不必湊成 100。例：A=30、B=70 → A 有 30% 機率；A=1、B=1 → 各 50%。
///   - 只有一個分支時等同必中；權重 &lt;= 0 的分支不會被抽中。
///   - 全部權重加總為 0（或沒有任何分支）時，觸發 onFallback。
/// </summary>
[AddComponentMenu("Game/UI/Random Router")]
public class RandomRouter : MonoBehaviour
{
    // 定義單個機率分支
    [Serializable]
    public class WeightedBranch
    {
        [Tooltip("僅供 Inspector 辨識用，例如 \"大成功\"")]
        public string label;

        [Tooltip("相對權重（機率）。不必湊成 100，例：30 vs 70。<= 0 表示不會被抽中。")]
        [Min(0f)]
        public float weight = 1f;

        public UnityEvent onTriggered;
    }

    [Header("機率分支清單")]
    public List<WeightedBranch> branches = new List<WeightedBranch>();

    [Header("保底事件 (無有效分支可抽時觸發)")]
    public UnityEvent onFallback;

    /// <summary>
    /// 綁定在 Unity Button 的 OnClick() 或事件上。
    /// 依權重加權隨機挑一個分支並觸發。
    /// </summary>
    public void Trigger()
    {
        WeightedBranch picked = Roll();

        if (picked != null)
        {
            picked.onTriggered?.Invoke();
        }
        else
        {
            Debug.LogWarning("[RandomRouter] 沒有有效分支可抽（權重全為 0 或清單為空），觸發 onFallback。");
            onFallback?.Invoke();
        }
    }

    /// <summary>
    /// 純抽選，回傳命中的分支（可能為 null），不觸發任何 UnityEvent。
    /// 需要在別處先知道結果再演出時可用。
    /// </summary>
    public WeightedBranch Roll()
    {
        if (branches == null || branches.Count == 0) return null;

        // 統計有效權重總和
        float total = 0f;
        foreach (var branch in branches)
        {
            if (branch != null && branch.weight > 0f)
                total += branch.weight;
        }

        if (total <= 0f) return null;

        // [0, total) 之間取亂數，落在哪個累加區間就選誰
        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var branch in branches)
        {
            if (branch == null || branch.weight <= 0f) continue;

            cumulative += branch.weight;
            if (roll < cumulative)
                return branch;
        }

        // 浮點誤差保險：回傳最後一個有效分支
        for (int i = branches.Count - 1; i >= 0; i--)
        {
            if (branches[i] != null && branches[i].weight > 0f)
                return branches[i];
        }

        return null;
    }
}
