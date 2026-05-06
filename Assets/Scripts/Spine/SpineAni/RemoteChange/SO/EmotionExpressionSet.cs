using System.Collections.Generic;
using UnityEngine;

// 你可以從 Assets/Create 選單裡建立此設定檔
[CreateAssetMenu(fileName = "EmotionSet_New", menuName = "Expression System/Emotion Expression Set")]
public class EmotionExpressionSet : ScriptableObject
{
    [Tooltip("這個設定檔對應哪一種情緒")]
    public HeroineEmotionType emotion; // 你的 HeroineEmotionType Enum

    [Tooltip("此情緒下可使用的「眼睛」動畫")]
    public List<string> eyeAnimations;
    [Tooltip("此情緒下可使用的「嘴巴」動畫")]
    public List<string> mouthAnimations;
    [Tooltip("此情緒下可使用的「眉毛」動畫")]
    public List<string> browAnimations;
    [Tooltip("此情緒下可使用的「臉部」動畫 (原 Blush)")]
    public List<string> faceAnimations;
    [Tooltip("此情緒下可使用的「汗水」動畫")]
    public List<string> sweatAnimations;
    [Tooltip("此情緒下可使用的「SF」動畫")]
    public List<string> sfAnimations;
    [Tooltip("此情緒下可使用的「Body」動畫")]
    public List<string> bodyAnimations;

    /// <summary>
    /// 根據部位取得對應的動畫清單 (完整 List 回傳，供 FacialPartController 依模式選取)。
    /// </summary>
    public List<string> GetListForPart(FacialPartType partType)
    {
        switch (partType)
        {
            case FacialPartType.Eyes: return eyeAnimations;
            case FacialPartType.Mouth: return mouthAnimations;
            case FacialPartType.Brows: return browAnimations;
            case FacialPartType.Face: return faceAnimations;
            case FacialPartType.Sweat: return sweatAnimations;
            case FacialPartType.SF: return sfAnimations;
            case FacialPartType.Body: return bodyAnimations;

            default:
                Debug.LogWarning($"GetListForPart: 找不到 {partType} 對應的清單");
                return null;
        }
    }
}