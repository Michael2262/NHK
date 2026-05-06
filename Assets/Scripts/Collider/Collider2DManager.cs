using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// 全域 Collider2D 管理器。
/// 由 Manager 自行掃描指定根物件下的所有 Collider2D 並註冊，
/// 個別 Collider2D 不需要掛任何額外腳本。
/// 註冊完成後會透過 UnityEvent 與 static event 通知外部。
/// </summary>
public class Collider2DManager : MonoBehaviour
{
    #region Singleton

    public static Collider2DManager Instance { get; private set; }

    /// <summary>
    /// 管理器是否已完成初始化（註冊完畢、可以安全使用）。
    /// 即使錯過事件，也可以用此旗標做 late-join 檢查。
    /// </summary>
    public static bool IsReady { get; private set; }

    #endregion

    // ─── Inspector 設定 ───────────────────────────────────

    [Serializable]
    public class ColliderGroupEntry
    {
        [Tooltip("此群組的列舉名稱")]
        public ColliderGroupName groupName;

        [Tooltip("掃描的根物件，會往下搜集所有 Collider2D（含自身）")]
        public Transform root;

        [Tooltip("是否包含 Inactive 的 GameObject")]
        public bool includeInactive = true;
    }

    [Header("群組設定 — 指定根物件，Manager 會自動掃描其下所有 Collider2D")]
    [SerializeField] private List<ColliderGroupEntry> groupEntries = new List<ColliderGroupEntry>();

    [Header("事件")]
    [Tooltip("所有群組註冊完畢後觸發（Inspector 可拖拉）")]
    [SerializeField] private UnityEvent onRegistrationComplete;

    // ─── Static Event（程式碼訂閱用）─────────────────────

    /// <summary>
    /// 所有 Collider 註冊完畢後觸發。
    /// 如果訂閱時已經 Ready，不會再補發——請搭配 IsReady 使用。
    /// </summary>
    public static event Action OnReady;

    // ─── 核心資料結構 ─────────────────────────────────────

    // [ColliderGroupName, [ColliderID, ColliderComponent]]
    private readonly Dictionary<ColliderGroupName, Dictionary<string, Collider2D>> colliderRegistry =
        new Dictionary<ColliderGroupName, Dictionary<string, Collider2D>>();

    // ─── Lifecycle ────────────────────────────────────────

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsReady = false;

        // 自動掃描並註冊
        ScanAndRegisterAll();

        // 標記完成 & 發送事件
        IsReady = true;
        onRegistrationComplete?.Invoke();
        OnReady?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsReady = false;
        }
    }

    // ─── 掃描邏輯 ─────────────────────────────────────────

    /// <summary>
    /// 根據 Inspector 設定的 groupEntries，掃描所有 Collider2D 並註冊。
    /// ID 預設使用 GameObject 名稱；若物件上掛有 ColliderIdOverride 則優先使用。
    /// </summary>
    private void ScanAndRegisterAll()
    {
        foreach (var entry in groupEntries)
        {
            if (entry.root == null)
            {
                Debug.LogWarning($"[Collider2DManager] 群組 '{entry.groupName}' 的 root 為 null，已跳過。");
                continue;
            }

            Collider2D[] colliders = entry.root.GetComponentsInChildren<Collider2D>(entry.includeInactive);

            foreach (Collider2D col in colliders)
            {
                string id = ResolveId(col);
                RegisterCollider(entry.groupName, id, col);
            }
        }
    }

    /// <summary>
    /// 決定 Collider 的 ID。
    /// 優先使用 ColliderIdOverride 元件的值，否則使用 GameObject 名稱。
    /// </summary>
    private string ResolveId(Collider2D col)
    {
        var overrideComp = col.GetComponent<ColliderIdOverride>();
        if (overrideComp != null && !string.IsNullOrEmpty(overrideComp.id))
        {
            return overrideComp.id;
        }
        return col.gameObject.name;
    }

    // ─── 註冊 / 反註冊（內部使用，也保留 public 供特殊需求）──

    /// <summary>
    /// 註冊一個 Collider 到管理器。
    /// 一般情況下由 Manager 自動呼叫，不需要外部手動呼叫。
    /// </summary>
    public void RegisterCollider(ColliderGroupName groupName, string colliderId, Collider2D collider)
    {
        if (!colliderRegistry.TryGetValue(groupName, out var group))
        {
            group = new Dictionary<string, Collider2D>();
            colliderRegistry[groupName] = group;
        }

        if (group.ContainsKey(colliderId))
        {
            Debug.LogWarning($"[Collider2DManager] ID '{colliderId}' 在群組 '{groupName}' 中已存在，將覆寫。");
        }
        group[colliderId] = collider;
    }

    /// <summary>
    /// 從管理器中取消註冊一個 Collider。
    /// </summary>
    public void UnregisterCollider(ColliderGroupName groupName, string colliderId)
    {
        if (colliderRegistry.TryGetValue(groupName, out var group))
        {
            group.Remove(colliderId);
        }
    }

    // ─── 執行時動態新增群組 ────────────────────────────────

    /// <summary>
    /// 執行時動態掃描一個新的根物件，將其下 Collider2D 加入指定群組。
    /// 適用於運行中動態生成的物件。
    /// </summary>
    public void ScanAndRegister(ColliderGroupName groupName, Transform root, bool includeInactive = true)
    {
        if (root == null) return;

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(includeInactive);
        foreach (Collider2D col in colliders)
        {
            string id = ResolveId(col);
            RegisterCollider(groupName, id, col);
        }
    }

    // ─── 核心功能 API ─────────────────────────────────────

    /// <summary>啟用指定群組中的所有 Collider。</summary>
    public void EnableGroup(ColliderGroupName groupName)
    {
        SetGroupState(groupName, true);
    }

    /// <summary>停用指定群組中的所有 Collider。</summary>
    public void DisableGroup(ColliderGroupName groupName)
    {
        SetGroupState(groupName, false);
    }

    private void SetGroupState(ColliderGroupName groupName, bool isEnabled)
    {
        if (!colliderRegistry.TryGetValue(groupName, out var group))
        {
            Debug.LogWarning($"[Collider2DManager] 群組 '{groupName}' 不存在。");
            return;
        }

        foreach (Collider2D collider in group.Values)
        {
            if (collider != null) collider.enabled = isEnabled;
        }
    }

    /// <summary>設定單一 Collider 的狀態（需指定群組）。</summary>
    public void SetColliderState(ColliderGroupName groupName, string colliderId, bool isEnabled)
    {
        if (colliderRegistry.TryGetValue(groupName, out var group) &&
            group.TryGetValue(colliderId, out Collider2D collider))
        {
            if (collider != null) collider.enabled = isEnabled;
        }
        else
        {
            Debug.LogWarning($"[Collider2DManager] 找不到 Collider '{groupName}/{colliderId}'。");
        }
    }

    /// <summary>啟用單一 Collider（需指定群組）。</summary>
    public void EnableCollider(ColliderGroupName groupName, string colliderId)
        => SetColliderState(groupName, colliderId, true);

    /// <summary>停用單一 Collider（需指定群組）。</summary>
    public void DisableCollider(ColliderGroupName groupName, string colliderId)
        => SetColliderState(groupName, colliderId, false);

    // ─── 僅透過 ID 查找（跨群組）──────────────────────────

    /// <summary>
    /// 僅透過 ID 設定 Collider 狀態，會遍歷所有群組查找。
    /// 適用於 ID 不重複的情境。
    /// </summary>
    public void SetColliderStateById(string colliderId, bool isEnabled)
    {
        foreach (var group in colliderRegistry.Values)
        {
            if (group.TryGetValue(colliderId, out Collider2D collider))
            {
                if (collider != null) collider.enabled = isEnabled;
                return;
            }
        }
        Debug.LogWarning($"[Collider2DManager] 找不到 Collider ID: '{colliderId}'");
    }

    /// <summary>僅透過 ID 啟用 Collider。</summary>
    public void EnableColliderById(string colliderId) => SetColliderStateById(colliderId, true);

    /// <summary>僅透過 ID 停用 Collider。</summary>
    public void DisableColliderById(string colliderId) => SetColliderStateById(colliderId, false);

    // ─── 查詢 API ─────────────────────────────────────────

    /// <summary>取得指定群組中某 Collider 的參照。</summary>
    public Collider2D GetCollider(ColliderGroupName groupName, string colliderId)
    {
        if (colliderRegistry.TryGetValue(groupName, out var group) &&
            group.TryGetValue(colliderId, out Collider2D collider))
        {
            return collider;
        }
        return null;
    }

    /// <summary>取得指定群組中所有已註冊的 Collider。</summary>
    public IReadOnlyDictionary<string, Collider2D> GetGroup(ColliderGroupName groupName)
    {
        if (colliderRegistry.TryGetValue(groupName, out var group))
        {
            return group;
        }
        return null;
    }
}