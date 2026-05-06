using System;

/// <summary>
/// 職責：ProtagonistStatusModel 的存檔數據容器。
/// NHK 版：保留類名，但移除上一款的體力、精神、行動點、不審度、射擊次數等系統。
/// </summary>
[Serializable]
public class ProtagonistSaveData
{
    // ───── 遊戲日程 ─────
    public int Day;

    // ───── NHK 主角核心數值 ─────
    public int Stress;       // 壓力：0～100，越高越危險
    public int LifePower;    // 生活力：0～100，越高越能維持正常生活
    public int SocialFear;   // 社會恐懼：0～100，越高越害怕外界
    public int Dependency;   // 依賴度：0～100，主角對妹妹的依賴

    // ───── 保留資源 ─────
    public int Money;
    public int SkillPoints;

    // ───── 每日狀態：若允許日中存檔，需保存 ─────
    public bool HasBathedToday;
    public bool HasCleanedRoomToday;
    public bool HasHadMealToday;
    public bool HasCheckedMailToday;
    public bool HasRepliedFamilyToday;
    public bool HasIgnoredPhoneToday;
    public bool HasGoneOutsideToday;
    public bool SucceededGoingOutsideToday;
    public bool FailedGoingOutsideToday;
    public bool HasEscapedToday;
    public bool StressCollapsedToday;

    // ───── 長期統計 ─────
    public int CollapseCount;
    public int OutsideSuccessCount;
    public int OutsideFailCount;
    public int ConsecutiveNoBathDays;
    public int ConsecutiveEscapeDays;
    public int DaysImprovedLife;
    public int DaysIgnoredReality;
    public int NightExtend1SuccessCount;
    public int NightExtend2SuccessCount;
    public int StayOverCount;
}
