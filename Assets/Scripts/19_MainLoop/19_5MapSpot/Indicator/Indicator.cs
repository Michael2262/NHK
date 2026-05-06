using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 【通用指示器】
/// 
/// 根據指定條件開關一張圖片,並通知父框 (IndicatorFrameController) 狀態變化。
/// 
/// 目前支援兩種判斷模式:
/// - HeroineAtLocation : 某位女主角是否在指定地點 list 之一
/// - FlagEnabled       : 某個 ProgressFlag 是否啟用
/// 
/// 未來擴充只需:
/// 1. 在 IndicatorMode enum 加新項
/// 2. 在 CheckMatched() 的 switch 加對應 case
/// 3. 在 SubscribeEvents() 加事件訂閱 (若需要)
/// 4. (選用) 在 Inspector 加對應資料欄位
/// </summary>
public class Indicator : MonoBehaviour
{
    // ==========================================================
    // Enum 定義
    // ==========================================================

    public enum IndicatorMode
    {
        HeroineAtLocation,  // 依「女主角所在地點」判斷
        FlagEnabled         // 依「某 Flag 是否啟用」判斷
    }

    public enum ToggleMode
    {
        ImageEnabled,       // 只切換 Image.enabled
        GameObjectActive    // 切換整個 GameObject
    }

    // ==========================================================
    // 共用欄位
    // ==========================================================

    [Header("=== 判斷模式 ===")]
    [Tooltip("選擇這個指示器用什麼條件判斷")]
    public IndicatorMode mode = IndicatorMode.HeroineAtLocation;

    [Tooltip("反向邏輯:條件「不符合」時才顯示")]
    public bool invertLogic = false;

    [Header("=== 顯示控制 ===")]
    [Tooltip("要被切換開關的圖片")]
    public Image targetImage;

    [Tooltip("切換模式:\n" +
             "ImageEnabled = 只切換 Image 的 enabled\n" +
             "GameObjectActive = 切換整個 GameObject")]
    public ToggleMode toggleMode = ToggleMode.ImageEnabled;

    // ==========================================================
    // 模式 A:女主角位置
    // ==========================================================

    [Header("=== [模式 A] 女主角位置 (只在 HeroineAtLocation 模式有效) ===")]
    [Tooltip("要監控的女主角 ID")]
    public string heroineID;

    [Tooltip("要監控的地點 ID list。女主角出現在「任一個」就算符合")]
    public List<string> locationIDs = new List<string>();

    // ==========================================================
    // 模式 B:Flag
    // ==========================================================

    [Header("=== [模式 B] Flag (只在 FlagEnabled 模式有效) ===")]
    [Tooltip("要監控的 Flag。此 Flag 啟用時算符合")]
    public ProgressFlagDefinition targetFlag;

    // ==========================================================
    // Debug
    // ==========================================================

    [Header("=== Debug ===")]
    [SerializeField, Tooltip("Readonly:目前是否顯示中")]
    private bool _isCurrentlyMatched;

    /// <summary>當顯示狀態改變時觸發 — 父框靠這個監聽</summary>
    public event Action<Indicator, bool> OnVisibilityChanged;

    /// <summary>目前是否顯示</summary>
    public bool IsVisible => _isCurrentlyMatched;

    // ==========================================================
    // 生命週期
    // ==========================================================

    private void OnEnable()
    {
        SubscribeEvents(true);
        Refresh();
    }

    private void OnDisable()
    {
        SubscribeEvents(false);
    }

    // ==========================================================
    // 事件訂閱 (依 mode 訂不同事件)
    // ==========================================================

    private void SubscribeEvents(bool subscribe)
    {
        var service = GameStatusService.Instance;
        if (service == null) return;

        switch (mode)
        {
            case IndicatorMode.HeroineAtLocation:
                if (service.Scenario != null)
                {
                    if (subscribe)
                    {
                        service.Scenario.OnHeroineMoved += HandleHeroineMoved;
                        service.Scenario.OnScenarioRecalculated += Refresh;
                    }
                    else
                    {
                        service.Scenario.OnHeroineMoved -= HandleHeroineMoved;
                        service.Scenario.OnScenarioRecalculated -= Refresh;
                    }
                }
                break;

            case IndicatorMode.FlagEnabled:
                if (service.ProgressFlags != null)
                {
                    if (subscribe)
                    {
                        service.ProgressFlags.OnFlagChanged += HandleFlagChanged;
                    }
                    else
                    {
                        service.ProgressFlags.OnFlagChanged -= HandleFlagChanged;
                    }
                }
                break;
        }
    }

    private void HandleHeroineMoved(string hID, string oldLoc, string newLoc, string newAct)
    {
        if (string.IsNullOrEmpty(heroineID)) return;
        if (!string.Equals(hID, heroineID, StringComparison.OrdinalIgnoreCase)) return;
        Refresh();
    }

    private void HandleFlagChanged(string flagID, bool isActive)
    {
        if (targetFlag == null) return;
        if (!string.Equals(flagID, targetFlag.FlagID, StringComparison.OrdinalIgnoreCase)) return;
        Refresh();
    }

    // ==========================================================
    // 核心刷新
    // ==========================================================

    public void Refresh()
    {
        bool matched = CheckMatched();
        if (invertLogic) matched = !matched;

        bool changed = (_isCurrentlyMatched != matched);
        _isCurrentlyMatched = matched;

        ApplyVisibility(matched);

        if (changed)
            OnVisibilityChanged?.Invoke(this, matched);
    }

    private bool CheckMatched()
    {
        switch (mode)
        {
            case IndicatorMode.HeroineAtLocation:
                return CheckHeroineAtLocation();

            case IndicatorMode.FlagEnabled:
                return CheckFlagEnabled();

            default:
                return false;
        }
    }

    // ==========================================================
    // 各模式的判斷邏輯
    // ==========================================================

    private bool CheckHeroineAtLocation()
    {
        if (string.IsNullOrEmpty(heroineID)) return false;
        if (locationIDs == null || locationIDs.Count == 0) return false;

        var service = GameStatusService.Instance;
        if (service == null || service.Scenario == null) return false;

        foreach (var locID in locationIDs)
        {
            if (string.IsNullOrEmpty(locID)) continue;

            var state = service.Scenario.GetState(locID);
            if (state == null || state.Heroines == null) continue;

            foreach (var h in state.Heroines)
            {
                if (string.Equals(h.HeroineID, heroineID, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private bool CheckFlagEnabled()
    {
        if (targetFlag == null) return false;

        var service = GameStatusService.Instance;
        if (service == null || service.ProgressFlags == null) return false;

        return service.ProgressFlags.Contains(targetFlag.FlagID);
    }

    // ==========================================================
    // 套用顯示狀態
    // ==========================================================

    private void ApplyVisibility(bool show)
    {
        if (targetImage == null) return;

        switch (toggleMode)
        {
            case ToggleMode.ImageEnabled:
                if (targetImage.enabled != show)
                    targetImage.enabled = show;
                break;

            case ToggleMode.GameObjectActive:
                if (targetImage.gameObject.activeSelf != show)
                    targetImage.gameObject.SetActive(show);
                break;
        }
    }

    // ==========================================================
    // Debug & Editor
    // ==========================================================

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
            Refresh();
    }
#endif

    [ContextMenu("Debug/強制重新整理")]
    public void DebugRefresh()
    {
        Refresh();
        string modeDesc = mode switch
        {
            IndicatorMode.HeroineAtLocation => $"女主角 '{heroineID}' 在指定地點",
            IndicatorMode.FlagEnabled => $"Flag '{targetFlag?.FlagID ?? "?"}' 啟用",
            _ => "?"
        };
        Debug.Log($"[Indicator] 模式: {modeDesc}, 顯示中: {_isCurrentlyMatched}", this);
    }
}
