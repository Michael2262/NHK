using UnityEngine;
using UnityEngine.EventSystems; 

// 2. 讓這個類別實作 (implement) EventSystem 的介面
[RequireComponent(typeof(Collider2D))]
public class CursorArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("此區域的鼠標 (Area-Specific)")]
    public Texture2D normalTexture;
    public Vector2 normalHotspot = Vector2.zero;
    public Texture2D clickTexture;
    public Vector2 clickHotspot = Vector2.zero;

    // 3. 當 EventSystem (透過 Physics 2D Raycaster) 偵測到滑鼠「進入」
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 檢查 GlobalCursorManager 是否存在
        if (GlobalCursorManager.Instance != null)
        {
            // 呼叫管理器，將鼠標設定為此腳本上指定的圖案
            GlobalCursorManager.Instance.SetCursorArea(
                normalTexture,
                normalHotspot,
                clickTexture,
                clickHotspot
            );
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
        }
    }

    
}