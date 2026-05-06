using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 單一女主角的基礎設定。
/// NHK 版保留多女主角架構，但核心數值改為情緒卡初始卡池。
/// </summary>
[Serializable]
public class HeroineStat
{
    [Header("基礎資訊")]
    [Tooltip("角色的唯一英文ID，絕不能重複。")]
    public string ID;

    [Tooltip("對應 TextTable 中的 Key，例如：HeroineName1。")]
    public string NameTextKey;

    [Tooltip("編輯器顯示用名稱，實際顯示請查表。")]
    public string Name;

    [Tooltip("年紀。僅供設定管理，遊戲實際顯示由劇情文本決定。")]
    public int Age = 20;

    [Tooltip("舊 UI 相容用：初次對象名稱 TextTable Key。NHK 原型可留空。")]
    public string FirstPartnerNameKey;

    [Header("初始情緒卡池 Override")]
    [Tooltip("若勾選，這位女主角使用下方自訂初始情緒卡數量；否則使用全域初始卡池。")]
    public bool UseCustomInitialEmotionDeck = false;

    [Min(0)] public int InitialAngryCards = 8;
    [Min(0)] public int InitialShyCards = 2;
    [Min(0)] public int InitialWorriedCards = 0;
    [Min(0)] public int InitialMaternalCards = 0;
    [Min(0)] public int InitialRelaxedCards = 0;
    [Min(0)] public int InitialDisappointedCards = 0;
}

[CreateAssetMenu(menuName = "Game/Config/HeroineStatus")]
public class HeroineStatusConfig : ScriptableObject
{
    [Header("角色列表 (Roster)")]
    public List<HeroineStat> HeroineStats = new List<HeroineStat>();

    [Header("NHK 情緒卡池設定")]
    [Min(1)] public int EmotionDeckMaxCount = 10;

    [Tooltip("全域初始：生氣卡數。")]
    [Min(0)] public int InitialAngryCards = 8;

    [Tooltip("全域初始：害羞卡數。")]
    [Min(0)] public int InitialShyCards = 2;

    [Tooltip("全域初始：擔心卡數。")]
    [Min(0)] public int InitialWorriedCards = 0;

    [Tooltip("全域初始：母性卡數。")]
    [Min(0)] public int InitialMaternalCards = 0;

    [Tooltip("全域初始：放鬆卡數。")]
    [Min(0)] public int InitialRelaxedCards = 0;

    [Tooltip("全域初始：失望卡數。")]
    [Min(0)] public int InitialDisappointedCards = 0;

    [Header("抽選表演設定")]
    [Min(0f)] public float SmallDrawDuration = 0.75f;
    [Min(0f)] public float BigDrawDuration = 2.0f;
    [Min(1)] public int SmallDrawFlipCount = 4;
    [Min(1)] public int BigDrawFlipCount = 12;


    [Header("舊數值系統相容設定（NHK 原型不主動使用）")]
    [Tooltip("舊開發度 Slider / 結算 UI 相容用。NHK 可暫時維持預設值。")]
    public List<int> lewdnessExpTable = new List<int> { 100, 100, 200, 200, 400, 800 };

    [Tooltip("舊好感度 UI 相容用。NHK 可暫時維持預設值。")]
    public List<int> affinityExpTable = new List<int> { 100, 100, 200, 200, 400, 800 };

    [Tooltip("舊興奮度 UI fallback。NHK 可暫時維持預設值。")]
    public int excitementExpPerLevel = 100;

    [Header("角色日程表 (保留多女主角架構用)")]
    [Tooltip("若仍使用日程表系統，可把 HeroineScheduleConfig.asset 拖到這裡。NHK 原型可留空。")]
    public List<HeroineScheduleConfig> AllSchedules;

    public HeroineScheduleConfig GetSchedule(string heroineID)
    {
        if (AllSchedules == null || string.IsNullOrEmpty(heroineID)) return null;
        return AllSchedules.Find(s => s != null && s.heroineID == heroineID);
    }

    public HeroineStat GetStat(string heroineID)
    {
        if (HeroineStats == null || string.IsNullOrEmpty(heroineID)) return null;
        return HeroineStats.Find(s => s != null && s.ID == heroineID);
    }

    public int GetInitialCardCount(HeroineStat stat, HeroineEmotionCardType type)
    {
        bool custom = stat != null && stat.UseCustomInitialEmotionDeck;
        switch (type)
        {
            case HeroineEmotionCardType.Angry: return custom ? stat.InitialAngryCards : InitialAngryCards;
            case HeroineEmotionCardType.Shy: return custom ? stat.InitialShyCards : InitialShyCards;
            case HeroineEmotionCardType.Worried: return custom ? stat.InitialWorriedCards : InitialWorriedCards;
            case HeroineEmotionCardType.Maternal: return custom ? stat.InitialMaternalCards : InitialMaternalCards;
            case HeroineEmotionCardType.Relaxed: return custom ? stat.InitialRelaxedCards : InitialRelaxedCards;
            case HeroineEmotionCardType.Disappointed: return custom ? stat.InitialDisappointedCards : InitialDisappointedCards;
            default: return 0;
        }
    }
}
