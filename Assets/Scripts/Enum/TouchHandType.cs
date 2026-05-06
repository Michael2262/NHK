/// <summary>
/// (新功能) 用於 AutoRecordedLastTouch 系統，標記觸控物件的類型
/// </summary>
public enum TouchHandType
{
    None,       // (預設) 不參與紀錄
    LeftHand,   // 紀錄在左手欄位
    RightHand,  // 紀錄在右手欄位
    RandomHand, // 隨機填入左手或右手 (若已滿則隨機取代)
    Special1,   // 紀錄在特殊欄位 1
    Special2    // 紀錄在特殊欄位 2
}