using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[AddComponentMenu("UI/Hide GameObject On Click")]
[DisallowMultipleComponent]
public sealed class HideGameObjectOnClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Target")]
    [Tooltip("要被隱藏的目標。不指定時會隱藏自己。")]
    [SerializeField] private GameObject target;

    [Header("Behavior")]
    [Tooltip("是否使用 SetActive(false) 直接關閉物件。若關閉，則改用 CanvasGroup 讓它不可見且不可互動。")]
    [SerializeField] private bool useSetActive = true;

    [Tooltip("點擊後延遲多少秒才隱藏。0 代表立即隱藏。")]
    [Min(0f)][SerializeField] private float delay = 0f;

    private Button _button;
    private CanvasGroup _cachedGroup;

    private void Awake()
    {
        if (target == null) target = gameObject;

        // 若這個物件上有 Button，就綁定 onClick；沒有也沒關係，下面還有 IPointerClickHandler 作保險
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(Hide);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(Hide);
    }

    /// <summary>
    /// 供 Button.onClick 或外部事件呼叫。
    /// </summary>
    [ContextMenu("Hide Now")]
    public void Hide()
    {
        if (!isActiveAndEnabled) return;

        if (delay > 0f)
            Invoke(nameof(DoHide), delay);
        else
            DoHide();
    }

    // 沒有 Button 的情況也能靠點擊觸發
    public void OnPointerClick(PointerEventData eventData)
    {
        // 如果已經有 Button 綁 onClick，就避免重複觸發
        if (_button != null) return;
        Hide();
    }

    private void DoHide()
    {
        if (target == null) return;

        if (useSetActive)
        {
            target.SetActive(false);
        }
        else
        {
            if (_cachedGroup == null) _cachedGroup = target.GetComponent<CanvasGroup>();
            if (_cachedGroup == null) _cachedGroup = target.AddComponent<CanvasGroup>();

            _cachedGroup.alpha = 0f;
            _cachedGroup.interactable = false;
            _cachedGroup.blocksRaycasts = false;
        }
    }
}
