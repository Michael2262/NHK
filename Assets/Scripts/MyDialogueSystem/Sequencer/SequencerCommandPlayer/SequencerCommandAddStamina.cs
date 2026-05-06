// ============================================================
// SequencerCommandAddStamina.cs
// NHK compatibility version
// 舊指令名稱保留：AddStamina(delta, reportMode)
// NHK 語意：
//   delta < 0：原本是扣體力，現在轉為增加壓力 -delta
//   delta > 0：原本是回體力，現在轉為降低壓力 delta
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandAddStamina : SequencerCommand
    {
        public void Awake()
        {
            int delta = GetParameterAsInt(0);
            string reportMode = GetParameter(1, string.Empty).Trim().ToLower();

            var service = GameStatusService.Instance;
            var protagonist = service != null ? service.Protagonist : null;

            if (protagonist != null)
            {
                int stressDelta = -delta;
                protagonist.AddStress(stressDelta);
                ReportStressChange(stressDelta, reportMode);
            }
            else
            {
                Debug.LogWarning("Dialogue System: AddStamina/NHK 指令找不到 GameStatusService 或 Protagonist 實例。");
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
                default: Debug.LogWarning($"[AddStamina/NHK] 未知的 reportMode: '{reportMode}'"); break;
            }
        }
    }
}
