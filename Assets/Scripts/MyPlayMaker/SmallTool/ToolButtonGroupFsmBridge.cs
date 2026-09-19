using UnityEngine;

/// <summary>
/// 掛在 PlayMakerFSM 所在物件，供該物件上的群組導覽 Action 共用控制器參照。
/// 只需設定一次；不自動搜尋場景或啟用選單。
/// </summary>
[DisallowMultipleComponent]
public class ToolButtonGroupFsmBridge : MonoBehaviour
{
    [Tooltip("此物件上所有 ToolButtonGroupNavigate Action 共用的群組控制器。")]
    [SerializeField] private ToolButtonGroupDisplayControl controller;

    public ToolButtonGroupDisplayControl Controller => controller;
}
