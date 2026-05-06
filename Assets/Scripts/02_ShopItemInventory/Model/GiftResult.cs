/// <summary>
/// 送禮操作的結果。
/// 包含是否成功、以及失敗時的 Dialogue System 對話 Title。
/// </summary>
public class GiftResult
{
    public bool IsSuccess { get; }

    /// <summary>
    /// 拒收時要播放的 Dialogue System 對話 Title。
    /// 成功時為 null。
    /// </summary>
    public string RejectConversationTitle { get; }

    public GiftResult(bool isSuccess, string rejectConversationTitle = null)
    {
        IsSuccess = isSuccess;
        RejectConversationTitle = rejectConversationTitle;
    }

    // ── 方便用的靜態工廠 ──

    public static readonly GiftResult Success = new GiftResult(true);

    public static readonly GiftResult InvalidTarget =
        new GiftResult(false, "Chat/System/Gift_InvalidTarget");

    public static GiftResult Rejected(string conversationTitle) =>
        new GiftResult(false, conversationTitle);
}