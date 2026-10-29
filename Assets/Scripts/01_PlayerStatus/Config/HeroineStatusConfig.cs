using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 單一女主角的基礎設定。
/// 保留多女主角身分與初始週期設定；共用數值位於 HeroineStatusModel 頂部。
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

    [Header("生理週期")]
    [Min(1)]
    [Tooltip("新遊戲的初始週期日，月經期第 1 天為起點，預設第 7 天。超過本輪總天數時循環折返。")]
    public int InitialCycleDay = HeroineStatusModel.DefaultInitialCycleDay;

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
    [Tooltip("僅供開發設定；遊戲初始化後固定，存檔不會切換此模式。")]
    public HeroineScheduleMode ScheduleMode = HeroineScheduleMode.Legacy;

    [Tooltip("Flag 模式使用的獨立規則表；不需要提供任何傳統時程表。")]
    public List<HeroineFlagScheduleConfig> FlagSchedules = new List<HeroineFlagScheduleConfig>();

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

public enum HeroineScheduleMode
{
    [InspectorName("傳統時程表")] Legacy = 0,
    [InspectorName("Flag 規則")] FlagRules = 1
}
