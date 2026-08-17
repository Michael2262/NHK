using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: Bark(對話標題, [說話者], [聆聽者])
    ///
    /// 讓指定角色從「他自己的 BarkUI」冒出一句對話 —— 等同 FSM 的 Bark action，但可從對話 sequence 呼叫。
    ///
    /// ⚠️ Dialogue System 原生「沒有」Bark sequencer command（HandleCommandInternally 清單裡沒有，
    ///    也沒有 SequencerCommandBark 檔）。這是自製版，底層直接呼叫 DialogueManager.Bark，
    ///    跟 FSM 的 Bark action 走完全相同的 API。
    ///
    /// 用法範例：
    ///   Bark(X98_Test/TestBark)                  → 由當前對話的說話者 bark
    ///   Bark(X98_Test/TestBark, SisterB)         → 指定 SisterB bark（找場景中 Actor=SisterB 的物件）
    ///   Bark(X98_Test/TestBark, SisterB, Player) → 額外指定 listener
    ///
    /// 參數：
    ///   參數 0: 對話標題（必填，就是資料庫裡的 Conversation Title，含分組斜線 X98_Test/TestBark）
    ///   參數 1: 說話者（選填，預設 = 當前 sequence 的 speaker）。可用角色名 / "speaker" / "listener"
    ///   參數 2: 聆聽者（選填）
    ///
    /// 前提：說話者身上（或其 DialogueActor 的「Bark UI」欄位）要掛/指定一顆 StandardBarkUI，字才有地方顯示。
    ///       BarkUI 的解析由 BarkController → DialogueActor.GetBarkUI(speaker) 處理，會讀 DialogueActor 欄位。
    /// </summary>
    public class SequencerCommandBark : SequencerCommand
    {
        public void Awake()
        {
            // 參數 0: 對話標題（必填）
            string conversationTitle = GetParameter(0);
            if (string.IsNullOrEmpty(conversationTitle))
            {
                if (DialogueDebug.logWarnings)
                    Debug.LogWarning("Sequencer: Bark(對話標題, [說話者], [聆聽者]) —— 對話標題不可為空");
                Stop();
                return;
            }

            // 參數 1: 說話者（預設用當前 sequence 的 speaker）
            Transform barkSpeaker = GetSubject(1, speaker);
            if (barkSpeaker == null)
            {
                if (DialogueDebug.logWarnings)
                    Debug.LogWarning($"Sequencer: Bark({conversationTitle}) —— 找不到說話者（場景中沒有對應角色物件），無法 bark");
                Stop();
                return;
            }

            // 參數 2: 聆聽者（選填）—— 只有明確填了才帶，未填就走單參數多載（對齊 FSM Bark action 行為）
            Transform barkListener = null;
            if (!string.IsNullOrEmpty(GetParameter(2)))
                barkListener = GetSubject(2, listener);

            if (DialogueDebug.logInfo)
                Debug.Log($"Sequencer: Bark({conversationTitle}, {barkSpeaker.name}" +
                          (barkListener != null ? $", {barkListener.name}" : "") + ")");

            if (barkListener != null)
                DialogueManager.Bark(conversationTitle, barkSpeaker, barkListener);
            else
                DialogueManager.Bark(conversationTitle, barkSpeaker);

            Stop(); // 指令完成（bark 本身是 fire-and-forget，不需等它演完）
        }
    }
}
