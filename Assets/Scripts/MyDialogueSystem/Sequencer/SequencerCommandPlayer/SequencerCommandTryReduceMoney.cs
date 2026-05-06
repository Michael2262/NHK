// ============================================================
// SequencerCommandTryReduceMoney.cs
// ============================================================
// 用法: TryReduceMoney(cost) / TryReduceMoney(cost, alert) / TryReduceMoney(cost, type)
// 範例: TryReduceMoney(50, type)
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandTryReduceMoney : SequencerCommand
    {
        public void Awake()
        {
            int cost = GetParameterAsInt(0);
            string reportMode = GetParameter(1, string.Empty).Trim().ToLower();

            if (GameStatusService.Instance != null && GameStatusService.Instance.Protagonist != null)
            {
                bool success = GameStatusService.Instance.Protagonist.TryReduceMoney(cost);

                if (success)
                {
                    if (cost != 0 && !string.IsNullOrEmpty(reportMode) && reportMode != "false")
                    {
                        var records = new List<ValueChangeRecord>
                        {
                            new ValueChangeRecord
                            {
                                isHeroineResource = false,
                                resourceTypeKey = "Money",
                                finalAmount = -cost,
                                effectResult = ValueChangeResult.EffectResult.Normal,
                                heroineNameKey = null
                            }
                        };

                        switch (reportMode)
                        {
                            case "alert": ValueChangeReporter.Report(records); break;
                            case "type": ValueChangeReporter.ReportToSubtitle(records); break;
                            default: Debug.LogWarning($"[TryReduceMoney] 未知的 reportMode: '{reportMode}'"); break;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Dialogue System: TryReduceMoney 失敗，玩家金錢不足以支付 {cost}。");
                }
            }
            else
            {
                Debug.LogWarning("Dialogue System: TryReduceMoney 指令找不到 GameStatusService 或 Protagonist 實例。");
            }

            Stop();
        }
    }
}