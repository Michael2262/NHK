using UnityEngine;

/// <summary>
/// 掛在 Collider2D 所在的 GameObject 上，用來覆寫預設的 ID（GameObject 名稱）。
/// 只有在 GameObject 名稱不適合當 ID 時才需要使用（例如多個同名物件）。
/// 此元件為純資料標記，不執行任何邏輯。
/// </summary>
public class ColliderIdOverride : MonoBehaviour
{
    [Tooltip("覆寫用的 Collider ID（留空則仍使用 GameObject 名稱）")]
    public string id;
}
