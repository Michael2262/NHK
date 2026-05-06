/// <summary>
/// 自用道具操作的結果。
/// 包含是否成功、以及失敗時的提示訊息 Key（對應 Text Table）。
/// </summary>
public class UseItemResult
{
    public bool IsSuccess { get; }

    /// <summary>
    /// 失敗時的提示訊息 Key（對應 Text Table 的 Field Name，用於多語系查表）。
    /// 成功時為 null。
    /// </summary>
    public string FailMessageKey { get; }

    public UseItemResult(bool isSuccess, string failMessageKey = null)
    {
        IsSuccess = isSuccess;
        FailMessageKey = failMessageKey;
    }

    // ── 方便用的靜態工廠 ──

    public static readonly UseItemResult Succeed = new UseItemResult(true);

    public static UseItemResult Failed(string messageKey) =>
        new UseItemResult(false, messageKey);
}