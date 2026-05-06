using System;
using System.Runtime.Serialization;

// 職責：僅用於存檔和讀檔的時間系統數據容器。
[Serializable]
public class TimeSaveData
{
    public int DayIndex;
    public string GameDate;
    public int CurrentPhaseIndex;
    public int CurrentSlotInPhase;
    // 週末狀態 (IsWeekend) 不需要存檔，因為它可以透過 DayIndex 計算得出。
}