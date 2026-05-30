using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在「不卸載的 Scene」上,負責操控一個指定的 Animator。
/// 透過 controllerId 註冊到靜態表,讓 Dialogue System 的 SequencerCommand
/// 或 PlayMaker 的 FSM Action 可以用 ID 找到並控制它。
///
/// Animator 內需事先綁好的 Trigger 名稱請見 EmotionAnimatorTrigger.cs。
/// </summary>
public class AnimatorEmotionController : MonoBehaviour
{
    [Header("目標 Animator")]
    [Tooltip("要被控制的 Animator;留空會自動抓同物件上的 Animator。")]
    public Animator targetAnimator;

    [Header("識別 ID")]
    [Tooltip("外部 (SequencerCommand / FSM Action) 用這個 ID 找到此控制器。")]
    public string controllerId = "Main";

    [Header("行為設定")]
    [Tooltip("觸發情緒前先清掉其他情緒 Trigger,避免殘留的 Trigger 造成多餘的轉場。")]
    public bool resetOtherEmotionsFirst = true;

    // 哪些 Trigger 屬於「情緒」(觸發前要清掉其他情緒)
    private static readonly HashSet<EmotionAnimatorTrigger> EmotionSet = new HashSet<EmotionAnimatorTrigger>
    {
        EmotionAnimatorTrigger.Angry,
        EmotionAnimatorTrigger.Shy,
        EmotionAnimatorTrigger.Worried,
        EmotionAnimatorTrigger.Maternal,
        EmotionAnimatorTrigger.Relaxed,
        EmotionAnimatorTrigger.Disappointed,
    };

    private static readonly Dictionary<string, AnimatorEmotionController> Registry =
        new Dictionary<string, AnimatorEmotionController>();

    // 快取 Trigger 的 hash,效能較佳
    private readonly Dictionary<EmotionAnimatorTrigger, int> _hashCache = new Dictionary<EmotionAnimatorTrigger, int>();

    private void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();
    }

    private void OnEnable() => Register();
    private void OnDisable() => Unregister();

    private void Register()
    {
        if (string.IsNullOrEmpty(controllerId)) return;
        Registry[controllerId] = this;
    }

    private void Unregister()
    {
        if (string.IsNullOrEmpty(controllerId)) return;
        if (Registry.TryGetValue(controllerId, out var existing) && existing == this)
            Registry.Remove(controllerId);
    }

    /// <summary>用 ID 取得已註冊的控制器;找不到回傳 null。</summary>
    public static AnimatorEmotionController Get(string id)
    {
        if (string.IsNullOrEmpty(id)) id = "Main";
        Registry.TryGetValue(id, out var ctrl);
        return ctrl;
    }

    /// <summary>判斷某個 Trigger 是否屬於情緒。</summary>
    public static bool IsEmotion(EmotionAnimatorTrigger t) => EmotionSet.Contains(t);

    private int GetHash(EmotionAnimatorTrigger t)
    {
        if (!_hashCache.TryGetValue(t, out var hash))
        {
            hash = Animator.StringToHash(t.ToString());
            _hashCache[t] = hash;
        }
        return hash;
    }

    // ---------- 對外 API ----------

    /// <summary>
    /// 通用入口:傳入任何 EmotionAnimatorTrigger。
    /// 情緒會走 PlayEmotion (含清除其他情緒);Think / Stop 則直接觸發。
    /// 外部系統 (Sequencer / FSM) 統一呼叫這個就好。
    /// </summary>
    public void Trigger(EmotionAnimatorTrigger t)
    {
        if (IsEmotion(t))
            PlayEmotion(t);
        else
            SetTrigger(t);
    }

    /// <summary>觸發指定情緒 (會視設定先清掉其他情緒)。</summary>
    public void PlayEmotion(EmotionAnimatorTrigger emotion)
    {
        if (resetOtherEmotionsFirst)
            ResetAllEmotions();

        SetTrigger(emotion);
    }

    /// <summary>觸發 Think。</summary>
    public void Think() => SetTrigger(EmotionAnimatorTrigger.Think);

    /// <summary>觸發 Stop。</summary>
    public void Stop() => SetTrigger(EmotionAnimatorTrigger.Stop);

    /// <summary>觸發任意 Trigger (底層)。</summary>
    public void SetTrigger(EmotionAnimatorTrigger t)
    {
        if (targetAnimator == null)
        {
            Debug.LogWarning($"[AnimatorEmotionController:{controllerId}] targetAnimator 未設定,無法觸發 '{t}'。", this);
            return;
        }
        targetAnimator.SetTrigger(GetHash(t));
    }

    /// <summary>清掉所有情緒 Trigger。</summary>
    public void ResetAllEmotions()
    {
        if (targetAnimator == null) return;
        foreach (var e in EmotionSet)
            targetAnimator.ResetTrigger(GetHash(e));
    }
}
