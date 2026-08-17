using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: BarkKey(TextTableKey, [說話者], [聆聽者])
    ///
    /// 讓角色冒一句 bark，但字**來自 Text Table**（依當前語言查表），而不是對話 entry。
    /// 適合把 bark 台詞跟其他 UI 字一樣統一放 Common_TextTable 維護（符合本專案多語系慣例）。
    ///
    /// 與 Bark() 的差別：
    ///   Bark(對話標題, 角色)   → 字存在對話 entry 裡，Dialogue System 自動挑語言
    ///   BarkKey(表格Key, 角色) → 字存在 Text Table 裡，這裡用 GetLocalizedText(key) 查當前語言後吐出
    ///
    /// 用法範例：
    ///   BarkKey(Bark.SisterB.Greet, SisterB)          → 查表 → SisterB 頭上冒該語言的字
    ///   BarkKey(Bark.Generic.Hmm, speaker)            → 由當前對話說話者冒
    ///   BarkKey(Bark.SisterB.Warn, SisterB, Player)   → 指定 listener
    ///
    /// 參數：
    ///   參數 0: Text Table Key（必填）
    ///   參數 1: 說話者（選填，預設 = 當前 sequence 的 speaker）
    ///   參數 2: 聆聽者（選填）
    ///
    /// 前提同 Bark()：說話者身上（或子物件）要有 StandardBarkUI，字才有地方顯示。
    /// 註：bark 是一次性表演，切語言當下不會即時重查（跟本專案其他程式塞字一致）。
    /// </summary>
    public class SequencerCommandBarkKey : SequencerCommand
    {
        public void Awake()
        {
            // 參數 0: Text Table Key（必填）
            string key = GetParameter(0);
            if (string.IsNullOrEmpty(key))
            {
                if (DialogueDebug.logWarnings)
                    Debug.LogWarning("Sequencer: BarkKey(TextTableKey, [說話者], [聆聽者]) —— Key 不可為空");
                Stop();
                return;
            }

            // 查表：依當前語言取字，查不到就 fallback 顯示 key 本身（對齊 StoryManager.Localize）
            string text = DialogueManager.GetLocalizedText(key);
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[BarkKey] Text Table 找不到 Key: {key}");
                text = key;
            }

            // 參數 1: 說話者（預設用當前 sequence 的 speaker）
            Transform barkSpeaker = GetSubject(1, speaker);
            if (barkSpeaker == null)
            {
                if (DialogueDebug.logWarnings)
                    Debug.LogWarning($"Sequencer: BarkKey({key}) —— 找不到說話者，無法 bark");
                Stop();
                return;
            }

            // 參數 2: 聆聽者（選填）
            Transform barkListener = null;
            if (!string.IsNullOrEmpty(GetParameter(2)))
                barkListener = GetSubject(2, listener);

            if (DialogueDebug.logInfo)
                Debug.Log($"Sequencer: BarkKey({key}) → \"{text}\" @ {barkSpeaker.name}");

            // BarkString 吐字面字串（已是查表後的當前語言字），BarkUI / 消失時機沿用原生邏輯
            DialogueManager.BarkString(text, barkSpeaker, barkListener);

            Stop();
        }
    }
}
