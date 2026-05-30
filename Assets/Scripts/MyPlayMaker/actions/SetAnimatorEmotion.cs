// 需要專案已安裝 PlayMaker 才能編譯。
// 放進專案後,可在 PlayMaker 的 Action Browser -> "Animator Emotion" 分類下找到。
using HutongGames.PlayMaker;

[ActionCategory("Animator Emotion")]
[Tooltip("透過 AnimatorEmotionController 觸發 Animator 的 Trigger (情緒 / Think / Stop)。")]
public class SetAnimatorEmotion : FsmStateAction
{
    [RequiredField]
    [Tooltip("控制器 ID,對應 AnimatorEmotionController.controllerId。")]
    public FsmString controllerId = "Main";

    [RequiredField]
    [ObjectType(typeof(EmotionAnimatorTrigger))]
    [Tooltip("要觸發的 Trigger (下拉選單)。")]
    public FsmEnum trigger;

    [Tooltip("找不到控制器時是否記錄錯誤。")]
    public FsmBool logErrorIfMissing;

    public override void Reset()
    {
        controllerId = "Main";
        trigger = null;
        logErrorIfMissing = true;
    }

    public override void OnEnter()
    {
        var ctrl = AnimatorEmotionController.Get(controllerId.Value);
        if (ctrl != null)
        {
            ctrl.Trigger((EmotionAnimatorTrigger)trigger.Value);
        }
        else if (logErrorIfMissing.Value)
        {
            LogError($"找不到 controllerId = '{controllerId.Value}' 的 AnimatorEmotionController。");
        }

        Finish();
    }
}
