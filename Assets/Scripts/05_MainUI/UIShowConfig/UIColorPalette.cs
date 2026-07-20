using UnityEngine;

/// <summary>
/// 全域 UI 色票。靜態色票表，供各 UI 直接抓取使用（例如 UIColorPalette.Red）。
/// 色票以 hex 寫死於此，無需在 Inspector 拖入、也無遊戲過程中動態改色的需求。
/// 未來要新增 / 調整色票，直接改這裡的 hex 字串即可。
/// </summary>
public static class UIColorPalette
{
    // ───── 色票（HEX，不含 #） ─────
    public const string HEX_WHITE = "FFFFFF";
    public const string HEX_RED   = "FF1C1C";
    public const string HEX_GREEN = "12B980";
    public const string HEX_GRAY  = "434343";
    public const string HEX_PINK  = "FF6666";
    public const string HEX_BLUE  = "2832FF";
    public const string HEX_DARK_RED = "8B0000";

    // ───── 對外色票（Color，已快取） ─────
    public static readonly Color White = FromHex(HEX_WHITE);
    public static readonly Color Red   = FromHex(HEX_RED);
    public static readonly Color Green = FromHex(HEX_GREEN);
    public static readonly Color Gray  = FromHex(HEX_GRAY);
    public static readonly Color Pink  = FromHex(HEX_PINK);
    public static readonly Color Blue  = FromHex(HEX_BLUE);
    public static readonly Color DarkRed = FromHex(HEX_DARK_RED);

    /// <summary>
    /// 把 6 碼（RGB）或 8 碼（RGBA）的 hex 字串轉成 Color，可含或不含開頭的 '#'。
    /// 解析失敗時回傳洋紅色（Color.magenta）以利在畫面上一眼看出設定錯誤。
    /// </summary>
    public static Color FromHex(string hex)
    {
        if (!string.IsNullOrEmpty(hex) && hex[0] == '#')
            hex = hex.Substring(1);

        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            return color;

        Debug.LogWarning($"[UIColorPalette] 無法解析色票 hex：\"{hex}\"，已回傳 magenta。");
        return Color.magenta;
    }
}
