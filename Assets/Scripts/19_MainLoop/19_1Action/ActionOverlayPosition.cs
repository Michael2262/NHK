/// <summary>
/// 行動遮罩的出現位置。
/// 新增位置時：在此加一個值，並在 ActionOverlayManager 的 positionMarkers 清單補上對應 marker。
/// </summary>
public enum ActionOverlayPosition
{
    /// <summary>偏右（預設）。</summary>
    Right = 0,

    /// <summary>正中央。</summary>
    Center = 1,
}
