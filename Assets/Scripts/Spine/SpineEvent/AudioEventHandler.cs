using UnityEngine;
using Spine;
using Spine.Unity;

[RequireComponent(typeof(SkeletonAnimation))]
public class AudioEventHandler : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;

    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonAnimation.AnimationState.Event += HandleAnimationEvent;
    }

    void OnDestroy()
    {
        if (skeletonAnimation != null)
        {
            skeletonAnimation.AnimationState.Event -= HandleAnimationEvent;
        }
    }

    /// <summary>
    /// 處理 Spine 動畫事件的函式
    /// </summary>
    // [修改] 明確指定使用 Spine.TrackEntry 和 Spine.Event 來解決命名衝突
    private void HandleAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {
        // 這個腳本只關心 "playSound" 事件
        if (e.Data.Name == "playSound")
        {
            string soundKey = e.String;
            if (!string.IsNullOrEmpty(soundKey))
            {
                // 通知 AudioManager 播放音效
                AudioManager.Instance.PlaySound(soundKey);
            }
        }
    }
}