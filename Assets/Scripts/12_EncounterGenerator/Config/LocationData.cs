using System.Collections.Generic;
using UnityEngine;

// 職責：定義一個「地點」的靜態規則。
// 這是一個純資料容器，通常會被序列化為 JSON 或儲存在 ScriptableObject 中。
[System.Serializable]
public class LocationData
{
    // 1. 基本資訊
    public string LocationID;   // 地點的唯一ID (例如："sister_room")
    public string LocationName; // 地點的顯示名稱 (例如："妹妹的房間")

    [Header("場景資訊")]
    [Tooltip("此地點對應的「互動 Hub 場景」的名稱")]
    public string hubSceneName;

    [Tooltip("預設解鎖?")]
    public bool UnlockedByDefault = false;
    [Tooltip("解鎖條件(如果不是預設解鎖，需要檢查的 ProgressFlag)")]
    public string RequiredFlag; // 如果不是預設解鎖，需要檢查的 ProgressFlag (例如："key_sister_room_bought")

    
    [Tooltip("可用時段")]
    public List<int> AllowedPhaseIndices; //可用時段 (使用 TimeSystemModel 的 Phase 索引)

    
    [Tooltip("進入的可疑度 (進入此地點的基礎代價)")]
    public int SuspicionCost = 0; // 這是玩家「一選擇這個地點」就會立刻增加的 Suspicion 值

    
    // 這些是「傳遞給小遊戲場景」的參數
    [Tooltip("環境變數")]
    public List<string> MinigameParameters; // 例如：["hide_closet", "hide_under_bed"] (可用躲藏點)
                                          // 例如：["door_locked"] (門是鎖上的)
}