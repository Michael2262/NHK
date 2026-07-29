using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在按鈕 / UI 群組上的「不可用」元件。
/// 當條件「不符合」時，將指定的 CanvasGroup 變半透明並停用互動（點擊無效）。
/// 與 RequirementBlocker 的差異：Blocker 是顯示遮擋物；本元件是讓 UI 本身呈現灰化不可點。
///
/// 兩種使用方式 (二選一，不互相影響)：
/// 1. 引用 RuleAsset：拖入一個 ProgressUnlockRuleAsset，自動用該 rule 的條件。
///    這是推薦做法，能與 ProgressUnlockConfig / RequirementBlocker 共用同一份條件來源。
/// 2. 自填 Conditions：手動填入 RequirementCondition 列表，支援 All/Any 邏輯。
///    若同時有 RuleAsset 和 Conditions，優先走 RuleAsset。
/// </summary>
public class RequirementUnavailable : MonoBehaviour
{
    public enum LogicMode { All, Any }

    [Header("總開關")]
    [Tooltip("是否啟用此功能。關閉時 CanvasGroup 會強制恢復可用狀態，條件完全不檢查。")]
    public bool Enabled = true;

    [Header("[優先] 引用規則 Asset")]
    [Tooltip(
        "若指定此 Rule Asset，會完全使用該 rule 的條件。\n" +
        "留空則使用下方的 Conditions 列表。"
    )]
    public ProgressUnlockRuleAsset RuleAsset;

    [Header("[備案] 手動條件 (RuleAsset 為空時使用)")]
    [Tooltip("All = 全部條件符合才可用；Any = 任一條件符合即可用")]
    public LogicMode Mode = LogicMode.All;

    [Tooltip("條件列表 (僅在 RuleAsset 為空時生效)")]
    public List<RequirementCondition> Conditions = new List<RequirementCondition>();

    [Header("UI 參照")]
    [Tooltip("要控制的 CanvasGroup。留空時自動抓取本物件上的 CanvasGroup。")]
    public CanvasGroup TargetGroup;

    [Header("顯示設定")]
    [Tooltip("條件不達成時的透明度")]
    [Range(0f, 1f)]
    public float UnavailableAlpha = 0.5f;

    [Tooltip("條件達成時的透明度")]
    [Range(0f, 1f)]
    public float AvailableAlpha = 1f;

    [Tooltip(
        "不達成時是否仍攔截 Raycast。\n" +
        "勾選 (預設)：點擊落在此群組上但無效果，不會穿透到後方。\n" +
        "不勾：點擊會直接穿透到後方的 UI / 場景物件。"
    )]
    public bool BlockRaycastsWhenUnavailable = true;

    [Tooltip("是否每個 Frame 自動重新檢查（關閉後只有事件觸發或手動 Refresh 時檢查）")]
    public bool AutoRefresh = true;

    private bool _subscribed;

    private void Awake()
    {
        if (TargetGroup == null) TargetGroup = GetComponent<CanvasGroup>();
        if (TargetGroup == null)
            Debug.LogWarning("[RequirementUnavailable] 找不到 CanvasGroup，請在 Inspector 指定或掛在有 CanvasGroup 的物件上。", this);
    }

    private void OnEnable()
    {
        SubscribeEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (AutoRefresh) Refresh();
    }

    /// <summary> 手動觸發一次檢查 </summary>
    public void Refresh()
    {
        // 功能未啟用：強制恢復可用，不做任何條件檢查
        if (!Enabled)
        {
            ApplyState(true);
            return;
        }

        ApplyState(EvaluatePassed());
    }

    /// <summary> 依 RuleAsset / Conditions 評估條件是否達成 </summary>
    private bool EvaluatePassed()
    {
        // 優先走 RuleAsset 路徑
        if (RuleAsset != null)
        {
            var svc = GameStatusService.Instance;
            if (svc == null) return false;

            // 含女主角條件時才需要解析 heroine；純主角條件的規則 heroine 為 null 也能評估
            HeroineStatusModel heroine = null;
            if (!string.IsNullOrEmpty(RuleAsset.heroineID) && svc.Heroines != null)
                svc.Heroines.TryGetValue(RuleAsset.heroineID, out heroine);

            return RuleAsset.IsConditionMet(heroine, svc.Protagonist);
        }

        // Conditions 路徑
        if (Conditions == null || Conditions.Count == 0) return true;

        if (Mode == LogicMode.All)
        {
            foreach (var c in Conditions)
            {
                if (c == null) continue;
                if (!c.IsMet()) return false;
            }
            return true;
        }
        else // Any
        {
            foreach (var c in Conditions)
            {
                if (c == null) continue;
                if (c.IsMet()) return true;
            }
            return false;
        }
    }

    /// <summary> 將達成 / 未達成狀態套用到 CanvasGroup </summary>
    private void ApplyState(bool passed)
    {
        if (TargetGroup == null) return;

        if (passed)
        {
            TargetGroup.alpha = AvailableAlpha;
            TargetGroup.interactable = true;
            TargetGroup.blocksRaycasts = true;
        }
        else
        {
            TargetGroup.alpha = UnavailableAlpha;
            TargetGroup.interactable = false;
            TargetGroup.blocksRaycasts = BlockRaycastsWhenUnavailable;
        }
    }

    // ==========================================================
    // 事件訂閱 (與 RequirementBlocker 相同的訂閱策略)
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
