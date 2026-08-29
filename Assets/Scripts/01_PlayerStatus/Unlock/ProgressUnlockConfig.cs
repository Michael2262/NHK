using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 解鎖規則分組容器。
/// 單純是一個 Rule Asset 的「分組收納盒」，方便在 GameStatusService 的 Inspector
/// 以角色為單位整批拖放，而不是把數十條 Rule 散落在同一個 List 裡。
///
/// 設計原則：
/// - Config 本身「無狀態、無權威」，不決定任何解鎖邏輯
/// - 規則的 heroineID、條件、動作，全部由 Rule Asset 自己定義
/// - 建議每個 Config 對應一位角色的規則 (命名如 ProgressUnlock_Sister.asset)，
///   但技術上不強制——Config 內混放多位角色的 Rule 也能運作
///
/// 建議在 Project 視窗 Create → Game → Progress → Progress Unlock Config 建立。
/// </summary>
[CreateAssetMenu(
    menuName = "Game/Progress/Progress Unlock Config",
    fileName = "ProgressUnlock_NewGroup"
)]
public class ProgressUnlockConfig : ScriptableObject
{
    [Tooltip(
        "此分組內的所有解鎖規則。\n" +
        "每條 Rule Asset 自帶 heroineID 與完整條件，Config 只負責收納。\n" +
        "OnlyCondition 類型的規則會被 Manager 跳過 (它們只用來當 UI 條件)。"
    )]
    public List<ProgressUnlockRuleAsset> rules = new List<ProgressUnlockRuleAsset>();

    [Header("數值映射 (Sync / 鏡像)")]
    [Tooltip(
        "把某個數值忠實同步到 Progress Value 變數 (數值一變就跟著變)。\n" +
        "女主角數值 (Libido / Trust / HCount) 需填 heroineID；主角數值留空。\n" +
        "與門檻規則不同：這裡不判斷大小，只是讓 Value 永遠等於來源的當下值。"
    )]
    public List<StatMirror> mirrors = new List<StatMirror>();

    [Header("布林 Flag 映射 (Sync / 鏡像)")]
    [Tooltip(
        "把主角布林狀態忠實同步到 Progress Flag（狀態一變就跟著開關）。\n" +
        "來源支援 BadHealthy / BadDependency；true 時加入 Persistent Flag，false 時移除 Flag。\n" +
        "此映射為單向：修改 Progress Flag 不會反向修改主角狀態。"
    )]
    public List<BoolFlagMirror> flagMirrors = new List<BoolFlagMirror>();
}
