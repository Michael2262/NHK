// ============================================================
// SequencerCommandAddMoney.cs
// ============================================================
// 用法: AddMoney(amount) / AddMoney(amount, alert) / AddMoney(amount, type)
// 範例: AddMoney(100, type)
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandAddMoney : SequencerCommand
    {
        public void Awake()
        {
            int delta = GetParameterAsInt(0);
            string reportMode = GetParameter(1, string.Empty).Trim().ToLower();

            if (GameStatusService.Instance != null && GameStatusService.Instance.Protagonist != null)
            {
                GameStatusService.Instance.Protagonist.AddMoney(delta);

                if (delta != 0 && !string.IsNullOrEmpty(reportMode) && reportMode != "false")
                {
                    var records = new List<ValueChangeRecord>
                    {
                        new ValueChangeRecord
                        {
                            isHeroineResource = false,
                            resourceTypeKey = "Money",
                            finalAmount = delta,
                            effectResult = ValueChangeResult.EffectResult.Normal,
                            heroineNameKey = null
                        }
                    };

                    switch (reportMode)
                    {
                        case "alert": ValueChangeReporter.Report(records); break;
                        case "type": ValueChangeReporter.ReportToSubtitle(records); break;
                        default: Debug.LogWarning($"[AddMoney] 未知的 reportMode: '{reportMode}'"); break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Dialogue System: AddMoney 指令找不到 GameStatusService 或 Protagonist 實例。");
            }

            Stop();
        }
    }
}