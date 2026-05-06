using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 觸碰事件發送 API。
/// 負責將觸碰資訊轉送給 AutoRecordedLastTouch 管理器。
/// </summary>
public class TouchRegistryAPI : MonoBehaviour
{
    [Header("啟用設定")]
    [Tooltip("是否啟用觸碰記錄功能")]
    public bool isEnabled = true;

    [Header("預設記錄設定")]
    [Tooltip("若觸碰時沒有指定發送者，預設記錄誰（通常是物件自身）")]
    public GameObject defaultSender;

    [Tooltip("預設觸碰手類型")]
    public TouchHandType defaultHandType;

    // ── 修正1：改用非 static 欄位，每次使用前重新驗證引用是否仍然存活 ──
    // 原本的 static _cachedManager 在場景切換後可能殘留已銷毀的引用，
    // Unity 的 == null 運算子會偵測到已銷毀的物件，但 static 不隨場景重置，
    // 仍可能在下個場景誤用上一個場景的物件。改為 instance 欄位可避免此問題。
    private AutoRecordedLastTouch _cachedManager;

    private void Awake()
    {
        if (defaultSender == null) defaultSender = gameObject;
    }

    /// <summary>
    /// 使用 Inspector 上設定的預設值進行記錄
    /// </summary>
    public void RegisterDefault()
    {
        SendToManager(defaultSender, defaultHandType);
    }

    /// <summary>
    /// 由程式碼呼叫，自由指定發送者與類型
    /// </summary>
    public void RegisterCustom(GameObject sender, TouchHandType handType)
    {
        SendToManager(sender, handType);
    }

    /// <summary>
    /// 供 UI Button UnityEvent 使用，透過 int index 指定手類型
    /// </summary>
    public void RegisterAsType(int handTypeIndex)
    {
        SendToManager(defaultSender, (TouchHandType)handTypeIndex);
    }

    private void SendToManager(GameObject sender, TouchHandType type)
    {
        if (!isEnabled) return;

        // 若快取為空，或快取目標已被銷毀（Unity operator== 會偵測），則重新搜尋
        if (_cachedManager == null)
        {
            _cachedManager = Object.FindAnyObjectByType<AutoRecordedLastTouch>();
        }

        if (_cachedManager != null)
        {
            _cachedManager.RegisterTouch(sender, type);
        }
        else
        {
            Debug.LogWarning($"[TouchRegistryAPI] 找不到 AutoRecordedLastTouch 管理器，無法記錄來自 {sender.name} 的觸碰。");
        }
    }
}