using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;

/// <summary>
/// 一個根據場景入口ID來決定播放哪個Spine動畫的任務。
/// </summary>
public class Task_PlaySpineByEntry : SceneReadyTaskBase
{
    // 使用一個可序列化的內部類別，方便在 Inspector 中設定
    [System.Serializable]
    public class EntryAnimation
    {
        public string entryID;
        [SpineAnimation(dataField: "spineAnimation")]
        public string animationName;
        public bool loop = false;
    }

    [Header("動畫目標")]
    [SerializeField] private SkeletonAnimation spineAnimation;
    [SerializeField] private int trackIndex = 0;

    [Header("入口與動畫的對應設定")]
    [Tooltip("設定每個入口ID對應要播放的動畫。")]
    [SerializeField] private List<EntryAnimation> entryAnimations;

    [Header("預設選項")]
    [Tooltip("如果傳入的入口ID在上面列表中找不到，則播放此預設動畫。")]
    [SpineAnimation(dataField: "spineAnimation")]
    [SerializeField] private string defaultAnimationName;

    private bool isAnimationComplete = false;

    public override IEnumerator ExecuteTask(string entryID)
    {
        string animToPlay = defaultAnimationName;
        bool shouldLoop = false;

        // 在列表中尋找與傳入的 entryID 相符的設定
        foreach (var entryAnim in entryAnimations)
        {
            if (entryAnim.entryID == entryID)
            {
                animToPlay = entryAnim.animationName;
                shouldLoop = entryAnim.loop;
                break;
            }
        }

        if (spineAnimation == null || string.IsNullOrEmpty(animToPlay))
        {
            yield break;
        }

        Debug.Log($"[SceneTask] 根據入口 '{entryID}'，播放 Spine 動畫: {animToPlay}");
        isAnimationComplete = false;
        var trackEntry = spineAnimation.AnimationState.SetAnimation(trackIndex, animToPlay, shouldLoop);

        if (shouldLoop)
        {
            yield break;
        }

        trackEntry.Complete += HandleAnimationComplete;
        yield return new WaitUntil(() => isAnimationComplete);
    }

    private void HandleAnimationComplete(Spine.TrackEntry trackEntry)
    {
        trackEntry.Complete -= HandleAnimationComplete;
        isAnimationComplete = true;
    }
}