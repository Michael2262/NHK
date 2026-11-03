using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 由 Event Trigger 傳入按下、放開事件；按下立即通知，按滿指定秒數再通知一次。
/// 請掛在接收輸入的按鈕上，進度 Image 使用獨立的子物件或其他物件。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EventTrigger))]
public class ButtonHoldTrigger : MonoBehaviour
{
    [Header("長按設定")]
    [Tooltip("持續按住多少秒後觸發成功，不需要放開。每次按下時固定本輪秒數。")]
    [Min(0.01f)] [SerializeField] private float holdDuration = 1f;
    [Tooltip("勾選後使用實際時間，不受 Time.timeScale 影響。")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("進度圖片（可留空）")]
    [Tooltip("請指定獨立 Image，不可使用按鈕本身或父物件。自動設為由左到右填滿；放開或成功後隱藏。")]
    [SerializeField] private Image progressImage;

    [Header("事件")]
    [Tooltip("有效按下的瞬間觸發一次。")]
    [SerializeField] private UnityEvent onPressed = new UnityEvent();
    [Tooltip("按滿指定秒數時立即觸發一次；繼續按住不會重複觸發。")]
    [SerializeField] private UnityEvent onHoldCompleted = new UnityEvent();

    public UnityEvent OnPressed => onPressed;
    public UnityEvent OnHoldCompleted => onHoldCompleted;

    private readonly HoldState state = new HoldState();
    private Selectable selectable;
    private Image fillImage;
    private bool currentUseUnscaledTime;

    private bool CanReceiveInput => isActiveAndEnabled &&
        (selectable == null || (selectable.isActiveAndEnabled && selectable.IsInteractable()));
    private double CurrentTime => currentUseUnscaledTime ? Time.unscaledTimeAsDouble : Time.timeAsDouble;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (progressImage == null) return;
        if (transform.IsChildOf(progressImage.transform))
        {
            Debug.LogWarning("[ButtonHoldTrigger] 進度 Image 不可放在按鈕本身或父物件，請改用獨立子物件；本次不顯示進度條。", this);
            return;
        }

        fillImage = progressImage;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;
        fillImage.enabled = true;
    }

    private void OnEnable() => CancelPress();
    private void OnDisable() => CancelPress();

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) CancelPress();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) CancelPress();
    }

    /// <summary>綁到 Event Trigger 的 Pointer Down，選擇 Dynamic BaseEventData 版本。</summary>
    public void HandlePointerDown(BaseEventData data)
    {
        if (!CanReceiveInput || !(data is PointerEventData pointer) ||
            pointer.button != PointerEventData.InputButton.Left || state.IsPressed) return;

        currentUseUnscaledTime = useUnscaledTime;
        if (!state.Begin(pointer.pointerId, CurrentTime, holdDuration)) return;
        RenderProgress();
        // 顯示 Image 的 OnEnable 回呼可能取消本輪或停用按鈕。
        if (CanReceiveInput && state.Matches(pointer.pointerId)) onPressed.Invoke();
    }

    /// <summary>綁到 Pointer Up；只接受開始本輪長按的指標，其他手指不會打斷。</summary>
    public void HandlePointerUp(BaseEventData data)
    {
        if (!(data is PointerEventData pointer) || pointer.button != PointerEventData.InputButton.Left ||
            !state.Matches(pointer.pointerId)) return;

        // 放開與 Update 同幀時，仍依實際經過時間判斷是否已達標。
        bool completed = CanReceiveInput && state.TryComplete(CurrentTime);
        CancelPress();
        if (completed && CanReceiveInput) onHoldCompleted.Invoke();
    }

    /// <summary>可選綁到 Pointer Exit：指標離開按鈕範圍時取消本輪。</summary>
    public void HandlePointerExit(BaseEventData data)
    {
        if (data is PointerEventData pointer && state.Matches(pointer.pointerId)) CancelPress();
    }

    /// <summary>外部可透過 UnityEvent 取消長按，取消不觸發成功事件。</summary>
    public void CancelPress()
    {
        state.Reset();
        RenderProgress();
    }

    private void Update()
    {
        if (!state.IsPressed) return;
        if (!CanReceiveInput)
        {
            CancelPress();
            return;
        }

        bool completed = state.TryComplete(CurrentTime);
        // 先標記完成並隱藏圖片，再通知外部，避免回呼重複觸發。
        RenderProgress();
        if (completed && CanReceiveInput && state.IsCompleted) onHoldCompleted.Invoke();
    }

    private void RenderProgress()
    {
        if (fillImage == null) return;
        bool visible = state.IsPressed && !state.IsCompleted;
        fillImage.fillAmount = visible ? (float)state.Progress(CurrentTime) : 0f;
        if (fillImage.gameObject.activeSelf != visible) fillImage.gameObject.SetActive(visible);
    }

    /// <summary>單次長按的純 C# 狀態；由 UI 傳入時間，不讀取 Unity 輸入或修改遊戲資料。</summary>
    private sealed class HoldState
    {
        public bool IsPressed { get; private set; }
        public bool IsCompleted { get; private set; }
        private int pointerID;
        private double startedAt;
        private double duration;

        public bool Begin(int id, double now, double seconds)
        {
            if (IsPressed) return false;
            pointerID = id;
            startedAt = now;
            duration = double.IsNaN(seconds) || double.IsInfinity(seconds) ? 1d : Math.Max(0.01d, seconds);
            IsPressed = true;
            IsCompleted = false;
            return true;
        }

        public bool Matches(int id) => IsPressed && pointerID == id;

        public double Progress(double now)
            => IsPressed ? Math.Max(0d, Math.Min(1d, (now - startedAt) / duration)) : 0d;

        public bool TryComplete(double now)
        {
            if (!IsPressed || IsCompleted || now - startedAt < duration) return false;
            IsCompleted = true;
            return true;
        }

        public void Reset()
        {
            IsPressed = false;
            IsCompleted = false;
        }
    }
}
