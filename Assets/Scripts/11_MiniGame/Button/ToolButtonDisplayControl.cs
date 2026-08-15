using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 工具按鈕群組的「顯示面」控制器（switch 類按鈕群）。
///
/// 只管顯示，不碰任何遊戲邏輯。負責三件事：
///   1. 依 Flag 決定某按鈕「是否出現」（visibilityFlag）。
///   2. 依 Flag 決定某按鈕「是否可點選」（interactableFlag）；不可點者交給 Button 內建的 Disabled Color。
///   3. switch 行為：點下某按鈕時它維持選中色、其餘退回原色。
///      是否啟用此行為由 <see cref="switchMode"/> 決定。
///
/// 選中色直接沿用 Button 本身 ColorBlock 的 Selected / Pressed Color（不另加物件）：
/// 作法是把「選中那顆」的 normalColor 覆蓋成選中色，使它在一般（Normal）狀態下持續顯示該色，
/// 因此「點其他地方也不會解除」——這正是 switch 想要的持久效果。
/// Highlighted Color（hover）屬短暫效果，不受影響。
///
/// 掛法：把本元件掛在群組的父物件上，於 Inspector 把每顆按鈕填進 <see cref="buttons"/>。
/// 各 Flag 欄位留空＝該條件永遠成立（永遠顯示 / 永遠可點）。
/// </summary>
[DisallowMultipleComponent]
public class ToolButtonDisplayControl : MonoBehaviour
{
    /// <summary>選中時持久顯示的顏色，取自 Button ColorBlock 的哪一欄。</summary>
    public enum SwitchColorSource
    {
        SelectedColor,
        PressedColor
    }

    [Serializable]
    public class ButtonEntry
    {
        [Tooltip("備註用途，不影響邏輯。")]
        public string label;

        [Tooltip("這顆按鈕本體。")]
        public Button button;

        [Header("是否出現")]
        [Tooltip("持有此 Flag 才顯示。留空＝永遠顯示。")]
        public ProgressFlagDefinition visibilityFlag;
        [Tooltip("反轉：改成『沒有此 Flag 才顯示』。")]
        public bool invertVisibility;

        [Header("是否可點選")]
        [Tooltip("持有此 Flag 才可點。留空＝永遠可點。")]
        public ProgressFlagDefinition interactableFlag;
        [Tooltip("反轉：改成『沒有此 Flag 才可點』。")]
        public bool invertInteractable;

        // ── 執行期快取 ──
        [NonSerialized] public Color originalNormalColor; // 未選中時要還原的原始 normalColor
    }

    [Header("按鈕清單")]
    [SerializeField] private List<ButtonEntry> buttons = new List<ButtonEntry>();

    [Header("switch 設定")]
    [Tooltip("是否啟用『點一顆維持選中色、其餘退回原色』的 switch 行為。")]
    [SerializeField] private bool switchMode = true;
    [Tooltip("選中時要持久顯示 Button ColorBlock 的哪個顏色。")]
    [SerializeField] private SwitchColorSource switchColorSource = SwitchColorSource.SelectedColor;
    [Tooltip("開場預設選中的按鈕索引（-1＝一開始都不選）。顯示上會立即套用選中色。")]
    [SerializeField] private int defaultSelectedIndex = 0;

    private int _selectedIndex = -1;

    // ==========================================================
    // 生命週期
    // ==========================================================

    private void Awake()
    {
        // 掛好 onClick 監聽、快取原始 normalColor。
        for (int i = 0; i < buttons.Count; i++)
        {
            var entry = buttons[i];
            if (entry?.button == null) continue;

            entry.originalNormalColor = entry.button.colors.normalColor;

            int captured = i; // 閉包捕捉當前索引
            entry.button.onClick.AddListener(() => OnButtonClicked(captured));
        }
    }

    private void OnEnable()
    {
        Subscribe(true);

        _selectedIndex = defaultSelectedIndex;
        Refresh();          // 先算好顯示 / 可點
        ApplySelection();   // 立即套上預設選中色
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    private void OnDestroy()
    {
        // 移除 onClick 監聽，避免殘留。
        foreach (var entry in buttons)
        {
            if (entry?.button != null)
                entry.button.onClick.RemoveAllListeners();
        }
    }

    // ==========================================================
    // 事件訂閱（Flag 變動 / 讀檔）
    // ==========================================================

    private void Subscribe(bool subscribe)
    {
        var service = GameStatusService.Instance;
        if (service == null) return;

        if (service.ProgressFlags != null)
        {
            if (subscribe)
            {
                service.ProgressFlags.OnFlagChanged += HandleFlagChanged;
                service.ProgressFlags.OnVariableChanged += HandleVariableChanged;
            }
            else
            {
                service.ProgressFlags.OnFlagChanged -= HandleFlagChanged;
                service.ProgressFlags.OnVariableChanged -= HandleVariableChanged;
            }
        }

        if (subscribe) service.OnGameStatusLoaded += Refresh;
        else service.OnGameStatusLoaded -= Refresh;
    }

    private void HandleFlagChanged(string flagID, bool isActive) => Refresh();
    private void HandleVariableChanged(string key, int value) => Refresh();

    // ==========================================================
    // 顯示 / 可點 刷新
    // ==========================================================

    /// <summary>依當前 Flag 狀態重算每顆按鈕的「是否出現」與「是否可點」。</summary>
    public void Refresh()
    {
        foreach (var entry in buttons)
        {
            if (entry?.button == null) continue;

            bool visible = Evaluate(entry.visibilityFlag, entry.invertVisibility);
            if (entry.button.gameObject.activeSelf != visible)
                entry.button.gameObject.SetActive(visible);

            // 不可點時直接 interactable=false，外觀交給 Button 內建的 Disabled Color。
            entry.button.interactable = Evaluate(entry.interactableFlag, entry.invertInteractable);
        }
    }

    /// <summary>
    /// 判斷單一 Flag 條件是否成立。Flag 為 null＝視為成立（永遠顯示 / 永遠可點）。
    /// </summary>
    private bool Evaluate(ProgressFlagDefinition flag, bool invert)
    {
        if (flag == null) return true;

        var service = GameStatusService.Instance;
        bool has = service != null
                   && service.ProgressFlags != null
                   && service.ProgressFlags.Contains(flag.FlagID);

        return invert ? !has : has;
    }

    // ==========================================================
    // switch 選中色（用 Button 自身 ColorBlock）
    // ==========================================================

    private void OnButtonClicked(int index)
    {
        if (!switchMode) return;
        if (index < 0 || index >= buttons.Count) return;

        _selectedIndex = index;
        ApplySelection();

        // 清掉 EventSystem 的選取狀態，避免 Unity 內建的 Selected 狀態
        // 把 selectedColor 疊到別顆或本顆上，干擾我們用 normalColor 維持的持久選中色。
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>依 <see cref="_selectedIndex"/> 把選中那顆的 normalColor 覆蓋成選中色，其餘還原。</summary>
    private void ApplySelection()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var entry = buttons[i];
            if (entry?.button == null) continue;

            bool selected = switchMode && i == _selectedIndex;
            ApplyButtonColor(entry, selected);
        }
    }

    private void ApplyButtonColor(ButtonEntry entry, bool selected)
    {
        var btn = entry.button;
        var cb = btn.colors; // pressed / selected 等維持原值，只覆蓋 normalColor

        Color target = selected
            ? (switchColorSource == SwitchColorSource.PressedColor ? cb.pressedColor : cb.selectedColor)
            : entry.originalNormalColor;

        if (cb.normalColor != target)
        {
            cb.normalColor = target;
            btn.colors = cb;
        }

        // 立即反映到目前畫面（Normal 狀態），不必等下一次狀態變化。
        // 不可點時交給 Unity 顯示 disabledColor，這裡不強套。
        if (btn.targetGraphic != null && btn.interactable)
            btn.targetGraphic.CrossFadeColor(target * cb.colorMultiplier, 0f, true, true);
    }

    /// <summary>外部（程式 / UnityEvent）指定選中某顆按鈕，不觸發其 onClick 邏輯。</summary>
    public void SetSelected(int index)
    {
        if (!switchMode) return;
        if (index < 0 || index >= buttons.Count) return;

        _selectedIndex = index;
        ApplySelection();
    }

    /// <summary>清除選中（所有按鈕退回原色）。</summary>
    public void ClearSelection()
    {
        _selectedIndex = -1;
        ApplySelection();
    }
}
