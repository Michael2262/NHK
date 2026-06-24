using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 輕量版行動觸發器。
///
/// 相對於 <see cref="ActionOverlayTrigger"/>（跑條演出 / 成功失敗判定 / UnityEvent / 音效），
/// 本元件只做一件事：以 actionId 持有一個 <see cref="ActionChanceProvider"/>，
/// 讓對話腳本能透過 SequencerCommandProviderTrigger 以 ID 查找它、取得成功率。
///
/// 用法：
/// - 掛在任意物件上，填 actionId，並指定（或在同物件上掛）一個 ActionChanceProvider。
/// - 不做任何演出、不跑條、不發 UnityEvent。
///
/// 註冊方式：沿用 OverlayTrigger 的「以 ID 自我註冊」慣例，但因為不需要演出管理器，
/// 改用本類別自身的 static 註冊表，省去額外的 Manager。actionId 留空者不註冊，
/// 自然就無法被對話以 ID 調用。
/// </summary>
public class ProviderTrigger : MonoBehaviour
{
    [Tooltip("供 Sequencer 以 ID 查找用。留空 = 不註冊 = 無法被對話以 ID 調用。")]
    public string actionId;

    [Tooltip("成功率計算器。留空時 Reset / 查找會自動抓同物件上的 ActionChanceProvider。")]
    public ActionChanceProvider chanceProvider;

    // 以 actionId 為 key 的全域註冊表。ProviderTrigger 不跑演出，因此不需要 ActionOverlayManager，
    // 直接用 static 字典自我註冊即可。
    private static readonly Dictionary<string, ProviderTrigger> Registry = new Dictionary<string, ProviderTrigger>();

    private void Reset()
    {
        // 方便掛在同一物件上時自動抓取。
        chanceProvider = GetComponent<ActionChanceProvider>();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(actionId)) return;

        if (Registry.TryGetValue(actionId, out var existing) && existing != null && existing != this)
        {
            Debug.LogWarning($"ProviderTrigger：actionId「{actionId}」重複註冊，後者會覆蓋前者。請確認場上沒有同 ID 的 ProviderTrigger。", this);
        }

        Registry[actionId] = this;
    }

    private void OnDisable()
    {
        if (string.IsNullOrEmpty(actionId)) return;

        // 只有當登記的仍是自己時才移除，避免把後來覆蓋上去的另一顆誤刪。
        if (Registry.TryGetValue(actionId, out var existing) && existing == this)
        {
            Registry.Remove(actionId);
        }
    }

    /// <summary>
    /// 以 actionId 查找已註冊（且已啟用）的 ProviderTrigger。找不到回傳 null。
    /// </summary>
    public static ProviderTrigger Find(string actionId)
    {
        if (string.IsNullOrEmpty(actionId)) return null;
        Registry.TryGetValue(actionId, out var trigger);
        return trigger;
    }

    /// <summary>
    /// 計算並回傳目前成功率（0~1，已由 Provider 套上下限）。
    /// 找不到 ActionChanceProvider 時回傳 -1（呼叫端據此判定失敗）。
    /// </summary>
    public float CalculateChance()
    {
        var provider = chanceProvider != null ? chanceProvider : GetComponent<ActionChanceProvider>();
        if (provider == null)
        {
            Debug.LogWarning($"ProviderTrigger「{actionId}」找不到 ActionChanceProvider，無法計算成功率。", this);
            return -1f;
        }

        return provider.CalculateSuccessChance();
    }
}
