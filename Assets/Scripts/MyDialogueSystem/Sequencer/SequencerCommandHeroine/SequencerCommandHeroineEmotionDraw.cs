// ============================================================
// SequencerCommandHeroineEmotionDraw.cs
// ============================================================
// 用法：
//   HeroineEmotionDraw(heroineID, small|big|fakebig, show, resultLuaVariable)
//   HeroineEmotionDraw(heroineID, fakebig, show, resultLuaVariable, fakeEmotion)
//   HeroineEmotionDraw(heroineID, fakebig, show, resultLuaVariable, fakeEmotion, wait)
//
// 範例：
//   HeroineEmotionDraw(sister, big, true, LastEmotionDraw)
//   HeroineEmotionDraw(sister, small, false, LastEmotionDraw)
//   HeroineEmotionDraw(sister, fakebig, true, LastEmotionDraw, Shy)
//
// 會寫入 Lua 變數：
//   Variable["LastEmotionDraw"] = "Shy"
//   Variable["LastEmotionDrawFakeRequested"] = true/false
//   Variable["LastEmotionDrawFakeSucceeded"] = true/false
//   Variable["LastEmotionDrawHeroineID"] = "sister"
//
// wait 預設 true。true 時 SequencerCommand 會等抽選動畫 callback 後才 Stop()，
// 對話序列可以等抽選結束再進下一步。
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
            string drawKindText = GetParameter(1, "big").Trim().ToLowerInvariant();
            bool playShow = ParseBool(GetParameter(2, "true"), true);
            string resultLuaVariable = GetParameter(3, "LastEmotionDraw").Trim();
            HeroineEmotionCardType fakeEmotion = ParseEmotion(GetParameter(4, "Angry"), HeroineEmotionCardType.Angry);
            bool wait = ParseBool(GetParameter(5, "true"), true);

            if (EmotionCardDrawMachine.Instance == null)
            {
                Debug.LogWarning("Dialogue System: HeroineEmotionDraw 找不到 EmotionCardDrawMachine。", this);
                Stop();
                return;
            }

            Action<EmotionDrawResult> onComplete = result =>
            {
                WriteLuaResult(resultLuaVariable, result);
                if (wait && !stopped)
                {
                    stopped = true;
                    Stop();
                }
            };

            if (!playShow)
            {
                EmotionDrawResult result;
                switch (drawKindText)
                {
                    case "small":
                    case "s":
                    case "小":
                    case "小抽選":
                        result = EmotionCardDrawMachine.Instance.DrawSmallWithoutShow(heroineID);
                        break;

                    case "fake":
                    case "fakebig":
                    case "force":
                    case "forced":
                    case "造假":
                    case "造假大抽選":
                        result = EmotionCardDrawMachine.Instance.DrawFakeBigWithoutShow(heroineID, fakeEmotion);
                        break;

                    case "big":
                    case "b":
                    case "大":
                    case "大抽選":
                    default:
                        result = EmotionCardDrawMachine.Instance.DrawBigWithoutShow(heroineID);
                        break;
                }

                WriteLuaResult(resultLuaVariable, result);
                Stop();
                return;
            }

            switch (drawKindText)
            {
                case "small":
                case "s":
                case "小":
                case "小抽選":
                    EmotionCardDrawMachine.Instance.StartSmallDraw(heroineID, onComplete);
                    break;

                case "fake":
                case "fakebig":
                case "force":
                case "forced":
                case "造假":
                case "造假大抽選":
                    EmotionCardDrawMachine.Instance.StartFakeBigDraw(heroineID, fakeEmotion, true, onComplete);
                    break;

                case "big":
                case "b":
                case "大":
                case "大抽選":
                default:
                    EmotionCardDrawMachine.Instance.StartBigDraw(heroineID, onComplete);
                    break;
            }

            if (!wait)
            {
                stopped = true;
                Stop();
            }
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
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
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
                    return true;

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
                    return false;

                default:
                    return fallback;
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
