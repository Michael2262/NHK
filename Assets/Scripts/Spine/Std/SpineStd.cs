// Assets/Scripts/Spine/Std/SpineStd.cs
using UnityEngine;
using Spine;
using Spine.Unity;

public static class SpineStd
{
    /// <summary>
    /// 標準播放：一律非 loop；播放完成自動 ClearTrack。
    /// 完成後會先清 track 再呼叫 onAfterCleared（可選）。
    /// 回傳 TrackEntry（如需額外監聽可再綁）。
    /// </summary>
    public static TrackEntry PlayNonLoopCleanTrack(
        SkeletonAnimation skel,
        int trackIndex,
        string animationName,
        System.Action<TrackEntry> onAfterCleared = null)
    {
        if (!skel)
        {
            Debug.LogWarning("[SpineStd] SkeletonAnimation 為空，無法播放");
            return null;
        }

        var state = skel.AnimationState;
        var anim = skel.Skeleton?.Data?.FindAnimation(animationName);
        if (anim == null)
        {
            Debug.LogWarning($"[SpineStd] 找不到動畫：{animationName}");
            return null;
        }

        var entry = state.SetAnimation(trackIndex, anim, false);

        // 避免重複綁定：先解一次，再綁一次
        entry.Complete -= HandleComplete;
        entry.Complete += HandleComplete;

        void HandleComplete(TrackEntry e)
        {
            if (e == null || e.TrackIndex != trackIndex) return;
            state.ClearTrack(trackIndex);            // 專案規範：播完清掉，不凍結最後幀
            onAfterCleared?.Invoke(e);

            // 解綁避免多次呼叫
            e.Complete -= HandleComplete;
        }

        return entry;
    }

    /// <summary>
    /// Reset 到 SetupPose（含 Apply），等同「清該 track 並回到初始姿態」。
    /// </summary>
    public static void ClearTrackAndSetupPose(SkeletonAnimation skel, int trackIndex)
    {
        if (!skel) return;
        skel.AnimationState.ClearTrack(trackIndex);
        skel.AnimationState.Apply(skel.Skeleton);
    }

    /// <summary>
    /// 只切換該物件（或含子物件）上的所有 Collider2D.enabled；不動 SetActive。
    /// </summary>
    public static void SetColliders2DEnabled(GameObject go, bool enabled, bool includeChildren = false)
    {
        if (!go) return;

        if (includeChildren)
        {
            var colsAll = go.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colsAll.Length; i++) colsAll[i].enabled = enabled;
        }
        else
        {
            var cols = go.GetComponents<Collider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = enabled;
        }
    }
}
