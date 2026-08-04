// ============================================================
// SequencerCommandRequest.cs
// ============================================================
// 用法：
//   Request(heroineID, performanceName)
//   Request(heroineID, performanceName, arg1, arg2, ...)
//
// - performanceName：對應 Resources/RequestPerformance/{performanceName}.asset
//   （一顆 RequestPerformanceConfig 子類資產）。
// - arg1.. ：額外參數，交給該表演自行解讀（例：CatalogPhase 用它當情緒名）。
//
// 範例：
//   Request(sister, ThinkPhase1, Normal)   → Normal 的 phase1 掂量臉，停 phase1 秒數
//   Request(sister, ThinkPhase2, Angry)    → Angry 的 phase2 猶豫臉，停 phase2 秒數
//   Request(sister, Ponder)                → 三階段沉思（讀 Flag_RequestPass 決定停點）
//
// 特性：
// - 指令只把表演丟進 RequestPerformanceManager 佇列，然後立即結束（不各自發 RequestDone）。
// - 同一批（連續排入、中間不斷播）的表演全部演完，才由「批次擁有者」發一次 RequestDone。
// - 成敗一律讀 Flag_RequestPass 傳給表演（不需要的表演會忽略）。
//
// 典型：
//   RequestRoll(sister,Kiss);
//   Request(sister, ThinkPhase1, Normal);
//   Request(sister, ThinkPhase2, Angry);
//   Request(sister, Ponder);
//   Continue()@Message(RequestDone)
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandRequest : SequencerCommand
    {
        private const string ResourcesFolder = "RequestPerformance/";
        private const string FlagName = "Flag_RequestPass";
        private const string DoneMessage = "RequestDone";

        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string performanceName = GetParameter(1, string.Empty).Trim();

            if (string.IsNullOrEmpty(performanceName))
            {
                Debug.LogError("[Request] 缺少表演名，指令中止。", this);
                FailSafe();
                return;
            }

            var config = Resources.Load<RequestPerformanceConfig>(ResourcesFolder + performanceName);
            if (config == null)
            {
                Debug.LogError($"[Request] 找不到表演 Resources/{ResourcesFolder}{performanceName}.asset，指令中止。", this);
                FailSafe();
                return;
            }

            string[] args = GetExtraArgs();
            bool pass = ReadPass();

            bool owner = RequestPerformanceManager.Instance.Enqueue(config, heroineID, pass, args);

            if (owner)
                StartCoroutine(WaitDrainThenDone());   // 這批的第一個 → 負責整批演完發 RequestDone
            else
                Stop();                                 // 併入進行中的表演批次
        }

        private IEnumerator WaitDrainThenDone()
        {
            yield return null; // 先等一幀，確保同幀後續的 Request 都已排進佇列

            var mgr = RequestPerformanceManager.Instance;
            while (!mgr.IsIdle) yield return null;

            Sequencer.Message(DoneMessage);
            Stop();
        }

        /// <summary>錯誤路徑：照樣發 RequestDone 收尾，避免對話永久卡住。</summary>
        private void FailSafe()
        {
            Sequencer.Message(DoneMessage);
            Stop();
        }

        private bool ReadPass()
        {
            var svc = GameStatusService.Instance;
            if (svc == null || svc.ProgressFlags == null) return false;
            return svc.ProgressFlags.Contains(FlagName);
        }

        /// <summary>取第 3 個起的額外參數（給表演自行解讀）。</summary>
        private string[] GetExtraArgs()
        {
            var list = new List<string>();
            for (int i = 2; ; i++)
            {
                string p = GetParameter(i, null);
                if (p == null) break;
                list.Add(p.Trim());
            }
            return list.ToArray();
        }
    }
}
