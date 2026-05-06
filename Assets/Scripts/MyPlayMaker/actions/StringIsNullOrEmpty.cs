using HutongGames.PlayMaker;

[ActionCategory(ActionCategory.Logic)]
[Tooltip("檢查字串是否為空或是 None。")]
public class StringIsNullOrEmpty : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    public FsmString stringVariable;

    [Tooltip("如果字串是空的（或 null），發送此事件。")]
    public FsmEvent isEmptyEvent;

    [Tooltip("如果字串有內容，發送此事件。")]
    public FsmEvent isNotEmptyEvent;

    [UIHint(UIHint.Variable)]
    [Tooltip("也可以將結果存入一個 Bool 變數。")]
    public FsmBool storeResult;

    public bool everyFrame;

    public override void OnEnter()
    {
        DoCheck();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoCheck();
    }

    void DoCheck()
    {
        // 使用 C# 標準檢查方式
        bool isNullOrEmpty = string.IsNullOrEmpty(stringVariable.Value);

        if (storeResult != null)
            storeResult.Value = isNullOrEmpty;

        if (isNullOrEmpty && isEmptyEvent != null)
            Fsm.Event(isEmptyEvent);
        else if (!isNullOrEmpty && isNotEmptyEvent != null)
            Fsm.Event(isNotEmptyEvent);
    }
}