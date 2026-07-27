using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全域道具目錄：ID → ItemConfigData 的唯一真實來源。
///
/// 【職責界線】
/// - ItemCatalog（本類別）：回答「這個 ID 是什麼道具、它的設定是什麼」。全域唯一，涵蓋所有道具（含不在任何商店販售的）。
/// - ItemDatabase：只負責「某間商店的貨架」（賣哪些、排序、解鎖條件）。可有多個，同一道具可同時出現在多間商店。
///
/// 【資料來源】
/// 初始化時自動掃描 Resources/SHOP/Item 下所有 ItemConfigData，
/// 新增道具只要把 asset 丟進該資料夾即可，無需手動登錄。
///
/// 由 GameStatusService 以 new 建立並持有，透過 GameStatusService.Instance.ItemCatalog 存取。
/// </summary>
public class ItemCatalog
{
    /// <summary>ItemConfigData 資產所在的 Resources 相對路徑。</summary>
    private const string ResourcePath = "SHOP/Item";

    private readonly Dictionary<string, ItemConfigData> _byId = new Dictionary<string, ItemConfigData>();

    public ItemCatalog()
    {
        Reload();
    }

    /// <summary>
    /// 重新掃描 Resources/SHOP/Item，重建 ID → ItemConfigData 對照表。
    /// </summary>
    public void Reload()
    {
        _byId.Clear();

        var all = Resources.LoadAll<ItemConfigData>(ResourcePath);
        foreach (var item in all)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemID)) continue;

            if (_byId.ContainsKey(item.ItemID))
            {
                Debug.LogWarning($"[ItemCatalog] 重複的 ItemID: '{item.ItemID}'（資產 '{item.name}'），已跳過後者。請確認 Resources/{ResourcePath} 下沒有 ID 撞號的道具。");
                continue;
            }
            _byId[item.ItemID] = item;
        }

        Debug.Log($"[ItemCatalog] 已載入 {_byId.Count} 個道具設定（來源 Resources/{ResourcePath}）。");
    }

    /// <summary>
    /// 依 ID 取得道具設定；找不到回傳 null。
    /// </summary>
    public ItemConfigData GetItemConfig(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;
        _byId.TryGetValue(itemID, out var config);
        return config;
    }

    /// <summary>
    /// 是否存在此 ID 的道具設定。
    /// </summary>
    public bool Contains(string itemID)
    {
        return !string.IsNullOrEmpty(itemID) && _byId.ContainsKey(itemID);
    }

    /// <summary>
    /// 全部道具設定的唯讀集合（給需要遍歷全部道具的地方用）。
    /// </summary>
    public IEnumerable<ItemConfigData> AllItems => _byId.Values;
}
