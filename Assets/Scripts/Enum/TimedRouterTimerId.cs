/// <summary>
/// 計時器名。同名互斥，不同名互不影響。
/// </summary>

public enum TimerId
{
    Main = 0,
    Emo = 1
}

/// <summary>
/// 計時分流器共用的計時種類。同一種類共用倒數，不同種類互不影響。
/// 數值供 PlayMaker 序列化使用；新增項目時請保留既有數值。
/// </summary>

public enum TimedRouterTimerId
{
    GrabHand = 0,
    GrabFoot = 1
}


