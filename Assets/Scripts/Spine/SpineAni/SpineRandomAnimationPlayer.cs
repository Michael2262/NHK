using System;
using System.Collections.Generic;
using MySpineSystem;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 依群組 ID 隨機播放一個動畫；每次呼叫只抽一次，不會自動接播。
/// 抽選紀錄僅保留於此元件的生命週期，不寫入存檔。
/// </summary>
[DisallowMultipleComponent]
public class SpineRandomAnimationPlayer : MonoBehaviour
{
    [Serializable]
    public class AnimationGroup
    {
        [Tooltip("播放時使用的唯一群組 ID，區分大小寫。")]
        public string id;

        [Tooltip("此組所有動畫共用的 Track。")]
        public AnimationTrack track = 0;

        [SpineAnimation]
        public List<string> animations = new List<string>();

        [Tooltip("允許每次從完整清單抽選，可能連續抽到同一個動畫。")]
        public bool allowRepeat = false;

        [Tooltip("僅在禁止重複時生效：全部抽完後，下次呼叫重新開始一輪。新一輪第一個可能與上一輪最後一個相同。")]
        public bool restartWhenExhausted = true;

    }

    [Header("播放控制器（留空時取得同物件上的元件）")]
    [SerializeField] private SpineAnimationController controller;

    [Header("動畫群組")]
    public List<AnimationGroup> groups = new List<AnimationGroup>();

    private readonly RandomAnimationModel model = new RandomAnimationModel();

    /// <summary>供 UnityEvent 或 PlayMaker 的 Call Method 以字串 ID 呼叫。</summary>
    public void PlayByID(string id)
    {
        TryPlayByID(id);
    }

    /// <summary>成功送出播放時回傳 true；抽完、設定錯誤或控制器未就緒時回傳 false。</summary>
    public bool TryPlayByID(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("[SpineRandomAnimationPlayer] 群組 ID 不可為空。", this);
            return false;
        }

        AnimationGroup group = null;
        if (groups != null)
        {
            foreach (var candidate in groups)
            {
                if (candidate == null || !string.Equals(candidate.id, id, StringComparison.Ordinal)) continue;
                if (group != null)
                {
                    Debug.LogWarning($"[SpineRandomAnimationPlayer] 群組 ID '{id}' 重複，請修正後再播放。", this);
                    return false;
                }
                group = candidate;
            }
        }

        if (group == null || group.animations == null || group.animations.Count == 0 || (int)group.track < 0)
        {
            Debug.LogWarning($"[SpineRandomAnimationPlayer] 群組 '{id}' 不存在、動畫清單為空或 Track 無效。", this);
            return false;
        }

        if (controller == null) controller = GetComponent<SpineAnimationController>();
        var state = controller != null ? controller.GetAnimationState() : null;
        if (state == null)
        {
            Debug.LogWarning("[SpineRandomAnimationPlayer] 找不到已初始化的 SpineAnimationController，請於初始化完成後呼叫。", this);
            return false;
        }

        // 先驗證整組，避免無效名稱讓 Controller 清軌後才拋出例外。
        // 同名動畫只算一個候選，不增加權重，也不繞過禁止重複的規則。
        var names = new List<string>();
        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in group.animations)
        {
            if (string.IsNullOrWhiteSpace(name) || state.Data.SkeletonData.FindAnimation(name) == null)
            {
                Debug.LogWarning($"[SpineRandomAnimationPlayer] 群組 '{id}' 的動畫 '{name}' 為空或不存在，請修正清單。", this);
                return false;
            }
            if (uniqueNames.Add(name)) names.Add(name);
        }

        if (!model.TrySelect(id, names, group.allowRepeat, group.restartWhenExhausted,
                out string selected, out bool startsNewRound)) return false;

        // 此工具固定於動畫完成時立即清軌。
        var entry = controller.PlayAnimation(group.track, selected, SpineAnimationController.ClearMode.ClearOnComplete);
        if (entry == null) return false;

        // 送出播放即視為已抽過；即使之後被其他動畫中斷，也不退回候選池。
        model.RecordPlayed(id, selected, startsNewRound);
        return true;
    }

    /// <summary>清除指定群組的抽選紀錄，不中斷正在播放的動畫。</summary>
    public void ResetGroup(string id)
    {
        if (!string.IsNullOrWhiteSpace(id)) model.ResetGroup(id);
    }

    /// <summary>清除所有群組的抽選紀錄，不中斷正在播放的動畫。</summary>
    public void ResetAllGroups()
    {
        model.ResetAll();
    }

    /// <summary>此播放器專用的純 C# 抽選 Model，不持有 Unity 元件或全域遊戲狀態。</summary>
    private sealed class RandomAnimationModel
    {
        private readonly Dictionary<string, HashSet<string>> playedByID =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly System.Random random = new System.Random();

        public bool TrySelect(string id, List<string> names, bool allowRepeat, bool restartWhenExhausted,
            out string selected, out bool startsNewRound)
        {
            selected = null;
            startsNewRound = false;
            playedByID.TryGetValue(id, out var played);
            var candidates = new List<string>();
            foreach (string name in names)
            {
                if (allowRepeat || played == null || !played.Contains(name)) candidates.Add(name);
            }

            if (candidates.Count == 0)
            {
                if (!restartWhenExhausted || names.Count == 0) return false;
                candidates.AddRange(names);
                startsNewRound = true;
            }

            selected = candidates[random.Next(candidates.Count)];
            return true;
        }

        public void RecordPlayed(string id, string name, bool startsNewRound)
        {
            if (!playedByID.TryGetValue(id, out var played))
            {
                played = new HashSet<string>(StringComparer.Ordinal);
                playedByID.Add(id, played);
            }
            if (startsNewRound) played.Clear();
            played.Add(name);
        }

        public void ResetGroup(string id) => playedByID.Remove(id);
        public void ResetAll() => playedByID.Clear();
    }
}
