using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// StatChangePackageDatabase.cs
// ============================================================
// 「數值變化套組」總資料庫。
//
// 目的：把散落在劇情/FSM 裡的數值增減集中設定，用 ID 調用。
//       將來要做數值平衡，只改這個 SO，不必動任何劇情腳本。
//
// 由 StatChangeService 在執行時讀取（透過 GameStatusService 持有），
// 實際套用與報告邏輯在 StatChangeService，本檔只負責「資料定義 + ID 查找」。
//
// 對應調用入口：SequencerCommandStatPackage → StatPackage(packageID, heroineID, reportMode)
// ============================================================

/// <summary>
/// 可被套組變更的數值種類。
/// 前四項屬主角（ProtagonistStatusModel），後兩項屬女主角（HeroineStatusModel）。
/// 列舉名稱會直接當成 ValueChangeReporter 的 resourceTypeKey 使用，請勿隨意更名。
/// </summary>
public enum StatKind
{
    // ── 主角 ──
    Stress,
    LifePower,
    Sociality,
    Dependency,
    RoomMessLevel,
    // ── 女主角 ──
    Libido,
    Trust,
    // ── 主角（追加，放最後以免打亂既有套組 .asset 的列舉索引） ──
    BodyDirtyLevel,
    // ── 女主角（追加，放最後以免打亂既有套組 .asset 的列舉索引） ──
    Affinity
}

/// <summary>
/// 數值操作方式。
/// Add = 增減（帶正負號，與既有 SequencerCommandProtagonistStatus 的 Add 系列一致）。
/// Set = 直接設定為指定值。
/// </summary>
public enum StatOp
{
    Add,
    Set
}

/// <summary>
/// 單一筆數值變化。
/// </summary>
[Serializable]
public class StatChangeOp
{
    [Tooltip("要變更的數值種類。Libido / Trust / Affinity 屬女主角，需在調用時提供 heroineID。")]
    public StatKind kind = StatKind.Stress;

    [Tooltip("操作方式：Add 增減（帶正負號）/ Set 直接設定。")]
    public StatOp op = StatOp.Add;

    [Tooltip("變化量。Add 時可填負數代表扣除；Set 時為要設定的目標值。")]
    public int amount = 0;

    /// <summary>是否為女主角數值（Libido / Trust / Affinity）。</summary>
    public bool IsHeroineStat => kind == StatKind.Libido || kind == StatKind.Trust || kind == StatKind.Affinity;
}

/// <summary>
/// 一個數值變化套組：以 ID 調用，內含一串數值變化。
/// </summary>
[Serializable]
public class StatChangeEntry
{
    [Tooltip("套組 ID，劇情/FSM 用此字串調用。請保持唯一。")]
    public string id = "";

    [Tooltip("備註（僅供編輯時辨識用，不影響執行）。")]
    public string note = "";

    [Tooltip("這個套組會依序執行的數值變化清單。")]
    public List<StatChangeOp> ops = new List<StatChangeOp>();
}

/// <summary>
/// 數值變化套組總資料庫（ScriptableObject）。
/// 在 Inspector 拖進場景上的 GameStatusService。
/// </summary>
[CreateAssetMenu(fileName = "StatChangePackageDatabase", menuName = "Game/Stat/Stat Change Package Database")]
public class StatChangePackageDatabase : ScriptableObject
{
    [Tooltip("所有數值變化套組。每個套組以 id 為 key 被調用。")]
    [SerializeField] private List<StatChangeEntry> entries = new List<StatChangeEntry>();

    public IReadOnlyList<StatChangeEntry> Entries => entries;

    // ID → Entry 的快取查找表（執行時建立）。
    private Dictionary<string, StatChangeEntry> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    /// <summary>
    /// 重建 ID 查找表。OnEnable 會自動呼叫；若執行期間動態改了 entries 可手動再呼叫。
    /// </summary>
    public void BuildLookup()
    {
        _lookup = new Dictionary<string, StatChangeEntry>();
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;

            if (_lookup.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[StatChangePackageDatabase] 發現重複的套組 ID：{entry.id}，後者會覆蓋前者。", this);
            }
            _lookup[entry.id] = entry;
        }
    }

    /// <summary>
    /// 依 ID 取得套組。找不到回傳 null。
    /// </summary>
    public StatChangeEntry GetEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (_lookup == null) BuildLookup();

        _lookup.TryGetValue(id, out var entry);
        return entry;
    }
}
