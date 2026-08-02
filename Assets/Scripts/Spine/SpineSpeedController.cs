// SpineSpeedController.cs
using UnityEngine;
using Spine.Unity;
using Spine;
using System.Collections.Generic;

/// <summary>
/// 提供一組 API 來控制 SkeletonAnimation 的播放速度。
/// 可控制全域 timeScale，或針對特定 Track 提供「持續性」的速度調整。
/// 這個腳本不處理任何 UI 事件，只負責接收指令並執行。
/// </summary>
public class SpineSpeedController : MonoBehaviour
{
    [Header("Spine 目標")]
    [SerializeField] private SkeletonAnimation skeleton;
    [Tooltip("true = 影響 SkeletonAnimation.timeScale；false = 僅影響指定 track 的 TrackEntry")]
    public bool affectGlobalTimeScale = true;
    [Tooltip("當只影響 Track 時，目標 Track Index")]
    public int trackIndex = 0;

    [Header("速度設定")]
    [Tooltip("加速時的倍率")]
    public float fastScale = 2f;
    [Tooltip("還原時的基礎倍率（通常是 1）")]
    public float baseScale = 1f;

    // --- 狀態註冊 (與舊版相同，靜態共享確保狀態一致) ---
    private static readonly HashSet<(SkeletonAnimation skel, int track)> TrackBoostOn = new();
    private static readonly Dictionary<SkeletonAnimation, bool> IsHooked = new();

    #region Unity生命週期
    void Reset()
    {
        skeleton = GetComponent<SkeletonAnimation>();
    }

    void Awake()
    {
        if (!skeleton) skeleton = GetComponent<SkeletonAnimation>();
        HookTrackStartIfNeeded(true); // 確保事件監聽已掛載
    }

    void OnDestroy()
    {
        HookTrackStartIfNeeded(false); // 在物件銷毀時嘗試卸載監聽
    }
    #endregion

    #region === 公開 API ===

    /// <summary>
    /// 將 Spine 動畫速度設定為指定的倍率。
    /// </summary>
    /// <param name="scale">目標速度倍率</param>
    public void SetSpeed(float scale)
    {
        if (!skeleton) return;
        float targetScale = Mathf.Max(0f, scale);

        if (affectGlobalTimeScale)
        {
            skeleton.timeScale = targetScale;
        }
        else
        {
            // 1) 立即影響當前 TrackEntry
            var entry = skeleton.AnimationState?.GetTrack(trackIndex);
            if (entry != null) entry.TimeScale = targetScale;

            // 2) 登記「之後新播的 entry 也要用此速度」
            // 注意：這裡簡化為只有 fastScale 才算 "BoostOn"，其他速度僅套用一次
            if (Mathf.Approximately(targetScale, fastScale))
            {
                TrackBoostOn.Add((skeleton, trackIndex));
            }
        }
    }

    /// <summary>
    /// 將 Spine 動畫速度設定為預設的 fastScale。
    /// </summary>
    public void SetFast()
    {
        SetSpeed(fastScale);
    }

    /// <summary>
    /// 將 Spine 動畫速度還原到初始的 baseScale。
    /// </summary>
    public void ResetSpeed()
    {
        if (!skeleton) return;

        if (affectGlobalTimeScale)
        {
            skeleton.timeScale = Mathf.Max(0f, baseScale);
        }
        else
        {
            // 1) 立即還原當前 TrackEntry
            var entry = skeleton.AnimationState?.GetTrack(trackIndex);
            if (entry != null) entry.TimeScale = Mathf.Max(0f, baseScale);

            // 2) 取消持續加速的登記
            TrackBoostOn.Remove((skeleton, trackIndex));
        }
    }

    #endregion

    #region === 內部邏輯 (持續性 Track 加速) ===

    private void HookTrackStartIfNeeded(bool ensure)
    {
        if (affectGlobalTimeScale || !Application.isPlaying) return;
        if (!skeleton || skeleton.AnimationState == null) return;

        if (ensure)
        {
            if (IsHooked.TryGetValue(skeleton, out bool hooked) && hooked) return;
            skeleton.AnimationState.Start += OnAnyEntryStart;
            IsHooked[skeleton] = true;
        }
        else
        {
            if (IsHooked.TryGetValue(skeleton, out bool hooked) && hooked)
            {
                skeleton.AnimationState.Start -= OnAnyEntryStart;
                IsHooked[skeleton] = false;
            }
        }
    }

    private void OnAnyEntryStart(TrackEntry entry)
    {
        if (entry == null || entry.TrackIndex != trackIndex) return;

        // 如果這個 track 被標記為需要持續加速，就將新 entry 的速度設為 fastScale
        if (TrackBoostOn.Contains((skeleton, entry.TrackIndex)))
        {
            entry.TimeScale = Mathf.Max(0f, fastScale);
        }
        // 注意：這裡沒有處理還原，因為還原操作是透過 ResetSpeed() 主動移除標記
    }
    #endregion
}