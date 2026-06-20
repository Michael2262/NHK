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

        public void Awake()
        {
            // 1. 收集所有權重參數（去掉空白與空字串）
            int count = (parameters != null) ? parameters.Length : 0;
            var weights = new System.Collections.Generic.List<int>(count);

            for (int i = 0; i < count; i++)
            {
                string raw = GetParameter(i);
                if (string.IsNullOrEmpty(raw)) continue;

                int w = GetParameterAsInt(i, 0);
                if (w < 0) w = 0; // 負數權重視為 0，避免破壞加總
                weights.Add(w);
            }

            if (weights.Count == 0)
            {
                Debug.LogWarning("[WeightedRandom] 沒有提供任何權重。用法：WeightedRandom(50, 30, 20)");
                DialogueLua.SetVariable(BranchPickVariable, 1);
                Stop();
                return;
            }

            // 2. 計算總和
            int total = 0;
            foreach (int w in weights) total += w;

            if (total <= 0)
            {
                Debug.LogWarning("[WeightedRandom] 權重總和為 0（全部都是 0 或負數），退回 BranchPick = 1。");
                DialogueLua.SetVariable(BranchPickVariable, 1);
                Stop();
                return;
            }

            // 3. 擲骰：roll 落在 [0, total) 的整數，再用累加區間找出落在第幾段
            int roll = Random.Range(0, total); // UnityEngine.Random，回傳 0 ~ total-1
            int cumulative = 0;
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
    }
}
