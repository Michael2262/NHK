using UnityEngine;
using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using MySpineSystem;

/// <summary>
/// 順序播放清單模式：每點一次播放清單中的當前動畫，
/// 當該動畫的點擊次數達到設定的 loopCount 後，自動切換到下一個動畫。
/// 整個清單播完後回到第一個，無限循環。
/// 
/// 範例：A(3次) → B(2次) → C(1次) → 回到 A(3次) → ...
/// 點擊順序：A, A, A, B, B, C, A, A, A, B, B, C, ...
/// </summary>
public class SpineListPlayOnPress : ConditionalPressReactionBase
{
    /*────────── Controller ──────────*/
    [Header("Controller")]
    [Tooltip("Spine 動畫控制器。未填會自動抓取同物件上的。")]
    public SpineAnimationController spineController;

    /*────────── Spine 設定 ──────────*/
    [Header("Spine Settings")]
    [Tooltip("骨架來源（供下拉選單使用）；未填會自動抓取同物件上的。")]
    public SkeletonAnimation targetSkeleton;

    [Tooltip("播放用 Track")]
    public AnimationTrack track;

    /*────────── 動畫清單 ──────────*/
    [Header("Animation List")]
    [Tooltip("依序播放的動畫清單。每個項目可設定動畫名稱與需要點擊的次數。")]
    public List<AnimationEntry> animationList = new List<AnimationEntry>();

    [Serializable]
    public class AnimationEntry
    {
        [SpineAnimation(dataField: "targetSkeleton")]
        [Tooltip("要播放的動畫名稱")]
        public string animationName;

        [Min(1)]
        [Tooltip("此動畫需要點擊幾次才會切換到下一個（最少 1）")]
        public int loopCount = 1;
    }

    /*────────── Reset & WatchOut 設定 ──────────*/
    [Header("Reset & WatchOut")]
    [Tooltip("Reset 專用的 Track (預設為 BodyAttach)")]
    public AnimationTrack resetTrack = AnimationTrack.BodyAttach; 

    [Header("Reset & WatchOut")]
    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("執行 ResetToOriginal 時播放的動畫。")]
    public string resetAnimationName;

    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("執行 WatchOut 時播放的動畫。")]
    public string watchOutAnimationName;

    /*────────── 內部狀態 ──────────*/
    /// <summary>目前播放到清單中的第幾個動畫（index）</summary>
    private int _currentIndex = 0;

    /// <summary>目前這個動畫已經被點擊了幾次</summary>
    private int _currentClickCount = 0;

    /*────────── Mono ──────────*/
    protected override void Awake()
    {
        base.Awake();

        if (!spineController) spineController = GetComponent<SpineAnimationController>();
        if (!targetSkeleton) targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    /*────────── 實作 ConditionalPressReactionBase ──────────*/


    /// <summary>
    /// 當邏輯被觸發時（無論是來自直接點擊、Proxy點擊或 AutoHover）。
    /// </summary>
    public override void OnTouched()
    {
        if (!spineController)
        {
            Debug.LogWarning($"[{name}] 找不到 spineController，無法播放。");
            return;
        }

        if (animationList == null || animationList.Count == 0)
        {
            Debug.LogWarning($"[{name}] animationList 為空，無法播放。");
            return;
        }

        // 安全檢查：確保 index 在範圍內
        if (_currentIndex >= animationList.Count)
        {
            _currentIndex = 0;
            _currentClickCount = 0;
        }

        var current = animationList[_currentIndex];
        string clip = current.animationName;

        if (!string.IsNullOrEmpty(clip))
        {
            // 動畫播放完成後凍結在最後一幀
            Action<TrackEntry> freezeOnComplete = (entry) =>
            {
                if (entry != null) entry.TimeScale = 0;
            };

            spineController.PlayAnimation(
                track,
                clip,
                SpineAnimationController.ClearMode.ClearOnComplete,
                -1f,
                freezeOnComplete
            );
        }
        ResetSpineAnimation();

        // 累計點擊次數，達到 loopCount 後切換到下一個動畫
        _currentClickCount++;
        if (_currentClickCount >= current.loopCount)
        {
            _currentIndex = (_currentIndex + 1) % animationList.Count;
            _currentClickCount = 0;
        }
    }

    /// <summary>
    /// 警戒狀態處理：停止邏輯並播放特定動畫。
    /// </summary>
    public override void WatchOut()
    {
        base.WatchOut();

        if (spineController != null && !string.IsNullOrEmpty(watchOutAnimationName))
        {
            spineController.PlayAnimation(
                track,
                watchOutAnimationName,
                SpineAnimationController.ClearMode.ClearOnComplete,
                -1f,
                null
            );
        }
    }

    public override void ResetToOriginal()
    {
        base.ResetToOriginal();

        ResetSpineAnimation();
        _currentIndex = 0;
        _currentClickCount = 0;
    }

    /*────────── 內部輔助 ──────────*/

    private void ResetSpineAnimation()
    {
        if (!spineController) return;

        // 修改：使用獨立的 resetTrack 進行停止與播放
        spineController.StopAnimation(resetTrack);

        if (!string.IsNullOrEmpty(resetAnimationName))
        {
            spineController.PlayAnimation(
                resetTrack,
                resetAnimationName,
                SpineAnimationController.ClearMode.ClearOnComplete,
                -1f,
                null
            );
        }
    }
}