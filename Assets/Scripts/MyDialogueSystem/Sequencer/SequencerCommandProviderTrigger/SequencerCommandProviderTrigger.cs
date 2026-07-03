using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：ProviderTrigger(actionId[, 變數名])
    ///   ProviderTrigger(myAction)
    ///     → 找場上 actionId = myAction 的 ProviderTrigger，用其 ActionChanceProvider 算成功率。
    ///       整數百分比(0~100) 寫進 Variable["myAction"]；
    ///       並當場依此機率擲骰一次，成功/失敗(true/false) 寫進 Variable["myAction_Result"]。
    ///   ProviderTrigger(myAction, MyVar)
    ///     → 改寫進 Variable["MyVar"]（百分比）與 Variable["MyVar_Result"]（擲骰結果）。
    ///
    /// 本命令瞬間完成（fire-and-forget），不跑任何演出、不 blocking，算完即 Stop。
    ///
    /// 此外，每次呼叫都會「固定再覆蓋一份公用變數」，方便用單一名稱讀最近一次結果：
    ///   Variable["Provider"]         // 最近一次的百分比(0~100)
    ///   Variable["Provider_Result"]  // 最近一次的擲骰結果(true/false)
    ///
    /// 對話條件式範例：
    ///   Variable["myAction"] >= 50            // 用機率數字判定
    ///   Variable["myAction_Result"] == true   // 用擲骰結果分支
    ///   Variable["Provider_Result"] == true   // 不在意是哪個 ID，只看最近一次結果
    ///
    /// === 使用限制 ===
    /// 與 ActionOverlay 相同：ProviderTrigger 以 ID 自我註冊，只有「目前已載入且 enable」的才在註冊表中。
    /// 請確保此命令與目標 ProviderTrigger 同場景、已啟用，且 actionId 一致；否則只印 Warning，不報錯。
    /// </summary>
    public class SequencerCommandProviderTrigger : SequencerCommand
    {
        private const string ResultVariableSuffix = "_Result";

        // 不論 actionId / 變數名為何，每次呼叫都會額外覆蓋這份公用變數，供對話用固定名稱讀最近一次結果。
        private const string SharedVariableName = "Provider";

        public void Awake()
        {
            string actionId = GetParameter(0);

            if (string.IsNullOrEmpty(actionId))
            {
                Debug.LogWarning("[ProviderTrigger] 缺少 actionId。用法：ProviderTrigger(actionId[, 變數名])");
                Stop();
                return;
            }

            // 變數名預設用 actionId。
            string variableName = GetParameter(1, actionId).Trim();
            if (string.IsNullOrEmpty(variableName)) variableName = actionId;

            ProviderTrigger trigger = ProviderTrigger.Find(actionId);
            if (trigger == null)
            {
                Debug.LogWarning($"[ProviderTrigger] 找不到已註冊的 actionId「{actionId}」。" +
                    "請確認目標 ProviderTrigger 與本對話在同一場景、已啟用，且 actionId 一致。");
                Stop();
                return;
            }

            float chance = trigger.CalculateChance();
            if (chance < 0f)
            {
                // CalculateChance 已印出 Warning（找不到 ActionChanceProvider）。
                Stop();
                return;
            }

            // 機率：整數百分比 (0~100)。與 ActionChanceProvider 的顯示換算一致。
            int percent = Mathf.RoundToInt(chance * 100f);

            // 結果：當場依此機率擲骰一次。
            bool isSuccess = Random.value <= chance;

            // 寫進以 ID（或自訂變數名）命名的變數。
            DialogueLua.SetVariable(variableName, percent);
            DialogueLua.SetVariable(variableName + ResultVariableSuffix, isSuccess);

            // 同時覆蓋公用變數，供對話用固定名稱讀最近一次結果。
            DialogueLua.SetVariable(SharedVariableName, percent);
            DialogueLua.SetVariable(SharedVariableName + ResultVariableSuffix, isSuccess);

            Debug.Log($"[ProviderTrigger] actionId「{actionId}」成功率 {percent}% → {(isSuccess ? "成功" : "失敗")}。" +
                $"已寫入 Variable[\"{variableName}\"]={percent}、Variable[\"{variableName}{ResultVariableSuffix}\"]={isSuccess}。");

            Stop();
        }
    }
}
