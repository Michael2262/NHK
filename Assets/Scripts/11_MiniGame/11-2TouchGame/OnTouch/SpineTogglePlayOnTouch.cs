using UnityEngine;
using Spine;
using Spine.Unity;
using System; // (★ V2: 為了 Action<TrackEntry>)
using MySpineSystem; // (★ V3: 新增，為了 AnimationTrack Enum)

/// <summary>
/// (V6-V2 - SpineAnimationController 版本)
/// 透過 SpineAnimationController 播放動畫。
/// 第一次點擊播放 AnimationA，第二次點擊播放 AnimationB，之後循環；
/// 播放完停在終幀 (使用 ClearMode.KeepTrack + onComplete)。
/// 無論播放是否成功，都會通知 FSM。
/// </summary>
public class SpineTogglePlayOnTouch : ConditionalTouchReactionBase
{
    /*────────── Controller (★ V2) ──────────*/
    [Header("Controller (★ V2)")]
    [UnityEngine.Tooltip("Spine 動畫的核心控制器。留空 → 嘗試抓取本物件上的 SpineAnimationController")]
    public SpineAnimationController spineController;


    /*────────── Spine 設定 (Dropdown) ──────────*/
    [Header("Spine (for Dropdown)")]
    [UnityEngine.Tooltip("僅用於 SpineAnimation 下拉選單；留空 → 嘗試抓取本物件上的 SkeletonAnimation")]
    public SkeletonAnimation targetSkeleton; // 保留此欄位以便 [SpineAnimation] 屬性運作

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationAName;

    [SpineAnimation(dataField: "targetSkeleton")]
    public string animationBName;

    // (★ V3: 從 int trackIndex 改為 AnimationTrack enum)
    [UnityEngine.Tooltip("播放用 Track (來自 MySpineSystem.AnimationTrack)")]
    // public int trackIndex = 0; // (★ V3: 移除)
    public AnimationTrack track; // (★ V3: 改為 Enum)


    /*────────── Reset 設定 ──────────*/
    [Header("Reset")]
    [SpineAnimation(dataField: "targetSkeleton")]
    [UnityEngine.Tooltip("Reset 時要播放的動畫；留空 = ClearTrack ＆ SetupPose")]
    public string resetAnimationName;

    private bool _playNextA = true;


    /*────────── Mono ──────────*/
    void Awake()
    {
        // (★ V2: 優先抓取 Controller)
        if (!spineController)
            spineController = GetComponent<SpineAnimationController>();

        // (★ V2: 其次抓取 Skeleton 供下拉選單使用)
        if (!targetSkeleton)
            targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    /*────────── 觸發 (★ 關鍵修改 V2) ──────────*/

    /// <summary>
    /// 實作 基底類別的 OnTouched() 方法
    /// </summary>
    public override void OnTouched()
    {
        // --- 1. 執行這個腳本的核心工作：播放 Spine 動畫 (V2) ---

        if (!spineController)
        {
            Debug.LogWarning($"[SpineTogglePlayOnTouch2] 找不到 spineController，無法播放動畫 on {name}");
        }
        else
        {
            // --- 播放 Spine 動畫 (邏輯保留) ---
            string clip = _playNextA ? animationAName : animationBName;
            if (string.IsNullOrEmpty(clip))
            {
                Debug.LogWarning($"[SpineTogglePlayOnTouch2] Animation clip name is null or empty on {name}");
            }
            else
            {
                // 1. 準備 onComplete 動作 (停在最後一幀)
                Action<TrackEntry> freezeOnComplete = (entry) =>
                {
                    if (entry != null)
                        entry.TimeScale = 0;
                };

                // 2. 播放
                // (★ V3: 改為傳入 Enum，移除 int 強制轉型)
                spineController.PlayAnimation(
                    track, // (★ V3)
                    clip,
                    SpineAnimationController.ClearMode.KeepTrack, //
                    -1f,
                    freezeOnComplete
                );

                _playNextA = !_playNextA;   // 下一次換另一支
            }
        }

        // --- 2. (★ V6 邏輯保留) ---
        NotifyFSM();
    }

    /*────────── Reset (★ V2.1 修正版) ──────────*/

    [ContextMenu("Reset Animation State")]
    public override void ResetToOriginal()
    {
        if (!spineController)
            spineController = GetComponent<SpineAnimationController>();

        if (!spineController)
        {
            Debug.LogWarning($"[SpineTogglePlayOnTouch2] Reset failed: spineController is null on {name}");
            return;
        }

        // (★ V3: 改為傳入 Enum)
        spineController.StopAnimation(track);

        if (string.IsNullOrEmpty(resetAnimationName))
        {
            // --- 情況 1：沒有指定重置動畫 ---
            if (!targetSkeleton)
                targetSkeleton = GetComponent<SkeletonAnimation>();

            if (targetSkeleton != null && targetSkeleton.skeleton != null)
            {
                targetSkeleton.skeleton.SetToSetupPose();
                targetSkeleton.AnimationState.Apply(targetSkeleton.skeleton);
            }
        }
        else
        {
            // --- 情況 2：有指定重置動畫 (★ V2.1 修正) ---
            // (★ V3: 改為傳入 Enum)
            spineController.PlayAnimation(
                track,
                resetAnimationName,
                SpineAnimationController.ClearMode.ClearOnComplete, // 播放完就清除
                -1f,
                null
            );
        }

        _playNextA = true;

        // (Log 還是可以 cast 成 int 方便閱讀)
        Debug.Log($"[SpineTogglePlayOnTouch2] 已觸發 ResetToOriginal() on {name}在track {(int)track}");
    }
}