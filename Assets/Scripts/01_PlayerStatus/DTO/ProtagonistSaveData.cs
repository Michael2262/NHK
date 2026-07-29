using System;

/// <summary>
/// 職責：ProtagonistStatusModel 的存檔數據容器。
/// Day 由 TimeSaveData 統一管理，不再重複存放。
/// </summary>
[Serializable]
public class ProtagonistSaveData
{
    // ───── NHK 主角核心數值 ─────
    public int Stress;       // 壓力：0～100，越高越危險
    public int LifePower;    // 生活力：0～100，越高越能維持正常生活
    public int Sociality;    // 社會性：0～100，越高越能面對外界
    public int Dependency;   // 依賴度：0～100，主角對妹妹的依賴

    // ───── 保留資源 ─────
    public int Money;
    public int SkillPoints;

    // ───── 射精次數 ─────
    public int ShootTimes;

    // ───── 房間髒亂度 ─────
    public int RoomMessLevel;

    // ───── 身體髒污度（0～25；舊存檔缺欄位時反序列化為 0） ─────
    public int BodyDirtyLevel;

    // ───── 狀態不好（換日自動解除；舊存檔缺欄位時反序列化預設 false） ─────
    public bool BadHealthy;
}