using UnityEngine;
using Spine;
using Spine.Unity;

public class SpinePlayNonLoopClean : MonoBehaviour
{
    [Header("Spine")]
    public SkeletonAnimation targetSkeleton;
    [SpineAnimation(dataField: nameof(targetSkeleton))]
    public string animationName;
    public int trackIndex = 0;

    [Header("UX (可選)")]
    public GameObject showOnPlay;
    public GameObject hideOnEnd;
    public bool closeShowOnEnd = false;
    [Min(0)] public float cooldown = 0.3f;

    private TrackEntry _entry;
    private float _nextTime;

    void Awake()
    {
        if (!targetSkeleton) targetSkeleton = GetComponent<SkeletonAnimation>();
    }

    public void PlayOnce()
    {
        if (!targetSkeleton || string.IsNullOrEmpty(animationName)) return;
        if (Time.time < _nextTime) return;
        _nextTime = Time.time + cooldown;

        if (showOnPlay) showOnPlay.SetActive(true);

        var state = targetSkeleton.AnimationState;
        _entry = state.SetAnimation(trackIndex, animationName, false); // 一律非 loop
        _entry.Complete -= OnComplete;   // 先解綁，防重複
        _entry.Complete += OnComplete;   // 播完只清 Track
    }

    private void OnComplete(TrackEntry e)
    {
        if (!targetSkeleton || e.TrackIndex != trackIndex) return;
        targetSkeleton.AnimationState.ClearTrack(trackIndex); // 核心：只清 Track
        e.Complete -= OnComplete;

        if (closeShowOnEnd && showOnPlay) showOnPlay.SetActive(false);
        if (hideOnEnd) hideOnEnd.SetActive(false);
        _entry = null;
    }

    // 需要回初始時才呼叫
    public void ResetPose()
    {
        if (!targetSkeleton) return;
        var state = targetSkeleton.AnimationState;
        var skeleton = targetSkeleton.Skeleton;
        state.ClearTrack(trackIndex);
        skeleton.SetupPose();
        state.Apply(skeleton);
    }
}
