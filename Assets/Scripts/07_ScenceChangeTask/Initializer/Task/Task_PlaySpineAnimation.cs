using UnityEngine;
using System.Collections;
using Spine;
using Spine.Unity;

/// <summary>
/// 【簡化版】一個專門用於播放「單一指定」Spine 動畫並等待其完成的任務。
/// 它不處理任何根據入口ID變化的邏輯。
/// </summary>
public class Task_PlaySpineAnimation : SceneReadyTaskBase
{
    [Header("Spine 動畫設定")]
    [SerializeField] private SkeletonAnimation spineAnimation;
    [SpineAnimation(dataField: "spineAnimation")]
    [SerializeField] private string animationName;
    [SerializeField] private int trackIndex = 0;
    [SerializeField] private bool loop = false;

    private bool isAnimationComplete = false;

    // 接收 entryID 參數但完全不使用它
    public override IEnumerator ExecuteTask(string entryID)
    {
        if (spineAnimation == null || string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[SceneTask] Spine 動畫未設定: {gameObject.name}");
            yield break;
        }

        Debug.Log($"[SceneTask] 正在播放固定 Spine 動畫: {animationName}");
        isAnimationComplete = false;

        TrackEntry trackEntry = spineAnimation.state.SetAnimation(trackIndex, animationName, loop);

        if (loop)
        {
            yield break;
        }

        trackEntry.Complete += HandleAnimationComplete;
        yield return new WaitUntil(() => isAnimationComplete);
        Debug.Log($"[SceneTask] Spine 動畫播放完畢: {animationName}");
    }

    private void HandleAnimationComplete(TrackEntry trackEntry)
    {
        trackEntry.Complete -= HandleAnimationComplete;
        isAnimationComplete = true;
    }
}