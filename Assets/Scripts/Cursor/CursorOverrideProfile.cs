using UnityEngine;

/// <summary>
/// 一個可重複使用的「鼠標覆蓋設定檔」。
/// 你可以為 "等待中"、"對話中"、"拖曳中" 等狀態各建立一個此腳本的 prefab 或物件。
/// 它包含了要設定的鼠標圖案（"設置滑鼠變數"），
/// 以及用來「開始鎖定」和「結束鎖定」的 API。
/// </summary>
public class CursorOverrideProfile : MonoBehaviour
{
    [Header("覆蓋用的鼠標圖案")]
    [Tooltip("要覆蓋的「平常」狀態鼠標圖案")]
    public Texture2D normalTexture;
    public Vector2 normalHotspot = Vector2.zero;

    [Space]
    [Tooltip("要覆蓋的「點擊」狀態鼠標圖案 (可選)")]
    public Texture2D clickTexture;
    public Vector2 clickHotspot = Vector2.zero;

    [Tooltip("如果為 true，點擊時將使用與平常相同的圖案 (例如旋轉中的沙漏)")]
    public bool useSameTextureForClick = true;


    // --- 這是你要的「傳遞出去鎖定住」的程式碼 ---
    /// <summary>
    /// 「開始」鎖定。
    /// 呼叫此函式，GlobalCursorManager 將會切換到這個腳本上設定的圖案，
    /// 並鎖定，忽略所有 CursorArea 的變化。
    /// </summary>
    [ContextMenu("手動啟動滑鼠覆蓋")]
    public void StartOverride()
    {
        if (GlobalCursorManager.Instance == null)
        {
            Debug.LogError("GlobalCursorManager 不存在，無法設定 Override 鼠標！");
            return;
        }

        Texture2D textureToUseForClick = clickTexture;
        Vector2 hotspotToUseForClick = clickHotspot;

        // 如果勾選了 "點擊時使用相同圖案"，或者 "點擊圖案" 根本沒設定
        if (useSameTextureForClick || clickTexture == null)
        {
            textureToUseForClick = normalTexture;
            hotspotToUseForClick = normalHotspot;
        }

        // 呼叫全局管理器的「鎖定」API，並 "傳遞" 我們自己的變數
        GlobalCursorManager.Instance.SetOverrideCursor(
            normalTexture,
            normalHotspot,
            textureToUseForClick,
            hotspotToUseForClick
        );
    }

    // 
    /// <summary>
    /// 「結束」鎖定。
    /// 呼叫此函式，GlobalCursorManager 將會解除鎖定，
    /// 鼠標會自動恢復成它當下應有的圖案。
    /// </summary>
    [ContextMenu("手動結束滑鼠覆蓋")]
    public void StopOverride()
    {
        if (GlobalCursorManager.Instance != null)
        {
            GlobalCursorManager.Instance.ReleaseOverrideCursor();
        }
    }
}