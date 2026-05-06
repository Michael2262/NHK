/// <summary>
/// 全域進度的所有 Flag Key 集中管理。
/// 使用方式：GameStatusService.Instance.UnlockGlobalFlag(GlobalFlagKeys.CLEAR_ONCE);
/// </summary>
public static class GlobalFlagKeys
{
    // ── 周目相關 ──
    public const string CLEAR_ONCE = "CLEAR_ONCE";           // 是否曾通關（開啟二周目）
    public const string CAN_SKIP_INTRO = "CAN_SKIP_INTRO";   // 是否可跳過開場

    // ── 畫廊 CG ──
    public const string GALLERY_CG_ENDING_A = "GALLERY_CG_ENDING_A";
    public const string GALLERY_CG_ENDING_B = "GALLERY_CG_ENDING_B";
    // 需要新的 CG 時在這裡繼續加...

    // ── 場景造訪紀錄 ──
    public const string SCENE_BEACH_VISITED = "SCENE_BEACH_VISITED";
    // 需要新的場景紀錄時在這裡繼續加...
}

/// <summary>
/// 全域進度的所有 Value Key 集中管理。
/// 使用方式：GameStatusService.Instance.AddGlobalValue(GlobalValueKeys.CLEAR_COUNT, 1);
/// </summary>
public static class GlobalValueKeys
{
    public const string CLEAR_COUNT = "ClearCount";       // 周回次數
    public const string INHERIT_GOLD = "InheritGold";     // 二周目繼承金幣
    // 需要新的數值時在這裡繼續加...
}
