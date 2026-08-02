// ============================================================
// SequencerCommandEmotionResultMessage.cs
// ============================================================
// 用法：
//   EmotionResultMessage(heroineID)
//   EmotionResultMessage(heroineID, emotion)
//   EmotionResultMessage(heroineID, emotion, duration)
//   EmotionResultMessage(heroineID, emotion, duration, wait)
//
// 範例：
//   EmotionResultMessage(sister)                        → 用上次抽選結果，預設秒數
//   EmotionResultMessage(sister, Shy)                   → 指定害羞
//   EmotionResultMessage(sister, Worried, 2.0)          → 指定擔心，顯示 2 秒
//   EmotionResultMessage(sister, , 1.5)                 → 用上次抽選結果，顯示 1.5 秒
//   EmotionResultMessage(sister, Shy, 2.0, false)       → 不等待
//
// 說明：
// - 透過 EmotionCardDrawView 顯示「{角色名} 覺得 {情緒名}」（TextTable: Emotion.Result）。
// - emotion 留空時，自動讀取 Lua 變數 LastEmotionDraw 作為預設情緒。
// - duration 預設 2.0 秒。
// - wait 預設 true，等文字顯示完畢後才 Stop()。
// ============================================================

using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandEmotionResultMessage : SequencerCommand
    {
        private bool stopped;

        private const float DefaultDuration = 2.0f;
        private const string DefaultLuaVariable = "LastEmotionDraw";

        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string emotionRaw = GetParameter(1, string.Empty).Trim();
            float duration = GetParameterAsFloat(2, DefaultDuration);
            bool wait = ParseBool(GetParameter(3, "true"), true);

            // 決定情緒：有填就用填的，沒填就讀 LastEmotionDraw
            HeroineEmotionCardType emotion;
            if (string.IsNullOrEmpty(emotionRaw))
            {
                string lastDraw = DialogueLua.GetVariable(DefaultLuaVariable).asString;
                emotion = ParseEmotion(lastDraw, HeroineEmotionCardType.Angry);
            }
            else
            {
                emotion = ParseEmotion(emotionRaw, HeroineEmotionCardType.Angry);
            }

            // 找 EmotionCardDrawMachine（立繪演出已內化到 Machine，不再經由 DrawView）
            if (EmotionCardDrawMachine.Instance == null)
            {
                Debug.LogWarning("Dialogue System: EmotionResultMessage 找不到 EmotionCardDrawMachine。", this);
                Stop();
                return;
            }

            // 如果 heroineID 沒填，用 Machine 上的 currentHeroineID
            if (string.IsNullOrEmpty(heroineID))
                heroineID = EmotionCardDrawMachine.Instance.CurrentHeroineID;

            EmotionCardDrawMachine.Instance.ShowEmotionResult(heroineID, emotion, duration, () =>
            {
                if (wait && !stopped)
                {
                    stopped = true;
                    Stop();
                }
            });

            if (!wait)
            {
                stopped = true;
                Stop();
            }
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
                case "on":
                case "等":
                case "等待":
                    return true;

                case "0":
                case "false":
                case "no":
                case "n":
                case "off":
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

            Debug.LogWarning($"[EmotionResultMessage] 無法解析情緒: {raw}，改用 {fallback}");
            return fallback;
        }
    }
}
