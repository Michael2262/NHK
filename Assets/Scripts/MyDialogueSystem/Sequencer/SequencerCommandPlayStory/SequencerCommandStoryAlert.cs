using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: StoryAlert(key, [duration], [arg0], [arg1], [arg2], ...)
    /// 
    /// 用法範例：
    ///   StoryAlert(UI.Mission.Complete)                     → 顯示本地化訊息，自動計算時間
    ///   StoryAlert(UI.Mission.Complete, 5)                  → 顯示本地化訊息，持續 5 秒
    ///   StoryAlert(UI.GoldReward, -1, 100)                  → 「你獲得了 {0} 金幣」→「你獲得了 100 金幣」
    ///   StoryAlert(UI.Trade, -1, 蘋果, 50)                  → 「你用 {0} 換取了 {1} 金幣」→「你用 蘋果 換取了 50 金幣」
    ///   StoryAlert(UI.Trade, 3, 蘋果, 50)                   → 同上，但固定顯示 3 秒
    /// 
    /// 參數說明：
    ///   參數 0: 本地化 Key (必填)
    ///   參數 1: 顯示時間，-1 或留空為自動計算 (選填，預設 -1)
    ///   參數 2+: 替換參數 {0}, {1}, {2}... (選填)
    /// </summary>
    public class SequencerCommandStoryAlert : SequencerCommand
    {
        public void Awake()
        {
            // 參數 0: 本地化 Key (必填)
            string key = GetParameter(0);

            if (string.IsNullOrEmpty(key))
            {
                if (DialogueDebug.logWarnings)
                    Debug.LogWarning("Sequencer Command StoryAlert(): 本地化 Key 不可為空");
                Stop();
                return;
            }

            // 參數 1: 顯示時間 (選填，預設 -1 自動計算)
            float duration = GetParameterAsFloat(1, -1f);

            // 參數 2+: 收集所有替換參數
            object[] args = CollectArgs(2);

            // 呼叫 StoryManager
            if (StoryManager.Instance != null)
            {
                if (args.Length > 0)
                {
                    // 有參數，使用帶參數的方法
                    StoryManager.Instance.ShowLocalizedMessageWithArgs(key, duration, args);
                }
                else
                {
                    // 無參數，使用簡單方法
                    StoryManager.Instance.ShowLocalizedMessage(key, duration);
                }
            }
            else
            {
                Debug.LogError("[StoryAlert] 找不到 StoryManager 實例！");
            }

            Stop(); // 指令完成
        }

        /// <summary>
        /// 從指定索引開始收集所有參數
        /// </summary>
        private object[] CollectArgs(int startIndex)
        {
            var argList = new System.Collections.Generic.List<object>();

            for (int i = startIndex; i < 20; i++) // 最多支援到參數 20
            {
                string param = GetParameter(i);

                // 空字串表示沒有更多參數了
                if (string.IsNullOrEmpty(param))
                    break;

                // 嘗試轉換為數字，否則保持字串
                if (int.TryParse(param, out int intVal))
                {
                    argList.Add(intVal);
                }
                else if (float.TryParse(param, out float floatVal))
                {
                    argList.Add(floatVal);
                }
                else
                {
                    argList.Add(param);
                }
            }

            return argList.ToArray();
        }
    }
}