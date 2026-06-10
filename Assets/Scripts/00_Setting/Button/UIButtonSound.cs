using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 確保這個腳本掛載的物件上一定有 Button 組件
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("在 AudioManager 中設定的音效 Key")]
    [SerializeField] private string soundKey = "ui_button_click"; // 預設的按鈕點擊音效 Key

    [Tooltip("滑鼠移入點擊範圍時播放的音效 Key（留空則不播）")]
    [SerializeField] private string hoverSoundKey = "ui_button_hover"; // 預設的滑鼠移入音效 Key

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        // 將 OnClick 方法添加到按鈕的點擊事件列表中
        button.onClick.AddListener(PlayClickSound);
    }

    void OnDestroy()
    {
        // 當物件銷毀時，移除監聽，避免錯誤
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    /// <summary>
    /// 播放點擊音效
    /// </summary>
    public void PlayClickSound()
    {
        PlaySound(soundKey);
    }

    /// <summary>
    /// 滑鼠移入點擊範圍時觸發（由 EventSystem 呼叫）。
    /// 需要場景中有 EventSystem，且按鈕能接收射線（Image 的 Raycast Target 勾選）。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 按鈕不可互動時不播 hover 音效
        if (button != null && !button.interactable) return;
        PlaySound(hoverSoundKey);
    }

    private void PlaySound(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        // 直接呼叫 AudioManager 的單例來播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(key);
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance 未找到！");
        }
    }
}