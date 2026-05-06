using UnityEngine;
using Spine;
using Spine.Unity;
using System; // (為了 Action<TrackEntry>)
using MySpineSystem; // (★ V3: 新增，為了 AnimationTrack Enum)

/// <summary>
/// (基於 SpineTogglePlayOnTouch 的簡化版)
/// 透過 SpineAnimationController 播放 *單一* 動畫。
/// 每次點擊都播放指定的 animationName。
/// 播放完停在終幀 (使用 ClearMode.KeepTrack + onComplete)。
/// 無論播放是否成功，都會通知 FSM。
/// </summary>
public class SpinePlayOnTouch : ConditionalTouchReactionBase
{
    /*────────── Controller ──────────*/
    [Header("Controller")]
    [UnityEngine.Tooltip("Spine 動畫的核心控制器。留空 → 嘗試抓取本物件上的 SpineAnimationController")]
    public SpineAnimationController spineController;


    /*────────── Spine 設定 (Dropdown) ──────────*/
    [Header("Spine (for Dropdown)")]
    [UnityEngine.Tooltip("僅用於 SpineAnimation 下拉選單；留空 → 嘗試抓取本物件上的 SkeletonAnimation")]
    public SkeletonAnimation targetSkeleton; // 保留此欄位以便 [SpineAnimation] 屬性運作

    [SpineAnimation(dataField: "targetSkeleton")]
    [UnityEngine.Tooltip("要播放的單一動畫名稱")]
    public string animationName;

    // (★ V3: 從 int trackIndex 改為 AnimationTrack enum)
    [Header("Spine (Animation)")]
    [UnityEngine.Tooltip("播放用 Track (來自 MySpineSystem.AnimationTrack)")]
    public AnimationTrack track; // (★ V3: 改為 Enum)


    /*────────── Mono ──────────*/
    void Awake()
    {
        // 抓取 Controller
        if (!spineController)
            spineController = GetComponent<SpineAnimationController>();

        // 抓取 Skeleton 供下拉選單使用
        if (!targetSkeleton)
            targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    /*────────── 觸發 (★ 簡化版) ──────────*/

    /// <summary>
    /// 實作 基底類別的 OnTouched() 方法
    /// </summary>
    public override void OnTouched()
    {
        // --- 1. 執行這個腳本的核心工作：播放 Spine 動畫 ---

        if (!spineController)
        {
            // (邏輯保留：只是報錯，不 return)
            Debug.LogWarning($"[SpinePlayOnTouch] 找不到 spineController，無法播放動畫 on {name}");
        }
        else
        {
            // --- 播放 Spine 動畫 (簡化版：只播 animationName) ---
            if (string.IsNullOrEmpty(animationName))
            {
                // (邏輯保留：只是報錯，不 return)
                Debug.LogWarning($"[SpinePlayOnTouch] Animation clip name is null or empty on {name}");
            }
            else
            {
                // (★ 核心邏輯：與 Toggle 版相同)

                // 1. 準備 onComplete 動作 (停在最後一幀)
                Action<TrackEntry> freezeOnComplete = (entry) =>
                {
                    if (entry != null)
                        entry.TimeScale = 0;
                };

                // 2. 播放
                // (★ V3: 修改：直接傳入 enum，移除 int 強制轉型)
                spineController.PlayAnimation(
                    track,
                    animationName,
                    SpineAnimationController.ClearMode.KeepTrack, // 確保動畫停在最後
                    -1f, // default delay (不適用於 KeepTrack)
                    freezeOnComplete // 傳入 "freeze" 動作
                );
            }
        }

        // --- 2. (★ V6 邏輯保留) ---
        // 無論 Spine 是否播放成功，都執行 NotifyFSM()
        NotifyFSM();
    }
}