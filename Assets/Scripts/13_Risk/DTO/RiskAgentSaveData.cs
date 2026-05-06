using System;

/// <summary>
/// 職責：僅用於存檔和讀檔的「風險代理人」(家人) 數據容器。
/// 必須標記 [Serializable] 才能被 C# 序列化/反序列化。
/// </summary>
[Serializable]
public class RiskAgentSaveData
{
    // 儲存動態數值
    public int PersonalSuspicion;

    // 儲存狀態旗標
    public bool IsGoneForever;
    public int AbsentUntilDay;
    public int AbsentUntilPhase;
}