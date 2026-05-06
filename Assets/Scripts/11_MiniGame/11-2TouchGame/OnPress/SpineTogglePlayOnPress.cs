using UnityEngine;
using Spine;
using Spine.Unity;
using System;
using MySpineSystem;

/// <summary>
/// (V7 - 多來源支援版) 透過 SpineAnimationController 播放動畫。
/// 配合加強後的 ConditionalPressReactionBase 使用，支援多個 Proxy 同時操作。
/// </summary>
public class SpineTogglePlayOnPress : ConditionalPressReactionBase
{
    /*────────── Press Loop 設定 ──────────*/

    /*────────── Controller ──────────*/
    [Header("Controller")]
    [Tooltip("Spine 動畫控制器。留空則自動抓取同物件組件。")]
    public SpineAnimationController spineController;

    /*────────── Spine 設定 (Dropdown) ──────────*/
    [Header("Spine Settings")]
    [Tooltip("僅用於選單標記；留空則自動抓取同物件組件。")]
    public SkeletonAnimation targetSkeleton;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationAName;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationBName;

    [Tooltip("播放用 Track")]
    public AnimationTrack track;

    /*────────── Reset & WatchOut 設定 ──────────*/
    [Header("Reset & WatchOut")]

    [Tooltip("Reset 專用的 Track ")]
    public AnimationTrack resetTrack; 

    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("執行 ResetToOriginal 時播放的動畫，每次普通層動畫播完，會在resetTrack也撥放一次，確保其他動作消失。")]
    public string resetAnimationName;

    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("執行 WatchOut 時播放的動畫。")]
    public string watchOutAnimationName;

    private bool _playNextA = true;

    /*────────── Mono ──────────*/
    protected override void Awake()
    {
        base.Awake(); // 確保執行基類的 Awake

        if (!spineController) spineController = GetComponent<SpineAnimationController>();
        if (!targetSkeleton) targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    /*────────── 實作 ConditionalPressReactionBase ──────────*/


    /// <summary>
    /// 當邏輯被觸發時（無論是來自自身點擊、Proxy點擊或 AutoHover）。
    /// </summary>
    public override void OnTouched()
    {
        if (!spineController)
        {
            Debug.LogWarning($"[{name}] 找不到 spineController，無法播放。");
            return;
        }

        string clip = _playNextA ? animationAName : animationBName;

        if (!string.IsNullOrEmpty(clip))
        {
            // 動畫播放完成後停在最後一幀
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
            ResetSpineAnimation(resetTrack);

            _playNextA = !_playNextA;
        }
    }

    /// <summary>
    /// 突發狀況處理：停止邏輯並播放特定動畫。
    /// </summary>
    public override void WatchOut()
    {
        // 核心改動：必須呼叫 base.WatchOut() 來重置基類的 _pressCount 和狀態
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
        // 核心改動：必須呼叫 base.ResetToOriginal() 來清除基類的 _pressCount
        base.ResetToOriginal();

        ResetSpineAnimation(track);
        _playNextA = true;
    }

    /*────────── 內部輔助 ──────────*/

    private void ResetSpineAnimation(AnimationTrack useTrack)
    {
        if (!spineController) return;

        spineController.StopAnimation(useTrack);

        if (!string.IsNullOrEmpty(resetAnimationName))
        {
            spineController.PlayAnimation(
                useTrack,
                resetAnimationName,
                SpineAnimationController.ClearMode.ClearOnComplete,
                -1f,
                null
            );
        }
    }
}