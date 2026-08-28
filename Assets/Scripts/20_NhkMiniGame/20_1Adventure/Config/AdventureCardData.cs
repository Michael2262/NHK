using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一張大冒險的牌。
///
/// 翻牌流程：
///   ① 套用「必有效果」(AlwaysEffects) —— 只要翻到就一定觸發
///   ② 依 OutcomeMode 決定要不要判定成敗
///   ③ 套用「成功效果」或「失敗效果」
///
/// OutcomeMode 決定 ① 之後怎麼收尾：
///   Judge        正常擲骰判成敗
///   AlwaysOnly   到 ① 就結束，②③ 都不跑
///   ForceSuccess 不擲骰，必定跑成功效果
/// Controller 讀它來決定演出節奏（要不要隔一段時間再演第二拍）。
/// </summary>
[CreateAssetMenu(menuName = "Game/Adventure/Card")]
public class AdventureCardData : ScriptableObject
{
    public string CardID => name;

    [Header("備註")]
    [Tooltip("給自己看的敘述，不影響任何功能")]
    [TextArea(2, 5)]
    public string Description;

    [Header("演出")]
    [Tooltip("啟用：這張牌不播飛入/翻面/淡出動畫、也不需要插圖。\n" +
             "只拿掉「視覺」——邏輯流程完全一樣：必有效果 → （Judge 仍會停下等玩家挑戰/繞遠路）→ 判定。")]
    public bool NoFlipCardAnimation = false;

    [Header("插圖")]
    [Tooltip("預設插圖。下面三張沒填時的 fallback")]
    public Sprite Illustration;

    [Tooltip("必有效果階段的插圖。留空 = 用預設插圖")]
    public Sprite AlwaysIllustration;

    [Tooltip("成功時的插圖。留空 = 用預設插圖")]
    public Sprite SuccessIllustration;

    [Tooltip("失敗時的插圖。留空 = 用預設插圖")]
    public Sprite FailureIllustration;

    [Header("判定")]
    [Tooltip("必有效果跑完之後怎麼收尾：\n" +
             "・Judge         正常依成功率判定成敗\n" +
             "・AlwaysOnly    到必有效果就結束，不判定成敗\n" +
             "・ForceSuccess  不擲骰，必定跑成功效果")]
    public AdventureOutcomeMode OutcomeMode = AdventureOutcomeMode.Judge;

    [Header("成功率算式：clamp 0~100")]
    public AdventureRateMode Mode = AdventureRateMode.Social;

    [Tooltip("A：基礎成功率(%)")]
    public float BaseRate = 50f;

    [Tooltip("B：社會性係數（每 1 點社會性加成的 %），Social / Both 模式使用")]
    public float SocialCoef = 0f;

    [Tooltip("C：生活力係數（每 1 點生活力加成的 %），Life / Both 模式使用")]
    public float LifeCoef = 0f;

    [Header("效果")]
    [Tooltip("只要翻到這張牌就一定會執行，不分成功失敗。先於成功/失敗效果執行")]
    [SerializeReference] public List<AdventureEffect> AlwaysEffects = new List<AdventureEffect>();

    [Tooltip("翻牌成功時依序執行")]
    [SerializeReference] public List<AdventureEffect> SuccessEffects = new List<AdventureEffect>();

    [Tooltip("翻牌失敗時依序執行（通常放 Stress）")]
    [SerializeReference] public List<AdventureEffect> FailureEffects = new List<AdventureEffect>();

    /// <summary>
    /// 依主角目前社會性 / 生活力計算成功率(%)，clamp 0~100。
    /// ForceSuccess 模式直接回傳 100。
    /// </summary>
    public float CalcSuccessRate(ProtagonistStatusModel p)
    {
        if (OutcomeMode == AdventureOutcomeMode.ForceSuccess) return 100f;
        if (p == null) return Mathf.Clamp(BaseRate, 0f, 100f);

        float rate = BaseRate;
        switch (Mode)
        {
            case AdventureRateMode.Social:
                rate += p.Sociality * SocialCoef;
                break;
            case AdventureRateMode.Life:
                rate += p.LifePower * LifeCoef;
                break;
            case AdventureRateMode.Both:
                rate += p.Sociality * SocialCoef + p.LifePower * LifeCoef;
                break;
        }
        return Mathf.Clamp(rate, 0f, 100f);
    }

    // ───── 插圖取用（沒填就 fallback 到預設插圖） ─────

    public Sprite GetAlwaysIllustration() => AlwaysIllustration != null ? AlwaysIllustration : Illustration;
    public Sprite GetSuccessIllustration() => SuccessIllustration != null ? SuccessIllustration : Illustration;
    public Sprite GetFailureIllustration() => FailureIllustration != null ? FailureIllustration : Illustration;

    /// <summary>依判定結果取對應插圖。</summary>
    public Sprite GetResultIllustration(bool success)
        => success ? GetSuccessIllustration() : GetFailureIllustration();
}
