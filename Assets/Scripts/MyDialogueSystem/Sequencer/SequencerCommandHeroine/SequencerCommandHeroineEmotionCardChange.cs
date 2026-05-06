// ============================================================
// SequencerCommandHeroineEmotionCardChange.cs
// ============================================================
// 用法：
//   HeroineEmotionCardChange(heroineID, add|remove, emotion)
//   HeroineEmotionCardChange(heroineID, add|remove, emotion, successLuaVariable)
//
// 範例：
//   HeroineEmotionCardChange(sister, add, Shy)
//   HeroineEmotionCardChange(sister, remove, Angry, LastEmotionCardChangeSuccess)
// ============================================================

using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandHeroineEmotionCardChange : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string operation = GetParameter(1, "add").Trim().ToLowerInvariant();
            HeroineEmotionCardType emotion = ParseEmotion(GetParameter(2, "Angry"), HeroineEmotionCardType.Angry);
            string successLuaVariable = GetParameter(3, string.Empty).Trim();

            bool success = false;

            if (GameStatusService.Instance == null || GameStatusService.Instance.Heroines == null)
            {
                Debug.LogWarning("Dialogue System: HeroineEmotionCardChange 找不到 GameStatusService 或 Heroines。", this);
                WriteSuccess(successLuaVariable, false);
                Stop();
                return;
            }

            if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID, out var heroine) || heroine == null)
            {
                Debug.LogWarning($"Dialogue System: HeroineEmotionCardChange 找不到女主角: {heroineID}", this);
                WriteSuccess(successLuaVariable, false);
                Stop();
                return;
            }

            switch (operation)
            {
                case "remove":
                case "minus":
                case "-":
                case "減":
                case "移除":
                    success = heroine.RemoveOneCardOfType(emotion);
                    break;

                case "add":
                case "replace":
                case "+":
                case "加":
                case "新增":
                case "替換":
                default:
                    heroine.ReplaceEmotionCard(emotion);
                    success = true;
                    break;
            }

            WriteSuccess(successLuaVariable, success);
            Stop();
        }

        private static void WriteSuccess(string variableName, bool success)
        {
            if (string.IsNullOrWhiteSpace(variableName)) return;
            DialogueLua.SetVariable(variableName, success);
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

            Debug.LogWarning($"[HeroineEmotionCardChange] 無法解析情緒卡: {raw}，改用 {fallback}");
            return fallback;
        }
    }
}
