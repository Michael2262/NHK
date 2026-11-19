using System;

/// <summary>
/// 單一數值面板的暫時顯示狀態，不保存遊戲資料，也不直接操作 Unity 元件。
/// 顯示總開關優先；變動模式只在實際開始演出後顯示，等自己的演出結束才倒數。
/// </summary>
public sealed class StatusPanelVisibilityState
{
    private enum Phase { Idle, Playing, Finished }
    private Phase _phase;
    private float _finishedAt;

    public bool AllowsDisplay { get; private set; }
    public bool ShowsOnChange { get; private set; }
    public bool IsPresenting => AllowsDisplay && ShowsOnChange && _phase != Phase.Idle;

    /// <summary>只有旗標的有效狀態改變才重設；重複通知不打斷演出。</summary>
    public bool Configure(bool allowsDisplay, bool showsOnChange)
    {
        if (AllowsDisplay == allowsDisplay && ShowsOnChange == showsOnChange) return false;
        AllowsDisplay = allowsDisplay;
        ShowsOnChange = showsOnChange;
        ResetPresentation();
        return true;
    }

    public void ResetPresentation()
    {
        _phase = Phase.Idle;
    }

    /// <summary>由佇列實際播放該筆數值時呼叫；入列、讀檔及 hover 不觸發。</summary>
    public void NotifyChangeStarted()
    {
        if (AllowsDisplay && ShowsOnChange) _phase = Phase.Playing;
    }

    public float GetAlpha(float now, bool hasOwnWork, float holdDuration, float fadeDuration)
    {
        if (!AllowsDisplay) return 0f;
        if (!ShowsOnChange) return 1f;
        if (_phase == Phase.Idle) return 0f;

        // 尚有自己待播的數值或飄字時保持顯示，連續變動也會重新延長。
        if (hasOwnWork)
        {
            _phase = Phase.Playing;
            return 1f;
        }
        if (_phase == Phase.Playing)
        {
            _phase = Phase.Finished;
            _finishedAt = now;
        }

        float fadeElapsed = now - _finishedAt - Math.Max(0f, holdDuration);
        if (fadeElapsed < 0f) return 1f;
        if (fadeDuration > 0f && fadeElapsed < fadeDuration)
            return 1f - fadeElapsed / fadeDuration;

        _phase = Phase.Idle;
        return 0f;
    }
}
