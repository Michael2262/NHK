using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;

/// <summary>
/// 掛在按鈕 / 圖片上的阻擋 UI 元件。
/// 當條件「不符合」時顯示遮擋 GameObject。
///
/// 兩種使用方式 (二選一，不互相影響)：
/// 1. 引用 RuleAsset：拖入一個 ProgressUnlockRuleAsset，Blocker 會自動用該 rule 的條件與提示文字。
///    這是推薦做法，能與 ProgressUnlockConfig 共用同一份條件來源，改一處全部同步。
/// 2. 自填 Conditions：手動填入 RequirementCondition 列表，支援 All/Any 邏輯。
///    若同時有 RuleAsset 和 Conditions，優先走 RuleAsset。
/// </summary>
public class RequirementBlocker : MonoBehaviour
{
    public enum LogicMode { All, Any }

    [Header("總開關")]
    [Tooltip("是否啟用此阻擋功能。關閉時遮擋物會強制隱藏，條件完全不檢查。")]
    public bool Enabled = true;

    [Header("[優先] 引用規則 Asset")]
    [Tooltip(
        "若指定此 Rule Asset，Blocker 會完全使用該 rule 的條件與 UI 提示。\n" +
        "留空則使用下方的 Conditions 列表 (舊行為)。"
    )]
    public ProgressUnlockRuleAsset RuleAsset;

    [Header("[備案] 手動條件 (RuleAsset 為空時使用)")]
    [Tooltip("All = 全部條件符合才解鎖；Any = 任一條件符合即解鎖")]
    public LogicMode Mode = LogicMode.All;

    [Tooltip("阻擋條件列表 (僅在 RuleAsset 為空時生效)")]
    public List<RequirementCondition> Conditions = new List<RequirementCondition>();

    [Header("UI 參照")]
    [Tooltip("遮擋物 GameObject（整個會開關）")]
    public GameObject BlockerObject;

    [Tooltip("顯示所需 LV 的 TextMeshPro（等級類條件才會寫入）")]
    public TMP_Text LevelText;

    [Tooltip("顯示條件類型名稱的 TextMeshPro（Libido / Trust / Stress … 等數值名稱；Flag 類型不變動）")]
    public TMP_Text TypeText;

    [Header("顯示設定")]
    [Tooltip("所需等級的顯示格式，{0} 會被換成等級數字")]
    public string LevelFormat = "LV.{0}";

    [Tooltip("是否每個 Frame 自動重新檢查（關閉後只有事件觸發或手動 Refresh 時檢查）")]
    public bool AutoRefresh = true;

    private bool _subscribed;
    private bool _blockerSanityChecked;

    private void Awake()
    {
        SanityCheckBlockerObject();
        if (BlockerObject != null) BlockerObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (BlockerObject != null) BlockerObject.SetActive(false);

        SubscribeEvents();
        PixelCrushers.UILocalizationManager.languageChanged += OnLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        PixelCrushers.UILocalizationManager.languageChanged -= OnLanguageChanged;
    }

    private void Update()
    {
        if (AutoRefresh) Refresh();
    }

    private void OnLanguageChanged(string language) => Refresh();

    private void SanityCheckBlockerObject()
    {
        if (_blockerSanityChecked) return;
        _blockerSanityChecked = true;

        if (BlockerObject == null) return;

        if (BlockerObject == this.gameObject)
        {
            Debug.LogError($"[RequirementBlocker] BlockerObject 不能設為自己！這會把整個物件關掉導致腳本停止運作。請改指向子物件（如遮擋圖）。", this);
            BlockerObject = null;
            return;
        }

        if (transform.IsChildOf(BlockerObject.transform) && BlockerObject.transform != this.transform)
        {
            Debug.LogError($"[RequirementBlocker] BlockerObject ({BlockerObject.name}) 是本物件的父層，會連帶關掉本物件。請改指向子物件。", this);
            BlockerObject = null;
        }
    }

    /// <summary> 手動觸發一次檢查 </summary>
    public void Refresh()
    {
        // 功能未啟用：強制關閉遮擋物，不做任何條件檢查
        if (!Enabled)
        {
            if (BlockerObject != null) BlockerObject.SetActive(false);
            return;
        }

        // 優先走 RuleAsset 路徑
        if (RuleAsset != null)
        {
            RefreshFromRuleAsset();
            return;
        }

        // 否則走舊的 Conditions 路徑
        RefreshFromConditions();
    }

    // ==========================================================
    // 路徑 A：Rule Asset 評估
    // ==========================================================
    private void RefreshFromRuleAsset()
    {
        var svc = GameStatusService.Instance;
        if (svc == null)
        {
            if (BlockerObject != null) BlockerObject.SetActive(true);
            return;
        }

        // 含女主角條件時才需要解析 heroine；純主角條件的規則 heroine 為 null 也能評估
        HeroineStatusModel heroine = null;
        if (!string.IsNullOrEmpty(RuleAsset.heroineID) && svc.Heroines != null)
            svc.Heroines.TryGetValue(RuleAsset.heroineID, out heroine);

        bool passed = RuleAsset.IsConditionMet(heroine, svc.Protagonist);

        if (BlockerObject != null)
            BlockerObject.SetActive(!passed);

        if (passed) return;

        // 從 RuleAsset 讀取 UI 提示
        if (LevelText != null)
        {
            int lv = RuleAsset.GetUIDisplayLevel();
            LevelText.text = string.Format(LevelFormat, lv);
        }

        if (TypeText != null)
        {
            string typeKey = RuleAsset.GetUIDisplayTypeKey();
            if (!string.IsNullOrEmpty(typeKey))
            {
                TypeText.text = LookupLocalizedText(typeKey);
            }
        }
    }

    // ==========================================================
    // 路徑 B：原本的 Conditions 評估 (完全不變)
    // ==========================================================
    private void RefreshFromConditions()
    {
        bool passed = Evaluate(out RequirementCondition firstFailed);

        if (BlockerObject != null)
            BlockerObject.SetActive(!passed);

        if (passed || firstFailed == null) return;

        if (firstFailed.IsStatType && LevelText != null)
        {
            LevelText.text = string.Format(LevelFormat, firstFailed.RequiredLevel);
        }

        if (TypeText != null)
        {
            string typeKey = firstFailed.GetTypeTextKey();
            if (!string.IsNullOrEmpty(typeKey))
            {
                TypeText.text = LookupLocalizedText(typeKey);
            }
        }
    }

    /// <summary>
    /// 參考 BackpackUI 的寫法，從 DialogueManager 的 textTable 查詢本地化字串。
    /// 找不到時回傳 key 本身，避免顯示空白。
    /// </summary>
    private string LookupLocalizedText(string fieldKey)
    {
        if (DialogueManager.instance == null
            || DialogueManager.displaySettings == null
            || DialogueManager.displaySettings.localizationSettings == null
            || DialogueManager.displaySettings.localizationSettings.textTable == null)
        {
            return fieldKey;
        }

        var textTable = DialogueManager.displaySettings.localizationSettings.textTable;
        if (textTable.HasField(fieldKey))
        {
            return textTable.GetFieldTextForLanguage(fieldKey, Localization.GetCurrentLanguageID(textTable));
        }
        return fieldKey;
    }

    /// <summary>
    /// 根據 Mode 評估所有 Conditions (不含 RuleAsset)。
    /// firstFailed 輸出第一個未達標的條件（用於顯示所需 LV 與類型文字）。
    /// </summary>
    private bool Evaluate(out RequirementCondition firstFailed)
    {
        firstFailed = null;
        if (Conditions == null || Conditions.Count == 0) return true;

        if (Mode == LogicMode.All)
        {
            foreach (var c in Conditions)
            {
                if (c == null) continue;
                if (!c.IsMet())
                {
                    if (firstFailed == null || (!firstFailed.IsStatType && c.IsStatType))
                        firstFailed = c;
                }
            }
            return firstFailed == null;
        }
        else // Any
        {
            RequirementCondition anyFail = null;
            foreach (var c in Conditions)
            {
                if (c == null) continue;
                if (c.IsMet()) return true;
                if (anyFail == null || (!anyFail.IsStatType && c.IsStatType))
                    anyFail = c;
            }
            firstFailed = anyFail;
            return false;
        }
    }

    // ==========================================================
    // 事件訂閱
    // ==========================================================

    private void SubscribeEvents()
    {
        if (_subscribed) return;
        var svc = GameStatusService.Instance;
        if (svc == null) return;

        // RuleAsset 路徑：依條件來源訂閱對應的數值事件
        if (RuleAsset != null)
        {
            if (RuleAsset.HasHeroineCondition
                && svc.Heroines != null && !string.IsNullOrEmpty(RuleAsset.heroineID)
                && svc.Heroines.TryGetValue(RuleAsset.heroineID, out var h) && h != null)
            {
                h.OnLibidoChanged += OnAnyChanged;
                h.OnTrustChanged += OnAnyChanged;
                h.OnHCountChanged += OnAnyChanged;
            }

            if (RuleAsset.HasProtagonistCondition && svc.Protagonist != null)
            {
                svc.Protagonist.OnStressChanged += OnAnyChanged;
                svc.Protagonist.OnLifePowerChanged += OnAnyChanged;
                svc.Protagonist.OnSocialityChanged += OnAnyChanged;
                svc.Protagonist.OnDependencyChanged += OnAnyChanged;
                svc.Protagonist.OnRoomMessLevelChanged += OnAnyChanged;
                svc.Protagonist.OnBodyDirtyLevelChanged += OnAnyChanged;
            }
        }
        else if (Conditions != null)
        {
            // Conditions 路徑：依條件的數值來源訂閱對應事件
            bool protagonistSubscribed = false;
            foreach (var c in Conditions)
            {
                if (c == null || !c.IsStatType) continue;

                if (c.IsHeroineStat)
                {
                    if (svc.Heroines == null || string.IsNullOrEmpty(c.HeroineID)) continue;
                    if (svc.Heroines.TryGetValue(c.HeroineID, out var h) && h != null)
                    {
                        h.OnLibidoChanged += OnAnyChanged;
                        h.OnTrustChanged += OnAnyChanged;
                        h.OnHCountChanged += OnAnyChanged;
                    }
                }
                else if (!protagonistSubscribed && svc.Protagonist != null)
                {
                    svc.Protagonist.OnStressChanged += OnAnyChanged;
                    svc.Protagonist.OnLifePowerChanged += OnAnyChanged;
                    svc.Protagonist.OnSocialityChanged += OnAnyChanged;
                    svc.Protagonist.OnDependencyChanged += OnAnyChanged;
                    svc.Protagonist.OnRoomMessLevelChanged += OnAnyChanged;
                    svc.Protagonist.OnBodyDirtyLevelChanged += OnAnyChanged;
                    protagonistSubscribed = true;
                }
            }
        }

        if (svc.ProgressFlags != null)
            svc.ProgressFlags.OnFlagChanged += OnFlagChanged;

        _subscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_subscribed) return;
        var svc = GameStatusService.Instance;
        if (svc == null) { _subscribed = false; return; }

        if (RuleAsset != null)
        {
            if (RuleAsset.HasHeroineCondition
                && svc.Heroines != null && !string.IsNullOrEmpty(RuleAsset.heroineID)
                && svc.Heroines.TryGetValue(RuleAsset.heroineID, out var h) && h != null)
            {
                h.OnLibidoChanged -= OnAnyChanged;
                h.OnTrustChanged -= OnAnyChanged;
                h.OnHCountChanged -= OnAnyChanged;
            }

            if (RuleAsset.HasProtagonistCondition && svc.Protagonist != null)
            {
                svc.Protagonist.OnStressChanged -= OnAnyChanged;
                svc.Protagonist.OnLifePowerChanged -= OnAnyChanged;
                svc.Protagonist.OnSocialityChanged -= OnAnyChanged;
                svc.Protagonist.OnDependencyChanged -= OnAnyChanged;
                svc.Protagonist.OnRoomMessLevelChanged -= OnAnyChanged;
                svc.Protagonist.OnBodyDirtyLevelChanged -= OnAnyChanged;
            }
        }
        else if (Conditions != null)
        {
            bool protagonistUnsubscribed = false;
            foreach (var c in Conditions)
            {
                if (c == null || !c.IsStatType) continue;

                if (c.IsHeroineStat)
                {
                    if (svc.Heroines == null || string.IsNullOrEmpty(c.HeroineID)) continue;
                    if (svc.Heroines.TryGetValue(c.HeroineID, out var h) && h != null)
                    {
                        h.OnLibidoChanged -= OnAnyChanged;
                        h.OnTrustChanged -= OnAnyChanged;
                        h.OnHCountChanged -= OnAnyChanged;
                    }
                }
                else if (!protagonistUnsubscribed && svc.Protagonist != null)
                {
                    svc.Protagonist.OnStressChanged -= OnAnyChanged;
                    svc.Protagonist.OnLifePowerChanged -= OnAnyChanged;
                    svc.Protagonist.OnSocialityChanged -= OnAnyChanged;
                    svc.Protagonist.OnDependencyChanged -= OnAnyChanged;
                    svc.Protagonist.OnRoomMessLevelChanged -= OnAnyChanged;
                    svc.Protagonist.OnBodyDirtyLevelChanged -= OnAnyChanged;
                    protagonistUnsubscribed = true;
                }
            }
        }

        if (svc.ProgressFlags != null)
            svc.ProgressFlags.OnFlagChanged -= OnFlagChanged;

        _subscribed = false;
    }

    private void OnAnyChanged(int _) => Refresh();
    private void OnFlagChanged(string _, bool __) => Refresh();
}
