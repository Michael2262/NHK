using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 場景用分組互斥狀態工具。初始皆不成立，不接存檔、不自動執行進入事件。
/// 設定於首次使用時建立快照；執行期間請透過 API 切換，勿修改設定清單。
/// </summary>
[AddComponentMenu("NHK/Scene Tools/Grouped State Controller")]
public sealed class GroupedStateController : MonoBehaviour
{
    [Serializable]
    public sealed class StateDefinition
    {
        [Tooltip("同組內唯一，區分大小寫，不可空白或包含 /。")]
        public string StateName;
        public UnityEvent OnEnter = new UnityEvent();
        public UnityEvent OnExit = new UnityEvent();
    }

    [Serializable]
    public sealed class StateGroup
    {
        [Tooltip("本元件內唯一，區分大小寫，不可空白或包含 /。")]
        public string GroupName;
        public List<StateDefinition> States = new List<StateDefinition>();
    }

    [Header("分組與狀態（每組初始皆不成立）")]
    [SerializeField] private List<StateGroup> groups = new List<StateGroup>();

    [Header("CheckState 的判斷結果")]
    [SerializeField] private UnityEvent onCheckTrue = new UnityEvent();
    [SerializeField] private UnityEvent onCheckFalse = new UnityEvent();

    private GroupedStateModel model;
    private bool initializationAttempted;

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>供 UnityEvent 使用，例如「房門/上鎖」。</summary>
    public void SetState(string statePath)
    {
        if (TryParsePath(statePath, out string groupId, out string stateId))
            SetState(groupId, stateId);
    }

    public void SetState(string groupId, string stateId)
    {
        if (EnsureInitialized()) model.SetState(groupId, stateId);
    }

    /// <summary>解除目前狀態並執行 OnExit；原本皆不成立時不做事。</summary>
    public void ClearState(string groupId)
    {
        if (EnsureInitialized()) model.ClearState(groupId);
    }

    /// <summary>無效組名或狀態名回傳 false；不觸發判斷事件。</summary>
    public bool IsState(string groupId, string stateId)
    {
        return EnsureInitialized() && model.IsState(groupId, stateId);
    }

    /// <summary>皆不成立或查無此組時回傳空字串；需要區分時用 TryGetCurrentState。</summary>
    public string GetCurrentState(string groupId)
    {
        TryGetCurrentState(groupId, out string stateId);
        return stateId;
    }

    /// <summary>回傳是否存在此組；存在但皆不成立時 stateId 為空字串。</summary>
    public bool TryGetCurrentState(string groupId, out string stateId)
    {
        stateId = string.Empty;
        return EnsureInitialized() && model.TryGetCurrentState(groupId, out stateId);
    }

    /// <summary>只讀取已建立的執行期資料，不因 Inspector 顯示而初始化元件。</summary>
    public bool TryGetStateSnapshot(string groupId, out string current, out string lastExited)
    {
        current = lastExited = string.Empty;
        return model != null && model.TryGetSnapshot(groupId, out current, out lastExited);
    }

    /// <summary>供 UnityEvent 使用，依「組名/狀態名」觸發判斷結果事件。</summary>
    public void CheckState(string statePath)
    {
        if (!TryParsePath(statePath, out string groupId, out string stateId)) return;
        if (!EnsureInitialized() || !model.ContainsState(groupId, stateId)) return;
        InvokeSafely(IsState(groupId, stateId) ? onCheckTrue : onCheckFalse);
    }

    private bool EnsureInitialized()
    {
        if (initializationAttempted) return model != null;
        initializationAttempted = true;
        var candidate = new GroupedStateModel(message => Debug.LogWarning(
            "[GroupedStateController] " + message, this));

        // 整份設定驗證成功後才啟用，避免重複名稱導向錯誤事件。
        try
        {
            if (groups == null) throw new ArgumentException("分組清單不可為 null。");
            foreach (StateGroup group in groups)
            {
                if (group == null) throw new ArgumentException("分組項目不可為 null。");
                ValidateName(group.GroupName, "組名");
                candidate.AddGroup(group.GroupName);
                if (group.States == null) throw new ArgumentException("狀態清單不可為 null。");
                foreach (StateDefinition state in group.States)
                {
                    if (state == null) throw new ArgumentException("狀態項目不可為 null。");
                    ValidateName(state.StateName, "狀態名");
                    UnityEvent enter = state.OnEnter;
                    UnityEvent exit = state.OnExit;
                    candidate.AddState(group.GroupName, state.StateName,
                        () => InvokeSafely(enter), () => InvokeSafely(exit));
                }
            }
            model = candidate;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError("[GroupedStateController] 設定無效，請修正後重新進入 Play Mode："
                + exception.Message, this);
        }
        return model != null;
    }

    private static void ValidateName(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("/") || value != value.Trim())
            throw new ArgumentException(label + "不可空白、包含 / 或帶有頭尾空白。");
    }

    private bool TryParsePath(string path, out string groupId, out string stateId)
    {
        groupId = stateId = string.Empty;
        int separator = path == null ? -1 : path.IndexOf('/');
        if (separator <= 0 || separator == path.Length - 1 || path.LastIndexOf('/') != separator)
        {
            Debug.LogWarning("[GroupedStateController] 請使用「組名/狀態名」格式。", this);
            return false;
        }
        groupId = path.Substring(0, separator);
        stateId = path.Substring(separator + 1);
        return true;
    }

    private void InvokeSafely(UnityEvent callback)
    {
        try { callback?.Invoke(); }
        catch (Exception exception)
        {
            // 已執行的場景操作無法回滾；記錄錯誤並讓狀態切換完成。
            Debug.LogException(exception, this);
        }
    }

    /// <summary>純 C# 場景狀態 Model；不依賴 Unity 生命週期或存檔服務。</summary>
    private sealed class GroupedStateModel
    {
        private sealed class State
        {
            public Action Enter;
            public Action Exit;
        }

        private sealed class Group
        {
            public readonly Dictionary<string, State> States = new Dictionary<string, State>(StringComparer.Ordinal);
            public string Current = string.Empty;
            public string LastExited = string.Empty;
        }

        private readonly Dictionary<string, Group> entries = new Dictionary<string, Group>(StringComparer.Ordinal);
        private readonly Queue<Action> pending = new Queue<Action>();
        private readonly Action<string> warn;
        private bool processing;
        private const int MaxRequestsPerBatch = 128;

        public GroupedStateModel(Action<string> warning) { warn = warning; }

        public void AddGroup(string id)
        {
            if (entries.ContainsKey(id)) throw new ArgumentException("組名重複：" + id);
            entries.Add(id, new Group());
        }

        public void AddState(string groupId, string stateId, Action enter, Action exit)
        {
            Group group = entries[groupId];
            if (group.States.ContainsKey(stateId))
                throw new ArgumentException("同組內狀態名重複：" + groupId + "/" + stateId);
            group.States.Add(stateId, new State { Enter = enter, Exit = exit });
        }

        public bool ContainsState(string groupId, string stateId)
        {
            if (groupId != null && stateId != null && entries.TryGetValue(groupId, out Group group)
                && group.States.ContainsKey(stateId)) return true;
            warn("找不到狀態：" + groupId + "/" + stateId);
            return false;
        }

        public bool TryGetCurrentState(string groupId, out string stateId)
        {
            stateId = string.Empty;
            if (groupId == null || !entries.TryGetValue(groupId, out Group group)) return false;
            stateId = group.Current;
            return true;
        }

        public bool IsState(string groupId, string stateId)
        {
            return !string.IsNullOrEmpty(stateId) && TryGetCurrentState(groupId, out string current)
                && string.Equals(current, stateId, StringComparison.Ordinal);
        }

        public bool TryGetSnapshot(string groupId, out string current, out string lastExited)
        {
            current = lastExited = string.Empty;
            if (groupId == null || !entries.TryGetValue(groupId, out Group group)) return false;
            current = group.Current;
            lastExited = group.LastExited;
            return true;
        }

        public void SetState(string groupId, string stateId)
        {
            // 驗證新狀態後才能解除舊狀態。
            if (ContainsState(groupId, stateId)) Enqueue(() => Transition(entries[groupId], stateId));
        }

        public void ClearState(string groupId)
        {
            if (groupId == null || !entries.TryGetValue(groupId, out Group group))
            {
                warn("找不到分組：" + groupId);
                return;
            }
            Enqueue(() => Transition(group, string.Empty));
        }

        private static void Transition(Group group, string next)
        {
            if (group.Current == next) return;
            // OnExit 查詢時仍是舊狀態；OnEnter 查詢時已是新狀態。
            if (group.Current.Length > 0)
            {
                group.States[group.Current].Exit();
                group.LastExited = group.Current;
            }
            group.Current = next;
            if (next.Length > 0) group.States[next].Enter();
        }

        private void Enqueue(Action request)
        {
            if (pending.Count >= MaxRequestsPerBatch)
            {
                warn("切換佇列已滿，忽略本次要求；請檢查事件是否循環觸發。");
                return;
            }
            pending.Enqueue(request);
            if (processing) return;
            processing = true;
            try
            {
                int processed = 0;
                while (pending.Count > 0 && processed < MaxRequestsPerBatch)
                {
                    processed++;
                    pending.Dequeue()();
                }
                if (pending.Count > 0) warn("連續切換超過 128 次，已中止剩餘要求；請檢查事件是否循環觸發。");
            }
            finally
            {
                pending.Clear();
                processing = false;
            }
        }
    }
}
