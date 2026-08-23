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
    /// === Blocking 用法（繼續模式現在由本命令自理，對話腳本不用再處理）===
    /// Blocking（wait = true）時，本命令會：
    ///   1. 在 Awake 主動把繼續模式鎖成 Never（不顯示繼續鈕），這樣行動演出跑條期間玩家無法用
    ///      「繼續」把這句台詞跳過去，整段 Sequence 會一直卡到演出真的跑完才結束。
    ///   2. 演出結束時把繼續模式還原成 Optional，發出 doneMessage，然後結束本命令；此時 Sequence
    ///      結束會自動前進「剛好一次」到下一句（不會早退、不會斷鍊）。
    /// 因此對話腳本現在「只要一行」即可：
    ///   ActionOverlay(myAction);
    /// 不必再自己寫 SetContinueMode(false) / SetContinueMode(Optional)@Message / Continue()@Message。
    /// 舊節點若仍保留那幾行也相容：doneMessage 仍會照發，`@Message(ActionOverlayDone)` 照樣接得到
    /// （多前進的部分被 Dialogue System 的 notifyOnFinishSubtitle 擋成一次，無害）。
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

        // 演出跑條期間鎖住的繼續模式：不顯示繼續鈕、玩家無法跳過，Sequence 會卡到演出跑完才結束。
        private const DisplaySettings.SubtitleSettings.ContinueButtonMode HoldMode =
            DisplaySettings.SubtitleSettings.ContinueButtonMode.Never;

        // 演出結束後還原的繼續模式（依需求固定還原成 Optional）。
        private const DisplaySettings.SubtitleSettings.ContinueButtonMode RestoreMode =
            DisplaySettings.SubtitleSettings.ContinueButtonMode.Optional;

        private bool stopped;
        private bool heldContinueMode;

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

            // Blocking：雙管齊下確保演出期間玩家無法跳過、且演出後會自動前進一次。
            //   1. PushContinueButtonLock：直接壓住繼續鈕（連「本節點沒有對白文字、DS 不重設繼續鈕、
            //      前一句遺留的繼續鈕還亮著」這種情況也擋得住）。這是真正防玩家提前點的關鍵。
            //   2. SetContinueMode(Never)：讓 waitForContinue = false，本命令 Stop() 使 Sequence 結束時
            //      會自動前進「一次」；否則若繼續模式是 Always 又鎖住繼續鈕，會變成沒人能前進的死鎖。
            // 兩者都不依賴對話腳本先寫 SetContinueMode(false)，因此不再受「前一句留下什麼」影響。
            if (wait)
            {
                NhkUISubtitlePanel.PushContinueButtonLock();
                DialogueManager.SetContinueMode(HoldMode);
                heldContinueMode = true;
            }

            bool ok = ActionOverlayManager.Instance.ExecuteById(actionId, (hasOutcome, isSuccess) =>
            {
                // 只有有做成功/失敗判定的 Trigger 才寫結果變數，供後續對話條件分支。
                // 必須在前進之前寫入，下一句的分岐條件才讀得到正確結果。
                if (hasOutcome)
                {
                    DialogueLua.SetVariable(resultVariable, isSuccess);
                }

                // 演出結束：先還原繼續模式成 Optional，再發完成 Message、結束本命令。
                RestoreContinueModeIfHeld();

                // 相容舊節點：仍發出完成 Message，讓還掛著 @Message(ActionOverlayDone) 的節點接得到。
                Sequencer.Message(doneMessage);

                // Blocking：結束本命令 → Sequence 結束 → Optional(waitForContinue=false) 自動前進「一次」。
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
                // 安全降級：找不到 Trigger 時，還原繼續模式並發出完成 Message，避免因為鎖了 Never
                // 又沒有演出來結束 Sequence 而永久卡住；直接跳過演出繼續往下。
                RestoreContinueModeIfHeld();
                Sequencer.Message(doneMessage);
                Stop();
                return;
            }

            // Fire-and-forget 模式：不動繼續模式，不等演出，立即結束本命令
            //（onComplete 仍會在演出結束時發 Message）。
            if (!wait)
            {
                stopped = true;
                Stop();
            }
        }

        /// <summary>
        /// 解除 blocking 期間的壓制：放開繼續鈕鎖，並把繼續模式從 Never 還原成 Optional
        /// （只在確實壓制過時執行，且只做一次，確保 Push/Pop 配對不外洩）。
        /// </summary>
        private void RestoreContinueModeIfHeld()
        {
            if (!heldContinueMode) return;
            heldContinueMode = false;
            // 先在「鎖仍生效」時還原繼續模式（此刻任何顯示繼續鈕的嘗試仍被吞掉），再放開鎖，
            // 避免還原模式的瞬間閃一下繼續鈕。放開後不主動顯示，交給換到下一句時自然重刷。
            DialogueManager.SetContinueMode(RestoreMode);
            NhkUISubtitlePanel.PopContinueButtonLock();
        }

        // 保底：對話中途被打斷（場景切換、強制結束）時，完成 callback 可能沒機會執行。
        // 命令物件被銷毀時在此補做一次解除，避免繼續鈕鎖永久外洩。
        private void OnDestroy()
        {
            RestoreContinueModeIfHeld();
        }
    }
}
