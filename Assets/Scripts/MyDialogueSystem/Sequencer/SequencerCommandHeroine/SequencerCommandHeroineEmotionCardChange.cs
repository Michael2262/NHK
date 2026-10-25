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
            Debug.LogWarning("[HeroineEmotionCardChange] 情緒卡池已退役，此命令不再生效。", this);
            WriteSuccess(GetParameter(3, string.Empty).Trim(), false);
            Stop();
        }

        private static void WriteSuccess(string variableName, bool success)
        {
            if (string.IsNullOrWhiteSpace(variableName)) return;
            DialogueLua.SetVariable(variableName, success);
        }

    }
}
