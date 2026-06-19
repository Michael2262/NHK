using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法（wait 預設為 true / Blocking，只有明確填 false 才變 Fire-and-forget）：
    ///   ActionOverlay(actionId)
    ///     → Blocking（預設），等演出跑完才結束本命令；完成時發 ActionOverlayDone，並把成功/失敗寫進 LastActionResult。
    ///   ActionOverlay(actionId, false)
    ///     → Fire-and-forget，立即放行對話，不等演出。
    ///   ActionOverlay(actionId, true, 自訂Message)
    ///     → Blocking，但完成時改發指定 Message。
    ///   ActionOverlay(actionId, true, 自訂Message, 自訂變數)
    ///     → Blocking，但成功/失敗改寫進指定的 Lua 變數。
    ///
    /// 以 ID 觸發場景中某顆 ActionOverlayTrigger 的行動演出（跑條 / 成功失敗 / UnityEvent）。
    /// actionId 對應 ActionOverlayTrigger.actionId，由該 Trigger 在 OnEnable 自我註冊到
    /// ActionOverlayManager；Trigger 的 actionId 留空者不會被註冊，因而無法被本命令呼叫。
    ///
    /// === 結果變數（供後續對話條件分支）===
    /// 只有「有做成功/失敗判定」的 Trigger（enableOutcomeResult = true）才會寫結果：
    ///   Variable["LastActionResult"] = true(成功) / false(失敗)
    /// 沒做判定的 Trigger 不寫任何變數，只發 Message 讓對話接下一句。
    /// 對話條件式範例：Variable["LastActionResult"] == true
    /// 注意：要讀得到結果，必須用 Blocking（wait = true）等演出跑完，否則對話會在結果寫入前就跑過去。
    ///
    /// === Blocking 用法（搭配對話腳本的 Continue Mode）===
    /// 演出結束時本命令會 Sequencer.Message(doneMessage)。要讓對話「等演出」，在對話腳本這樣寫：
    ///   SetContinueMode(false);
    ///   SetContinueMode(Optional)@Message(ActionOverlayDone);
    ///   ActionOverlay(myAction, true);
    /// 不寫 SetContinueMode 兩行時，發出的 Message 沒人接收，等同 Fire-and-forget（無害）。
    ///
    /// === 使用限制（務必遵守）===
    /// 1.【不能跨場景】ActionOverlayTrigger 在會卸載的一般場景上，只有「目前已載入且 enable」的
    ///    Trigger 才在註冊表中。若對話在別的場景跑、或目標 Trigger 不在場上 / 被 disable，會找不到
    ///    ID（本命令只印 Warning，不報錯）。請確保此命令與目標 Trigger 同場景。
    /// 2.【建議放在對話「最後一動」】行動演出本身會佔用畫面數秒。即使用 Blocking，也建議把本命令擺在
    ///    對話節點的最後，讓玩家看完對話後才接演出，避免演出蓋住後續對話。
    /// </summary>
    public class SequencerCommandActionOverlay : SequencerCommand
    {
        private const string DefaultDoneMessage = "ActionOverlayDone";
        private const string DefaultResultVariable = "LastActionResult";

        private bool stopped;

        public void Awake()
        {
            if (ActionOverlayManager.Instance == null)
            {
                Debug.LogWarning("[ActionOverlay] 場景中找不到 ActionOverlayManager 實例。");
                Stop();
                return;
            }

            string actionId = GetParameter(0);

            if (string.IsNullOrEmpty(actionId))
            {
                Debug.LogWarning("[ActionOverlay] 缺少 actionId。用法：ActionOverlay(actionId[, true[, doneMessage[, resultVariable]]])");
                Stop();
                return;
            }

            // wait 預設為 true（blocking）；只有明確填 false 才改為 fire-and-forget。
            bool wait = !string.Equals(GetParameter(1, "true").Trim(), "false", System.StringComparison.OrdinalIgnoreCase);

            string doneMessage = GetParameter(2, DefaultDoneMessage).Trim();
            if (string.IsNullOrEmpty(doneMessage)) doneMessage = DefaultDoneMessage;

            string resultVariable = GetParameter(3, DefaultResultVariable).Trim();
            if (string.IsNullOrEmpty(resultVariable)) resultVariable = DefaultResultVariable;

            bool ok = ActionOverlayManager.Instance.ExecuteById(actionId, (hasOutcome, isSuccess) =>
            {
                // 只有有做成功/失敗判定的 Trigger 才寫結果變數，供後續對話條件分支。
                if (hasOutcome)
                {
                    DialogueLua.SetVariable(resultVariable, isSuccess);
                }

                // 演出結束：發出完成 Message，讓有掛 @Message 的對話放行。
                Sequencer.Message(doneMessage);

                // Blocking 模式下，等到此刻才結束本命令。
                if (wait && !stopped)
                {
                    stopped = true;
                    Stop();
                }
            });

            if (!ok)
            {
                Debug.LogWarning($"[ActionOverlay] 找不到已註冊的 actionId「{actionId}」。" +
                    "請確認目標 ActionOverlayTrigger 與本對話在同一場景、已啟用，且 actionId 一致。");
                // 安全降級：即使找不到 Trigger，也發出完成 Message，讓有掛 @Message 的對話不會永久卡死，
                // 而是直接跳過演出繼續往下。
                Sequencer.Message(doneMessage);
                Stop();
                return;
            }

            // Fire-and-forget 模式：不等演出，立即結束本命令（onComplete 仍會在演出結束時發 Message）。
            if (!wait)
            {
                stopped = true;
                Stop();
            }
        }
    }
}
