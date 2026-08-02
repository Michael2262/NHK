using System.Collections.Generic;

// ============================================================
// StatPackagePreviewPresenter.cs
// ============================================================
// hover 預覽的「協調器」（純靜態，不掛在物件上）。
//
// 職責：
//   1. 拿 packageID → 呼叫 StatChangeService.Preview() 試算（不套用）。
//   2. 把結果拆成「主角數值」與「女主角數值」兩份。
//   3. 分別交給 LobbyUI_V2 / HeroineUI 顯示 +X / -X。
//
// 邏輯（試算）在 Model；本類別只做拆分與轉發，UI 只負責顯示（CLAUDE.md 鐵則 5）。
//
// 由 StatPackagePreviewHoverTrigger 在滑鼠進入 / 離開時呼叫。
// ============================================================

public static class StatPackagePreviewPresenter
{
    /// <summary>
    /// 總開關：關閉後，整個「hover 預先預覽」不顯示（變動飄字不受影響）。
    /// 由 LobbyUI_V2 的 Inspector 選項驅動。預設開啟。
    /// </summary>
    public static bool Enabled = true;

    /// <summary>顯示指定套組的預覽。找不到套組或無變化時等同 Hide()。</summary>
    public static void Show(string packageID)
    {
        if (!Enabled) return; // 總開關關閉：不顯示任何預先預覽
        if (string.IsNullOrEmpty(packageID)) { Hide(); return; }

        var service = GameStatusService.Instance;
        var statChange = service?.StatChangeService;
        if (statChange == null) return;

        // 女主角項以 HeroineUI 目前顯示的角色為對象；面板沒開時為 null（試算會自動跳過女主角項）。
        string heroineID = HeroineUI.Instance != null ? HeroineUI.Instance.CurrentHeroineID : null;

        var items = statChange.Preview(packageID, heroineID);

        var protagonistItems = new List<StatPreviewItem>();
        var heroineItems = new List<StatPreviewItem>();

        if (items != null)
        {
            foreach (var it in items)
            {
                if (it.IsHeroine) heroineItems.Add(it);
                else protagonistItems.Add(it);
            }
        }

        LobbyUI_V2.Instance?.ShowStatPreview(protagonistItems);
        HeroineUI.Instance?.ShowStatPreview(heroineItems);
    }

    /// <summary>清掉兩個面板上的預覽字。</summary>
    public static void Hide()
    {
        LobbyUI_V2.Instance?.ClearStatPreview();
        HeroineUI.Instance?.ClearStatPreview();
    }
}
