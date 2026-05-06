using UnityEngine;
using Spine;
using Spine.Unity;
using System;
using HutongGames.PlayMaker;
using MySpineSystem;
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 點擊時僅向指定的 PlayMaker FSM 發送事件，
/// 動畫播放完全由 FSM 內部邏輯觸發。
/// 本腳本儲存至多 4 組 Spine 動畫供 FSM 透過公開方法呼叫。
/// </summary>
public class CallFSMPlaySpineOnPress : ConditionalPressReactionBase
{
    /*══════════════ Press Loop 設定 ══════════════*/
    [Header("Press Loop Settings")]
    [Tooltip("在長按/Auto 狀態下，每隔幾秒自動觸發一次")]
    public float pressLoopInterval = 1.0f;

    /*══════════════ FSM 設定 ══════════════*/
    [Header("FSM Settings")]
    [Tooltip("要接收事件的 PlayMaker FSM 所在的 GameObject。未填則使用自身物件。")]
    public GameObject fsmGameObject;

    [Tooltip("指定 FSM 名稱（即 PlayMakerFSM 的 FsmName）。留空則使用該物件上的第一個 FSM。")]
    public string fsmName;

    [Tooltip("點擊時發送給 FSM 的事件名稱")]
    public string fsmTriggerEvent = "TRIGGER";

    private PlayMakerFSM _resolvedFSM;

    /*══════════════ Controller ══════════════*/
    [Header("Controller")]
    [Tooltip("Spine 動畫控制器。未填會自動抓取同物件上的。")]
    public SpineAnimationController spineController;

    /*══════════════ Spine 設定 ══════════════*/
    [Header("Spine Settings")]
    [Tooltip("骨架來源（供下拉選單使用）；未填會自動抓取同物件上的。")]
    public SkeletonAnimation targetSkeleton;

    [Tooltip("播放用 Track")]
    public AnimationTrack track;

    /*══════════════ 動畫儲存槽（供 FSM 使用）══════════════*/
    [Header("Animation Slots (供 FSM 呼叫)")]
    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("動畫槽 0")]
    public string animation0;

    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("動畫槽 1")]
    public string animation1;

    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("動畫槽 2")]
    public string animation2;

    [SpineAnimation(dataField: "targetSkeleton")]
    [Tooltip("動畫槽 3")]
    public string animation3;

    /*══════════════ Mono ══════════════*/
    protected override void Awake()
    {
        base.Awake();

        // 只在 Inspector 完全沒指定時才 fallback 到自身物件
        if (spineController == null) spineController = GetComponent<SpineAnimationController>();
        if (targetSkeleton == null) targetSkeleton = GetComponent<SkeletonAnimation>();

        if (spineController == null)
            Debug.LogWarning($"[{name}] Awake 後 spineController 仍為 null，請確認 Inspector 是否有正確拖入。");

        ResolveFSM();
    }

    private void ResolveFSM()
    {
        GameObject target = fsmGameObject != null ? fsmGameObject : gameObject;

        if (string.IsNullOrEmpty(fsmName))
        {
            // 未指定名稱 → 取第一個
            _resolvedFSM = target.GetComponent<PlayMakerFSM>();
        }
        else
        {
            // 依名稱比對
            var allFSMs = target.GetComponents<PlayMakerFSM>();
            foreach (var fsm in allFSMs)
            {
                if (string.Equals(fsm.FsmName, fsmName, StringComparison.Ordinal))
                {
                    _resolvedFSM = fsm;
                    break;
                }
            }
        }

        if (_resolvedFSM == null)
        {
            string targetName = target.name;
            string detail = string.IsNullOrEmpty(fsmName) ? "任意 FSM" : $"FSM 名稱 '{fsmName}'";
            Debug.LogWarning($"[{name}] 在 '{targetName}' 上找不到{detail}。");
        }
    }

    /*══════════════ 實作 ConditionalPressReactionBase ══════════════*/

    protected override float GetHoverInterval() => pressLoopInterval;

    /// <summary>
    /// 點擊觸發：僅向指定 FSM 發送事件，不直接播放動畫。
    /// </summary>
    public override void OnTouched()
    {
        if (_resolvedFSM == null)
        {
            Debug.LogWarning($"[{name}] 找不到目標 FSM，無法發送事件。");
            return;
        }

        if (!string.IsNullOrEmpty(fsmTriggerEvent))
        {
            _resolvedFSM.SendEvent(fsmTriggerEvent);
        }
    }

    /*══════════════ 公開方法：供 FSM 呼叫播放動畫 ══════════════*/

    /// <summary>
    /// 依照 slot 編號 (0~3) 播放對應的動畫。
    /// 可在 PlayMaker 中透過 CallMethod 或 SendMessage 呼叫。
    /// </summary>
    public void PlaySlot(int slotIndex)
    {
        string clip = GetAnimationBySlot(slotIndex);

        if (string.IsNullOrEmpty(clip))
        {
            Debug.LogWarning($"[{name}] 動畫槽 {slotIndex} 為空或索引超出範圍。");
            return;
        }

        PlayClip(clip, null);
    }

    /// <summary>
    /// 播放指定 slot 的動畫，播完後凍結在最後一幀。
    /// </summary>
    public void PlaySlotAndFreeze(int slotIndex)
    {
        string clip = GetAnimationBySlot(slotIndex);

        if (string.IsNullOrEmpty(clip))
        {
            Debug.LogWarning($"[{name}] 動畫槽 {slotIndex} 為空或索引超出範圍。");
            return;
        }

        PlayClip(clip, (entry) =>
        {
            if (entry != null) entry.TimeScale = 0;
        });
    }

    /// <summary>
    /// 直接以動畫名稱播放（不透過 slot）。
    /// </summary>
    public void PlayByName(string animationName)
    {
        if (string.IsNullOrEmpty(animationName)) return;
        PlayClip(animationName, null);
    }

    /// <summary>
    /// 取得指定 slot 的動畫名稱。
    /// </summary>
    public string GetAnimationBySlot(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return animation0;
            case 1: return animation1;
            case 2: return animation2;
            case 3: return animation3;
            default: return null;
        }
    }

    /// <summary>
    /// 停止目前 track 上的動畫。
    /// </summary>
    public void StopCurrentAnimation()
    {
        if (spineController != null)
            spineController.StopAnimation(track);
    }

    /*══════════════ 父類必要覆寫（最小實作）══════════════*/

    public override void WatchOut()
    {
        base.WatchOut();
        // 此腳本不主動處理 WatchOut 動畫，由 FSM 決定
    }

    public override void ResetToOriginal()
    {
        base.ResetToOriginal();
        // 此腳本不主動處理 Reset 動畫，由 FSM 決定
    }

    /*══════════════ 內部輔助 ══════════════*/

    private void PlayClip(string clip, Action<TrackEntry> onComplete)
    {
        if (spineController == null)
        {
            Debug.LogWarning($"[{name}] 找不到 spineController，無法播放 '{clip}'。" +
                $"\n  → 請確認 Inspector 中 spineController 欄位是否有拖入，且目標物件未被 Destroy。");
            return;
        }

        spineController.PlayAnimation(
            track,
            clip,
            SpineAnimationController.ClearMode.ClearOnComplete,
            -1f,
            onComplete
        );
    }
}