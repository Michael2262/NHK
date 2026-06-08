// ============================================================
// SequencerCommandStatPackage.cs
// ============================================================
// 以 ID 調用「數值變化套組」，一次套用整包數值變化（主角 + 女主角）。
// 數值內容集中設定在 StatChangePackageDatabase（拖在 GameStatusService 上）。
//
// 用法：
//   StatPackage(packageID)
//     → 套用純主角套組，不顯示通知
//   StatPackage(packageID, heroineID)
//     → 套組含女主角數值（Libido/Trust）時，指定要套到哪位女主角
//   StatPackage(packageID, heroineID, alert)
//     → 套用後彈出 ValueChangeReporter 公告
//   StatPackage(packageID, heroineID, type)
//     → 套用後把變化寫進當前對話字幕
//
// 純主角套組想顯示通知時，heroineID 留空即可：
//   StatPackage(MyProtaPack, , alert)
//
// 範例：
//   StatPackage(DateSuccess, sister, alert)
//   StatPackage(WorkOvertime, , type)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandStatPackage : SequencerCommand
    {
        public void Awake()
        {
            string packageID = GetParameter(0, string.Empty).Trim();
            string heroineID = GetParameter(1, string.Empty).Trim();
            string reportMode = GetParameter(2, string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(packageID))
            {
                Debug.LogWarning("Dialogue System: StatPackage 未提供 packageID。", this);
                Stop();
                return;
            }

            var service = GameStatusService.Instance;
            if (service == null || service.StatChangeService == null)
            {
                Debug.LogWarning("Dialogue System: StatPackage 找不到 GameStatusService 或 StatChangeService。", this);
                Stop();
                return;
            }

            List<ValueChangeRecord> records = service.StatChangeService.Apply(packageID, heroineID);
            Report(reportMode, records);

            Stop();
        }

        /// <summary>
        /// 依 reportMode 顯示變化結果。
        /// reportMode: "alert" / "type" / 空字串 / "false" / 其他。
        /// </summary>
        private void Report(string reportMode, List<ValueChangeRecord> records)
        {
            if (records == null || records.Count == 0) return;
            if (string.IsNullOrEmpty(reportMode)) return;
            if (reportMode == "false") return;

            switch (reportMode)
            {
                case "alert":
                    ValueChangeReporter.Report(records);
                    break;
                case "type":
                    ValueChangeReporter.ReportToSubtitle(records);
                    break;
                default:
                    Debug.LogWarning($"[StatPackage] 未知的 reportMode: '{reportMode}'", this);
                    break;
            }
        }
    }
}
