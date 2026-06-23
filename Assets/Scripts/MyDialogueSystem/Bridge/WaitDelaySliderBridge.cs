using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 將 Slider 綁定到 StoryManager.customDelay（{{wait}} 的延遲秒數）。
/// 掛在 Slider 物件上即可：
///   - 啟動時讀取目前值初始化滑桿
///   - 拖動滑桿 → 立刻覆寫 customDelay
///   - 其他來源改了 customDelay → 透過事件即時同步回滑桿
/// StoryManager 位於不卸載場景，保證早於本元件生成。
/// </summary>
[RequireComponent(typeof(Slider))]
public class WaitDelaySliderBridge : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Reset()
    {
        slider = GetComponent<Slider>();
    }

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (slider == null || StoryManager.Instance == null) return;

        // 先用目前的 customDelay 初始化 slider（會被 slider 的 min/max 夾住）
        slider.SetValueWithoutNotify(StoryManager.Instance.CustomDelay);

        // 把夾住後的值同步回 StoryManager，確保兩邊一致
        StoryManager.Instance.SetCustomDelay(slider.value);

        // 監聽：使用者拖動 / 其他來源改值
        slider.onValueChanged.AddListener(OnSliderChanged);
        StoryManager.Instance.OnCustomDelayChanged += OnDelayChangedExternally;
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderChanged);

        if (StoryManager.Instance != null)
            StoryManager.Instance.OnCustomDelayChanged -= OnDelayChangedExternally;
    }

    // 使用者拖動滑桿 → 量化成 0.1 階後寫入 StoryManager
    // （StoryManager 會回拋 OnCustomDelayChanged，使滑桿外觀 snap 到量化後的值）
    private void OnSliderChanged(float value)
    {
        value = Mathf.Round(value * 10f) / 10f;
        if (StoryManager.Instance != null)
            StoryManager.Instance.SetCustomDelay(value);
    }

    // 其他來源改了 customDelay → 更新滑桿外觀（不再回觸發 onValueChanged）
    private void OnDelayChangedExternally(float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }
}
