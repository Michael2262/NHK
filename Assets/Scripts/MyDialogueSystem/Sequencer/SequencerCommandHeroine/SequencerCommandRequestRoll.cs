// ============================================================
// SequencerCommandRequestRoll.cs
// ============================================================
// 用法：
//   RequestRoll(heroineID, archetypeID)
//   RequestRoll(heroineID, archetypeID, flagName)
//
// 範例：
//   RequestRoll(sister, 邀約)
//     → 用 Resources/RequestRoll/邀約.asset 對 sister 擲骰，
//       過:加 Flag_RequestPass；敗:移除 Flag_RequestPass（Scene 生命週期）
//   RequestRoll(sister, 邀約, Flag_MyResult)
//     → 同上，但結果寫到 Flag_MyResult
//
// 說明：
// - 原型從 Resources/RequestRoll/{archetypeID} 載入（檔名即 id）。
// - 只做「算率 + 擲骰 + 寫 Flag」，不含任何表演，瞬間完成。
// - 對話端用 Lua 條件 Flag("Flag_RequestPass") 分支成功/失敗。
// - 寫完 Flag 後發送 Sequencer Message "RequestRollDone"，
//   方便把後續 Continue 明確排在擲骰之後（避免同訊息競態）：
//     Request(sister,Think);
//     RequestRoll(sister,邀約)@Message(RequestDone);
//     Continue()@Message(RequestRollDone)
// ============================================================

using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandRequestRoll : SequencerCommand
    {
        private const string ResourcesFolder = "RequestRoll/";
        private const string DefaultFlagName = "Flag_RequestPass";
        private const string DoneMessage = "RequestRollDone";

        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string archetypeID = GetParameter(1, string.Empty).Trim();
            string flagName = GetParameter(2, DefaultFlagName).Trim();
            if (string.IsNullOrEmpty(flagName)) flagName = DefaultFlagName;

            if (string.IsNullOrEmpty(archetypeID))
            {
                Debug.LogError("[RequestRoll] 缺少 archetypeID，指令中止。", this);
                Complete();
                return;
            }

            var archetype = Resources.Load<RequestArchetype>(ResourcesFolder + archetypeID);
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

            RequestRollResult r = RequestRoller.Roll(archetype, heroineID);

            if (r.Pass)
                svc.ProgressFlags.AddSceneFlag(flagName);   // 過 → flag 存在
            else
                svc.ProgressFlags.RemoveFlag(flagName);      // 敗 → flag 不存在

            string outcome = r.Pass
                ? "<color=#3DDC84><b>通過</b></color>"
                : "<color=#FF5A5A><b>失敗</b></color>";
            string guaranteedNote = r.Guaranteed ? "　（達穩過線・保證過）" : "";

            Debug.Log(
                $"【請求擲骰】{archetypeID}　{outcome}{guaranteedNote}\n" +
                $"　驅動：{DriverLabel(archetype.Driver)} ＝ {r.DriverValue}\n" +
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
