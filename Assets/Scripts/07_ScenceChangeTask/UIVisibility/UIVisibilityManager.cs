using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class UIVisibilityManager : MonoBehaviour
{
    [Serializable]
    public class UIGroup
    {
        public string groupName;
        public CanvasGroup canvasGroup;

        [Header("匹配模式 (不分大小寫)")]
        [Tooltip("符合這些『資料夾路徑』或『名稱前綴』則隱藏")]
        public List<string> hideRules = new List<string>();

        [Tooltip("符合這些『資料夾路徑』或『名稱前綴』則顯示 (若為空則預設顯示)")]
        public List<string> showRules = new List<string>();

        [Header("動畫設定")]
        public float fadeDuration = 0.3f;
    }

    public List<UIGroup> uiGroups = new List<UIGroup>();

    void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;
    void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

    void Start()
    {
        // 初始場景判定，通常第一關或主選單需要立即顯示/隱藏
        RefreshUI(SceneManager.GetActiveScene(), true);
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        RefreshUI(newScene, false);
    }

    public void RefreshUI(Scene scene, bool immediate)
    {
        foreach (var group in uiGroups)
        {
            bool shouldShow = DetermineVisibility(group, scene);
            UpdateGroupVisibility(group, shouldShow, immediate);
        }
    }

    private bool DetermineVisibility(UIGroup group, Scene scene)
    {
        string fullPath = scene.path; // 格式如: Assets/Scenes/Minigames/Level1.unity
        string name = scene.name;     // 格式如: Level1

        // 1. 優先檢查黑名單 (不分大小寫)
        foreach (var rule in group.hideRules)
        {
            if (string.IsNullOrEmpty(rule)) continue;

            // 檢查路徑是否包含該字串 OR 名稱是否以該字串開頭
            bool pathMatch = fullPath.IndexOf(rule, StringComparison.OrdinalIgnoreCase) >= 0;
            bool nameMatch = name.StartsWith(rule, StringComparison.OrdinalIgnoreCase);

            if (pathMatch || nameMatch) return false;
        }

        // 2. 如果白名單是空的，預設為顯示
        if (group.showRules == null || group.showRules.Count == 0) return true;

        // 3. 檢查白名單 (不分大小寫)
        foreach (var rule in group.showRules)
        {
            if (string.IsNullOrEmpty(rule)) continue;

            bool pathMatch = fullPath.IndexOf(rule, StringComparison.OrdinalIgnoreCase) >= 0;
            bool nameMatch = name.StartsWith(rule, StringComparison.OrdinalIgnoreCase);

            if (pathMatch || nameMatch) return true;
        }

        return false;
    }

    private void UpdateGroupVisibility(UIGroup group, bool show, bool immediate)
    {
        if (group.canvasGroup == null) return;

        float targetAlpha = show ? 1f : 0f;

        // 停止之前的動畫，避免衝突
        group.canvasGroup.DOKill();

        if (immediate)
        {
            group.canvasGroup.alpha = targetAlpha;
            SetInteraction(group.canvasGroup, show);
        }
        else
        {
            group.canvasGroup.DOFade(targetAlpha, group.fadeDuration)
                .SetUpdate(true) // 確保在 TimeScale = 0 時也能運作
                .OnStart(() => {
                    // 如果是要顯示，動畫開始時就打開互動，避免卡住
                    if (show) SetInteraction(group.canvasGroup, true);
                })
                .OnComplete(() => {
                    // 如果是要隱藏，動畫結束後關閉互動
                    if (!show) SetInteraction(group.canvasGroup, false);
                });
        }
    }

    private void SetInteraction(CanvasGroup cg, bool enable)
    {
        cg.blocksRaycasts = enable;
        cg.interactable = enable;
    }
}