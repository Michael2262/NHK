using System;
using System.Collections.Generic;

/// <summary>
/// 所有小遊戲都必須實作的介面 (Interface)。
/// MinigameManager 將通過這個介面與小遊戲溝通。
/// </summary>
public interface IMinigameController
{
    /// <summary>
    /// 事件：當小遊戲結束時，必須觸發此事件將結果回報給 Manager。
    /// </summary>
    event Action OnGameFinished;

    // 新增屬性：定義此小遊戲結束後是否消耗時間時段
    bool AdvanceTimeOnFinish { get; }

    /// <summary>
    /// 接收一個 Context (背包)，裡面有所有你需要的資料
    /// </summary>
    void Initialize(MinigameContext context);

    /// <summary>
    /// 執行階段：Manager 呼叫此方法，正式開始小遊戲。
    /// </summary>
    void StartGame();
}