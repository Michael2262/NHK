using UnityEngine;
using PixelCrushers.DialogueSystem;
using System.Collections.Generic;

public enum DialogueMode { Skip, Interrupt, Queue, Priority }

public struct ConversationRequest
{
    public string title;
    public DialogueMode mode;

    public ConversationRequest(string title, DialogueMode mode)
    {
        this.title = title;
        this.mode = mode;
    }
}

public class StoryManager : MonoBehaviour
{
    private static StoryManager _instance;

    [Header("Alert 設定")]
    [SerializeField] private float alertCharsPerSecond = 20f; // 每秒讀幾個字
    [SerializeField] private float minAlertDuration = 1.5f;   // 公告最少顯示多久
    [Tooltip("顯示公告時一併播放的音效 Key（留空則不播放）")]
    [SerializeField] private string alertSoundKey = "alert_show"; // 顯示公告時的音效

    public static StoryManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<StoryManager>();
            return _instance;
        }
    }

    [SerializeField]
    [Tooltip("全域唯一延遲秒數，對應 {{wait}}（Inspector 值僅作為首次未存檔時的預設）")]
    private float customDelay = 3f;

    // {{wait}} 延遲秒數的設定存檔 Key（與音量等設定一樣走 PlayerPrefs，不進遊戲存檔）
    private const string WAIT_DELAY_KEY = "DialogueWaitDelay";

    private LinkedList<ConversationRequest> _conversationQueue = new LinkedList<ConversationRequest>();

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);

        // 載入上次儲存的延遲秒數（沒存過則用 Inspector 的預設值）
        customDelay = PlayerPrefs.GetFloat(WAIT_DELAY_KEY, customDelay);
        SyncWaitWithCustomDelay();
    }

    private void OnApplicationQuit()
    {
        // 保險：把設定確實寫盤（拖動當下只 SetFloat，不做磁碟 I/O）
        PlayerPrefs.Save();
    }

    // --- 延遲控制 API ---

    /// <summary>目前 {{wait}} 對應的延遲秒數（供 UI 初始化讀取）</summary>
    public float CustomDelay => customDelay;

    /// <summary>customDelay 變動時觸發（參數為新值），供 UI 即時同步</summary>
    public event System.Action<float> OnCustomDelayChanged;

    public void SetCustomDelay(float newValue)
    {
        customDelay = newValue;
        SyncWaitWithCustomDelay();
        PlayerPrefs.SetFloat(WAIT_DELAY_KEY, customDelay); // 持久化（離開遊戲時自動寫盤）
        OnCustomDelayChanged?.Invoke(customDelay);
    }

    private void SyncWaitWithCustomDelay()
    {
        // 強制鎖定 {{wait}} 的行為
        // 用 InvariantCulture，避免在「逗號當小數點」的地區產生 Delay(1,5) 導致解析失敗
        string delayStr = customDelay.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Sequencer.RegisterShortcut("wait", $"Delay({delayStr})");
    }

    // --- 核心播放邏輯 (Skip, Interrupt, Queue, Priority) ---

    public void PlayConversation(string title, DialogueMode mode = DialogueMode.Skip)
    {
        SyncWaitWithCustomDelay();

        Debug.Log($"[StoryManager] 進入 PlayConversation: {title}, 模式: {mode}, 目前對話狀態: {DialogueManager.isConversationActive}");

        if (!DialogueManager.isConversationActive)
        {
            Debug.Log($"[StoryManager] 當前無對話，直接播放: {title}");
            DialogueManager.StartConversation(title);
            return;
        }

        switch (mode)
        {
            case DialogueMode.Queue:
                _conversationQueue.AddLast(new ConversationRequest(title, mode));
                Debug.Log($"[StoryManager] 已將 {title} 加入隊列尾端。目前隊列總數: {_conversationQueue.Count}");
                break;

            case DialogueMode.Priority:
                _conversationQueue.AddFirst(new ConversationRequest(title, mode));
                Debug.Log($"[StoryManager] 已將 {title} 插隊至隊列最前。目前隊列總數: {_conversationQueue.Count}");
                break;

            case DialogueMode.Interrupt:
                Debug.Log($"[StoryManager] 強制中斷並跳轉至: {title}");
                DialogueManager.StopConversation();
                DialogueManager.StartConversation(title);
                break;

            case DialogueMode.Skip:
                Debug.LogWarning($"[StoryManager] 跳過新請求: {title}");
                break;
        }
    }

    // --- 隊列管理 API ---

    /// <summary>
    /// 清除目前所有未播放的隊列對話
    /// </summary>
    public void ClearConversationQueue()
    {
        int count = _conversationQueue.Count;
        _conversationQueue.Clear();
        Debug.Log($"[StoryManager] 已清除隊列，共移除 {count} 筆等待中的對話");
    }

    // ============================================================
    //  Lua 函數註冊 / 反註冊
    // ============================================================
    //
    //  命名規則:
    //    T   = Text (無色)
    //    TR  = Red    [em2] 負面
    //    TE  = Energy [em3] 綠色-體力
    //    TG  = Gray   [em4] 灰色-已閱讀
    //    TP  = Pink   [em5] 粉色-色情
    //
    //  後綴:
    //    (無) = key only
    //    n   = key + double          → {0} 填數值
    //    s   = key + argKey(查表)    → {0} 填翻譯字串
    //    sn  = key + argKey + double → {0} 填翻譯字串, {1} 填數值
    // ============================================================

    private void OnEnable()
    {
        Invoke(nameof(SafeSubscribe), 0.1f);
    }

    private void SafeSubscribe()
    {
        if (DialogueManager.instance != null)
        {
            // 事件訂閱 (先解除再訂閱，防止重複)
            DialogueManager.instance.conversationEnded -= OnConversationEnd;
            DialogueManager.instance.conversationEnded += OnConversationEnd;
            Debug.Log("[StoryManager] 已成功訂閱 DialogueManager 事件");

            RegisterAllLuaFunctions();
        }
        else
        {
            Debug.LogError("[StoryManager] 找不到 DialogueManager，訂閱失敗！");
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationEnded -= OnConversationEnd;
            UnregisterAllLuaFunctions();
        }
    }

    private void RegisterAllLuaFunctions()
    {
        // --- 無色 (T) ---
        Lua.RegisterFunction("T", this, SymbolExtensions.GetMethodInfo(() => T(string.Empty)));
        Lua.RegisterFunction("Tn", this, SymbolExtensions.GetMethodInfo(() => Tn(string.Empty, (double)0)));
        Lua.RegisterFunction("Ts", this, SymbolExtensions.GetMethodInfo(() => Ts(string.Empty, string.Empty)));
        Lua.RegisterFunction("Tsn", this, SymbolExtensions.GetMethodInfo(() => Tsn(string.Empty, string.Empty, (double)0)));

        // --- 紅色 R [em2] 負面 ---
        Lua.RegisterFunction("TR", this, SymbolExtensions.GetMethodInfo(() => TR(string.Empty)));
        Lua.RegisterFunction("TRn", this, SymbolExtensions.GetMethodInfo(() => TRn(string.Empty, (double)0)));
        Lua.RegisterFunction("TRs", this, SymbolExtensions.GetMethodInfo(() => TRs(string.Empty, string.Empty)));
        Lua.RegisterFunction("TRsn", this, SymbolExtensions.GetMethodInfo(() => TRsn(string.Empty, string.Empty, (double)0)));

        // --- 綠色 E [em3] 體力 ---
        Lua.RegisterFunction("TE", this, SymbolExtensions.GetMethodInfo(() => TE(string.Empty)));
        Lua.RegisterFunction("TEn", this, SymbolExtensions.GetMethodInfo(() => TEn(string.Empty, (double)0)));
        Lua.RegisterFunction("TEs", this, SymbolExtensions.GetMethodInfo(() => TEs(string.Empty, string.Empty)));
        Lua.RegisterFunction("TEsn", this, SymbolExtensions.GetMethodInfo(() => TEsn(string.Empty, string.Empty, (double)0)));

        // --- 灰色 G [em4] 已閱讀 ---
        Lua.RegisterFunction("TG", this, SymbolExtensions.GetMethodInfo(() => TG(string.Empty)));
        Lua.RegisterFunction("TGn", this, SymbolExtensions.GetMethodInfo(() => TGn(string.Empty, (double)0)));
        Lua.RegisterFunction("TGs", this, SymbolExtensions.GetMethodInfo(() => TGs(string.Empty, string.Empty)));
        Lua.RegisterFunction("TGsn", this, SymbolExtensions.GetMethodInfo(() => TGsn(string.Empty, string.Empty, (double)0)));

        // --- 粉色 P [em5] 色情 ---
        Lua.RegisterFunction("TP", this, SymbolExtensions.GetMethodInfo(() => TP(string.Empty)));
        Lua.RegisterFunction("TPn", this, SymbolExtensions.GetMethodInfo(() => TPn(string.Empty, (double)0)));
        Lua.RegisterFunction("TPs", this, SymbolExtensions.GetMethodInfo(() => TPs(string.Empty, string.Empty)));
        Lua.RegisterFunction("TPsn", this, SymbolExtensions.GetMethodInfo(() => TPsn(string.Empty, string.Empty, (double)0)));
    }

    private void UnregisterAllLuaFunctions()
    {
        // 無色
        Lua.UnregisterFunction("T");
        Lua.UnregisterFunction("Tn");
        Lua.UnregisterFunction("Ts");
        Lua.UnregisterFunction("Tsn");

        // 紅 R
        Lua.UnregisterFunction("TR");
        Lua.UnregisterFunction("TRn");
        Lua.UnregisterFunction("TRs");
        Lua.UnregisterFunction("TRsn");

        // 綠 E
        Lua.UnregisterFunction("TE");
        Lua.UnregisterFunction("TEn");
        Lua.UnregisterFunction("TEs");
        Lua.UnregisterFunction("TEsn");

        // 灰 G
        Lua.UnregisterFunction("TG");
        Lua.UnregisterFunction("TGn");
        Lua.UnregisterFunction("TGs");
        Lua.UnregisterFunction("TGsn");

        // 粉 P
        Lua.UnregisterFunction("TP");
        Lua.UnregisterFunction("TPn");
        Lua.UnregisterFunction("TPs");
        Lua.UnregisterFunction("TPsn");
    }

    // ============================================================
    //  核心查表 & 格式化 (私有工具方法)
    // ============================================================

    /// <summary>只查表，不做任何 Format</summary>
    private string Localize(string key)
    {
        string text = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[StoryManager] Text Table 找不到 Key: {key}");
            return key;
        }
        return text;
    }

    /// <summary>查表後把 argKey 也查表，填入 {0}</summary>
    private string LocalizeWithString(string key, string argKey)
    {
        string template = Localize(key);
        string arg = DialogueManager.GetLocalizedText(argKey);
        if (string.IsNullOrEmpty(arg)) arg = argKey;
        return string.Format(template, arg);
    }

    /// <summary>查表後填入 {0}=數值</summary>
    private string LocalizeWithNumber(string key, double value)
    {
        return string.Format(Localize(key), value);
    }

    /// <summary>查表後填入 {0}=翻譯字串, {1}=數值</summary>
    private string LocalizeWithStringAndNumber(string key, string argKey, double value)
    {
        string template = Localize(key);
        string arg = DialogueManager.GetLocalizedText(argKey);
        if (string.IsNullOrEmpty(arg)) arg = argKey;
        return string.Format(template, arg, value);
    }

    /// <summary>用 emTag 包裹文字</summary>
    private string Wrap(string emTag, string content)
    {
        return $"[{emTag}]{content}[/{emTag}]";
    }

    // ============================================================
    //  無色 (T) — 純文字，不包任何標籤
    // ============================================================

    /// <summary>Lua: T("key") → 純查表</summary>
    public string T(string key) => Localize(key);

    /// <summary>Lua: Tn("key", 數值) → {0} 填數值</summary>
    public string Tn(string key, double value) => LocalizeWithNumber(key, value);

    /// <summary>Lua: Ts("key", "argKey") → {0} 填翻譯字串</summary>
    public string Ts(string key, string argKey) => LocalizeWithString(key, argKey);

    /// <summary>Lua: Tsn("key", "argKey", 數值) → {0} 填翻譯字串, {1} 填數值</summary>
    public string Tsn(string key, string argKey, double value) => LocalizeWithStringAndNumber(key, argKey, value);

    // ============================================================
    //  紅色 R [em2] — 負面相關
    // ============================================================

    public string TR(string key) => Wrap("em2", T(key));
    public string TRn(string key, double value) => Wrap("em2", Tn(key, value));
    public string TRs(string key, string argKey) => Wrap("em2", Ts(key, argKey));
    public string TRsn(string key, string argKey, double value) => Wrap("em2", Tsn(key, argKey, value));

    // ============================================================
    //  綠色 E [em3] — 體力相關
    // ============================================================

    public string TE(string key) => Wrap("em3", T(key));
    public string TEn(string key, double value) => Wrap("em3", Tn(key, value));
    public string TEs(string key, string argKey) => Wrap("em3", Ts(key, argKey));
    public string TEsn(string key, string argKey, double value) => Wrap("em3", Tsn(key, argKey, value));

    // ============================================================
    //  灰色 G [em4] — 已閱讀相關
    // ============================================================

    public string TG(string key) => Wrap("em4", T(key));
    public string TGn(string key, double value) => Wrap("em4", Tn(key, value));
    public string TGs(string key, string argKey) => Wrap("em4", Ts(key, argKey));
    public string TGsn(string key, string argKey, double value) => Wrap("em4", Tsn(key, argKey, value));

    // ============================================================
    //  粉色 P [em5] — 色情相關
    // ============================================================

    public string TP(string key) => Wrap("em5", T(key));
    public string TPn(string key, double value) => Wrap("em5", Tn(key, value));
    public string TPs(string key, string argKey) => Wrap("em5", Ts(key, argKey));
    public string TPsn(string key, string argKey, double value) => Wrap("em5", Tsn(key, argKey, value));

    // ============================================================
    //  對話結束 → 隊列處理
    // ============================================================

    private void OnConversationEnd(Transform actor)
    {
        StartCoroutine(QueuedConversationNextFrame());
    }

    private System.Collections.IEnumerator QueuedConversationNextFrame()
    {
        yield return null;

        if (_conversationQueue.Count > 0)
        {
            ConversationRequest next = _conversationQueue.First.Value;
            _conversationQueue.RemoveFirst();
            Debug.Log($"[StoryManager] 從隊列啟動 {next.title}");
            SyncWaitWithCustomDelay();
            DialogueManager.StartConversation(next.title);
        }
    }

    #region ====== 系統公告 (Alert) ======

    /// <summary>
    /// 顯示系統公告 (直接傳入文字)
    /// </summary>
    public void ShowSystemMessage(string message, float duration = -1)
    {
        float finalDuration = CalculateDuration(message, duration);
        ShowAlertInternal(message, finalDuration);
    }

    /// <summary>
    /// 顯示本地化的系統公告 (傳入 Localization Key)
    /// </summary>
    public void ShowLocalizedMessage(string key, float duration = -1)
    {
        string localizedText = DialogueManager.GetLocalizedText(key);

        if (string.IsNullOrEmpty(localizedText))
        {
            localizedText = key;
            Debug.LogWarning($"[StoryManager] 找不到本地化 Key: {key}");
        }

        float finalDuration = CalculateDuration(localizedText, duration);
        ShowAlertInternal(localizedText, finalDuration);
    }

    // --- 特殊事件公告 ---

    /// <summary>顯示「綠色-體力」公告 [em3]</summary>
    public void ShowEnergyMessage(string message, float duration = -1)
    {
        ShowSystemMessage($"[em3]{message}[/em3]", duration);
    }

    /// <summary>顯示「紅色-負面」公告 [em2]</summary>
    public void ShowBadMessage(string message, float duration = -1)
    {
        ShowSystemMessage($"[em2]{message}[/em2]", duration);
    }

    /// <summary>顯示本地化的「綠色-體力」公告 [em3]</summary>
    public void ShowLocalizedEnergyMessage(string key, float duration = -1, params object[] args)
    {
        string localizedText = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(localizedText)) localizedText = key;

        try { localizedText = string.Format(localizedText, args); }
        catch { Debug.LogError($"[StoryManager] 格式錯誤: {key}"); }

        ShowSystemMessage($"[em3]{localizedText}[/em3]", duration);
    }

    /// <summary>顯示本地化的「紅色-負面」公告 [em2]</summary>
    public void ShowLocalizedBadMessage(string key, float duration = -1, params object[] args)
    {
        string localizedText = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(localizedText)) localizedText = key;

        try { localizedText = string.Format(localizedText, args); }
        catch { Debug.LogError($"[StoryManager] 格式錯誤: {key}"); }

        ShowSystemMessage($"[em2]{localizedText}[/em2]", duration);
    }

    /// <summary>
    /// 顯示帶有參數替換的本地化公告
    /// 例如: "你獲得了 {0} 金幣" → ShowLocalizedMessageWithArgs("UI.GoldReward", -1, 100)
    /// </summary>
    public void ShowLocalizedMessageWithArgs(string key, float duration = -1, params object[] args)
    {
        string localizedText = DialogueManager.GetLocalizedText(key);

        if (string.IsNullOrEmpty(localizedText))
        {
            localizedText = key;
            Debug.LogWarning($"[StoryManager] 找不到本地化 Key: {key}");
        }

        try
        {
            localizedText = string.Format(localizedText, args);
        }
        catch (System.FormatException e)
        {
            Debug.LogError($"[StoryManager] 本地化字串格式錯誤: {key}, Error: {e.Message}");
        }

        float finalDuration = CalculateDuration(localizedText, duration);
        ShowAlertInternal(localizedText, finalDuration);
    }

    /// <summary>
    /// 所有公告的統一出口：先播放公告音效，再呼叫 DialogueManager 顯示
    /// </summary>
    private void ShowAlertInternal(string message, float duration)
    {
        if (!string.IsNullOrEmpty(alertSoundKey) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(alertSoundKey);
        }

        DialogueManager.ShowAlert(message, duration);
    }

    /// <summary>計算顯示時間</summary>
    private float CalculateDuration(string message, float duration)
    {
        if (duration > 0)
        {
            return duration;
        }
        return Mathf.Max(minAlertDuration, message.Length / alertCharsPerSecond);
    }

    #endregion
}