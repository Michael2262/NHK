// ============================================================
// SequencerCommandRequestRoll.cs
// ============================================================
// 用法：
//   RequestRoll(archetypeID)
//   RequestRoll(archetypeID, heroineID)
//   RequestRoll(archetypeID, heroineID, bonus)
//   RequestRoll(archetypeID, heroineID, bonus, flagName)
//
// heroineID：可省略，預設 sister。主角數值型 Request 不會使用此參數。
// bonus：本次臨時對「主驅動數值」加減（可正可負，不影響數值本身）。
//
// 範例：
//   RequestRoll(打球)
//     → 用 Resources/RequestRoll/打球.asset 擲骰；若原型需要女主角數值，預設讀 sister
//   RequestRoll(邀約, sister)
//     → 用 Resources/RequestRoll/邀約.asset 對 sister 擲骰，
//       過:加 Flag_RequestPass；敗:移除 Flag_RequestPass（Scene 生命週期）
//   RequestRoll(邀約, sister, +10)
//     → 驅動值臨時 +10 再判定（例：非常強烈的請求）
//   RequestRoll(邀約, sister, -10)
//     → 驅動值臨時 -10 再判定（例：女主心情不好）
//   RequestRoll(邀約, sister, 0, Flag_MyResult)
//     → 無加減，結果改寫到 Flag_MyResult
//
// 說明：
// - 原型從 Resources/RequestRoll/{archetypeID} 載入（檔名即 id）。
// - 只做「算率 + 擲骰 + 寫 Flag」，不含任何表演，瞬間完成。
// - 對話端用 Lua 條件 Flag("Flag_RequestPass") 分支成功/失敗。
// - 寫完 Flag 後發送 Sequencer Message "RequestRollDone"，
//   方便把後續 Continue 明確排在擲骰之後（避免同訊息競態）：
//     Request(sister,Think);
//     RequestRoll(邀約,sister)@Message(RequestDone);
//     Continue()@Message(RequestRollDone)
// ============================================================

using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandRequestRoll : SequencerCommand
    {
        private const string ResourcesFolder = "RequestRoll/";
        private const string DefaultHeroineID = "sister";
        private const string DefaultFlagName = "Flag_RequestPass";
        private const string DoneMessage = "RequestRollDone";

        public void Awake()
        {
            string firstParameter = GetParameter(0, string.Empty).Trim();
            string secondParameter = GetParameter(1, string.Empty).Trim();

            if (string.IsNullOrEmpty(firstParameter))
            {
                Debug.LogError("[RequestRoll] 缺少 archetypeID，指令中止。", this);
                Complete();
                return;
            }

            // 新格式：RequestRoll(archetypeID, heroineID?, bonus?, flagName?)
            string archetypeID = firstParameter;
            string heroineID = string.IsNullOrEmpty(secondParameter)
                ? DefaultHeroineID
                : secondParameter;
            var archetype = Resources.Load<RequestArchetype>(ResourcesFolder + archetypeID);

            // 舊格式相容：RequestRoll(heroineID, archetypeID, bonus?, flagName?)
            // 第一參數找不到原型、第二參數能找到時，自動按舊順序解析。
            if (archetype == null && !string.IsNullOrEmpty(secondParameter))
            {
                var legacyArchetype = Resources.Load<RequestArchetype>(ResourcesFolder + secondParameter);
                if (legacyArchetype != null)
                {
                    heroineID = firstParameter;
                    archetypeID = secondParameter;
                    archetype = legacyArchetype;
                    Debug.LogWarning(
                        $"[RequestRoll] 偵測到舊格式 RequestRoll({heroineID}, {archetypeID})；" +
                        $"建議改為 RequestRoll({archetypeID}, {heroineID})。",
                        this);
                }
            }

            string bonusRaw = GetParameter(2, string.Empty).Trim();
            int bonus = 0;
            if (!string.IsNullOrEmpty(bonusRaw) && !int.TryParse(bonusRaw, out bonus))
            {
                Debug.LogWarning($"[RequestRoll] 無法解析臨時加減值 '{bonusRaw}'，當作 0。", this);
                bonus = 0;
            }

            string flagName = GetParameter(3, DefaultFlagName).Trim();
            if (string.IsNullOrEmpty(flagName)) flagName = DefaultFlagName;

            if (archetype == null)
            {
                Debug.LogError($"[RequestRoll] 找不到原型 Resources/{ResourcesFolder}{archetypeID}.asset，指令中止。", this);
                Complete();
                return;
            }

            var svc = GameStatusService.Instance;
            if (svc == null || svc.ProgressFlags == null)
            {
                Debug.LogWarning("[RequestRoll] GameStatusService / ProgressFlags 未就緒，指令中止。", this);
                Complete();
                return;
            }

            RequestRollResult r = RequestRoller.Roll(archetype, heroineID, bonus);

            if (r.Pass)
                svc.ProgressFlags.AddSceneFlag(flagName);   // 過 → flag 存在
            else
                svc.ProgressFlags.RemoveFlag(flagName);      // 敗 → flag 不存在

            string outcome = r.Pass
                ? "<color=#3DDC84><b>通過</b></color>"
                : "<color=#FF5A5A><b>失敗</b></color>";
            string guaranteedNote = r.Guaranteed ? "　（達穩過線・保證過）" : "";

            string valueText = r.Bonus != 0
                ? $"{r.RawDriverValue}（臨時 {(r.Bonus > 0 ? "+" : "")}{r.Bonus} → {r.DriverValue}）"
                : $"{r.DriverValue}";

            Debug.Log(
                $"【請求擲骰】{archetypeID}　{outcome}{guaranteedNote}\n" +
                $"　驅動：{DriverLabel(archetype.Driver)} ＝ {valueText}\n" +
                $"　成功率：{r.SuccessRate:0.#}%　（低標 {archetype.TLow}／穩過線 {archetype.THigh}）\n" +
                $"　旗標：{flagName} → {(r.Pass ? "已設定" : "已清除")}");

            Complete();
        }

        /// <summary>把 DriverStat 轉成好讀的中文標籤（僅供 log 顯示）。</summary>
        private static string DriverLabel(DriverStat driver)
        {
            switch (driver)
            {
                case DriverStat.Heroine_Trust: return "女主・信賴";
                case DriverStat.Heroine_Libido: return "女主・性慾";
                case DriverStat.Protagonist_LifePower: return "主角・生活力";
                case DriverStat.Protagonist_Sociality: return "主角・社會性";
                case DriverStat.Protagonist_Dependency: return "主角・依賴度";
                default: return driver.ToString();
            }
        }

        /// <summary>發送完成訊息並結束指令。</summary>
        private void Complete()
        {
            Sequencer.Message(DoneMessage);
            Stop();
        }
    }
}
