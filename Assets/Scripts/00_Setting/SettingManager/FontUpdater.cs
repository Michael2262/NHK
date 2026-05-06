using UnityEngine;
using TMPro; // 引用 TextMesh Pro 命名空間
using UnityEngine.Localization; // 引用 Localization 命名空間
using UnityEngine.Localization.Settings; // 引用 Localization 設定
using UnityEngine.ResourceManagement.AsyncOperations; // 引用非同步操作

public class FontUpdater : MonoBehaviour
{
    // [Tooltip("將 Localized Font Asset Table 中的 UI_MainFont 項目拖曳到這裡")]
    // 這是我們在 Inspector 中連結到 Asset Table 的接口
    public LocalizedAsset<TMP_FontAsset> localizedFont;

    private void OnEnable()
    {
        // 當此物件啟用時，開始監聽 localizedFont 的變化
        // 當語言切換時，Asset Table 會找到對應的字體，並透過這個事件通知我們
        localizedFont.AssetChanged += OnFontChanged;
    }

    private void OnDisable()
    {
        // 當此物件被禁用或銷毀時，務必取消監聽，避免記憶體洩漏
        localizedFont.AssetChanged -= OnFontChanged;
    }

    // 當字體資源成功載入後，這個函式會被自動呼叫
    private void OnFontChanged(TMP_FontAsset newFont)
    {
        // 先檢查後備字體列表是否為空
        if (TMP_Settings.fallbackFontAssets == null || TMP_Settings.fallbackFontAssets.Count == 0)
        {
            // 如果是空的，就新增一個
            TMP_Settings.fallbackFontAssets = new System.Collections.Generic.List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets.Add(newFont);
        }
        else
        {
            // 如果不是空的，就替換第一個
            TMP_Settings.fallbackFontAssets[0] = newFont;
        }

        // 現在可以安全地印出日誌了
        Debug.Log($"字體已成功切換為: {newFont.name}");
    }
}