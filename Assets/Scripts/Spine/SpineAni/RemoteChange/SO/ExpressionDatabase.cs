using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 為了 .FirstOrDefault

[CreateAssetMenu(fileName = "ExpressionDatabase", menuName = "Expression System/Expression Database")]
public class ExpressionDatabase : ScriptableObject
{
    [Tooltip("把所有 EmotionSet_XXX.asset 拉到這裡")]
    public List<EmotionExpressionSet> allEmotionSets;

    /// <summary>
    /// 【原有方法】隨機回傳一個動畫名稱 (保留向下相容)。
    /// </summary>
    public string GetRandomAnimation(HeroineEmotionType emotion, FacialPartType part)
    {
        List<string> animationList = GetAnimationList(emotion, part);

        if (animationList == null || animationList.Count == 0)
            return null;

        return animationList[Random.Range(0, animationList.Count)];
    }

    /// <summary>
    /// 【新增方法】回傳該情緒 + 該部位的完整動畫清單。
    /// 供 FacialPartController 根據 PlaybackMode 自行選取。
    /// </summary>
    public List<string> GetAnimationList(HeroineEmotionType emotion, FacialPartType part)
    {
        // 找到對應的情緒設定檔 (例如 EmotionSet_Happy)
        EmotionExpressionSet emotionSet = allEmotionSets.FirstOrDefault(set => set.emotion == emotion);

        if (emotionSet == null)
            return null;

        return emotionSet.GetListForPart(part);
    }
}