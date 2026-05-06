using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 【[CreateAssetMenu]】
/// 職責：作為一個「檔案櫃」(.asset 檔案)，在 Unity 編輯器中儲存「所有」的地點資料。
/// 它繼承自 ScriptableObject。
/// </summary>
[CreateAssetMenu(menuName = "Game/Config/Location Database")]
public class LocationDatabase : ScriptableObject
{
    [Header("地點資料庫")]
    public List<LocationData> allLocations;

    // (選用，但強烈推薦)
    /// <summary>
    /// 提供一個方便的方法，讓 Manager 可以透過 ID 快速找到地點資料。
    /// </summary>
    public LocationData FindLocationByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // 這裡未來可以用 Dictionary 優化，但 List.Find 對幾十個地點來說也夠快了
        return allLocations.Find(location => location.LocationID == id);
    }
}