/// <summary>
/// 開發度結算的完整分項數據。
/// 由 MinigameResultHandler.BuildBreakdown() 建立，
/// 傳給 LewdnessSliderPerformance 做動畫演出。
/// </summary>
public class LewdnessBreakdown
{
    // 基本資訊
    public int SlotIndex;
    public string HeroineName;

    // 起始狀態
    public int StartLevel;
    public int StartExp;
    public int ExpThreshold;

    // 遊戲得分（新增）
    public int GameScore;            // FSM 回報的原始遊戲得分
    public int GameScoreConverted;   // 經過 gameScoreToExpRatio 轉換後的值

    // 結束方式
    public MinigameEndReason Reason;
    public int BaseScore;            // 結束方式對應的得分（可為負數）
    public string ReasonDisplayName; // 結束方式的多語系顯示名稱

    // 興奮度
    public int LocalExcitedLv;
    public int ExcitedLvBonus;

    // 高潮次數
    public int OrgasmTimes;
    public int OrgasmTimesBonus;

    // 射精次數
    public int ShootTimes;
    public int ShootTimesBonus;

    // 超越極限射精
    public int OverShootTimes;
    public int OverShootTimesBonus;

    // 乘倍
    public bool DangerScene;
    public int DangerSceneMultiplier;
    public bool ChallengeAccepted;
    public int ChallengeAcceptedMultiplier;

    // 最終結果
    public int TotalAddedExp;
}
