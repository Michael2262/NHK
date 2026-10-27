using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Curtain(Cover[, 秒數[, Phase]])：遮住背景與立繪，保留較高排序的常駐 UI。
    /// Curtain(Reveal[, 秒數])：保留壓幕顏色，淡化至透明。
    /// 秒數省略、0 或負數使用 SceneCurtain 的獨立預設值。
    /// Phase 省略或負數使用目前時段；0=白天、1=黃昏、2=晚上、3=深夜。
    /// 場景中需有且僅有一個啟用的 SceneCurtain；指令等動畫完成後才 Stop。
    /// 範例：Curtain(Cover, 0.5, 2); 或 Curtain(Reveal, 0.5);
    /// 同一 Sequence 若需先遮後揭，請用 @Message 排程，不要僅靠分號排列。
    /// </summary>
    public class SequencerCommandCurtain : SequencerCommand
    {
        public void Awake()
        {
            string action = GetParameter(0)?.Trim();
            bool cover = string.Equals(action, "Cover", StringComparison.OrdinalIgnoreCase);
            bool reveal = string.Equals(action, "Reveal", StringComparison.OrdinalIgnoreCase);
            if (!cover && !reveal)
            {
                Debug.LogWarning($"[Curtain] 不支援動作「{action}」。用法：Curtain(Cover, 秒數, Phase) 或 Curtain(Reveal, 秒數)。");
                Stop();
                return;
            }

            // 只尋找顯示元件，不另建保存遊戲狀態的 Singleton。
            SceneCurtain curtain = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<SceneCurtain>(FindObjectsSortMode.None))
            {
                if (!candidate.isActiveAndEnabled) continue;
                if (curtain != null)
                {
                    Debug.LogWarning("[Curtain] 找到多個啟用的 SceneCurtain，請只保留一個，避免控制錯誤遮幕。");
                    Stop();
                    return;
                }
                curtain = candidate;
            }

            if (curtain == null)
            {
                Debug.LogWarning("[Curtain] 找不到啟用的 SceneCurtain，請在常駐場景的遮幕 Image 上掛載並啟用元件。");
                Stop();
                return;
            }

            float duration = GetParameterAsFloat(1, -1f);
            var operation = cover
                ? curtain.Cover(duration, GetParameterAsInt(2, -1))
                : curtain.Reveal(duration);
            StartCoroutine(WaitForCompletion(operation));
        }

        private IEnumerator WaitForCompletion(SceneCurtain.FadeOperation operation)
        {
            while (!operation.IsDone) yield return null;
            Stop();
        }
    }
}
