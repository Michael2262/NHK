// ============================================================
// SequencerCommandTryReduceStamina.cs
// NHK compatibility version
// 舊指令名稱保留：TryReduceStamina(cost, reportMode)
// NHK 語意：嘗試承受壓力 cost。若 Stress + cost <= 100 則成功並增加 Stress。
// 用法: TryReduceStamina(cost) / TryReduceStamina(cost, alert) / TryReduceStamina(cost, type)
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandTryReduceStamina : SequencerCommand
    {
        public void Awake()
        {
            int cost = GetParameterAsInt(0);
            string reportMode = GetParameter(1, string.Empty).Trim().ToLower();

            var service = GameStatusService.Instance;
            var protagonist = service != null ? service.Protagonist : null;

            if (protagonist != null)
            {
                int stressCost = Mathf.Max(0, cost);
                bool success = protagonist.Stress + stressCost <= 100;

                if (success)
                {
                    protagonist.AddStress(stressCost);
                    ReportStressChange(stressCost, reportMode);
                }
                else
                {
                    Debug.LogWarning($"Dialogue System: TryReduceStamina/NHK 壓力承受失敗。Stress={protagonist.Stress}, Cost={stressCost}");
                }
            }
            else
            {
                Debug.LogWarning("Dialogue System: TryReduceStamina 指令找不到 GameStatusService 或 Protagonist 實例。");
            }

            Stop();
        }

        private void ReportStressChange(int amount, string reportMode)
        {
            if (amount == 0 || string.IsNullOrEmpty(reportMode) || reportMode == "false") return;

            var records = new List<ValueChangeRecord>
            {
                new ValueChangeRecord
                {
                    isHeroineResource = false,
                    resourceTypeKey = "Stress",
                    finalAmount = amount,
                    effectResult = ValueChangeResult.EffectResult.Normal,
                    heroineNameKey = null
                }
            };

            switch (reportMode)
            {
                case "alert": ValueChangeReporter.Report(records); break;
                case "type": ValueChangeReporter.ReportToSubtitle(records); break;
                default: Debug.LogWarning($"[TryReduceStamina/NHK] 未知的 reportMode: '{reportMode}'"); break;
            }
        }
    }
}
