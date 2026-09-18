// ============================================================
// SequencerCommandHeroineStatChange.cs
// ============================================================
// 用法：
//   HeroineStatChange(heroineID, statName, operation, amount)
//   HeroineStatChange(heroineID, statName, operation, amount, resultLuaVariable)
//
// statName   = libido | trust | affinity | excitement | orgasm
// operation  = add | set | get | weightedadd（僅 excitement）
// get 的 amount 填 0 作為佔位；不修改數值，結果寫入 resultLuaVariable。
// weightedadd 僅正數套用性慾倍率；倍率、四捨五入與上下限由 Model 處理。
//
// 範例：
//   HeroineStatChange(sister, excitement, add, 10)
//   HeroineStatChange(sister, excitement, weightedadd, 10)
//   HeroineStatChange(sister, excitement, set, 50)
//   HeroineStatChange(sister, excitement, get, 0, LastExcitement)
//   HeroineStatChange(sister, orgasm, add, -10)
//   HeroineStatChange(sister, orgasm, set, 50)
//   HeroineStatChange(sister, orgasm, get, 0, LastOrgasm)
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
            if (operation == "weightedadd" && statName != "excitement" && statName != "興奮度")
            {
                Debug.LogWarning("[HeroineStatChange] weightedadd 僅支援 excitement / 興奮度。", this);
                Stop();
                return;
            }

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

                case "excitement":
                case "興奮度":
                    ApplyOperation(heroine, operation, amount, StatTarget.Excitement);
                    resultValue = heroine.GetExcitement();
                    break;

                case "orgasm":
                case "高潮度":
                    ApplyOperation(heroine, operation, amount, StatTarget.Orgasm);
                    resultValue = heroine.GetOrgasm();
                    break;

                default:
                    Debug.LogWarning($"[HeroineStatChange] 無法辨識 statName: {statName}，支援 libido / trust / affinity / excitement / orgasm", this);
                    Stop();
                    return;
            }

            // ── 寫入 Lua 變數 ──
            if (!string.IsNullOrWhiteSpace(resultLuaVariable))
            {
                DialogueLua.SetVariable(resultLuaVariable, resultValue);
            }

            Stop();
        }

        private enum StatTarget { Libido, Trust, Affinity, Excitement, Orgasm }

        private static void ApplyOperation(HeroineStatusModel heroine, string operation, int amount, StatTarget target)
        {
            if (operation == "get") return;
            if (operation == "weightedadd")
            {
                if (target == StatTarget.Excitement) heroine.WeightedAddExcitement(amount);
                return;
            }

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
                case StatTarget.Excitement:
                    if (isSet) heroine.SetExcitement(amount); else heroine.AddExcitement(amount);
                    break;
                case StatTarget.Orgasm:
                    if (isSet) heroine.SetOrgasm(amount); else heroine.AddOrgasm(amount);
                    break;
            }
        }
    }
}
