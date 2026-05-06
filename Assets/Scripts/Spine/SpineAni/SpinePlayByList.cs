using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;
using MySpineSystem; // for AnimationTrack enum
using SpineAnimationState = Spine.AnimationState; // 避免與 UnityEngine.AnimationState 衝突

/// <summary>
/// Spine 動畫播放順序器（每組可自訂 isLoop / isRandom / nextGroupName / track）:
/// - isLoop: 播完整輪是否再來一輪（預設 false）
/// - isRandom: 每一輪是否隨機順序（預設 false）
/// - nextGroupName: 若有填，該組的 isLoop 失效；播完本輪後自動接到 nextGroupName（預設空）
/// - track: 指定此動畫組要在哪個軌道(AnimationTrack)上播放（預設為 AnimationTrack 第一個值）
/// - ConditionalTransitionCheck: 外部條件檢查委派，返回 true 表示條件滿足。
/// - checkGroupName: 各組可自訂，當條件滿足時要跳轉的目標組名。
/// - StopPlaying(): 停止並清除正在播放的軌道
/// - PlayGroup(name): 中斷現有播放、清除目標軌道後從指定組開始；若該組含 NextGroup 會一路串接到鏈末端
/// - PlayGroupAndGoBack(name): 播放指定組一次（會先檢查外部條件轉場），然後返回播放呼叫前的組。
/// - OnGroupCompleted(string startGroupName): 整條鏈播放完畢後觸發，回傳最初 PlayGroup() 的組名
/// - GetByControllerID(id): 透過 SpineAnimationController 的 ID 系統取得對應的 SpinePlayByList
/// </summary>
public class SpinePlayByList : MonoBehaviour
{
    // ==========================================
    // 靜態便捷方法：透過 Controller ID 查找
    // ==========================================

    /// <summary>
    /// 透過 SpineAnimationController 的 ID 取得同物件上的 SpinePlayByList。
    /// </summary>
    public static SpinePlayByList GetByControllerID(string id)
    {
        var controller = SpineAnimationController.GetByID(id);
        if (controller == null) return null;

        var playByList = controller.GetComponent<SpinePlayByList>();
        if (playByList == null)
        {
            Debug.LogWarning($"[{nameof(SpinePlayByList)}] Controller ID '{id}' 存在，但該物件上沒有 SpinePlayByList 組件。");
        }
        return playByList;
    }

    // ==========================================

    // SpineClip 結構 - 保持不變
    [Serializable]
    public class SpineClip
    {
        [SpineAnimation] public string animationName;
        [Min(1)] public int repeatCount = 1;
    }

    // SpinePlayGroup 結構 - trackIndex 改為 AnimationTrack
    [Serializable]
    public class SpinePlayGroup
    {
        public string groupName;
        public List<SpineClip> clips = new List<SpineClip>();

        [Header("Options (per-group)")]
        [Tooltip("指定此動畫組要在哪個軌道(AnimationTrack)上播放。")]
        public AnimationTrack track = 0;
        public bool isLoop = false;
        public bool isRandom = false;

        [Tooltip("若有填，此組播完會接續播放該組；同時忽略本組 isLoop。")]
        public string nextGroupName = null;

        [Tooltip("若外部條件成立，要跳轉到的目標組名；若為空則不檢查。")]
        public string checkGroupName = null;
    }

    [Header("Controller (可不填，會自動抓同物件上的)")]
    [SerializeField] private SpineAnimationController _controller;

    [Header("Groups")]
    public List<SpinePlayGroup> groups = new List<SpinePlayGroup>();

    [Header("Debug / Testing")]
    [Tooltip("若設為 true，將會強制觸發外部條件檢查 (ConditionalTransitionCheck)")]
    public bool simulateConditionalTransitionCheck = false;

    public event Action<string> OnGroupStarted;
    public event Action<string> OnGroupCompleted;

    public bool IsPlaying => _playRoutine != null;
    public string CurrentGroupName { get; private set; }

    Coroutine _playRoutine;
    SpineAnimationState _state; // 快取，透過 controller 取得

    string _rootGroupNameForCompletion;
    string _returnGroupName = null;

    public Func<bool> ConditionalTransitionCheck { get; set; }

    void Awake()
    {
        EnsureController();
    }

    /// <summary>
    /// 確保 controller 與 _state 已初始化。
    /// </summary>
    private bool EnsureController()
    {
        if (_controller == null)
            _controller = GetComponent<SpineAnimationController>();

        if (_controller == null)
        {
            Debug.LogWarning($"[{nameof(SpinePlayByList)}] 找不到 SpineAnimationController，無法操作。", this);
            return false;
        }

        _controller.Initialize(); // 確保 controller 已初始化
        _state = _controller.GetAnimationState();

        if (_state == null)
        {
            Debug.LogWarning($"[{nameof(SpinePlayByList)}] SpineAnimationController 尚未取得 AnimationState。", this);
            return false;
        }

        return true;
    }

    public void StopPlaying()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (_state == null && !EnsureController()) { /* 清不了，跳過 */ }
        else if (!string.IsNullOrEmpty(CurrentGroupName))
        {
            var currentGroup = FindGroup(CurrentGroupName);
            if (currentGroup != null && _controller != null)
            {
                // 透過 controller 清軌，確保 delayed clear 等邏輯一致
                _controller.ClearTrack(currentGroup.track);
            }
        }

        CurrentGroupName = null;
        _rootGroupNameForCompletion = null;
        _returnGroupName = null;
    }

    public void PlayGroup(string groupName)
    {
        if (!EnsureController()) return;

        var group = FindGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"[{nameof(SpinePlayByList)}] 找不到名為 '{groupName}' 的組。", this);
            return;
        }

        string immediateNext = CheckForImmediateTransition(group);
        if (!string.IsNullOrEmpty(immediateNext))
        {
            group = FindGroup(immediateNext);
            if (group == null)
            {
                Debug.LogWarning($"[{nameof(SpinePlayByList)}] 立即轉場目標 '{immediateNext}' 不存在。", this);
                return;
            }
        }

        _returnGroupName = null;

        if (_playRoutine != null) StopCoroutine(_playRoutine);

        _rootGroupNameForCompletion = groupName;
        _playRoutine = StartCoroutine(CoPlayGroupChain(group));
    }

    public void PlayGroupAndGoBack(string groupName)
    {
        if (!EnsureController()) return;

        var groupToPlay = FindGroup(groupName);
        if (groupToPlay == null)
        {
            Debug.LogWarning($"[{nameof(SpinePlayByList)}] [PlayGroupAndGoBack] 找不到名為 '{groupName}' 的組。", this);
            return;
        }

        string immediateNext = CheckForImmediateTransition(groupToPlay);
        if (!string.IsNullOrEmpty(immediateNext))
        {
            var newGroup = FindGroup(immediateNext);
            if (newGroup == null)
            {
                Debug.LogWarning($"[{nameof(SpinePlayByList)}] [PlayGroupAndGoBack] 立即轉場目標 '{immediateNext}' 不存在。取消播放。", this);
                return;
            }
            groupToPlay = newGroup;
        }

        if (groupToPlay.isLoop)
        {
            Debug.LogError($"[{nameof(SpinePlayByList)}] [PlayGroupAndGoBack] 最終要播放的組 '{groupToPlay.groupName}' 被設定為 isLoop，無法用於此功能，因為它永遠不會結束。", this);
            return;
        }

        if (!string.IsNullOrEmpty(groupToPlay.nextGroupName))
        {
            Debug.LogError($"[{nameof(SpinePlayByList)}] [PlayGroupAndGoBack] 最終要播放的組 '{groupToPlay.groupName}' 包含 nextGroupName ('{groupToPlay.nextGroupName}')，無法用於此功能，因為它會串接到下一組而不是返回。", this);
            return;
        }

        _returnGroupName = CurrentGroupName;

        if (_playRoutine != null) StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(CoPlayOnceAndReturn(groupToPlay));
    }

    IEnumerator CoPlayOnceAndReturn(SpinePlayGroup groupToPlay)
    {
        CurrentGroupName = groupToPlay.groupName;
        OnGroupStarted?.Invoke(CurrentGroupName);

        int trackIndex = (int)groupToPlay.track;
        int clipCount = groupToPlay.clips?.Count ?? 0;
        var order = BuildOrder(clipCount, groupToPlay.isRandom);

        for (int oi = 0; oi < order.Count; oi++)
        {
            var idx = order[oi];
            var clip = SafeGet(groupToPlay.clips, idx);
            if (clip == null || string.IsNullOrEmpty(clip.animationName)) continue;

            int times = Mathf.Max(1, clip.repeatCount);
            for (int r = 0; r < times; r++)
            {
                if (_state == null) yield break;

                _controller.ClearTrack(groupToPlay.track);
                TrackEntry te = _state.SetAnimation(trackIndex, clip.animationName, false);

                while (te != null && !te.IsComplete)
                {
                    yield return null;
                }
            }
        }

        CurrentGroupName = null;
        _playRoutine = null;

        if (!string.IsNullOrEmpty(_returnGroupName))
        {
            string returnTarget = _returnGroupName;
            _returnGroupName = null;
            PlayGroup(returnTarget);
        }
    }

    IEnumerator CoPlayGroupChain(SpinePlayGroup startGroup)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        SpinePlayGroup group = startGroup;

        while (group != null)
        {
            if (visited.Contains(group.groupName))
            {
                Debug.LogWarning($"[{nameof(SpinePlayByList)}] 偵測到 NextGroup 循環（'{group.groupName}' 已重複）。鏈結終止。", this);
                break;
            }
            visited.Add(group.groupName);

            CurrentGroupName = group.groupName;
            OnGroupStarted?.Invoke(CurrentGroupName);

            int trackIndex = (int)group.track;
            bool hasNext = !string.IsNullOrEmpty(group.nextGroupName);
            bool allowLoopThisGroup = !hasNext && group.isLoop;
            int clipCount = group.clips?.Count ?? 0;

            bool transitioned = false;

            do
            {
                var order = BuildOrder(clipCount, group.isRandom);
                for (int oi = 0; oi < order.Count; oi++)
                {
                    var idx = order[oi];
                    var clip = SafeGet(group.clips, idx);
                    if (clip == null || string.IsNullOrEmpty(clip.animationName)) continue;

                    int times = Mathf.Max(1, clip.repeatCount);
                    for (int r = 0; r < times; r++)
                    {
                        if (_state == null) yield break;

                        _controller.ClearTrack(group.track);
                        TrackEntry te = _state.SetAnimation(trackIndex, clip.animationName, false);

                        while (te != null && !te.IsComplete)
                        {
                            yield return null;
                        }
                    }
                }

                // 在每一輪播放完畢後，檢查外部條件
                if (!string.IsNullOrEmpty(group.checkGroupName) && IsConditionMet())
                {
                    var targetGroup = FindGroup(group.checkGroupName);
                    if (targetGroup != null)
                    {
                        Debug.Log($"[{nameof(SpinePlayByList)}] 外部條件滿足。從 '{group.groupName}' 跳轉至 '{group.checkGroupName}'。", this);
                        group = targetGroup;
                        transitioned = true;
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(SpinePlayByList)}] 組 '{group.groupName}' 的條件跳轉目標 '{group.checkGroupName}' 不存在。繼續正常流程。", this);
                    }
                }

            } while (allowLoopThisGroup);

            if (transitioned)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(group.nextGroupName))
            {
                var next = FindGroup(group.nextGroupName);
                if (next == null)
                {
                    Debug.LogWarning($"[{nameof(SpinePlayByList)}] NextGroup '{group.nextGroupName}' 不存在（來源 '{group.groupName}'）。鏈結終止。", this);
                    break;
                }
                group = next;
                continue;
            }

            break;
        }

        var startName = _rootGroupNameForCompletion ?? startGroup.groupName;
        CurrentGroupName = null;
        _playRoutine = null;
        OnGroupCompleted?.Invoke(startName);
    }

    private string CheckForImmediateTransition(SpinePlayGroup initialGroup)
    {
        if (string.IsNullOrEmpty(initialGroup.checkGroupName))
            return null;

        if (IsConditionMet())
        {
            var targetGroup = FindGroup(initialGroup.checkGroupName);
            if (targetGroup != null)
            {
                Debug.Log($"[{nameof(SpinePlayByList)}] [立即跳轉] 外部條件滿足。從 '{initialGroup.groupName}' 跳轉至 '{initialGroup.checkGroupName}'。", this);
                return initialGroup.checkGroupName;
            }
            else
            {
                Debug.LogWarning($"[{nameof(SpinePlayByList)}] [立即跳轉] 組 '{initialGroup.groupName}' 的條件跳轉目標 '{initialGroup.checkGroupName}' 不存在。", this);
            }
        }
        return null;
    }

    private bool IsConditionMet()
    {
        if (simulateConditionalTransitionCheck)
        {
            return true;
        }
        if (ConditionalTransitionCheck != null)
        {
            return ConditionalTransitionCheck.Invoke();
        }
        return false;
    }

    SpinePlayGroup FindGroup(string name)
    {
        return groups.Find(g => string.Equals(g.groupName, name, StringComparison.Ordinal));
    }

    static List<int> BuildOrder(int count, bool random)
    {
        var order = new List<int>(count);
        for (int i = 0; i < count; i++) order.Add(i);

        if (random && count > 1)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }
        return order;
    }

    static T SafeGet<T>(List<T> list, int index) where T : class
    {
        if (list == null) return null;
        if (index < 0 || index >= list.Count) return null;
        return list[index];
    }
}