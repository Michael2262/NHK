// 檔案：HeroineDebugUI.cs
using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 職責：顯示「單一」女主角的 NHK 除錯資訊，並訂閱其數據變更事件。
/// 顯示項目：名稱、當前情緒與正式女主角數值。
/// </summary>
public class HeroineDebugUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("對應 HeroineStatusConfig 中的角色 ID")]
    [SerializeField] private string heroineID;

    [Header("Identity")]
    [SerializeField] private TextMeshProUGUI textName;

    [Header("Emotions")]
    [SerializeField] private TextMeshProUGUI textCurrentEmotion;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI textLibido;
    [SerializeField] private TextMeshProUGUI textHCount;

    [SerializeField] private TextMeshProUGUI textTrust;
    [SerializeField] private TextMeshProUGUI textAffinity;
    [SerializeField] private TextMeshProUGUI textMood;
    [SerializeField] private TextMeshProUGUI textExcitement;
    [SerializeField] private TextMeshProUGUI textOrgasm;

    private HeroineStatusModel _model;

    /// <summary>
    /// 若 Inspector 有填 heroineID，自動從 GameStatusService 取得對應 Model。
    /// 也可由外部呼叫 Initialize() 手動指定。
    ///
    /// 同時訂閱 OnGameStatusLoaded：女主角 Model 會在新遊戲 / 重置時被
    /// InitializeHeroineModels() 整批 new 出新實例，舊實例變成孤兒。
    /// 收到事件後依 heroineID 重新抓取新實例並重綁，避免一直顯示舊資料。
    /// </summary>
    private void Start()
    {
        var service = GameStatusService.Instance;
        if (service != null)
            service.OnGameStatusLoaded += HandleGameStatusLoaded;

        if (_model != null) return; // 已經由外部 Initialize 過了

        BindFromService();
    }

    /// <summary>
    /// Model 被整批重建後（OnGameStatusLoaded），依 heroineID 重新綁定到新實例。
    /// 由外部 Initialize() 指定（未填 heroineID）的情況不自動重綁，交由外部負責。
    /// </summary>
    private void HandleGameStatusLoaded()
    {
        if (string.IsNullOrEmpty(heroineID)) return;
        BindFromService();
    }

    /// <summary>依 heroineID 從 GameStatusService 取得對應 Model 並 Initialize。</summary>
    private void BindFromService()
    {
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning($"[HeroineDebugUI] heroineID 未填寫，無法自動初始化。", this);
            return;
        }

        var service = GameStatusService.Instance;
        if (service == null)
        {
            Debug.LogError($"[HeroineDebugUI] GameStatusService.Instance 為 null。", this);
            return;
        }

        if (service.Heroines.TryGetValue(heroineID, out var model))
        {
            Initialize(model);
        }
        else
        {
            Debug.LogError($"[HeroineDebugUI] 找不到 heroineID=\"{heroineID}\" 的女主角。", this);
        }
    }

    /// <summary>
    /// 從外部初始化，告訴這個 UI 要監聽哪一位女主角。
    /// </summary>
    public void Initialize(HeroineStatusModel model)
    {
        Unsubscribe();
        _model = model;
        if (_model != null) _model.OnChanged += RefreshUI;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_model == null) return;
        if (textName != null) textName.text = _model.Name;
        if (textCurrentEmotion != null) textCurrentEmotion.text = _model.CurrentEmotion.ToString();
        if (textLibido != null) textLibido.text = _model.Libido.ToString();
        if (textHCount != null) textHCount.text = _model.HCount.ToString();
        if (textTrust != null) textTrust.text = _model.Trust.ToString();
        if (textAffinity != null) textAffinity.text = _model.Affinity.ToString();
        if (textMood != null) textMood.text = _model.Mood.ToString();
        if (textExcitement != null) textExcitement.text = _model.Excitement.ToString();
        if (textOrgasm != null) textOrgasm.text = _model.Orgasm.ToString();
    }

    private void Unsubscribe()
    { if (_model != null) _model.OnChanged -= RefreshUI; }

    private void OnDestroy()
    {
        var service = GameStatusService.Instance;
        if (service != null)
            service.OnGameStatusLoaded -= HandleGameStatusLoaded;

        Unsubscribe();
    }
}