using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 加權隨機分支：依各選項的權重，隨機挑出一個，把「第幾個」寫進對話變數 BranchPick。
    ///
    /// 語法：WeightedRandom(權重1, 權重2, 權重3, ...)
    ///
    /// 範例：
    ///   WeightedRandom(50, 30, 20)
    ///     -> 50% 寫入 BranchPick = 1
    ///        30% 寫入 BranchPick = 2
    ///        20% 寫入 BranchPick = 3
    ///   WeightedRandom([var=FriedRice], 80-prev)
    ///     第二格以 80 減去前一格的權重。例如第一格為 20，第二格就是 60。
    ///   WeightedRandom([var=A], [var=B], 100-sum)
    ///     最後一格以 100 減去前面所有已算出的權重。例如 A 為 10、B 為 20，最後一格就是 70。
    ///
    /// 用法：在父節點的 Sequence 放這行指令，子節點 Conditions 改成編號相等比較：
    ///   句A 的 Conditions： Variable["BranchPick"] == 1
    ///   句B 的 Conditions： Variable["BranchPick"] == 2
    ///   句C 的 Conditions： Variable["BranchPick"] == 3
    ///
    /// 說明：
    /// - 隨機數只在這支命令內部運算（C# 區域變數），不會污染對話系統變數清單。
    /// - 唯一寫入對話系統的就是結果 BranchPick（1-based，從 1 開始）。
    /// - 權重不需要加總為 100，內部會自動依總和換算比例（例如 1,1,2 = 25%/25%/50%）。
    /// - 權重可填整數；負數視為 0；總和為 0 時會退回 BranchPick = 1 並警告。
    /// </summary>
    public class SequencerCommandWeightedRandom : SequencerCommand
    {
        // 結果寫入的對話變數名稱。子節點 Conditions 就是讀這個變數。
        private const string BranchPickVariable = "BranchPick";
        private const string PreviousWeightSuffix = "-prev";
        private const string SumSuffix = "-sum";

        public void Awake()
        {
            // 1. 收集所有權重參數（去掉空白與空字串）
            int count = (parameters != null) ? parameters.Length : 0;
            var weights = new System.Collections.Generic.List<int>(count);
            long total = 0;

            for (int i = 0; i < count; i++)
            {
                string raw = GetParameter(i);
                if (string.IsNullOrEmpty(raw)) continue;

                int weight;
                bool subtractSum = raw.EndsWith(SumSuffix, System.StringComparison.OrdinalIgnoreCase);
                if (subtractSum || raw.EndsWith(PreviousWeightSuffix, System.StringComparison.OrdinalIgnoreCase))
                {
                    string suffix = subtractSum ? SumSuffix : PreviousWeightSuffix;
                    string baseText = raw.Substring(0, raw.Length - suffix.Length);
                    if ((!subtractSum && weights.Count == 0) || !int.TryParse(baseText, out int basis) || basis < 0)
                    {
                        Debug.LogWarning($"[WeightedRandom] 第 {i + 1} 格「{raw}」無法計算。基準須為非負整數，使用 -prev 時須有前一格權重。本格權重設為 0。");
                        weight = 0;
                    }
                    else
                    {
                        long deduction = subtractSum ? total : weights[weights.Count - 1];
                        weight = (int)System.Math.Max(0L, basis - deduction);
                    }
                }
                else
                {
                    weight = Mathf.Max(0, GetParameterAsInt(i, 0));
                }
                weights.Add(weight);
                total += weight;
            }

            if (weights.Count == 0)
            {
                Debug.LogWarning("[WeightedRandom] 沒有提供任何權重。用法：WeightedRandom(50, 30, 20)");
                DialogueLua.SetVariable(BranchPickVariable, 1);
                Stop();
                return;
            }

            if (total == 0)
            {
                Debug.LogWarning("[WeightedRandom] 權重總和為 0（全部都是 0 或負數），退回 BranchPick = 1。");
                DialogueLua.SetVariable(BranchPickVariable, 1);
                Stop();
                return;
            }

            // 3. 擲骰：roll 落在 [0, total) 的整數，再用累加區間找出落在第幾段
            long roll = RollWeight(total);
            long cumulative = 0;
            int pick = weights.Count; // 預設取最後一個，避免浮點/邊界誤差漏接

            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    pick = i + 1; // 1-based，對應子節點 BranchPick == n
                    break;
                }
            }

            // 4. 把結果寫回對話系統，供子節點 Conditions 判斷
            DialogueLua.SetVariable(BranchPickVariable, pick);

            if (DialogueDebug.logInfo)
            {
                Debug.Log($"[WeightedRandom] 權重=[{string.Join(",", weights)}] 總和={total} roll={roll} -> BranchPick={pick}");
            }

            Stop();
        }

        private static long RollWeight(long total)
        {
            if (total <= int.MaxValue) return Random.Range(0, (int)total);

            // 總和超過單次整數抽樣範圍時，先抽區塊再抽位置，重抽末塊超出總和的部分。
            const int blockSize = int.MaxValue;
            int blockCount = (int)((total - 1) / blockSize + 1);
            long roll;
            do
            {
                roll = (long)Random.Range(0, blockCount) * blockSize + Random.Range(0, blockSize);
            } while (roll >= total);
            return roll;
        }
    }
}
