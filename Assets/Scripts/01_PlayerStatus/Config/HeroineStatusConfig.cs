using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 單一女主角的基礎設定。
/// 保留多女主角身分設定；可調數值統一位於 HeroineStatusModel 頂部。
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

}

[CreateAssetMenu(menuName = "Game/Config/HeroineStatus")]
public class HeroineStatusConfig : ScriptableObject
{
    [Header("角色列表 (Roster)")]
    public List<HeroineStat> HeroineStats = new List<HeroineStat>();

    [Header("抽選表演設定")]
    [Min(0f)] public float SmallDrawDuration = 0.75f;
    [Min(0f)] public float BigDrawDuration = 2.0f;

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

}
