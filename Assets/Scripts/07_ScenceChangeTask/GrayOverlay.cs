using UnityEngine;

/// <summary>掛在共用場景中持續啟用的物件上，供劇情與 FSM 開關壓灰圖層。</summary>
[DisallowMultipleComponent]
public sealed class GrayOverlay : MonoBehaviour
{
    [SerializeField, Tooltip("拖入共用的 GRAY_OVERLAY 圖層物件。")]
    private GameObject overlayTarget;

    public static void SetVisible(bool visible)
    {
        GrayOverlay controller = null;
        foreach (var candidate in Object.FindObjectsByType<GrayOverlay>(FindObjectsSortMode.None))
        {
            if (!candidate.isActiveAndEnabled) continue;
            if (controller != null)
            {
                Debug.LogWarning("[GrayOverlay] 找到多個啟用的控制元件，請在共用場景中只保留一個。", candidate);
                return;
            }
            controller = candidate;
        }

        if (controller == null)
        {
            Debug.LogWarning("[GrayOverlay] 找不到啟用的控制元件，請將 GrayOverlay 掛在持續啟用的物件上。");
            return;
        }

        var target = controller.overlayTarget;
        if (target == null)
        {
            Debug.LogWarning("[GrayOverlay] 請在 Inspector 的 Overlay Target 指定壓灰圖層。", controller);
            return;
        }

        if (controller.transform.IsChildOf(target.transform))
        {
            Debug.LogWarning("[GrayOverlay] 關閉圖層會連帶停用控制元件，請把控制元件移到圖層的父物件或其他常駐物件。", controller);
            return;
        }

        if (target.activeSelf != visible) target.SetActive(visible);
    }
}
