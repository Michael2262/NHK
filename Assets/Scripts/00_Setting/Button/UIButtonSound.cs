using UnityEngine;
using UnityEngine.UI;

// 確保這個腳本掛載的物件上一定有 Button 組件
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Tooltip("在 AudioManager 中設定的音效 Key")]
    [SerializeField] private string soundKey = "ui_button_click"; // 預設的按鈕點擊音效 Key

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
        // 直接呼叫 AudioManager 的單例來播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(soundKey);
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance 未找到！");
        }
    }
}