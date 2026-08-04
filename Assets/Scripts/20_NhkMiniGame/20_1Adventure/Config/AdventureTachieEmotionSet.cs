using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個女主角的「情緒 → 立繪變體」設定。
/// 每個情緒可放多個變體，每個變體是一組 Sequencer 命令字串（跟對話系統同一套語法）。
/// 「調用 Tachie 變化」時，依該女主角目前的 CurrentEmotion 隨機挑一個變體來跑。
/// </summary>
[CreateAssetMenu(menuName = "Game/Adventure/Tachie Emotion Set")]
public class AdventureTachieEmotionSet : ScriptableObject
{
    [Tooltip("這組表情屬於哪個女主角 ID。\n用來讀 CurrentEmotion，也會替換命令字串裡的 {actor}")]
    public string HeroineID = "Sister";

    [Tooltip("各情緒對應的立繪變體。找不到目前情緒的設定時，改用清單第一筆")]
    public List<EmotionVariants> Entries = new List<EmotionVariants>();

    [Serializable]
    public class EmotionVariants
    {
        public HeroineEmotionCardType Emotion = HeroineEmotionCardType.Normal;

        [Tooltip("多個變體，隨機挑一個。每個變體是一組 Sequencer 命令字串（跟對話 sequence 同語法，" +
                 "多行或以 ; 分隔皆可）。\n可用 {actor} 代表女主角 ID。\n" +
                 "例：TachieControl(Expression, {actor}, Sad1); TachieControl(Mouth, {actor}, Close2)")]
        [TextArea(2, 5)]
        public List<string> Variants = new List<string>();
    }

    /// <summary>
    /// 依情緒取一個隨機變體字串。
    /// 找不到該情緒的設定 → 用清單第一筆；完全沒有可用變體 → 回傳 null。
    /// </summary>
    public string PickVariant(HeroineEmotionCardType emotion)
    {
        if (Entries == null || Entries.Count == 0) return null;

        var entry = Entries.Find(e => e != null && e.Emotion == emotion);
        if (entry == null) entry = Entries[0]; // 沒設置的情緒 → 用第一筆

        if (entry.Variants == null || entry.Variants.Count == 0) return null;

        // 過濾空字串後隨機挑
        var valid = entry.Variants.FindAll(s => !string.IsNullOrWhiteSpace(s));
        if (valid.Count == 0) return null;
        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }
}
