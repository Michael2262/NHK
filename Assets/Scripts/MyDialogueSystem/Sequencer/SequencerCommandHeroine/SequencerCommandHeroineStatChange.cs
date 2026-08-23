// ============================================================
// SequencerCommandHeroineStatChange.cs
// ============================================================
// 用法：
//   HeroineStatChange(heroineID, statName, operation, amount)
//   HeroineStatChange(heroineID, statName, operation, amount, resultLuaVariable)
//
// statName   = libido | trust | affinity
// operation  = add | set
//
// 範例：
//   HeroineStatChange(sister, libido, add, 10)
//     → 姐姐性慾 +10
//   HeroineStatChange(sister, trust, add, -5)
//     → 姐姐信賴 -5
//   HeroineStatChange(sister, libido, set, 100)
//     → 姐姐性慾直接設為 100
//   HeroineStatChange(sister, trust, add, 20, LastTrustValue)
//     → 姐姐信賴 +20，並把結果寫入 Lua 變數 LastTrustValue
//
// 會寫入 Lua 變數（若有指定 resultLuaVariable）：
//   Variable["LastTrustValue"] = 70   （變更後的數值）
// ============================================================

using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandHeroineStatChange : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string statName = GetParameter(1, string.Empty).Trim().ToLowerInvariant();
            string operation = GetParameter(2, "add").Trim().ToLowerInvariant();
            int amount = GetParameterAsInt(3, 0);
            string resultLuaVariable = GetParameter(4, string.Empty).Trim();

            // ── 查找女主角 ──
            if (GameStatusService.Instance == null || GameStatusService.Instance.Heroines == null)
            {
                Debug.LogWarning("Dialogue System: HeroineStatChange 找不到 GameStatusService 或 Heroines。", this);
                Stop();
                return;
            }

            if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID, out var heroine) || heroine == null)
            {
                Debug.LogWarning($"Dialogue System: HeroineStatChange 找不到女主角: {heroineID}", this);
                Stop();
                return;
            }

            // ── 執行操作 ──
            int resultValue = 0;

            switch (statName)
            {
                case "libido":
                case "性慾":
                    ApplyOperation(heroine, operation, amount, StatTarget.Libido);
                    resultValue = heroine.Libido;
                    break;

                case "trust":
                case "信賴":
                    ApplyOperation(heroine, operation, amount, StatTarget.Trust);
                    resultValue = heroine.Trust;
                    break;

                case "affinity":
                case "好感度":
                    ApplyOperation(heroine, operation, amount, StatTarget.Affinity);
                    resultValue = heroine.Affinity;
                    break;

                default:
                    Debug.LogWarning($"[HeroineStatChange] 無法辨識 statName: {statName}，支援 libido / trust / affinity", this);
                    break;
            }

            // ── 寫入 Lua 變數 ──
            if (!string.IsNullOrWhiteSpace(resultLuaVariable))
            {
                DialogueLua.SetVariable(resultLuaVariable, resultValue);
            }

            Stop();
        }

        private enum StatTarget { Libido, Trust, Affinity }

        private static void ApplyOperation(HeroineStatusModel heroine, string operation, int amount, StatTarget target)
        {
            bool isSet;
            switch (operation)
            {
                case "set":
                case "設定":
                case "=":
                    isSet = true;
                    break;

                case "add":
                case "增減":
                case "+":
                case "-":
                default:
                    isSet = false;
                    break;
            }

            switch (target)
            {
                case StatTarget.Libido:
                    if (isSet) heroine.SetLibido(amount); else heroine.AddLibido(amount);
                    break;
                case StatTarget.Trust:
                    if (isSet) heroine.SetTrust(amount); else heroine.AddTrust(amount);
                    break;
                case StatTarget.Affinity:
                    if (isSet) heroine.SetAffinity(amount); else heroine.AddAffinity(amount);
                    break;
            }
        }
    }
}
