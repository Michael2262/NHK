/// <summary>
/// 地圖使用範圍（MapScope）對應的 scene-lifetime 旗標名稱。
///
/// scope 不另立 enum / 單例 —— 它只活在地圖場景、只被「地點亮暗」與「IxMenu 選單篩選」讀取，
/// 因此直接用一個 scene 旗標承載，切場景時由 ProgressFlagModel 自動清除。
///
/// 用途（此旗標＝「當前地圖處於哪個模式」的唯一真相來源）：
///   - Task_InitMapScope 進場時依 entryID 設定其中一個旗標。
///   - MapSpotView 讀 Unlock 旗標決定地點亮/暗。
///   - IxMenuService 讀此旗標得知當前 scope，再比對每個 IxOption.scope（enum）決定選單顯示。
///     （選項本身用 IxScope 下拉宣告顯示模式，不再填字串。）
///
/// 集中在此的唯一理由：避免這兩個字串散落在多個檔案變成魔法字串。
/// </summary>
public static class MapScopeFlags
{
    /// <summary>挑戰模式：亮「未解鎖」的地點；選單顯示「前往（跑 minigame）」類選項。</summary>
    public const string Unlock = "Scope_Unlock";

    /// <summary>拜訪模式：亮「已解鎖」的地點；選單顯示活動類選項。</summary>
    public const string Visit = "Scope_Visit";
}
