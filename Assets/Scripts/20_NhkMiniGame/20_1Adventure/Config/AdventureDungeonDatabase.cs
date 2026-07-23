using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 冒險地點清單，給地點選單使用。拖進選單 UI 或 Controller 即可列出所有 Dungeon。
/// </summary>
[CreateAssetMenu(menuName = "Game/Adventure/Dungeon Database")]
public class AdventureDungeonDatabase : ScriptableObject
{
    public List<AdventureDungeonData> Dungeons = new List<AdventureDungeonData>();

    public AdventureDungeonData Find(string dungeonID)
        => Dungeons.Find(d => d != null && d.DungeonID == dungeonID);
}
