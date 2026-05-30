using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer 指令:Anim(triggerName[, controllerId])
    ///
    /// 範例:
    ///   Anim(Angry)          觸發 Angry 情緒 (預設 Main 控制器)
    ///   Anim(Think)          觸發 Think
    ///   Anim(Stop)           觸發 Stop
    ///   Anim(Shy, Boss)      對 controllerId = Boss 的控制器觸發 Shy
    ///
    /// triggerName 必須是 EmotionAnimatorTrigger 列舉裡的名稱 (大小寫不敏感)。
    /// 這是「立即指令」,執行完馬上結束,不會卡住對話流程。
    /// </summary>
    public class SequencerCommandAnim : SequencerCommand
    {
        public void Start()
        {
            string raw = GetParameter(0);
            string id = GetParameter(1, "Main");

            // 字串 -> enum (忽略大小寫),並排除數字 / 非法值
            if (!System.Enum.TryParse<EmotionAnimatorTrigger>(raw, true, out var trigger)
                || !System.Enum.IsDefined(typeof(EmotionAnimatorTrigger), trigger))
            {
                if (DialogueDebug.logWarnings)
                    Debug.LogWarning($"Dialogue System: Sequencer: Anim({raw}) '{raw}' 不是合法的 EmotionAnimatorTrigger。");
                Stop();
                return;
            }

            var ctrl = AnimatorEmotionController.Get(id);
            if (ctrl != null)
            {
                ctrl.Trigger(trigger);
            }
            else if (DialogueDebug.logWarnings)
            {
                Debug.LogWarning($"Dialogue System: Sequencer: Anim({raw}, {id}) 找不到 controllerId = '{id}' 的 AnimatorEmotionController。");
            }

            Stop(); // 立即指令,執行完就結束
        }
    }
}
