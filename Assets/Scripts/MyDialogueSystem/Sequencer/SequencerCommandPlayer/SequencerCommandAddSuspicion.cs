// ============================================================
// SequencerCommandAddSuspicion.cs
// NHK compatibility version
// 舊指令名稱保留：AddSuspicion(amount, reportMode)
// NHK 語意：AddStress(amount)
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandAddSuspicion : SequencerCommand
    {
        public void Awake()
        {
            int amount = GetParameterAsInt(0);
            string reportMode = GetParameter(1, string.Empty).Trim().ToLower();

            var service = GameStatusService.Instance;
            var protagonist = service != null ? service.Protagonist : null;

            if (protagonist != null)
            {
                protagonist.AddStress(amount);
                ReportStressChange(amount, reportMode);
            }
            else
            {
                Debug.LogWarning("Dialogue System: AddSuspicion/NHK 指令找不到 GameStatusService 或 Protagonist 實例。");
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
                default: Debug.LogWarning($"[AddSuspicion/NHK] 未知的 reportMode: '{reportMode}'"); break;
            }
        }
    }
}
