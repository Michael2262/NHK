using System;
using UnityEngine;
using MySpineSystem;

/// <summary>
/// 單段 Spine 動畫的 UnityEvent 入口。優先使用拖入的 Controller，否則在呼叫時依 ID 查找。
/// 軌道及播放模式在 Inspector 設定，播放操作交由 Controller 執行。
/// </summary>
[AddComponentMenu("Spine/Bridge/Spine Ani Bridge")]
public class SpineAniBridge : MonoBehaviour
{
    [Header("動畫目標（引用優先）")]
    [SerializeField] private SpineAnimationController controller;
    [Tooltip("未指定 Controller 引用時使用。目標必須已啟用並註冊此 ID。")]
    [SerializeField] private string controllerID;

    [Header("單段動畫設定")]
    [SerializeField] private AnimationTrack track = AnimationTrack.Body;
    [SerializeField] private string animationName;
    [SerializeField] private SpineAnimationController.ClearMode clearMode =
        SpineAnimationController.ClearMode.ClearOnComplete;
    [Tooltip("ClearAfterDelay 模式使用；負數代表使用 Controller 的預設延遲。")]
    [SerializeField] private float clearDelaySeconds = -1f;

    /// <summary>播放 Inspector 設定的動畫。</summary>
    public void PlayConfiguredAnimation()
    {
        PlayAnimation(animationName);
    }

    /// <summary>UnityEvent 傳入動畫名稱；軌道、模式與延遲使用 Inspector 設定。</summary>
    public void PlayAnimation(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning($"[{nameof(SpineAniBridge)}] 尚未指定動畫名稱。", this);
            return;
        }

        var target = ResolveController();
        if (target == null) return;
        target.Initialize();
        var state = target.GetAnimationState();
        if (state == null)
        {
            Debug.LogWarning($"[{nameof(SpineAniBridge)}] Controller 尚未取得 AnimationState。", this);
            return;
        }
        if (state.Data.SkeletonData.FindAnimation(name) == null)
        {
            Debug.LogWarning($"[{nameof(SpineAniBridge)}] 找不到動畫 '{name}'。", this);
            return;
        }

        target.PlayAnimation(track, name, clearMode, clearDelaySeconds);
    }

    /// <summary>停止設定的軌道；同物件上的清單若正在使用該軌道，也先停止其協程。</summary>
    public void StopAnimation()
    {
        var target = ResolveController();
        if (target == null) return;

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

    /// <summary>停止同物件的清單，並清除目標 Controller 的全部軌道。</summary>
    public void StopAll()
    {
        var target = ResolveController();
        if (target == null) return;

        // 單純清軌不會停止清單協程，因此先停止清單，避免之後又播放下一段。
        var list = target.GetComponent<SpinePlayByList>();
        if (list != null) list.StopPlaying();
        target.Initialize();
        target.StopAll();
    }

    private SpineAnimationController ResolveController()
    {
        if (controller != null) return controller;
        if (!string.IsNullOrWhiteSpace(controllerID))
            return SpineAnimationController.GetByID(controllerID);

        Debug.LogWarning($"[{nameof(SpineAniBridge)}] 請拖入 SpineAnimationController 或填寫 Controller ID。", this);
        return null;
    }
}
