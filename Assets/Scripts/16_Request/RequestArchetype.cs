using UnityEngine;

/// <summary>
/// 請求原型：一種「請求」的機率設定。
///
/// 成功率採「兩點分段線性」映射（門檻式）：
///   低於低標 → 保底率；高於穩過線 → 穩過率；中間線性內插。
///   成功率 = clamp( FloorRate + (CeilRate−FloorRate) × (V−TLow)/(THigh−TLow), FloorRate, CeilRate )
///   其中 V = 依 Driver 從 GameStatusService 取的「絕對值」（不正規化）。
///
/// 用法：建成 .asset 放進 Resources/RequestRoll/，檔名即對話呼叫用的 id，
///       例如 Resources/RequestRoll/邀約.asset ←→ RequestRoll(sister, 邀約)。
/// </summary>
[CreateAssetMenu(menuName = "NHK/Request Archetype", fileName = "RequestArchetype")]
public class RequestArchetype : ScriptableObject
{
    [Header("主驅動數值")]
    [Tooltip("成功率由哪個數值牽動（單一）。")]
    public DriverStat Driver = DriverStat.Heroine_Trust;

    [Header("門檻（絕對值）")]
    [Tooltip("低標：驅動值低於此 → 只剩保底率。")]
    public int TLow = 20;

    [Tooltip("穩過線：驅動值高於此 → 幾乎穩過（= 穩過率）。")]
    public int THigh = 40;

    [Header("上下限（%）")]
    [Tooltip("保底率：曲線下限。")]
    [Range(0, 100)] public int FloorRate = 15;

    [Tooltip("穩過率：曲線上限。")]
    [Range(0, 100)] public int CeilRate = 95;

    [Header("選項")]
    [Tooltip("驅動值 ≥ 穩過線時直接必過（忽略保底率與擲骰）。")]
    public bool GuaranteedAbove = false;

    /// <summary>
    /// 依驅動值算成功率（0~100）。兩點分段線性，夾在 [FloorRate, CeilRate]。
    /// </summary>
    public float ComputeSuccessRate(int driverValue)
    {
        if (THigh <= TLow)
            return driverValue >= THigh ? CeilRate : FloorRate;

        float t = (float)(driverValue - TLow) / (THigh - TLow);
        float rate = FloorRate + (CeilRate - FloorRate) * t;
        return Mathf.Clamp(rate, FloorRate, CeilRate);
    }

    private void OnValidate()
    {
        if (THigh < TLow) THigh = TLow;
        FloorRate = Mathf.Clamp(FloorRate, 0, 100);
        CeilRate = Mathf.Clamp(CeilRate, 0, 100);
        if (CeilRate < FloorRate) CeilRate = FloorRate;
    }
}
