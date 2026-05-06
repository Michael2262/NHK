using UnityEngine;
using System.Collections;

/// <summary>
/// 極簡動畫播放：收到 GesturePressLogicProxy 的觸發後，
/// 播放指定動畫（位移、透明度等），播完後關閉指定的多個 GameObject。
/// 音效請直接在 Animation Clip 內設定。
/// </summary>
public class SimpleAniPlayOnPress : ConditionalPressReactionBase
{
    /*────────── 動畫設定 ──────────*/
    [Header("Animation Settings")]
    [Tooltip("目標 Animator，未指定則自動抓同物件上的")]
    public Animator targetAnimator;

    [Tooltip("要播放的 Animator Trigger 名稱")]
    public string triggerName;

    /*────────── 播完後關閉 ──────────*/
    [Header("Disable After Animation")]
    [Tooltip("動畫播完後要關閉的 GameObject 清單，留空則不關")]
    public GameObject[] objectsToDisable;

    private Coroutine _waitCoroutine;

    protected override void Awake()
    {
        base.Awake();
        if (!targetAnimator)
            targetAnimator = GetComponent<Animator>();
    }

    public override void OnTouched()
    {
        if (targetAnimator == null || string.IsNullOrEmpty(triggerName))
            return;

        targetAnimator.SetTrigger(triggerName);

        // 如果有指定要關閉的物件，等動畫播完再關
        if (objectsToDisable != null && objectsToDisable.Length > 0)
        {
            if (_waitCoroutine != null)
                StopCoroutine(_waitCoroutine);
            _waitCoroutine = StartCoroutine(WaitAndDisable());
        }
    }

    private IEnumerator WaitAndDisable()
    {
        // 等一幀讓 Animator 進入新的 State
        yield return null;

        // 取得當前播放的動畫資訊
        AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;

        // 等動畫播完
        yield return new WaitForSeconds(clipLength);

        foreach (var obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
        _waitCoroutine = null;
    }

    public override void WatchOut() { }
    public override void ResetToOriginal() { }
}