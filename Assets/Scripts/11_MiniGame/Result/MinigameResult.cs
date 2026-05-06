using System;

/// <summary>
/// 用於從小遊戲傳遞結果給 MinigameManager 的資料結構。
/// </summary>
public class MinigameResult
{
    public bool WasSuccessful { get; private set; }
    public int Score { get; private set; }
    public string ResultType { get; private set; } // e.g., "Perfect", "Good", "Fail"

    // 構造函數，方便小遊戲打包結果
    public MinigameResult(bool success, int score, string resultType = "")
    {
        WasSuccessful = success;
        Score = score;
        ResultType = resultType;
    }
}