using UnityEngine;
using System;

/// <summary>
/// 各主導情緒對應的基礎失敗率設定。
/// 建立方式：右鍵 → Create → ActionChance → EmotionFailureRateConfig
/// 可以為不同女主角或不同場景建立不同的 config 資產。
/// </summary>
[CreateAssetMenu(fileName = "EmotionFailureRateConfig", menuName = "ActionChance/EmotionFailureRateConfig")]
public class EmotionFailureRateConfig : ScriptableObject
{
    [Serializable]
    public struct EmotionFailureEntry
    {
        public HeroineEmotionCardType emotion;

        [Tooltip("該情緒作為主導情緒時的基礎失敗率（百分比，例如 30 = 30%）。")]
        [Range(0f, 100f)]
        public float failureRate;
    }

    [Tooltip("針對每種主導情緒填入基礎失敗率。未列出的情緒預設使用 defaultFailureRate。")]
    public EmotionFailureEntry[] entries = new EmotionFailureEntry[]
    {
        new EmotionFailureEntry { emotion = HeroineEmotionCardType.Angry,        failureRate = 40f },
        new EmotionFailureEntry { emotion = HeroineEmotionCardType.Shy,          failureRate = 15f },
        new EmotionFailureEntry { emotion = HeroineEmotionCardType.Worried,      failureRate = 30f },
        new EmotionFailureEntry { emotion = HeroineEmotionCardType.Maternal,     failureRate = 10f },
        new EmotionFailureEntry { emotion = HeroineEmotionCardType.Relaxed,      failureRate = 10f },
        new EmotionFailureEntry { emotion = HeroineEmotionCardType.Disappointed, failureRate = 50f },
    };

    [Tooltip("若情緒不在 entries 中，使用此預設失敗率。")]
    [Range(0f, 100f)]
    public float defaultFailureRate = 25f;

    /// <summary>
    /// 取得指定情緒的基礎失敗率（百分比）。
    /// </summary>
    public float GetFailureRate(HeroineEmotionCardType emotion)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].emotion == emotion)
                return entries[i].failureRate;
        }
        return defaultFailureRate;
    }
}
