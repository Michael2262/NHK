using UnityEngine;
using Spine;
using Spine.Unity;
using System;
using MySpineSystem;

/// <summary>
/// (Hover 版) 透過 SpineAnimationController 播放動畫。
/// 修正版：移除不存在的 NotifyFSM，改由基底類別自動處理事件發送。
/// </summary>
public class SpineTogglePlayOnHover : ConditionalHoverReactionBase
{
    /*────────── Hover 設定 ──────────*/
    [Header("Hover Loop Settings")]
    [UnityEngine.Tooltip("在長按/AutoHover 狀態下，每隔幾秒觸發一次 (切換 A/B)")]
    public float hoverLoopInterval = 1.0f;

    /*────────── Controller ──────────*/
    [Header("Controller")]
    [UnityEngine.Tooltip("Spine 動畫的核心控制器。")]
    public SpineAnimationController spineController;

    /*────────── Spine 設定 (Dropdown) ──────────*/
    [Header("Spine (for Dropdown)")]
    public SkeletonAnimation targetSkeleton;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationAName;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationBName;

    [UnityEngine.Tooltip("播放用 Track")]
    public AnimationTrack track;

    /*────────── Reset 設定 ──────────*/
    [Header("Reset")]
    [SpineAnimation(dataField: "targetSkeleton")]
    public string resetAnimationName;

    [Tooltip("當重置(放手/安全)時發送的 FSM 事件")]
    public string resetFsmEvent = "STOPHOVER";

    private bool _playNextA = true;

    /*────────── Mono ──────────*/
    protected override void Awake()
    {
        base.Awake(); // 記得呼叫基底類別的 Awake 以抓取 Button 組件
        if (!spineController)
            spineController = GetComponent<SpineAnimationController>();

        if (!targetSkeleton)
            targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    /*────────── 實作 ConditionalHoverReactionBase ──────────*/

    protected override float GetHoverInterval()
    {
        return hoverLoopInterval;
    }

    /// <summary>
    /// 觸發反應：播放動畫 (A/B 切換)
    /// 注意：基底類別的 ExecuteTrigger 會在執行完 OnTouched 後自動發送 FSM 事件。
    /// </summary>
    public override void OnTouched()
    {
        if (!spineController)
        {
            Debug.LogWarning($"[SpineTogglePlayOnHover] 找不到 spineController on {name}");
            return;
        }

        string clip = _playNextA ? animationAName : animationBName;

        if (!string.IsNullOrEmpty(clip))
        {
            // 準備 onComplete 動作 (停在最後一幀)
            Action<TrackEntry> freezeOnComplete = (entry) =>
            {
                if (entry != null)
                    entry.TimeScale = 0;
            };

            // 播放
            spineController.PlayAnimation(
                track,
                clip,
                SpineAnimationController.ClearMode.KeepTrack,
                -1f,
                freezeOnComplete
            );

            // 切換下一次要播的動畫
            _playNextA = !_playNextA;
        }

        // --- 原本的 NotifyFSM(); 已刪除 ---
        // 因為基底類別的 ExecuteTrigger() 會自動呼叫 SendFsmEvent(hoverTriggerEvent);
    }

    public override void ResetToOriginal()
    {
        // 1. 執行基底重置 (停止 Loop、重置狀態)
        base.ResetToOriginal();

        // 2. Spine 動畫重置邏輯
        ResetSpineAnimation();

        // 3. 重置 A/B 切換順序
        _playNextA = true;

        // 4. 發送停止/安全事件
        SendFsmEvent(resetFsmEvent);
    }

    private void ResetSpineAnimation()
    {
        if (!spineController) spineController = GetComponent<SpineAnimationController>();
        if (!spineController) return;

        spineController.StopAnimation(track);

        if (string.IsNullOrEmpty(resetAnimationName))
        {
            if (!targetSkeleton) targetSkeleton = GetComponent<SkeletonAnimation>();
            if (targetSkeleton != null && targetSkeleton.skeleton != null)
            {
                targetSkeleton.skeleton.SetupPose();
                targetSkeleton.AnimationState.Apply(targetSkeleton.skeleton);
            }
        }
        else
        {
            spineController.PlayAnimation(
                track,
                resetAnimationName,
                SpineAnimationController.ClearMode.ClearOnComplete,
                -1f,
                null
            );
        }
    }
}