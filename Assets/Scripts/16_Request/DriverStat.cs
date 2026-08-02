/// <summary>
/// RequestRoll 的主驅動數值來源（單一選擇，決定成功率由誰牽動）。
/// 五個選項橫跨女主角與主角兩個 Model；實際取值在 RequestRoller.ResolveDriverValue。
/// 新增選項時，記得同步在 RequestRoller 補上對應的取值分支。
/// </summary>
public enum DriverStat
{
    /// <summary>女主角信賴值（HeroineStatusModel.Trust，0~TrustMax）。</summary>
    Heroine_Trust,

    /// <summary>女主角性慾值（HeroineStatusModel.Libido，0~LibidoMax）。</summary>
    Heroine_Libido,

    /// <summary>主角生活力（ProtagonistStatusModel.LifePower，0~100）。</summary>
    Protagonist_LifePower,

    /// <summary>主角社會性（ProtagonistStatusModel.Sociality，0~100）。</summary>
    Protagonist_Sociality,

    /// <summary>主角依賴度（ProtagonistStatusModel.Dependency，0~150）。</summary>
    Protagonist_Dependency
}
