using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 單筆資源變動記錄
/// </summary>
public struct ValueChangeRecord
{
    public bool isHeroineResource;           // 是否為女主角資源
    public string resourceTypeKey;           // 資源類型字串 (如 "Money", "AffinityExp")
    public int finalAmount;                  // 最終變動量 (正=增加, 負=減少)
    public ValueChangeResult.EffectResult effectResult;  // 效果結果 (Normal/Good/Bad)
    public string heroineNameKey;            // 女主角名稱的 Text Table Key (僅女主角資源使用)

    public bool IsAdd => finalAmount > 0;
    public bool IsReduce => finalAmount < 0;
}

/// <summary>
/// 資源變動報告器
/// 負責將 ValueChangeResult 的執行結果轉換成本地化文字並顯示
/// 支援透過 ValueChangeReportConfig 進行配置
///
/// ⚠️ 【過渡中 — 已標記 Obsolete】
/// 「提示玩家數值改變」的功能，目前已由 UI 提示直接顯示（各數值 UI 自身的跳動提示），
/// 本報告器（alert toast 的 Report / 覆寫字幕的 ReportToSubtitle 兩條）都不再是必要路徑。
/// 暫時保留程式以免既有調用端編譯錯誤，但暫時不往內做更新／維護；
/// 待確認新 UI 提示完全取代後，再連同 caller 一併移除。
/// </summary>
[Obsolete("提示玩家數值改變的功能已由 UI 提示直接顯示，本報告器為過渡保留、暫時不往內做更新。待新 UI 提示確認取代後再移除。")]
public static class ValueChangeReporter
{
    // ==================================================
    // Config 參考
    // ==================================================

    private static ValueChangeReportConfig _config;

    public static ValueChangeReportConfig Config
    {
        get => _config;
        set => _config = value;
    }

    // ==================================================
    // 預設值 (當沒有 Config 時使用)
    // ==================================================

    private const string DEFAULT_KEY_PREFIX = "Result";
    private const string DEFAULT_KEY_SEPARATOR = ".";
    private const string DEFAULT_GOOD_TAG = "em3";
    private const string DEFAULT_BAD_TAG = "em2";

    private static readonly HashSet<string> _defaultIgnoredTypes = new HashSet<string>
    {
        "Action",
        "Time"
    };

    private static readonly HashSet<string> _defaultInverseTypes = new HashSet<string>
    {
        "Suspicion"
    };

    // ==================================================
    // 主要 API
    // ==================================================

    public static void Initialize()
    {
        if (_config == null)
        {
            _config = Resources.Load<ValueChangeReportConfig>("ValueChangeReportConfig");
            if (_config != null)
            {
                Debug.Log("[ValueChangeReporter] 已從 Resources 載入 Config");
            }
        }
    }

    /// <summary>
    /// 根據變動記錄列表生成並顯示報告（彈公告）
    /// </summary>
    public static void Report(List<ValueChangeRecord> records)
    {
        if (records == null || records.Count == 0) return;
        if (_config == null) Initialize();
        if (StoryManager.Instance == null) return;

        // 每一筆變動各自彈一則公告（各自一個 toast 外框、可堆疊）
        // ★ 不再串成單一多行字串 —— 交由 AlertToastManager 逐一交錯冒出
        foreach (var record in records)
        {
            string line = BuildReportLine(record);
            if (!string.IsNullOrEmpty(line))
            {
                StoryManager.Instance.ShowSystemMessage(line);
            }
        }
    }

    /// <summary>
    /// 將報告文字寫進當前對話框的 Subtitle Panel，取代目前顯示的對話文字。
    /// 必須在 Sequencer 指令中使用（需要有進行中的對話）。
    /// </summary>
    public static void ReportToSubtitle(List<ValueChangeRecord> records)
    {
        if (records == null || records.Count == 0) return;
        if (_config == null) Initialize();

        string reportText = BuildFullReport(records);
        if (string.IsNullOrEmpty(reportText))
        {
            Debug.LogWarning("[ValueChangeReporter] ReportToSubtitle：沒有可顯示的報告內容。");
            return;
        }

        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.StartCoroutine(OverwriteSubtitleNextFrame(reportText));
        }
        else
        {
            Debug.LogWarning("[ValueChangeReporter] ReportToSubtitle 失敗：DialogueManager.instance 為 null。改用 Alert。");
            if (StoryManager.Instance != null)
                StoryManager.Instance.ShowSystemMessage(reportText);
        }
    }

    /// <summary>
    /// 等到當前幀結束後，覆寫 Subtitle Panel 的文字。
    /// 此時 Dialogue System 的 SetContent 已經執行完畢，不會再被覆蓋。
    /// </summary>
    private static IEnumerator OverwriteSubtitleNextFrame(string reportText)
    {
        yield return null;

        // ★ 將 [em] tag 轉成 TMP rich text（粗體 + 顏色）
        reportText = ConvertEmphasisToRichText(reportText);

        var dialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
        if (dialogueUI == null)
        {
            Debug.LogWarning("[ValueChangeReporter] 找不到 StandardDialogueUI，改用 Alert。");
            if (StoryManager.Instance != null)
                StoryManager.Instance.ShowSystemMessage(reportText);
            yield break;
        }

        var panels = dialogueUI.conversationUIElements.subtitlePanels;
        bool found = false;

        if (panels != null)
        {
            foreach (var panel in panels)
            {
                if (panel != null && panel.hasFocus && panel.subtitleText != null)
                {
                    TypewriterUtility.StopTyping(panel.subtitleText);
                    panel.subtitleText.text = reportText;
                    found = true;
                    break;
                }
            }
        }

        if (!found)
        {
            var defaultPanel = dialogueUI.conversationUIElements.defaultNPCSubtitlePanel;
            if (defaultPanel != null && defaultPanel.subtitleText != null)
            {
                TypewriterUtility.StopTyping(defaultPanel.subtitleText);
                defaultPanel.subtitleText.text = reportText;
            }
            else
            {
                Debug.LogWarning("[ValueChangeReporter] 找不到任何可用的 Subtitle Panel，改用 Alert。");
                if (StoryManager.Instance != null)
                    StoryManager.Instance.ShowSystemMessage(reportText);
            }
        }
    }

    // ==================================================
    // Emphasis → TMP Rich Text 轉換
    // ==================================================

    /// <summary>
    /// 將 Dialogue System 的 emphasis tag（如 [em2]...[/em2]）
    /// 轉換為 TextMeshPro 的 rich text 格式（粗體 + 顏色）。
    /// 用於 ReportToSubtitle 直接寫入 subtitleText.text 時，
    /// 因為 Dialogue System 不會再解析 emphasis tag。
    /// </summary>
    private static string ConvertEmphasisToRichText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // em2 = 紅色(負面)
        text = text.Replace("[em2]", "<b><color=#FF3B3B>");
        text = text.Replace("[/em2]", "</color></b>");

        // em3 = 綠色(正面)
        text = text.Replace("[em3]", "<b><color=#12B980>");
        text = text.Replace("[/em3]", "</color></b>");

        // em4 = 灰色(已閱讀)
        text = text.Replace("[em4]", "<b><color=#848484>");
        text = text.Replace("[/em4]", "</color></b>");

        // em5 = 粉色(色情/親密)
        text = text.Replace("[em5]", "<b><color=#FF5D87>");
        text = text.Replace("[/em5]", "</color></b>");

        return text;
    }

    // ==================================================
    // 報告組裝
    // ==================================================

    private static string BuildFullReport(List<ValueChangeRecord> records)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var record in records)
        {
            string line = BuildReportLine(record);
            if (!string.IsNullOrEmpty(line))
            {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append(line);
            }
        }
        return sb.ToString();
    }

    public static string BuildReportLine(ValueChangeRecord record)
    {
        if (IsIgnoredType(record.resourceTypeKey))
            return null;

        string template = GetTemplate(record.resourceTypeKey, record.IsAdd, record.effectResult.ToString());

        if (string.IsNullOrEmpty(template))
        {
            Debug.LogWarning($"[ValueChangeReporter] 找不到任何可用的 Key for {record.resourceTypeKey}");
            return null;
        }

        string formattedText = FormatTemplate(template, record);
        string colorTag = DetermineColorTag(record);
        return $"[{colorTag}]{formattedText}[/{colorTag}]";
    }

    // ==================================================
    // 模板取得 (含 Fallback 規則)
    // ==================================================

    private static string GetTemplate(string typeKey, bool isAdd, string effect)
    {
        string mainKey = BuildKey(typeKey, isAdd, effect);
        string template = DialogueManager.GetLocalizedText(mainKey);

        if (!string.IsNullOrEmpty(template))
            return template;

        if (effect != "Normal")
        {
            string normalKey = BuildKey(typeKey, isAdd, "Normal");
            template = DialogueManager.GetLocalizedText(normalKey);

            if (!string.IsNullOrEmpty(template))
            {
                Debug.Log($"[ValueChangeReporter] {mainKey} 找不到，使用 {normalKey}");
                return template;
            }
        }

        return null;
    }

    // ==================================================
    // Key 建構
    // ==================================================

    private static string BuildKey(string typeKey, bool isAdd, string effect)
    {
        if (_config != null)
            return _config.BuildKey(typeKey, isAdd, effect);

        string actionStr = isAdd ? "Add" : "Reduce";
        return $"{DEFAULT_KEY_PREFIX}{DEFAULT_KEY_SEPARATOR}{actionStr}{typeKey}{DEFAULT_KEY_SEPARATOR}{effect}";
    }

    // ==================================================
    // 類型檢查
    // ==================================================

    private static bool IsIgnoredType(string typeKey)
    {
        if (_config != null)
            return _config.IsIgnoredType(typeKey);
        return _defaultIgnoredTypes.Contains(typeKey);
    }

    private static bool IsInverseType(string typeKey)
    {
        if (_config != null)
            return _config.IsInverseType(typeKey);
        return _defaultInverseTypes.Contains(typeKey);
    }

    // ==================================================
    // 輔助方法
    // ==================================================

    private static string FormatTemplate(string template, ValueChangeRecord record)
    {
        try
        {
            int absAmount = Mathf.Abs(record.finalAmount);

            if (record.isHeroineResource && !string.IsNullOrEmpty(record.heroineNameKey))
            {
                string heroineName = DialogueManager.GetLocalizedText(record.heroineNameKey);
                if (string.IsNullOrEmpty(heroineName)) heroineName = record.heroineNameKey;
                return string.Format(template, absAmount, heroineName);
            }
            else
            {
                return string.Format(template, absAmount);
            }
        }
        catch (System.FormatException e)
        {
            Debug.LogError($"[ValueChangeReporter] 格式化錯誤: {e.Message}");
            return template;
        }
    }

    private static string DetermineColorTag(ValueChangeRecord record)
    {
        if (record.effectResult == ValueChangeResult.EffectResult.Good)
            return "em5";

        if (record.effectResult == ValueChangeResult.EffectResult.Bad)
            return DEFAULT_BAD_TAG;

        bool isGood;

        if (IsInverseType(record.resourceTypeKey))
            isGood = record.IsReduce;
        else
            isGood = record.IsAdd;

        if (_config != null)
            return _config.GetColorTag(isGood);

        return isGood ? DEFAULT_GOOD_TAG : DEFAULT_BAD_TAG;
    }

    // ==================================================
    // 手動設定 API (向下相容，無 Config 時使用)
    // ==================================================

    public static void AddIgnoredType(string typeKey)
    {
        _defaultIgnoredTypes.Add(typeKey);
    }

    public static void RemoveIgnoredType(string typeKey)
    {
        _defaultIgnoredTypes.Remove(typeKey);
    }

    public static void AddInverseType(string typeKey)
    {
        _defaultInverseTypes.Add(typeKey);
    }

    public static void RemoveInverseType(string typeKey)
    {
        _defaultInverseTypes.Remove(typeKey);
    }
}