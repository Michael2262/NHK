using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 將符合條件的選項依序填入預先擺好的按鈕，不改動位置、尺寸或階層。
/// 掛在按鈕群組父物件；固定按鈕的 On Click 留空，事件設定在選項上。
/// 文字由本元件查 Text Table，請勿在同一文字物件掛 LocalizeUI。
/// </summary>
[DisallowMultipleComponent]
public class ToolButtonMenuDisplayControl : MonoBehaviour
{
    [Serializable]
    public class ButtonSlot
    {
        [Tooltip("預先擺好的按鈕；此物件會依選項數量開關。")]
        public Button button;
        [Tooltip("由系統填入多語系文字，請勿同時使用 LocalizeUI。")]
        public TMP_Text text;
    }

    [Serializable]
    public class MenuOption
    {
        [Tooltip("Inspector 備註，不影響顯示內容。")]
        public string label;
        [Tooltip("Common_TextTable 中的文字 Key。")]
        public string textKey;

        [Header("是否出現")]
        [Tooltip("持有此 Flag 才顯示；留空永遠成立，不受反轉影響。")]
        public ProgressFlagDefinition visibilityFlag;
        public bool invertVisibility;

        [Header("是否可點選")]
        [Tooltip("持有此 Flag 才可點；留空永遠成立。不可點的選項仍占位置。")]
        public ProgressFlagDefinition interactableFlag;
        public bool invertInteractable;

        [Header("點擊事件")]
        public UnityEvent onClick = new UnityEvent();
    }

    [Header("固定按鈕（依填入順序排列）")]
    [SerializeField] private List<ButtonSlot> slots = new List<ButtonSlot>();
    [Header("選項（越前面順位越高）")]
    [SerializeField] private List<MenuOption> options = new List<MenuOption>();
    [Header("顯示上限")]
    [Tooltip("最多同時顯示幾個選項；0 表示全部隱藏，且不會超過有效按鈕數。")]
    [Min(0)] [SerializeField] private int maxVisibleCount = 3;

    private sealed class SlotBinding
    {
        public ButtonSlot slot;
        public MenuOption option;
        public UnityAction listener;
    }

    private readonly List<SlotBinding> _bindings = new List<SlotBinding>();
    private GameStatusService _service;
    private ProgressFlagModel _flags;

    private void Awake()
    {
        var seen = new HashSet<Button>();
        foreach (var slot in slots)
        {
            if (slot == null || slot.button == null || slot.text == null)
            {
                Debug.LogWarning("[ToolButtonMenuDisplayControl] 槽位缺少 Button 或文字元件，已略過。", this);
                continue;
            }
            if (transform.IsChildOf(slot.button.transform))
            {
                Debug.LogError("[ToolButtonMenuDisplayControl] 按鈕不能是控制器本身或其父物件，否則隱藏按鈕會停用控制器。", this);
                continue;
            }
            if (!seen.Add(slot.button))
            {
                Debug.LogWarning("[ToolButtonMenuDisplayControl] 同一按鈕不可重複指定，已略過重複槽位。", this);
                continue;
            }

            var binding = new SlotBinding { slot = slot };
            binding.listener = () => HandleClick(binding);
            _bindings.Add(binding);
        }
    }

    private void OnEnable()
    {
        foreach (var binding in _bindings)
            binding.slot.button.onClick.AddListener(binding.listener);

        _service = GameStatusService.Instance;
        _flags = _service != null ? _service.ProgressFlags : null;
        if (_flags != null)
        {
            _flags.OnFlagChanged += HandleFlagChanged;
            _flags.OnVariableChanged += HandleVariableChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded += Refresh;
        PixelCrushers.UILocalizationManager.languageChanged += HandleLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        foreach (var binding in _bindings)
        {
            if (binding.slot.button != null)
                binding.slot.button.onClick.RemoveListener(binding.listener);
            binding.option = null;
        }
        if (_flags != null)
        {
            _flags.OnFlagChanged -= HandleFlagChanged;
            _flags.OnVariableChanged -= HandleVariableChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded -= Refresh;
        PixelCrushers.UILocalizationManager.languageChanged -= HandleLanguageChanged;
        _flags = null;
        _service = null;
    }

    private void HandleFlagChanged(string flagID, bool active) => Refresh();
    private void HandleVariableChanged(string key, int value) => Refresh();
    private void HandleLanguageChanged(string language) => Refresh();

    /// <summary>依順位重新填入文字、點擊事件及可點狀態，可由 UnityEvent 呼叫。</summary>
    public void Refresh()
    {
        if (!isActiveAndEnabled) return;

        int nextOption = 0;
        int displayed = 0;
        foreach (var binding in _bindings)
        {
            var slot = binding.slot;
            if (slot.button == null || slot.text == null) continue;

            MenuOption option = null;
            while (displayed < maxVisibleCount && nextOption < options.Count)
            {
                var candidate = options[nextOption++];
                if (candidate != null && Evaluate(candidate.visibilityFlag, candidate.invertVisibility))
                {
                    option = candidate;
                    break;
                }
            }

            // 先更新對應與文字，再啟用按鈕，避免開啟當下顯示上一個選項。
            binding.option = option;
            slot.text.text = option != null ? Localize(option.textKey) : string.Empty;
            slot.button.interactable = option != null
                && Evaluate(option.interactableFlag, option.invertInteractable);
            bool visible = option != null;
            if (slot.button.gameObject.activeSelf != visible)
                slot.button.gameObject.SetActive(visible);
            if (visible) displayed++;
        }
    }

    private bool Evaluate(ProgressFlagDefinition flag, bool invert)
    {
        if (flag == null) return true;
        var service = GameStatusService.Instance;
        // 服務尚未就緒時，不顯示需要條件判斷的選項。
        if (service == null || service.ProgressFlags == null) return false;
        bool hasFlag = service.ProgressFlags.Contains(flag.FlagID);
        return invert ? !hasFlag : hasFlag;
    }

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        string text = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[ToolButtonMenuDisplayControl] Text Table 找不到 Key: {key}", this);
            return key;
        }
        return text;
    }

    private void HandleClick(SlotBinding binding)
    {
        var option = binding.option;
        if (!isActiveAndEnabled || option == null || binding.slot.button == null
            || !binding.slot.button.isActiveAndEnabled || !binding.slot.button.IsInteractable()) return;
        if (!Evaluate(option.visibilityFlag, option.invertVisibility)
            || !Evaluate(option.interactableFlag, option.invertInteractable))
        {
            Refresh();
            return;
        }

        // 使用當下選項的事件；事件可自行關閉面板或修改 Flag。
        option.onClick?.Invoke();
        if (this != null && isActiveAndEnabled) Refresh();
    }
}
