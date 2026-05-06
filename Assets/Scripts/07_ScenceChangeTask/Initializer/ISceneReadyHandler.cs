using System.Collections;

/// <summary>
/// 定義了一個「場景準備員」的合約。
/// 任何實作此介面的物件，都必須提供一個 OnSceneReady 的方法，
/// 讓 SceneController 可以在場景載入後、帷幕揭開前呼叫。
/// </summary>
public interface ISceneReadyHandler
{
    /// <summary>
    /// 當新場景載入完成，但在淡入動畫（揭幕）開始前執行。
    /// </summary>
    /// <returns>IEnumerator 以支援協程，允許非同步等待。</returns>
    IEnumerator OnSceneReady();
}