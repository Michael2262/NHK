using UnityEngine;
using UnityEngine.UI; // 必要的 UI 命名空間
using Spine;
using Spine.Unity;
using System;
using MySpineSystem;

/// <summary>
/// (UI Button Hover 版) 透過 SpineAnimationController 播放動畫。
/// 繼承自 ConditionalHoverButtonReactionBase，因此支援 UI Button 的 Interactable 檢查。
/// 
/// 觸發邏輯：按下 (或 AutoHover) 時，依據設定的間隔時間循環觸發，在 A/B 動畫間切換。
/// 放開時：執行 ResetToOriginal (停止動畫並發送 Stop 事件)。
/// </summary>
[RequireComponent(typeof(Button))] // 強制要求 Button 組件
public class SpineTogglePlayOnHoverButton : ConditionalHoverButtonReactionBase
{
    /*────────── Hover 設定 ──────────*/
    [Header("Hover Loop Settings")]
    [UnityEngine.Tooltip("在長按/AutoHover 狀態下，每隔幾秒觸發一次 (切換 A/B)")]
    public float hoverLoopInterval = 1.0f;

    /*────────── Controller ──────────*/
    [Header("Controller")]
    [UnityEngine.Tooltip("Spine 動畫的核心控制器。留空 → 嘗試抓取本物件上的 SpineAnimationController")]
    public SpineAnimationController spineController;

    /*────────── Spine 設定 (Dropdown) ──────────*/
    [Header("Spine (for Dropdown)")]
    [UnityEngine.Tooltip("僅用於 SpineAnimation 下拉選單；留空 → 嘗試抓取本物件上的 SkeletonAnimation")]
    public SkeletonAnimation targetSkeleton;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationAName;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationBName;

    [UnityEngine.Tooltip("播放用 Track (來自 MySpineSystem.AnimationTrack)")]
    public AnimationTrack track;

    /*────────── Reset 設定 ──────────*/
    [Header("Reset")]
    [SpineAnimation(dataField: "targetSkeleton")]
    [UnityEngine.Tooltip("Reset 時要播放的動畫；留空 = ClearTrack ＆ SetupPose")]
    public string resetAnimationName;

    [Tooltip("當重置(放手/安全)時發送的 FSM 事件")]
    public string resetFsmEvent = "STOPHOVER";

    private bool _playNextA = true;

    

    /*────────── Mono ──────────*/
    protected override void Awake()
    {
        base.Awake(); // 記得呼叫基底的 Awake 以初始化 Button 緩存

        if (!spineController)
            spineController = GetComponent<SpineAnimationController>();

        if (!targetSkeleton)
            targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    /*────────── 實作 ConditionalHoverButtonReactionBase ──────────*/

    /// <summary>
    /// (1) 告訴基底類別，循環間隔是多久
    /// </summary>
    protected override float GetHoverInterval()
    {
        return hoverLoopInterval;
    }

    /// <summary>
    /// (2) 觸發反應：播放動畫 (A/B 切換)
    /// 注意：不需要在此手動呼叫 SendFsmEvent，基底類別會在呼叫此方法後自動發送 hoverTriggerEvent。
    /// </summary>
    public override void OnTouched()
    {
        // --- 播放 Spine 動畫邏輯 ---
        if (!spineController)
        {
            Debug.LogWarning($"[SpineTogglePlayOnHoverButton] 找不到 spineController，無法播放 on {name}");
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
    }

    /// <summary>
    /// (3) 重置反應：放開手或關閉 AutoHover 時呼叫
    /// </summary>
    public override void ResetToOriginal()
    {
        // 1. 執行基底重置 (停止計時、重置 Auto 狀態)
        base.ResetToOriginal();

        // 2. Spine 動畫重置邏輯
        ResetSpineAnimation();

        // 3. 重置 A/B 切換順序 (下次從 A 開始)
        _playNextA = true;

        // 4. 發送 FSM 安全/停止事件
        // (這部分基底類別不會自動做，必須手動發送)
        SendFsmEvent(resetFsmEvent);
    }

    

    // --- 內部輔助方法 ---
    private void ResetSpineAnimation()
    {
        if (!spineController) spineController = GetComponent<SpineAnimationController>();
        if (!spineController) return;

        spineController.StopAnimation(track);

        if (string.IsNullOrEmpty(resetAnimationName))
        {
            // 沒設定就回 SetupPose
            if (!targetSkeleton) targetSkeleton = GetComponent<SkeletonAnimation>();
            if (targetSkeleton != null && targetSkeleton.skeleton != null)
            {
                targetSkeleton.skeleton.SetToSetupPose();
                targetSkeleton.AnimationState.Apply(targetSkeleton.skeleton);
            }
        }
        else
        {
            // 有設定就播 Reset 動畫
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