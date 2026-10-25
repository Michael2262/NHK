using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 已退役的不滿值 UI；只保留序列化欄位供手動搬移與清除。
/// 掛在獨立的填色 Image 上，外框與背景由其他 UI 物件提供。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class FrustrationSliderVisualizer : MonoBehaviour
{
    [Header("連動角色")]
    [SerializeField] private string heroineID = "sister";

    [Header("填色與平滑過渡")]
    [SerializeField] private Color fillColor = new Color(1f, 0.4f, 0.35f, 1f);
    [Tooltip("每次目標改變後，從目前顯示位置過渡到新值所需秒數。")]
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("比例門檻通知")]
    [Tooltip("填色向上抵達或跨過 25% 時觸發一次；數值降到門檻以下後可再次觸發。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedQuarter = new UnityEvent();
    [Tooltip("填色向上抵達或跨過 50% 時觸發一次；數值降到門檻以下後可再次觸發。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedHalf = new UnityEvent();
    [Tooltip("填色向上抵達或跨過 75% 時觸發一次；數值降到門檻以下後可再次觸發。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedThreeQuarters = new UnityEvent();

    [Header("滿格通知")]
    [Tooltip("填色抵達滿格時觸發一次；維持滿格不重複觸發，不自動歸零。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedMax = new UnityEvent();

    public UnityEvent OnReachedQuarter => onReachedQuarter;
    public UnityEvent OnReachedHalf => onReachedHalf;
    public UnityEvent OnReachedThreeQuarters => onReachedThreeQuarters;
    public UnityEvent OnReachedMax => onReachedMax;
    public float DisplayedValue => 0f;

    private void OnEnable()
    {
        var image = GetComponent<Image>();
        if (image != null) image.enabled = false;
        WarnRetired();
        enabled = false;
    }

    // 只保留舊場景的型別與綁定，不觸發原本的門檻事件。
    public void SetHeroineID(string id) { heroineID = id; }
    public void SetFrustration(int value) { WarnRetired(); }
    public void AddFrustration(int amount) { WarnRetired(); }
    public void SetFrustrationMax(int value) { WarnRetired(); }
    private void WarnRetired() => Debug.LogWarning("[FrustrationSliderVisualizer] 不滿值已退役，請移除此 UI 與相關事件綁定。", this);
}
