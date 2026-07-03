using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 職責：提供給 Unity Events (如按鈕、動畫事件) 呼叫的女主角狀態除錯接口，
/// 並將請求安全地轉發給 HeroineStatusModel 執行。
/// 女主角 ID 直接在 Inspector 中指定。
///
/// 已對齊 NHK 版 HeroineStatusModel：操作情緒卡池、主導情緒（CurrentEmotion）、
/// 性慾（Libido）、信賴（Trust）、H 次數（HCount）。
/// 不再使用舊專案遺留的 Lewdness / Affinity stub。
///
/// 為了讓 Button.onClick / UnityEvent 能無參數呼叫，多數操作同時提供：
/// - 帶 int 參數的版本（int 為 HeroineEmotionCardType 列舉索引）
/// - 無參數的 "Default" 版本（使用 Inspector 上的 defaultEmotion 欄位）
/// </summary>
public class HeroineStatusDebugBridge : MonoBehaviour
{
    [Header("目標女主角設定")]
    [Tooltip("要操作的女主角唯一 ID (例如 \"Sayo\"、\"Mika\")")]
    [SerializeField] private string heroineID = "";

    [Header("情緒卡預設值")]
    [Tooltip("無參數的 Default 版方法（AddEmotionCardDefault 等）會使用此情緒。")]
    [SerializeField] private HeroineEmotionCardType defaultEmotion = HeroineEmotionCardType.Angry;

    // 輔助屬性：安全地獲取目標女主角的 Model 實例
    private HeroineStatusModel Model
    {
        get
        {
            if (string.IsNullOrEmpty(heroineID))
            {
                Debug.LogError("[HeroineStatusDebugBridge] heroineID 尚未設定！請在 Inspector 中填入女主角 ID。");
                return null;
            }

            if (GameStatusService.Instance == null)
            {
                Debug.LogError("[HeroineStatusDebugBridge] GameStatusService 尚未初始化或不存在！");
                return null;
            }

            if (GameStatusService.Instance.Heroines.TryGetValue(heroineID, out HeroineStatusModel heroine))
            {
                return heroine;
            }

            Debug.LogError($"[HeroineStatusDebugBridge] 找不到 ID 為 '{heroineID}' 的女主角！");
            return null;
        }
    }

    /// <summary>把 int 轉成合法的 HeroineEmotionCardType；非法值回傳 defaultEmotion。</summary>
    private HeroineEmotionCardType ToEmotion(int emotionIndex)
    {
        if (Enum.IsDefined(typeof(HeroineEmotionCardType), emotionIndex))
            return (HeroineEmotionCardType)emotionIndex;

        Debug.LogWarning($"[HeroineStatusDebugBridge] 無效的情緒索引 {emotionIndex}，改用 {defaultEmotion}。");
        return defaultEmotion;
    }

    // ==========================================================
    // 情緒卡池 (Emotion Deck)
    // ==========================================================

    /// <summary>新增一張指定情緒卡（依規則自動替換最舊的對立卡）。int = 列舉索引。</summary>
    public void AddEmotionCard(int emotionIndex)
    {
        var m = Model;
        if (m == null) return;
        var type = ToEmotion(emotionIndex);
        m.ReplaceEmotionCard(type);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 新增情緒卡 {type} → 目前 {type} x{m.GetCardCount(type)}");
    }

    /// <summary>新增一張 defaultEmotion 情緒卡（無參數，供按鈕呼叫）。</summary>
    public void AddEmotionCardDefault() => AddEmotionCard((int)defaultEmotion);

    /// <summary>移除一張指定情緒卡。卡池沒有該情緒則不變。int = 列舉索引。</summary>
    public void RemoveEmotionCard(int emotionIndex)
    {
        var m = Model;
        if (m == null) return;
        var type = ToEmotion(emotionIndex);
        bool ok = m.RemoveOneCardOfType(type);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 移除情緒卡 {type} {(ok ? "成功" : "失敗（卡池無此卡）")} → 目前 {type} x{m.GetCardCount(type)}");
    }

    /// <summary>移除一張 defaultEmotion 情緒卡（無參數，供按鈕呼叫）。</summary>
    public void RemoveEmotionCardDefault() => RemoveEmotionCard((int)defaultEmotion);

    /// <summary>移除卡池中最舊的一張卡（不指定情緒）。</summary>
    public void RemoveOldestCard()
    {
        var m = Model;
        if (m == null) return;
        m.RemoveOldestCard();
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 移除最舊情緒卡 → DominantEmotion = {m.DominantEmotion}");
    }

    /// <summary>把情緒卡池重置回預設初始牌組。</summary>
    public void ResetEmotionDeck()
    {
        var m = Model;
        if (m == null) return;
        m.InitializeDefaultEmotionDeck();
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 情緒卡池已重置為預設牌組。");
    }

    /// <summary>輸出情緒卡池內容（各情緒數量 + 大宗情緒）到 Console。</summary>
    public void LogEmotionDeck()
    {
        var m = Model;
        if (m == null) return;

        var lines = Enum.GetValues(typeof(HeroineEmotionCardType))
            .Cast<HeroineEmotionCardType>()
            .Select(t => $"  {t,-13}: {m.GetCardCount(t)}");

        Debug.Log(
            $"[HeroineStatusDebugBridge] ({heroineID}) 情緒卡池（{m.GetEmotionDeckCount()}/{m.EmotionDeckMaxCount}）\n" +
            string.Join("\n", lines) + "\n" +
            $"  DominantEmotion : {m.DominantEmotion}\n" +
            $"  CurrentEmotion  : {m.CurrentEmotion}"
        );
    }

    // ==========================================================
    // 主導情緒 (CurrentEmotion)
    // ==========================================================

    /// <summary>設定主導情緒（CurrentEmotion）。int = 列舉索引。</summary>
    public void SetCurrentEmotion(int emotionIndex)
    {
        var m = Model;
        if (m == null) return;
        var type = ToEmotion(emotionIndex);
        m.SetCurrentEmotion(type);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 CurrentEmotion = {type}");
    }

    /// <summary>把主導情緒設為 defaultEmotion（無參數，供按鈕呼叫）。</summary>
    public void SetCurrentEmotionDefault() => SetCurrentEmotion((int)defaultEmotion);

    // ==========================================================
    // 性慾 (Libido)
    // ==========================================================

    /// <summary>增減性慾值（正增負減，自動 Clamp 0~150）。</summary>
    public void AddLibido(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddLibido(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Libido {(amount >= 0 ? "+" : "")}{amount} → {m.Libido}");
    }

    /// <summary>直接設定性慾值（自動 Clamp 0~150）。</summary>
    public void SetLibido(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetLibido(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 Libido = {m.Libido}");
    }

    /// <summary>套用一次每日性慾衰減。</summary>
    public void ApplyLibidoDecay()
    {
        var m = Model;
        if (m == null) return;
        m.ApplyLibidoDecay();
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 套用每日衰減 → Libido = {m.Libido}");
    }

    /// <summary>輸出目前性慾值到 Console。</summary>
    public void LogLibido()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Libido = {m.Libido} / {HeroineStatusModel.LibidoMax}");
    }

    // ==========================================================
    // 信賴 (Trust)
    // ==========================================================

    /// <summary>增減信賴值（正增負減，自動 Clamp 0~150）。</summary>
    public void AddTrust(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddTrust(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Trust {(amount >= 0 ? "+" : "")}{amount} → {m.Trust}");
    }

    /// <summary>直接設定信賴值（自動 Clamp 0~150）。</summary>
    public void SetTrust(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetTrust(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 Trust = {m.Trust}");
    }

    /// <summary>輸出目前信賴值到 Console。</summary>
    public void LogTrust()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Trust = {m.Trust} / {HeroineStatusModel.TrustMax}");
    }

    // ==========================================================
    // H 次數 (HCount)
    // ==========================================================

    /// <summary>增加 H 次數（預設 +1，可傳負值）。</summary>
    public void AddHCount(int amount = 1)
    {
        var m = Model;
        if (m == null) return;
        m.AddHCount(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) HCount {(amount >= 0 ? "+" : "")}{amount} → {m.HCount}");
    }

    /// <summary>直接設定 H 次數。</summary>
    public void SetHCount(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetHCount(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 HCount = {m.HCount}");
    }

    /// <summary>輸出目前 H 次數到 Console。</summary>
    public void LogHCount()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) HCount = {m.HCount}");
    }

    // ==========================================================
    // 一鍵輸出全部狀態 (Debug Summary)
    // ==========================================================

    /// <summary>輸出目前所有現役數值的完整摘要到 Console。</summary>
    public void LogAllStatus()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log(
            $"[HeroineStatusDebugBridge] ===== {heroineID} 狀態摘要 =====\n" +
            $"  DominantEmotion : {m.DominantEmotion}\n" +
            $"  CurrentEmotion  : {m.CurrentEmotion}\n" +
            $"  EmotionDeck     : {m.GetEmotionDeckCount()} / {m.EmotionDeckMaxCount}\n" +
            $"  Libido          : {m.Libido} / {HeroineStatusModel.LibidoMax}\n" +
            $"  Trust           : {m.Trust} / {HeroineStatusModel.TrustMax}\n" +
            $"  HCount          : {m.HCount}"
        );
    }
}
