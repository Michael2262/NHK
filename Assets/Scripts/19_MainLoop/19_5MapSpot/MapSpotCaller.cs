using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// MapSpotCaller（掛在每個地圖地點按鈕上）
/// 
/// 點擊時呼叫 IxMenuService.Instance.Show() 顯示互動選單。
/// 支援手動擺放或動態生成，不需在任何地方預先註冊。
/// 
/// Inspector 設定：
///   - spotNameKey：地點名稱 key（多語系）
///   - phaseImages：依時間相位切換的地點圖片
///   - options：互動選項列表（含 Flag 條件 + 優先次序 + UnityEvent）
/// </summary>
[RequireComponent(typeof(Button))]
public class MapSpotCaller : MonoBehaviour
{
    [Header("=== 地點基本資訊 ===")]
    [Tooltip("地點名稱 key（用於 LocalizeUIText 翻譯）")]
    [SerializeField] private string spotNameKey;

    [Header("=== 依時間相位顯示的圖片 ===")]
    [Tooltip("若當前 phase 沒有定義圖片，會使用前一 phase 的")]
    [SerializeField]
    private List<IxMenuService.SpotPhaseImage> phaseImages
        = new List<IxMenuService.SpotPhaseImage>();

    [Header("=== 互動選項 ===")]
    [Tooltip("依 Flag + 優先次序篩選，最多顯示數量由 IxMenuService 的按鈕數決定")]
    [SerializeField]
    private List<IxMenuService.IxOption> options
        = new List<IxMenuService.IxOption>();

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(OnSpotClicked);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnSpotClicked);
    }

    private void OnSpotClicked()
    {
        if (IxMenuService.Instance == null)
        {
            Debug.LogWarning("[MapSpotCaller] IxMenuService 單例不存在，請確認 Map 場景中已放置。");
            return;
        }

        IxMenuService.Instance.Show(spotNameKey, phaseImages, options);
    }

    /// <summary>
    /// 關閉 IxMenu。方便在 IxOption 的 UnityEvent 上直接綁定。
    /// </summary>
    public void CloseIxMenu()
    {
        if (IxMenuService.Instance != null)
            IxMenuService.Instance.Hide();
    }
}