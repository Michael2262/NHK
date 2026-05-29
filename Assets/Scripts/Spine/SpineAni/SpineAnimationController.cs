using UnityEngine;
using Spine.Unity;
using Spine; // TrackEntry
using System;
using System.Collections;
using System.Collections.Generic;
using MySpineSystem; // for AnimationTrack enum



/// <summary>
/// Spine 動畫播放的核心控制器（擴充版 + ID 系統）。
/// </summary>
[DisallowMultipleComponent]
public class SpineAnimationController : MonoBehaviour
{
    // ==========================================
    // [新功能] ID 系統與靜態註冊表
    // ==========================================
    [Header("Identity (可選)")]
    [Tooltip("為此控制器指定一個唯一的 ID，方便透過靜態方法獲取。")]
    [SerializeField] private string controllerID;

    private static readonly Dictionary<string, SpineAnimationController> _registry = new();

    /// <summary>
    /// 透過 ID 取得場景中的 SpineAnimationController。
    /// </summary>
    public static SpineAnimationController GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_registry.TryGetValue(id, out var controller))
        {
            return controller;
        }
        Debug.LogWarning($"[SpineAnimationController] 找不到 ID 為 '{id}' 的控制器，請檢查場景物件是否有填寫 ID。");
        return null;
    }

    private void OnEnable()
    {
        RegisterID();
    }

    private void OnDisable()
    {
        UnregisterID();
    }

    private void RegisterID()
    {
        if (string.IsNullOrEmpty(controllerID)) return;

        if (_registry.ContainsKey(controllerID))
        {
            if (_registry[controllerID] != this)
            {
                Debug.LogWarning($"[SpineAnimationController] 偵測到重複的 ID: '{controllerID}'。目前僅能保留最後一個註冊的物件。", this);
                _registry[controllerID] = this;
            }
        }
        else
        {
            _registry.Add(controllerID, this);
        }
    }

    private void UnregisterID()
    {
        if (string.IsNullOrEmpty(controllerID)) return;

        if (_registry.TryGetValue(controllerID, out var controller) && controller == this)
        {
            _registry.Remove(controllerID);
        }
    }
    // ==========================================

    public enum ClearMode
    {
        /// <summary>完成事件觸發時立即清除此 Track（舊行為）。</summary>
        ClearOnComplete = 0,
        /// <summary>完成後保持在最後一幀，不自動清軌。</summary>
        KeepTrack = 1,
        /// <summary>完成後等待一段時間再清軌。</summary>
        ClearAfterDelay = 2,
        /// <summary>播完後自動重播，直到同 Track 有新動畫或主動清軌為止。</summary>
        Loop = 3,
    }

    [Header("Targets (可不填，會自動抓)")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private SkeletonGraphic skeletonGraphic;

    [Header("Defaults")]
    [Tooltip("當 ClearMode = ClearAfterDelay 時，未特別指定時使用的延遲秒數。")]
    [Min(0f)] public float defaultClearDelaySeconds = 2f;

    private Spine.AnimationState _state;

    public event Action<AnimationTrack, string> OnAnimationCompleted;

    private readonly Dictionary<TrackEntry, (ClearMode mode, float delay)> _entryPolicies = new();
    private readonly Dictionary<int, Coroutine> _delayedClearRoutines = new();

    #region Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_state != null)
        {
            _state.Complete -= HandleAnimationComplete;
        }
        // 確保 Destroy 時也清理註冊（防呆）
        UnregisterID();
    }
    #endregion

    public void Initialize()
    {
        if (_state != null) return;

        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonGraphic == null) skeletonGraphic = GetComponent<SkeletonGraphic>();

        if (skeletonAnimation != null) _state = skeletonAnimation.AnimationState;
        else if (skeletonGraphic != null) _state = skeletonGraphic.AnimationState;
        else
        {
            Debug.LogError($"[{nameof(SpineAnimationController)}] 找不到 SkeletonAnimation 或 SkeletonGraphic。", this);
            return;
        }

        _state.Complete += HandleAnimationComplete;
    }

    public Spine.AnimationState GetAnimationState() => _state;

    public void StopAnimation(AnimationTrack track)
    {
        if (_state == null) return;
        int t = (int)track;
        CancelDelayedClearOnTrack(t);
        _state.ClearTrack(t);
    }

    public void StopAll()
    {
        if (_state == null) return;
        foreach (var kv in _delayedClearRoutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        _delayedClearRoutines.Clear();
        _state.ClearTracks();
        _entryPolicies.Clear();
    }

    public TrackEntry PlayAnimation(
        AnimationTrack track,
        string animationName,
        ClearMode mode = ClearMode.ClearOnComplete,
        float delaySeconds = -1f,
        Action<TrackEntry> onComplete = null)
    {
        if (_state == null || string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[{nameof(SpineAnimationController)}] AnimationState 未初始化或動畫名稱為空，無法播放。", this);
            return null;
        }

        int t = (int)track;

        // 若是 Loop 模式，且該 track 已在播同名動畫，直接跳過不重啟
        if (mode == ClearMode.Loop)
        {
            var current = _state.GetCurrent(t);
            if (current != null && current.Animation?.Name == animationName)
            {
                if (_entryPolicies.TryGetValue(current, out var existingPolicy) && existingPolicy.mode == ClearMode.Loop)
                    return current;
            }
        }

        CancelDelayedClearOnTrack(t);
        _state.ClearTrack(t);
        TryStopPlayByListIfSameTrack(t);

        var entry = _state.SetAnimation(t, animationName, false);

        if (delaySeconds < 0f) delaySeconds = defaultClearDelaySeconds;
        _entryPolicies[entry] = (mode, delaySeconds);

        if (onComplete != null)
            entry.Complete += _ => onComplete(entry);

        return entry;
    }

    public TrackEntry AddAnimation(
        AnimationTrack track,
        string animationName,
        float delayFromPrevious,
        ClearMode mode = ClearMode.ClearOnComplete,
        float delaySeconds = -1f,
        Action<TrackEntry> onComplete = null)
    {
        if (_state == null || string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[{nameof(SpineAnimationController)}] AnimationState 未初始化或動畫名稱為空，無法 AddAnimation。", this);
            return null;
        }

        int t = (int)track;

        var entry = _state.AddAnimation(t, animationName, false, delayFromPrevious);

        if (delaySeconds < 0f) delaySeconds = defaultClearDelaySeconds;
        _entryPolicies[entry] = (mode, delaySeconds);

        if (onComplete != null)
            entry.Complete += _ => onComplete(entry);

        return entry;
    }

    private void HandleAnimationComplete(TrackEntry entry)
    {
        if (!_entryPolicies.TryGetValue(entry, out var policy))
        {
            OnAnimationCompleted?.Invoke((AnimationTrack)entry.TrackIndex, entry.Animation?.Name);
            return;
        }

        OnAnimationCompleted?.Invoke((AnimationTrack)entry.TrackIndex, entry.Animation?.Name);

        switch (policy.mode)
        {
            case ClearMode.ClearOnComplete:
                _state.ClearTrack(entry.TrackIndex);
                _entryPolicies.Remove(entry);
                break;
            case ClearMode.KeepTrack:
                _entryPolicies.Remove(entry);
                break;
            case ClearMode.ClearAfterDelay:
                CancelDelayedClearOnTrack(entry.TrackIndex);
                var co = StartCoroutine(CoClearTrackAfterDelay(entry.TrackIndex, entry, policy.delay));
                _delayedClearRoutines[entry.TrackIndex] = co;
                _entryPolicies.Remove(entry);
                break;

            case ClearMode.Loop:
                _entryPolicies.Remove(entry);
                // 確認此 Track 沒有被新動畫佔用才重播
                var currentEntry = _state.GetCurrent(entry.TrackIndex);
                if (currentEntry == null || currentEntry == entry)
                {
                    var loopEntry = _state.SetAnimation(entry.TrackIndex, entry.Animation.Name, false);
                    _entryPolicies[loopEntry] = (ClearMode.Loop, policy.delay);
                }
                break;
        }
    }

    private IEnumerator CoClearTrackAfterDelay(int trackIndex, TrackEntry sourceEntry, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        var current = _state?.GetCurrent(trackIndex);
        if (current == sourceEntry)
        {
            _state.ClearTrack(trackIndex);
        }

        _delayedClearRoutines.Remove(trackIndex);
    }

    private void CancelDelayedClearOnTrack(int trackIndex)
    {
        if (_delayedClearRoutines.TryGetValue(trackIndex, out var co) && co != null)
        {
            StopCoroutine(co);
        }
        _delayedClearRoutines.Remove(trackIndex);
    }

    /// <summary>
    /// 當 Controller 要在某個 track 上播放新動畫時，
    /// 檢查 SpinePlayByList 是否正在使用同一個 track，如果是就停掉它。
    /// （已從反射改為直接型別引用）
    /// </summary>
    private void TryStopPlayByListIfSameTrack(int trackIndex)
    {
        var playByList = GetComponent<SpinePlayByList>();
        if (playByList == null || !playByList.IsPlaying) return;

        string currentName = playByList.CurrentGroupName;
        if (string.IsNullOrEmpty(currentName)) return;

        var group = playByList.groups.Find(
            g => string.Equals(g.groupName, currentName, StringComparison.Ordinal));

        if (group != null && (int)group.track == trackIndex)
        {
            playByList.StopPlaying();
        }
    }

    public void PlayAnimationSimple(AnimationTrack track, string animationName)
    {
        PlayAnimation(track, animationName, ClearMode.ClearOnComplete, -1f, null);
    }

    public void PlayAnimationWithOptions(AnimationTrack track, string animationName, ClearMode mode, float delaySeconds)
    {
        PlayAnimation(track, animationName, mode, delaySeconds, null);
    }

    public void ClearTrackIndex(int trackIndex)
    {
        if (_state == null) return;
        CancelDelayedClearOnTrack(trackIndex);
        _state.ClearTrack(trackIndex);
    }

    public void ClearTrack(AnimationTrack track)
    {
        ClearTrackIndex((int)track);
    }

    public void ClearTracks(params int[] trackIndices)
    {
        if (_state == null || trackIndices == null) return;

        for (int i = 0; i < trackIndices.Length; i++)
        {
            int t = trackIndices[i];
            CancelDelayedClearOnTrack(t);
            _state.ClearTrack(t);
        }
    }

    public void ClearTracksByEnum(params AnimationTrack[] tracks)
    {
        if (tracks == null) return;
        var list = new List<int>(tracks.Length);
        for (int i = 0; i < tracks.Length; i++)
            list.Add((int)tracks[i]);
        ClearTracks(list.ToArray());
    }

    public void ClearTracks678()
    {
        ClearTracks(6, 7, 8);
    }

    public void ClearTracks101112()
    {
        ClearTracks(10, 11, 12);
    }


    //直接操作 Skeleton 物件來更換貼圖的函式
    public void SetSlotAttachment(string slotName, string attachmentName)
    {
        // 獲取目前的 Skeleton 物件 (相容 SkeletonAnimation 與 SkeletonGraphic)
        var skeleton = (skeletonAnimation != null) ? skeletonAnimation.Skeleton : skeletonGraphic.Skeleton;

        if (skeleton == null) return;

        // 設定附件。如果 attachmentName 為 null 或空字串，該插槽會變透明（隱藏圖案）
        skeleton.SetAttachment(slotName, attachmentName);
    }



}