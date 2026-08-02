// ============================================================
// SequencerCommandRequest.cs
// ============================================================
// 用法：
//   Request(heroineID, performanceType)
//
// performanceType（目前只有 Think）：
//   Think → 固定跑 Big + Current 的兩段式立繪表演（掂量 → 猶豫）
//
// 範例：
//   Request(sister, Think)
//     → sister 播兩段式「掂量」表演，演完發訊息 RequestDone，並結束本指令
//
// 特性（相對 HeroineEmotionDraw 的精簡版）：
//   - 純表演。不做任何情緒抽選、不使用結果、不寫入任何 Lua 變數。
//   - 表演結束時發送 Sequencer Message "RequestDone" 通知對話系統。
//   - 指令會阻塞到表演結束才 Stop()，方便對話端等待。
//
// 對話端等待表演結束的寫法（擇一）：
//   Request(sister,Think);
//   Continue()@Message(RequestDone)
// ============================================================

using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandRequest : SequencerCommand
    {
        private const string DoneMessage = "RequestDone";

        private bool finished;

        public void Awake()
        {
            string heroineID = GetParameter(0, string.Empty).Trim();
            string performanceText = GetParameter(1, "Think").Trim();

            if (string.IsNullOrEmpty(heroineID))
            {
                Debug.LogError("[Request] 缺少 heroineID，指令中止。", this);
                Complete();
                return;
            }

            if (!TryParsePerformance(performanceText, out RequestPerformance performance))
            {
                Debug.LogWarning($"[Request] 未知的表演類型 '{performanceText}'，改用 Think。", this);
                performance = RequestPerformance.Think;
            }

            if (EmotionCardDrawMachine.Instance == null)
            {
                Debug.LogWarning("[Request] 找不到 EmotionCardDrawMachine，指令中止。", this);
                Complete();
                return;
            }

            PlayPerformance(heroineID, performance);
        }

        // ─────────────────────────────────────────────────────────
        // 表演
        // ─────────────────────────────────────────────────────────

        private void PlayPerformance(string heroineID, RequestPerformance performance)
        {
            Action<EmotionDrawResult> onComplete = _ => Complete();

            switch (performance)
            {
                case RequestPerformance.Think:
                default:
                    // 固定 Big 表演 + Current 結果模式：純演出，不使用抽選結果。
                    EmotionCardDrawMachine.Instance.StartBigDraw(heroineID, onComplete);
                    break;
            }
        }

        /// <summary>表演結束（或提前中止）：通知對話系統並結束指令，保證只執行一次。</summary>
        private void Complete()
        {
            if (finished) return;
            finished = true;

            Sequencer.Message(DoneMessage);
            Stop();
        }

        // ─────────────────────────────────────────────────────────
        // 表演類型（保留擴充空間，目前只有 Think）
        // ─────────────────────────────────────────────────────────

        private enum RequestPerformance
        {
            Think
        }

        private static bool TryParsePerformance(string text, out RequestPerformance performance)
        {
            performance = RequestPerformance.Think;
            if (string.IsNullOrEmpty(text)) return false;

            switch (text.ToLowerInvariant())
            {
                case "think":
                case "掂量":
                case "思考":
                case "猶豫":
                    performance = RequestPerformance.Think;
                    return true;
                default:
                    return false;
            }
        }
    }
}
