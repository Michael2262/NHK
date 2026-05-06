/// <summary>
/// 排卵周期日類型。每一日歸類為危險日、通常日或安全日，決定當日懷孕機率。
/// </summary>
public enum OvulationDayType
{
    /// <summary>安全日 (懷孕機率 0%)</summary>
    Safe,
    /// <summary>通常日 (懷孕機率 20%)</summary>
    Normal,
    /// <summary>危險日 (懷孕機率 40%)</summary>
    Danger
}

/// <summary>
/// 懷孕狀態。
/// NotPregnant -> (TryConceive 成功) -> EarlyPregnancy -> (不穩定期過後) -> Pregnant
/// EarlyPregnancy 期間可被事後避孕藥取消回 NotPregnant。
/// </summary>
public enum PregnancyState
{
    /// <summary>未懷孕</summary>
    NotPregnant,
    /// <summary>早期懷孕（不穩定期，可被事後避孕藥取消）</summary>
    EarlyPregnancy,
    /// <summary>確定懷孕（事後避孕藥無效）</summary>
    Pregnant
}

/// <summary>
/// 懷孕判定的情境修飾。透過此 struct 傳遞各種影響懷孕機率的條件。
/// 之後要擴充新修飾（體質、體位、道具等）時，只要加欄位，不必改 method signature。
/// </summary>
public struct ConceiveContext
{
    /// <summary>機率倍率。排卵藥會設為 >1f，一般為 1f。</summary>
    public float ProbabilityMultiplier;

    /// <summary>true = 機率強制歸零（保險套）。優先級高於 ForceConceive。</summary>
    public bool ForceZero;

    /// <summary>true = 強制懷孕（劇情事件預留）。</summary>
    public bool ForceConceive;

    /// <summary>預設情境：機率倍率 1f，無強制。</summary>
    public static ConceiveContext Default =>
        new ConceiveContext { ProbabilityMultiplier = 1f };

    /// <summary>保險套情境：機率歸零。</summary>
    public static ConceiveContext Condom =>
        new ConceiveContext { ForceZero = true };

    /// <summary>排卵藥情境：指定機率倍率。</summary>
    public static ConceiveContext Fertility(float multiplier) =>
        new ConceiveContext { ProbabilityMultiplier = multiplier };

    /// <summary>劇情強制懷孕情境。</summary>
    public static ConceiveContext Forced =>
        new ConceiveContext { ForceConceive = true };
}
