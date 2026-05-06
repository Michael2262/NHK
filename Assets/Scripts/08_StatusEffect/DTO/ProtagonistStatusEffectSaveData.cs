using System;
using System.Collections.Generic;

// 存檔數據容器的整體結構，這個類別只負責儲存數據，不包含任何邏輯。我們需要記錄下每個生效中效果的 ID 和剩餘天數。
[Serializable]
public class ProtagonistStatusEffectSaveData
{
    public List<ActiveStatusEffectSaveData> ActiveEffects = new List<ActiveStatusEffectSaveData>();
}

// 用於儲存單一狀態效果的數據
[Serializable]
public class ActiveStatusEffectSaveData
{
    public string EffectID;
    public int RemainingDays;
}