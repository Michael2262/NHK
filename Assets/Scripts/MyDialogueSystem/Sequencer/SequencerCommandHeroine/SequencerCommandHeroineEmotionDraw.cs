// ============================================================
// SequencerCommandHeroineEmotionDraw.cs
// ============================================================
// 用法：
//   HeroineEmotionDraw(heroineID, small|medium|big|fakebig, show, resultLuaVariable)
//   HeroineEmotionDraw(heroineID, small|medium|big, show, resultLuaVariable, wait, doneMessage)
//   HeroineEmotionDraw(heroineID, small|medium|big, show, resultLuaVariable, doneMessage)
//   HeroineEmotionDraw(heroineID, fakebig, show, resultLuaVariable, fakeEmotion)
//   HeroineEmotionDraw(heroineID, fakebig, show, resultLuaVariable, fakeEmotion, wait, doneMessage)
//   HeroineEmotionDraw(heroineID, fakebig, show, resultLuaVariable, fakeEmotion, doneMessage)
//
// 範例：
//   HeroineEmotionDraw(sister, small, true, LastEmotionDraw)
//   HeroineEmotionDraw(sister, medium, true, LastEmotionDraw)
//   HeroineEmotionDraw(sister, big, true, LastEmotionDraw, true, EmotionDrawDone)
//   HeroineEmotionDraw(sister, fakebig, true, LastEmotionDraw, Shy, true, EmotionDrawDone)
//
// 會寫入 Lua 變數：
//   Variable["LastEmotionDraw"] = "Shy"
//   Variable["LastEmotionDrawFakeRequested"] = true/false
//   Variable["LastEmotionDrawFakeSucceeded"] = true/false
//   Variable["LastEmotionDrawHeroineID"] = "sister"
//
// wait 預設 true。true 時 SequencerCommand 會等抽選動畫 callback 後才 Stop()。
// doneMessage 若有填，會在抽選 callback 完成後呼叫 Sequencer.Message(doneMessage)，
// 可搭配 SetContinueMode(Optional)@Message(doneMessage) 使用。
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

            bool isFakeDraw = IsFakeDrawKind(drawKindText);
            HeroineEmotionCardType fakeEmotion = isFakeDraw
                ? ParseEmotion(GetParameter(4, "Angry"), HeroineEmotionCardType.Angry)
                : HeroineEmotionCardType.Angry;

            ParseWaitAndDoneMessage(isFakeDraw, out bool wait, out string doneMessage);

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

                    case "medium":
                    case "m":
                    case "middle":
                    case "mid":
                    case "中":
                    case "中抽選":
                        result = EmotionCardDrawMachine.Instance.DrawMediumWithoutShow(heroineID);
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
                SendDoneMessage(doneMessage);
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

                case "medium":
                case "m":
                case "middle":
                case "mid":
                case "中":
                case "中抽選":
                    EmotionCardDrawMachine.Instance.StartMediumDraw(heroineID, onComplete);
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

        private void ParseWaitAndDoneMessage(bool isFakeDraw, out bool wait, out string doneMessage)
        {
            wait = true;
            doneMessage = string.Empty;

            string firstExtra = isFakeDraw ? GetParameter(5, string.Empty).Trim() : GetParameter(4, string.Empty).Trim();
            string secondExtra = isFakeDraw ? GetParameter(6, string.Empty).Trim() : GetParameter(5, string.Empty).Trim();

            if (TryParseBool(firstExtra, out bool parsedWait))
            {
                wait = parsedWait;
                doneMessage = secondExtra;
                return;
            }

            if (!string.IsNullOrWhiteSpace(firstExtra))
            {
                doneMessage = firstExtra;
            }

            if (TryParseBool(secondExtra, out parsedWait))
            {
                wait = parsedWait;
            }
            else if (!string.IsNullOrWhiteSpace(secondExtra))
            {
                doneMessage = secondExtra;
            }
        }

        private static bool IsFakeDrawKind(string drawKindText)
        {
            switch (drawKindText)
            {
                case "fake":
                case "fakebig":
                case "force":
                case "forced":
                case "造假":
                case "造假大抽選":
                    return true;

                default:
                    return false;
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
                    result = true;
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
                    result = false;
                    return true;

                default:
                    return false;
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
