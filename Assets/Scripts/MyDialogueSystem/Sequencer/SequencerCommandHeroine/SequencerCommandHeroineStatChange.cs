// ============================================================
// SequencerCommandHeroineStatChange.cs
// ============================================================
// 用法：
//   HeroineStatChange(heroineID, statName, operation, amount)
//   HeroineStatChange(heroineID, statName, operation, amount, resultLuaVariable)
//   HeroineStatChange(heroineID, mood, cappedadd, amount, cap, resultLuaVariable)
//
// statName   = interest | libido | trust | affinity | mood | excitement | orgasm
// operation  = add | set | get | weightedadd（僅 excitement）| cappedadd（僅 mood）
// get 的 amount 填 0 作為佔位；不修改數值，結果寫入 resultLuaVariable。
// weightedadd 保留舊指令名稱，現在與 add 相同，不再套用性欲倍率。
// cappedadd 的 cap 可省略，預設 8；此操作的結果變數改填第六個參數。
// cappedadd 僅接受正數增量；已達或超過 cap 時不變，增加後最多到 cap。
// cap 限制於 Mood 的合法範圍 0～10，僅影響本次增加。
//
// 範例：
//   HeroineStatChange(sister, mood, cappedadd, 3)
//     → 心情 +3，最多到 8；原本為 9 時維持 9
//   HeroineStatChange(sister, mood, cappedadd, 3, 7)
//     → 心情 +3，最多到 7
//   HeroineStatChange(sister, mood, cappedadd, 3, 8, LastMood)
//     → 心情最多加到 8，並把最終值寫入 LastMood
//   HeroineStatChange(sister, interest, add, 10)
//   HeroineStatChange(sister, interest, get, 0, LastInterest)
//   HeroineStatChange(sister, excitement, add, 10)
//   HeroineStatChange(sister, excitement, weightedadd, 10)
//   HeroineStatChange(sister, excitement, set, 50)
//   HeroineStatChange(sister, excitement, get, 0, LastExcitement)
//   HeroineStatChange(sister, orgasm, add, -10)
//   HeroineStatChange(sister, orgasm, set, 50)
//   HeroineStatChange(sister, orgasm, get, 0, LastOrgasm)
//   HeroineStatChange(sister, libido, add, 10)
//     → 姐姐性欲 +10
//   HeroineStatChange(sister, trust, add, -5)
//     → 姐姐信賴 -5
//   HeroineStatChange(sister, libido, set, 100)
//     → 姐姐性欲直接設為 100
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
        private const int DefaultMoodAddCap = 8;

        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string statName = GetParameter(1, string.Empty).Trim().ToLowerInvariant();
            string operation = GetParameter(2, "add").Trim().ToLowerInvariant();
            int amount = GetParameterAsInt(3, 0);
            bool isCappedAdd = operation == "cappedadd";
            int moodCap = isCappedAdd ? GetParameterAsInt(4, DefaultMoodAddCap) : DefaultMoodAddCap;
            string resultLuaVariable = GetParameter(isCappedAdd ? 5 : 4, string.Empty).Trim();

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

            if (isCappedAdd && statName != "mood" && statName != "心情")
            {
                Debug.LogWarning("[HeroineStatChange] cappedadd 僅支援 mood / 心情。", this);
                Stop();
                return;
            }

            int resultValue = 0;

            switch (statName)
            {
                case "interest":
                case "性興味":
                    ApplyOperation(heroine, operation, amount, StatTarget.Interest);
                    resultValue = heroine.Interest;
                    break;

                case "libido":
                case "性欲":
                case "性慾": // 保留舊指令別名
                    ApplyOperation(heroine, operation, amount, StatTarget.Libido);
                    resultValue = heroine.Libido;
                    break;

                case "trust":
                case "信賴":
                    ApplyOperation(heroine, operation, amount, StatTarget.Trust);
                    resultValue = heroine.Trust;
                    break;

                case "mood":
                case "心情":
                    if (isCappedAdd) ApplyCappedMoodAdd(heroine, amount, moodCap);
                    else ApplyOperation(heroine, operation, amount, StatTarget.Mood);
                    resultValue = heroine.Mood;
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
                    Debug.LogWarning($"[HeroineStatChange] 無法辨識 statName: {statName}，支援 interest / libido / trust / affinity / mood / excitement / orgasm", this);
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

        // 這次指令的增加限制；實際變更與通知仍透過 Model 的 SetMood。
        private static void ApplyCappedMoodAdd(HeroineStatusModel heroine, int amount, int cap)
        {
            cap = Math.Max(HeroineStatusModel.MoodMin, Math.Min(HeroineStatusModel.MoodMax, cap));
            if (amount <= 0 || heroine.Mood >= cap) return;
            heroine.SetMood((int)Math.Min((long)heroine.Mood + amount, cap));
        }

        private enum StatTarget { Libido, Trust, Affinity, Excitement, Orgasm, Mood, Interest }

        private static void ApplyOperation(HeroineStatusModel heroine, string operation, int amount, StatTarget target)
        {
            if (operation == "get") return;
            if (operation == "weightedadd")
            {
                if (target == StatTarget.Excitement) heroine.AddExcitement(amount);
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
                case StatTarget.Interest:
                    if (isSet) heroine.SetInterest(amount); else heroine.AddInterest(amount);
                    break;
                case StatTarget.Libido:
                    if (isSet) heroine.SetLibido(amount); else heroine.AddLibido(amount);
                    break;
                case StatTarget.Trust:
                    if (isSet) heroine.SetTrust(amount); else heroine.AddTrust(amount);
                    break;
                case StatTarget.Mood:
                    if (isSet) heroine.SetMood(amount); else heroine.AddMood(amount);
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
