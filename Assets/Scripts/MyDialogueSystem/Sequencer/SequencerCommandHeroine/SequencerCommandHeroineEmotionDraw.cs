// ============================================================
// SequencerCommandHeroineEmotionDraw.cs
// ============================================================
// 用法：
//   HeroineEmotionDraw(heroineID, resultMode, showMode, resultLuaVariable)
//   HeroineEmotionDraw(heroineID, resultMode, showMode, resultLuaVariable, specifiedEmotion)
//   HeroineEmotionDraw(heroineID, resultMode, showMode, resultLuaVariable, specifiedEmotion, wait, doneMessage)
//
// resultMode = current | bulk | random | fake | mock | adjacent
// showMode   = none | small | medium | big
//
// 範例：
//   HeroineEmotionDraw(sister, current, small, LastEmotionDraw)
//     → 結果=主導情緒，小抽選表演
//   HeroineEmotionDraw(sister, random, big, LastEmotionDraw)
//     → 結果=隨機抽選，大抽選表演
//   HeroineEmotionDraw(sister, bulk, none, LastEmotionDraw)
//     → 結果=大宗情緒，無表演
//   HeroineEmotionDraw(sister, fake, big, LastEmotionDraw, Shy)
//     → 結果=指定害羞(須卡池有)，大抽選表演
//   HeroineEmotionDraw(sister, mock, small, LastEmotionDraw, Worried)
//     → 結果=強制指定擔心(不管卡池)，小抽選表演
//   HeroineEmotionDraw(sister, random, big, LastEmotionDraw, , true, EmotionDrawDone)
//     → 結果=隨機，大抽選表演，等待，完成後發 Message
//
// 會寫入 Lua 變數：
//   Variable["LastEmotionDraw"] = "Shy"
//   Variable["LastEmotionDrawFakeRequested"] = true/false
//   Variable["LastEmotionDrawFakeSucceeded"] = true/false
//   Variable["LastEmotionDrawHeroineID"] = "sister"
// ============================================================

using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandHeroineEmotionDraw : SequencerCommand
    {
        private bool stopped;

        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string param1 = GetParameter(1, string.Empty).Trim().ToLowerInvariant();
            string param2 = GetParameter(2, string.Empty).Trim().ToLowerInvariant();

            RunNew(heroineID, param1, param2);
        }

        // ─────────────────────────────────────────────────────────
        // 新格式
        // ─────────────────────────────────────────────────────────

        private void RunNew(string heroineID, string resultModeText, string showModeText)
        {
            EmotionResultMode resultMode = ParseResultMode(resultModeText);
            EmotionShowMode showMode = ParseShowMode(showModeText);
            string resultLuaVariable = GetParameter(3, "LastEmotionDraw").Trim();

            bool needsEmotion = resultMode == EmotionResultMode.Fake || resultMode == EmotionResultMode.Mock;
            HeroineEmotionCardType specifiedEmotion = needsEmotion
                ? ParseEmotion(GetParameter(4, "Angry"), HeroineEmotionCardType.Angry)
                : HeroineEmotionCardType.Angry;

            int waitParamIndex = needsEmotion ? 5 : 4;
            int doneParamIndex = needsEmotion ? 6 : 5;

            bool wait = ParseBool(GetParameter(waitParamIndex, "true"), true);
            string doneMessage = GetParameter(doneParamIndex, string.Empty).Trim();

            // 如果 waitParam 不是 bool，當作 doneMessage
            string rawWaitParam = GetParameter(waitParamIndex, string.Empty).Trim();
            if (!TryParseBool(rawWaitParam, out _) && !string.IsNullOrEmpty(rawWaitParam))
            {
                doneMessage = rawWaitParam;
                wait = true;
            }

            ExecuteDraw(heroineID, resultMode, showMode, specifiedEmotion, resultLuaVariable, wait, doneMessage);
        }

        // ─────────────────────────────────────────────────────────
        // 共用執行
        // ─────────────────────────────────────────────────────────

        private void ExecuteDraw(string heroineID, EmotionResultMode resultMode, EmotionShowMode showMode,
            HeroineEmotionCardType specifiedEmotion, string resultLuaVariable, bool wait, string doneMessage)
        {
            if (EmotionCardDrawMachine.Instance == null)
            {
                Debug.LogWarning("Dialogue System: HeroineEmotionDraw 找不到 EmotionCardDrawMachine。", this);
                Stop();
                return;
            }

            Action<EmotionDrawResult> onComplete = result =>
            {
                WriteLuaResult(resultLuaVariable, result);
                SendDoneMessage(doneMessage);

                if (wait && !stopped)
                {
                    stopped = true;
                    Stop();
                }
            };

            EmotionCardDrawMachine.Instance.StartDraw(heroineID, resultMode, showMode, specifiedEmotion, onComplete);

            if (!wait)
            {
                stopped = true;
                Stop();
            }
        }

        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────

        private static EmotionResultMode ParseResultMode(string text)
        {
            switch (text)
            {
                case "current": case "主導": case "cur": case "now": return EmotionResultMode.Current;
                case "bulk": case "大宗": case "blk": return EmotionResultMode.Bulk;
                case "random": case "隨機": case "rand": case "rng": return EmotionResultMode.Random;
                case "fake": case "造假": return EmotionResultMode.Fake;
                case "mock": case "強制": case "指定": return EmotionResultMode.Mock;
                case "adjacent": case "鄰近": case "adj": case "near": return EmotionResultMode.Adjacent;
                default:
                    Debug.LogWarning($"[HeroineEmotionDraw] 無法解析 resultMode: {text}，使用 Current");
                    return EmotionResultMode.Current;
            }
        }

        private static EmotionShowMode ParseShowMode(string text)
        {
            switch (text)
            {
                case "none": case "無": case "n": case "hide": return EmotionShowMode.None;
                case "small": case "小": case "s": return EmotionShowMode.Small;
                case "medium": case "中": case "m": case "mid": return EmotionShowMode.Medium;
                case "big": case "大": case "b": return EmotionShowMode.Big;
                default:
                    Debug.LogWarning($"[HeroineEmotionDraw] 無法解析 showMode: {text}，使用 Small");
                    return EmotionShowMode.Small;
            }
        }

        private static void SendDoneMessage(string doneMessage)
        {
            if (string.IsNullOrWhiteSpace(doneMessage)) return;
            Sequencer.Message(doneMessage.Trim());
        }

        private static void WriteLuaResult(string variableName, EmotionDrawResult result)
        {
            if (string.IsNullOrWhiteSpace(variableName) || result == null) return;

            DialogueLua.SetVariable(variableName, result.ResultEmotion.ToString());
            DialogueLua.SetVariable(variableName + "FakeRequested", result.FakeRequested);
            DialogueLua.SetVariable(variableName + "FakeSucceeded", result.FakeSucceeded);
            DialogueLua.SetVariable(variableName + "HeroineID", result.HeroineID ?? string.Empty);
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            return TryParseBool(raw, out bool result) ? result : fallback;
        }

        private static bool TryParseBool(string raw, out bool result)
        {
            result = false;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            raw = raw.Trim().ToLowerInvariant();

            switch (raw)
            {
                case "1":
                case "true":
                case "yes":
                case "y":
                case "show":
                case "on":
                case "有":
                case "有表演":
                case "等":
                case "等待":
                    result = true; return true;

                case "0":
                case "false":
                case "no":
                case "n":
                case "hide":
                case "off":
                case "無":
                case "沒":
                case "無表演":
                case "不等":
                    result = false; return true;

                default: return false;
            }
        }

        private static HeroineEmotionCardType ParseEmotion(string raw, HeroineEmotionCardType fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            raw = raw.Trim();

            switch (raw)
            {
                case "生氣": return HeroineEmotionCardType.Angry;
                case "害羞": return HeroineEmotionCardType.Shy;
                case "擔心": return HeroineEmotionCardType.Worried;
                case "母性": return HeroineEmotionCardType.Maternal;
                case "放鬆": return HeroineEmotionCardType.Relaxed;
                case "失望": return HeroineEmotionCardType.Disappointed;
            }

            if (Enum.TryParse(raw, true, out HeroineEmotionCardType parsed))
                return parsed;

            Debug.LogWarning($"[HeroineEmotionDraw] 無法解析情緒卡: {raw}，改用 {fallback}");
            return fallback;
        }
    }
}