using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

// ==========================================================
// 1. GetAffinityEvaluation — 取得「對你的評價」TextTable Key
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("根據指定女主角的親密度等級，取得「對你的評價」的 TextTable Key。")]
public class GetAffinityEvaluation : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [Tooltip("指定的親密度等級。若留空 (None)，則自動讀取該角色當前的 BaseAffinityLevel。")]
    [UIHint(UIHint.Variable)]
    public FsmInt overrideLevel;

    [RequiredField]
    [Tooltip("存入：查到的 TextTable Key")]
    [UIHint(UIHint.Variable)]
    public FsmString storeTextKey;

    [Tooltip("需要在 Inspector 中指定 HeroineEvaluationConfig 資產")]
    public HeroineEvaluationConfig evaluationConfig;

    public override void Reset()
    {
        heroineID = null;
        overrideLevel = new FsmInt { UseVariable = true };
        storeTextKey = null;
        evaluationConfig = null;
    }

    public override void OnEnter()
    {
        Debug.LogWarning("[GetAffinityEvaluation] 舊等級評價已退役，請改用自行定義的新數值條件。");
        if (storeTextKey != null && !storeTextKey.IsNone) storeTextKey.Value = "";
        Finish();
    }
}

// ==========================================================
// 2. GetLewdnessEvaluation — 取得「H的評價」TextTable Key
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("根據指定女主角的開發度等級，取得「H的評價」的 TextTable Key。")]
public class GetLewdnessEvaluation : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [Tooltip("指定的開發度等級。若留空 (None)，則自動讀取該角色當前的 LewdnessLevel。")]
    [UIHint(UIHint.Variable)]
    public FsmInt overrideLevel;

    [RequiredField]
    [Tooltip("存入：查到的 TextTable Key")]
    [UIHint(UIHint.Variable)]
    public FsmString storeTextKey;

    [Tooltip("需要在 Inspector 中指定 HeroineEvaluationConfig 資產")]
    public HeroineEvaluationConfig evaluationConfig;

    public override void Reset()
    {
        heroineID = null;
        overrideLevel = new FsmInt { UseVariable = true };
        storeTextKey = null;
        evaluationConfig = null;
    }

    public override void OnEnter()
    {
        Debug.LogWarning("[GetLewdnessEvaluation] 舊等級評價已退役，請改用自行定義的新數值條件。");
        if (storeTextKey != null && !storeTextKey.IsNone) storeTextKey.Value = "";
        Finish();
    }
}

// ==========================================================
// 3. GetRelationshipLabel — 取得「當前關係」TextTable Key
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("根據指定女主角的親密度等級，取得「當前關係」的 TextTable Key。")]
public class GetRelationshipLabel : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [Tooltip("指定的親密度等級。若留空 (None)，則自動讀取該角色當前的 BaseAffinityLevel。")]
    [UIHint(UIHint.Variable)]
    public FsmInt overrideLevel;

    [RequiredField]
    [Tooltip("存入：查到的 TextTable Key")]
    [UIHint(UIHint.Variable)]
    public FsmString storeTextKey;

    [Tooltip("需要在 Inspector 中指定 HeroineEvaluationConfig 資產")]
    public HeroineEvaluationConfig evaluationConfig;

    public override void Reset()
    {
        heroineID = null;
        overrideLevel = new FsmInt { UseVariable = true };
        storeTextKey = null;
        evaluationConfig = null;
    }

    public override void OnEnter()
    {
        Debug.LogWarning("[GetRelationshipLabel] 舊等級評價已退役，請改用自行定義的新數值條件。");
        if (storeTextKey != null && !storeTextKey.IsNone) storeTextKey.Value = "";
        Finish();
    }
}
