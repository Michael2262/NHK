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
    public int Sociality;    // 社會性：0～100，越高越能面對外界
    public int Dependency;   // 依賴度：0～100，主角對妹妹的依賴

    // ───── 保留資源 ─────
    public int Money;
    public int SkillPoints;
}