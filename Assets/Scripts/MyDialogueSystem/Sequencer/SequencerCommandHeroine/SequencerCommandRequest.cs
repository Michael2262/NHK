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
// - 表演期間會鎖住繼續鈕（玩家無法按繼續跳過）；由擁有者處理繼續模式，整批演完後
//   會自動前進「一次」，因此對話腳本【不需要】再寫 Continue()@Message(RequestDone)。
//   （舊節點仍留著那行也相容，會被擋成只前進一次。）
//
// 典型：
//   RequestRoll(sister,Kiss);
//   Request(sister, ThinkPhase1, Normal);
//   Request(sister, ThinkPhase2, Angry);
//   Request(sister, Ponder);
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

        // 演出期間鎖住的繼續模式（Never）：不顯示繼續鈕、waitForContinue=false，
        // 讓擁有者 Stop() 使 Sequence 結束時自動前進一次；演出後還原成 Optional。
        private const DisplaySettings.SubtitleSettings.ContinueButtonMode HoldMode =
            DisplaySettings.SubtitleSettings.ContinueButtonMode.Never;
        private const DisplaySettings.SubtitleSettings.ContinueButtonMode RestoreMode =
            DisplaySettings.SubtitleSettings.ContinueButtonMode.Optional;

        // 只有「批次擁有者」會鎖繼續鈕＋鎖繼續模式（與「只由擁有者發 RequestDone」對稱），
        // 表演期間玩家因而無法按繼續把整批表演跳過。用旗標確保只解除一次、不外洩。
        private bool heldContinueLock;

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
            {
                // 這批的第一個 → 負責整批演完發 RequestDone。表演期間：
                //   1. PushContinueButtonLock：壓住繼續鈕，玩家無法按繼續跳過表演。
                //   2. SetContinueMode(Never)：讓演完 Stop() 時 Sequence 結束能自動前進一次，
                //      因此對話腳本「不需要」再寫 Continue()@Message(RequestDone)。
                NhkUISubtitlePanel.PushContinueButtonLock();
                DialogueManager.SetContinueMode(HoldMode);
                heldContinueLock = true;
                StartCoroutine(WaitDrainThenDone());
            }
            else
            {
                Stop();                                 // 併入進行中的表演批次（鎖由擁有者持有）
            }
        }

        private IEnumerator WaitDrainThenDone()
        {
            yield return null; // 先等一幀，確保同幀後續的 Request 都已排進佇列

            var mgr = RequestPerformanceManager.Instance;
            while (!mgr.IsIdle) yield return null;

            // 整批演完：先還原繼續模式並放開繼續鈕鎖，再發 RequestDone，然後 Stop()。
            // Stop() 使 Sequence 結束、Optional(waitForContinue=false) 自動前進一次；
            // 若舊節點仍寫著 Continue()@Message(RequestDone) 也相容（多前進的被擋成一次）。
            ReleaseContinueLockIfHeld();
            Sequencer.Message(DoneMessage);
            Stop();
        }

        /// <summary>錯誤路徑：照樣發 RequestDone 收尾，避免對話永久卡住。</summary>
        private void FailSafe()
        {
            Sequencer.Message(DoneMessage);
            Stop();
        }

        /// <summary>
        /// 解除擁有者的演出壓制：先在「鎖仍生效」時把繼續模式從 Never 還原成 Optional
        /// （避免還原瞬間閃一下繼續鈕），再放開繼續鈕鎖。只在確實壓制過時執行，且只做一次。
        /// </summary>
        private void ReleaseContinueLockIfHeld()
        {
            if (!heldContinueLock) return;
            heldContinueLock = false;
            DialogueManager.SetContinueMode(RestoreMode);
            NhkUISubtitlePanel.PopContinueButtonLock();
        }

        // 保底：對話中途被打斷（場景切換、強制結束）會殺掉 WaitDrainThenDone coroutine，
        // 使 Pop 沒機會執行。命令物件被銷毀時在此補放開一次，避免繼續鈕鎖永久外洩。
        private void OnDestroy()
        {
            ReleaseContinueLockIfHeld();
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
