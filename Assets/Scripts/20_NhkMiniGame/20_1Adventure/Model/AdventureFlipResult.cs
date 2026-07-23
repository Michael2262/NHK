using System.Collections.Generic;

/// <summary>
/// 一次翻牌的結果，回傳給 UI / FSM 用。
/// </summary>
public class AdventureFlipResult
{
    /// <summary>這次翻的牌</summary>
    public AdventureCardData Card;

    /// <summary>成功 / 失敗（OutcomeResolved 為 false 時無意義）</summary>
    public bool Success;

    /// <summary>當下計算出的成功率(%)。Always100Success 的牌為 100</summary>
    public float SuccessRate;

    /// <summary>
    /// 是否真的有跑成功/失敗判定。
    /// 若「必有效果」直接結束了大冒險，就不會再判定，此值為 false。
    /// </summary>
    public bool OutcomeResolved = true;

    /// <summary>「必有效果」造成的數值變動</summary>
    public List<AdventureChangeRecord> AlwaysChanges = new List<AdventureChangeRecord>();

    /// <summary>「成功/失敗效果」造成的數值變動</summary>
    public List<AdventureChangeRecord> Changes = new List<AdventureChangeRecord>();

    /// <summary>這次翻牌（含必有效果）造成的里程淨變化</summary>
    public int MileageDelta;

    /// <summary>套用完所有效果後的里程</summary>
    public int NewMileage;

    /// <summary>這次翻牌是否讓大冒險結束（牌上有結束效果）</summary>
    public bool Ended;

    /// <summary>這次要顯示的結果插圖（依成功/失敗自動取，沒填則為預設插圖）</summary>
    public UnityEngine.Sprite ResultIllustration
        => Card != null ? Card.GetResultIllustration(Success) : null;
}
