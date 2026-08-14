using UnityEngine;
using UnityEngine.EventSystems;

// 2. 讓這個類別實作 (implement) EventSystem 的介面
[RequireComponent(typeof(Collider2D))]
public class CursorArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("此區域的鼠標 (Area-Specific)")]
    [Tooltip("只換圖案；hotspot（對位點/縮放軸心）沿用預設游標的設定。")]
    public Texture2D normalTexture;
    public Texture2D clickTexture;

    [Header("進入時放大 (可選)")]
    [Tooltip("勾選後，滑鼠進入此區域時游標會放大並「維持」，離開才縮回。")]
    public bool enableHoverScale = false;
    [Tooltip("進入時放大到的倍率（1 = 原大小）")]
    public float hoverScale = 1.2f;
    [Tooltip("放大 / 縮回的補間時間（秒）")]
    public float hoverDuration = 0.12f;

    // 3. 當 EventSystem (透過 Physics 2D Raycaster) 偵測到滑鼠「進入」
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 檢查 GlobalCursorManager 是否存在
        if (GlobalCursorManager.Instance != null)
        {
            // 呼叫管理器，只換圖案；hotspot 沿用預設游標的設定
            GlobalCursorManager.Instance.SetCursorArea(
                normalTexture,
                clickTexture
            );

            // 可選：進入時放大並維持
            if (enableHoverScale)
            {
                GlobalCursorManager.Instance.ApplyHoverScale(hoverScale, hoverDuration);
            }
        }
    }

    // 4. 當 EventSystem (透過 Physics 2D Raycaster) 偵測到滑鼠「離開」
    public void OnPointerExit(PointerEventData eventData)
    {
        // 檢查 GlobalCursorManager 是否存在
        if (GlobalCursorManager.Instance != null)
        {
            // 呼叫管理器，將鼠標恢復為「預設」圖案
            GlobalCursorManager.Instance.ResetToDefaultCursor();

            // 可選：離開時縮回原大小
            if (enableHoverScale)
            {
                GlobalCursorManager.Instance.ClearHoverScale(hoverDuration);
            }
        }
    }


}
