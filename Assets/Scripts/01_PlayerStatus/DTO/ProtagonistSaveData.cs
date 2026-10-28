using System;

/// <summary>主角存檔。欄位初始值供舊 JSON 缺欄位時使用；退役欄位由 Newtonsoft 忽略。</summary>
[Serializable]
public class ProtagonistSaveData
{
    public int StaminaMax = ProtagonistStatusModel.InitialStaminaMax;
    public int Stamina = ProtagonistStatusModel.InitialStaminaMax;
    public int ShootTimesMax = ProtagonistStatusModel.InitialShootTimesMax;
    public int ShootTimes = ProtagonistStatusModel.InitialShootTimesMax;
    public int Lust = ProtagonistStatusModel.InitialLust;
    public int StickerCount = ProtagonistStatusModel.InitialStickerCount;
    public int Money = ProtagonistStatusModel.InitialMoney;
    public int SkillPoints = ProtagonistStatusModel.InitialSkillPoints;
    public int Semen = ProtagonistStatusModel.InitialSemen;
    public int RoomMessLevel = ProtagonistStatusModel.InitialRoomMessLevel;
}
