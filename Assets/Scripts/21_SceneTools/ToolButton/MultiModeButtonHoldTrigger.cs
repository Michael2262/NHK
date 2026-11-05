using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>由 Event Trigger 接收指標事件，依目前模式執行按下與長按完成事件。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EventTrigger))]
public sealed class MultiModeButtonHoldTrigger : MonoBehaviour
{
    [Serializable]
    private sealed class ButtonHoldMode
    {
        public string modeID;
        [Tooltip("顯示於按鈕上的 Text Table key。")]
        public string textTableKey;
        [Min(0.01f)] public float holdDuration = 1f;
        public UnityEvent onPressed = new UnityEvent();
        public UnityEvent onHoldCompleted = new UnityEvent();
        public bool showCompletionFeedback;
        [Tooltip("完成提示的 Text Table key，留空使用按鈕的預設值。")]
        public string completionTextKey;
    }

    [Header("按鈕與模式")]
    [Tooltip("供其他程式指定按鈕，同時啟用的按鈕應使用不同 ID。")]
    [SerializeField] private string buttonID;
    [Tooltip("預設使用第一個模式，每個模式應有不同的 ID。")]
    [SerializeField] private List<ButtonHoldMode> modes = new List<ButtonHoldMode>();
    [Header("共用長按設定")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("指定專門顯示進度的 Image。")]
    [SerializeField] private Image progressImage;
    [SerializeField] private TMP_Text labelText;
    [Header("共用完成提示")]
    [Tooltip("與按鈕名稱分開的文字元件，請保持其物件啟用。")]
    [SerializeField] private TMP_Text completionText;
    [SerializeField] private string defaultCompletionTextKey;
    [SerializeField] private string successSoundKey = "action_success";

    private const double FeedbackSeconds = 0.2d;
    private static readonly List<MultiModeButtonHoldTrigger> activeButtons = new List<MultiModeButtonHoldTrigger>();
    private ButtonHoldMode currentMode;
    private Selectable selectable;
    private int? pressedPointerID;
    private bool holdCompleted;
    private bool pressUsesUnscaledTime;
    private double startedAt;
    private double duration;
    private double hideFeedbackAt;

    private double PressTime => pressUsesUnscaledTime ? Time.unscaledTimeAsDouble : Time.timeAsDouble;
    private bool CanReceiveInput => isActiveAndEnabled &&
        (selectable == null || (selectable.isActiveAndEnabled && selectable.IsInteractable()));

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (currentMode == null && modes.Count > 0) currentMode = modes[0];
        if (completionText != null && completionText == labelText)
        {
            Debug.LogWarning("[MultiModeButtonHoldTrigger] 完成提示與按鈕名稱需要各自的文字元件。", this);
            completionText = null;
        }
        if (completionText != null) completionText.raycastTarget = false;
        if (progressImage == null) return;
        if (transform.IsChildOf(progressImage.transform))
        {
            Debug.LogWarning("[MultiModeButtonHoldTrigger] 請指定獨立的進度圖片，避免影響按鈕本身或父物件。", this);
            progressImage = null;
            return;
        }
        progressImage.raycastTarget = false;
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Horizontal;
        progressImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void OnEnable()
    {
        activeButtons.Add(this);
        CancelPress();
        HideFeedback();
        RefreshLabel();
    }

    private void OnDisable()
    {
        activeButtons.Remove(this);
        CancelPress();
        HideFeedback();
    }

    /// <summary>以按鈕 ID 找到啟用中的元件，再切換模式。找不到或 ID 重複時回傳 false。</summary>
    public static bool SetMode(string buttonID, string modeID)
    {
        if (string.IsNullOrWhiteSpace(buttonID)) return false;
        MultiModeButtonHoldTrigger target = null;
        foreach (var button in activeButtons)
        {
            if (button.buttonID != buttonID) continue;
            if (target != null)
            {
                Debug.LogWarning($"[MultiModeButtonHoldTrigger] 啟用中的按鈕 ID 重複：{buttonID}", button);
                return false;
            }
            target = button;
        }
        return target != null && target.SetMode(modeID);
    }

    /// <summary>供 UnityEvent 綁定，參數填入本按鈕的目標模式 ID。</summary>
    public void SetModeByID(string modeID) => SetMode(modeID);

    /// <summary>切換本按鈕的模式。切換到另一個模式會取消進行中的長按。</summary>
    public bool SetMode(string modeID)
    {
        if (string.IsNullOrWhiteSpace(modeID)) return false;
        ButtonHoldMode target = null;
        foreach (var mode in modes)
        {
            if (mode == null || mode.modeID != modeID) continue;
            if (target != null)
            {
                Debug.LogWarning($"[MultiModeButtonHoldTrigger] 按鈕 {buttonID} 的模式 ID 重複：{modeID}", this);
                return false;
            }
            target = mode;
        }
        if (target == null) return false;
        if (currentMode != target) CancelPress();
        currentMode = target;
        if (isActiveAndEnabled) RefreshLabel();
        return true;
    }

    private void RefreshLabel()
    {
        if (labelText != null) labelText.text = Localize(currentMode?.textTableKey);
    }

    /// <summary>綁到 Event Trigger 的 Pointer Down，選擇 Dynamic BaseEventData。</summary>
    public void HandlePointerDown(BaseEventData data)
    {
        if (!CanReceiveInput || currentMode == null || pressedPointerID.HasValue ||
            !(data is PointerEventData pointer) || pointer.button != PointerEventData.InputButton.Left) return;

        pressedPointerID = pointer.pointerId;
        pressUsesUnscaledTime = useUnscaledTime;
        startedAt = PressTime;
        float seconds = currentMode.holdDuration;
        duration = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 1d : Math.Max(0.01d, seconds);
        if (progressImage != null) progressImage.enabled = true;
        currentMode.onPressed.Invoke();
    }

    /// <summary>綁到 Pointer Up；放開當幀也檢查是否已達標。</summary>
    public void HandlePointerUp(BaseEventData data)
    {
        if (!(data is PointerEventData pointer) || pointer.button != PointerEventData.InputButton.Left ||
            pressedPointerID != pointer.pointerId) return;
        bool completedOnRelease = CanReceiveInput && !holdCompleted && PressTime - startedAt >= duration;
        CancelPress();
        if (completedOnRelease) NotifyCompleted(currentMode);
    }

    /// <summary>可選綁到 Pointer Exit，指標離開時取消。</summary>
    public void HandlePointerExit(BaseEventData data)
    {
        if (data is PointerEventData pointer && pressedPointerID == pointer.pointerId) CancelPress();
    }

    public void CancelPress()
    {
        pressedPointerID = null;
        holdCompleted = false;
        if (progressImage == null) return;
        progressImage.fillAmount = 0f;
        progressImage.enabled = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelPress();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) CancelPress();
    }

    private void Update()
    {
        if (hideFeedbackAt > 0d && Time.unscaledTimeAsDouble >= hideFeedbackAt) HideFeedback();
        if (!pressedPointerID.HasValue) return;
        if (!CanReceiveInput)
        {
            CancelPress();
            return;
        }
        if (holdCompleted) return;
        double elapsed = PressTime - startedAt;
        if (elapsed < duration)
        {
            if (progressImage != null) progressImage.fillAmount = Mathf.Clamp01((float)(elapsed / duration));
            return;
        }
        holdCompleted = true;
        if (progressImage != null) progressImage.enabled = false;
        NotifyCompleted(currentMode);
    }

    private void NotifyCompleted(ButtonHoldMode mode)
    {
        // 先依本輪模式顯示提示；完成事件可以立即切換按鈕功能。
        if (mode.showCompletionFeedback)
        {
            if (completionText != null)
            {
                string key = string.IsNullOrWhiteSpace(mode.completionTextKey) ? defaultCompletionTextKey : mode.completionTextKey;
                completionText.text = Localize(key);
                completionText.enabled = true;
                hideFeedbackAt = Time.unscaledTimeAsDouble + FeedbackSeconds;
            }
            if (!string.IsNullOrWhiteSpace(successSoundKey) && AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(successSoundKey);
        }
        mode.onHoldCompleted.Invoke();
    }

    private void HideFeedback()
    {
        hideFeedbackAt = 0d;
        if (completionText != null) completionText.enabled = false;
    }

    private string Localize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        string text = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);
        if (!string.IsNullOrEmpty(text)) return text;
        Debug.LogWarning($"[MultiModeButtonHoldTrigger] Text Table 找不到 Key：{key}", this);
        return key;
    }
}
