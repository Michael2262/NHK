// ============================================================
// SequencerCommandAddLewdnessExp.cs
// ============================================================
// 用法: AddLewdnessExp(heroineID, amount) / AddLewdnessExp(heroineID, amount, alert) / AddLewdnessExp(heroineID, amount, type)
// 範例: AddLewdnessExp(Heroine_A, 50, type)
// ★ 正向使用 em5(粉色)顯示
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandAddLewdnessExp : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);
            int delta = GetParameterAsInt(1);
            string reportMode = GetParameter(2, string.Empty).Trim().ToLower();

            if (GameStatusService.Instance != null &&
                GameStatusService.Instance.Heroines.ContainsKey(heroineID))
            {
                var heroine = GameStatusService.Instance.Heroines[heroineID];

                heroine.AddLewdnessExp(delta);

                if (delta != 0 && !string.IsNullOrEmpty(reportMode) && reportMode != "false")
                {
                    var records = new List<ValueChangeRecord>
                    {
                        new ValueChangeRecord
                        {
                            isHeroineResource = true,
                            resourceTypeKey = "LewdnessExp",
                            finalAmount = delta,
                            effectResult = delta > 0
                                ? ValueChangeResult.EffectResult.Good
                                : ValueChangeResult.EffectResult.Bad,
                            heroineNameKey = heroine.NameTextKey
                        }
                    };

                    switch (reportMode)
                    {
                        case "alert": ValueChangeReporter.Report(records); break;
                        case "type": ValueChangeReporter.ReportToSubtitle(records); break;
                        default: Debug.LogWarning($"[AddLewdnessExp] 未知的 reportMode: '{reportMode}'"); break;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Dialogue System: AddLewdnessExp 找不到 ID 為 '{heroineID}' 的女主角。");
            }

            Stop();
        }
    }
}