using System;
using UnityEngine;
using MySpineSystem;

/// <summary>
/// 分軌 Bridge 的共用轉接層；目標引用優先，未指定時依 ID 查找。
/// 共用方法不公開，避免出現在 UnityEvent 的方法選單。
/// </summary>
public abstract class SpineAniTrackBridgeBase : MonoBehaviour
{
    [Header("動畫目標（引用優先）")]
    [SerializeField] private SpineAnimationController controller;
    [Tooltip("未指定 Controller 引用時使用。目標必須已啟用並註冊此 ID。")]
    [SerializeField] private string controllerID;

    protected void PlayTrack(AnimationTrack track, string animationName,
        SpineAnimationController.ClearMode mode, float delaySeconds)
    {
        if (string.IsNullOrWhiteSpace(animationName))
        {
            Debug.LogWarning($"[{GetType().Name}] 尚未指定動畫名稱。", this);
            return;
        }

        var target = ResolveController();
        if (target == null) return;

        target.Initialize();
        var state = target.GetAnimationState();
        if (state == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Controller 尚未取得 AnimationState。", this);
            return;
        }
        if (state.Data.SkeletonData.FindAnimation(animationName) == null)
        {
            Debug.LogWarning($"[{GetType().Name}] 找不到動畫 '{animationName}'。", this);
            return;
        }

        target.PlayAnimation(track, animationName, mode, delaySeconds);
    }

    protected void StopTrack(AnimationTrack track)
    {
        var target = ResolveController();
        if (target == null) return;

        // 先停止使用同軌的清單協程，避免清軌後又自動播放下一段。
        var list = target.GetComponent<SpinePlayByList>();
        if (list != null && list.IsPlaying && list.groups != null)
        {
            var current = list.groups.Find(g => g != null &&
                string.Equals(g.groupName, list.CurrentGroupName, StringComparison.Ordinal));
            if (current != null && current.track == track) list.StopPlaying();
        }

        target.Initialize();
        target.StopAnimation(track);
    }

    private SpineAnimationController ResolveController()
    {
        if (controller != null) return controller;
        if (!string.IsNullOrWhiteSpace(controllerID))
            return SpineAnimationController.GetByID(controllerID);

        Debug.LogWarning($"[{GetType().Name}] 請拖入 SpineAnimationController 或填寫 Controller ID。", this);
        return null;
    }
}
