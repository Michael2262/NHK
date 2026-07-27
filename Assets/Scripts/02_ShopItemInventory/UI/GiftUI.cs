using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 送禮介面主控腳本(單例)。
/// 使用流程:
///   GiftUI.Instance.SetTargetHeroine(id) → GiftUI.Instance.Show()
/// 或直接:
///   GiftUI.Instance.ShowForHeroine(id)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GiftUI : MonoBehaviour, IItemButtonHost
{
    // ══════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════
    public static GiftUI Instance { get; private set; }

    [Header("UI 引用")]
    [SerializeField] private Transform _giftButtonContainer;
    [Tooltip("掛有 BackpackItemButton 元件的按鈕 Prefab")]
    [SerializeField] private GameObject _giftButtonPrefab;
    [SerializeField] private Button _sendButton;

    [Header("捲動")]
    [SerializeField] private Button _scrollLeftButton;
    [SerializeField] private Button _scrollRightButton;
    [Tooltip("UI 中可見的禮物插槽數量,同時也是每次翻頁的步進量")]
    [SerializeField] private int _visibleSlots = 7;

    [Header("標題列(未選/選中時顯示)")]
    [Tooltip("標題文字的 LocalizeUI,預設 fieldName 為 Gift.NeedPick")]
    [SerializeField] private LocalizeUI _titleLocalizer;
    [Tooltip("標題文字的 TextMeshProUGUI(選中道具時由程式直接塞字)")]
    [SerializeField] private TextMeshProUGUI _titleTMP;

   

    // ══════════════════════════════════════════
    //  內部狀態
    // ══════════════════════════════════════════
    private CanvasGroup _canvasGroup;
    private string _currentDisplayingHeroineID;
    private ItemConfigData _selectedGiftItem = null;
    private BackpackItemButton _selectedGiftButton = null;
    private List<ItemConfigData> _giftableItems = new List<ItemConfigData>();
    private List<BackpackItemButton> _visibleSlotButtons = new List<BackpackItemButton>();
    private int _startIndex = 0;

    // 下次 Show 要用的目標女主角 ID
    private string _targetHeroineIDForNextShow = null;

    // 多語系格式字串
    private const string DEFAULT_TITLE_KEY = "Gift.NeedPick";
    private const string QUANTITY_FORMAT_KEY = "Store.Quantity";
    private string _quantityFormatString = "持有數量:{0}";
    private string _defaultTitleString = "請選擇要贈送的禮物";

    // ══════════════════════════════════════════
    //  Unity 生命週期
    // ══════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        _canvasGroup = GetComponent<CanvasGroup>();
        _sendButton.onClick.AddListener(OnSendButtonClicked);
        _scrollLeftButton.onClick.AddListener(ScrollLeft);
        _scrollRightButton.onClick.AddListener(ScrollRight);

        InitializeVisibleSlots();

        

        SetCanvasGroupActive(false);
        _sendButton.interactable = false;
    }

    private void Start()
    {
        CheckLocalizationStrings();
        ShowDefaultTitle();
    }

    private void OnEnable()
    {
        PixelCrushers.UILocalizationManager.languageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        PixelCrushers.UILocalizationManager.languageChanged -= OnLanguageChanged;
    }

    private void InitializeVisibleSlots()
    {
        foreach (Transform child in _giftButtonContainer) { Destroy(child.gameObject); }
        _visibleSlotButtons.Clear();

        for (int i = 0; i < _visibleSlots; i++)
        {
            GameObject buttonGO = Instantiate(_giftButtonPrefab, _giftButtonContainer);
            BackpackItemButton buttonScript = buttonGO.GetComponent<BackpackItemButton>();
            if (buttonScript != null)
            {
                _visibleSlotButtons.Add(buttonScript);
                buttonScript.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("Gift Button Prefab 沒有掛載 BackpackItemButton 腳本!", buttonGO);
                Destroy(buttonGO);
            }
        }
    }

    // ══════════════════════════════════════════
    //  公開 API
    // ══════════════════════════════════════════

    public void SetTargetHeroine(string heroineID)
    {
        _targetHeroineIDForNextShow = heroineID;
    }

    public void Show()
    {
        if (string.IsNullOrEmpty(_targetHeroineIDForNextShow))
        {
            Debug.LogError("GiftUI Show 失敗:尚未透過 SetTargetHeroine 設定目標女主角 ID!");
            SetCanvasGroupActive(false);
            return;
        }

        _currentDisplayingHeroineID = _targetHeroineIDForNextShow;

        _selectedGiftItem = null;
        _selectedGiftButton = null;
        _sendButton.interactable = false;
        _startIndex = 0;

        CheckLocalizationStrings();
        ShowDefaultTitle();

        PopulateAndRefresh();
        SetCanvasGroupActive(true);
    }

    /// <summary>給 UI Button 綁定用,一步到位。</summary>
    public void ShowForHeroine(string heroineID)
    {
        SetTargetHeroine(heroineID);
        Show();
    }

    public void Hide()
    {
        SetCanvasGroupActive(false);
        _currentDisplayingHeroineID = null;
        _selectedGiftItem = null;
        _selectedGiftButton = null;
    }

    public bool IsOpen => _canvasGroup.alpha > 0f;

    private void SetCanvasGroupActive(bool active)
    {
        _canvasGroup.alpha = active ? 1f : 0f;
        _canvasGroup.interactable = active;
        _canvasGroup.blocksRaycasts = active;
    }

    // ══════════════════════════════════════════
    //  多語系
    // ══════════════════════════════════════════

    private void OnLanguageChanged(string language)
    {
        CheckLocalizationStrings();

        if (_selectedGiftItem != null)
            RefreshTitleForSelectedItem();
        else
            ShowDefaultTitle();
    }

    private void CheckLocalizationStrings()
    {
        if (DialogueManager.instance == null || DialogueManager.displaySettings == null ||
            DialogueManager.displaySettings.localizationSettings == null ||
            DialogueManager.displaySettings.localizationSettings.textTable == null)
        {
            return;
        }

        var textTable = DialogueManager.displaySettings.localizationSettings.textTable;
        var langID = Localization.GetCurrentLanguageID(textTable);

        _quantityFormatString = textTable.HasField(QUANTITY_FORMAT_KEY)
            ? textTable.GetFieldTextForLanguage(QUANTITY_FORMAT_KEY, langID)
            : "持有數量:{0}";

        _defaultTitleString = textTable.HasField(DEFAULT_TITLE_KEY)
            ? textTable.GetFieldTextForLanguage(DEFAULT_TITLE_KEY, langID)
            : "請選擇要贈送的禮物";
    }

    /// <summary>回到「請選擇要贈送的禮物」,透過 LocalizeUI 播放。</summary>
    private void ShowDefaultTitle()
    {
        if (_titleLocalizer != null)
        {
            _titleLocalizer.fieldName = DEFAULT_TITLE_KEY;
            _titleLocalizer.UpdateText();
        }
        else if (_titleTMP != null)
        {
            _titleTMP.text = _defaultTitleString;
        }
    }

    /// <summary>
    /// 選中某道具時,標題顯示「道具名 (持有數量:N)」。
    /// 道具名走多語系查表,數量用格式字串拼接在 TMP 上。
    /// </summary>
    private void RefreshTitleForSelectedItem()
    {
        if (_selectedGiftItem == null) { ShowDefaultTitle(); return; }

        int quantity = GameStatusService.Instance.Inventory.GetItemCount(_selectedGiftItem.ItemID);
        string quantityText = string.Format(_quantityFormatString, quantity);
        string localizedItemName = GetLocalizedItemName(_selectedGiftItem);

        string finalText = $"{localizedItemName} ({quantityText})";

        if (_titleLocalizer != null)
        {
            _titleLocalizer.fieldName = string.Empty;
        }
        if (_titleTMP != null)
        {
            _titleTMP.text = finalText;
        }
    }

    /// <summary>從 TextTable 查道具顯示名(多語系)。查不到則回傳 DisplayName 作為後備。</summary>
    private string GetLocalizedItemName(ItemConfigData item)
    {
        if (item == null) return string.Empty;

        if (DialogueManager.instance != null &&
            DialogueManager.displaySettings?.localizationSettings?.textTable != null)
        {
            var textTable = DialogueManager.displaySettings.localizationSettings.textTable;
            if (!string.IsNullOrEmpty(item.DisplayNameKey) && textTable.HasField(item.DisplayNameKey))
            {
                var langID = Localization.GetCurrentLanguageID(textTable);
                return textTable.GetFieldTextForLanguage(item.DisplayNameKey, langID);
            }
        }
        return item.DisplayName;
    }

    // ══════════════════════════════════════════
    //  道具列表與插槽
    // ══════════════════════════════════════════

    private void PopulateAndRefresh()
    {
        if (string.IsNullOrEmpty(_currentDisplayingHeroineID)) return;

        var inventory = GameStatusService.Instance.Inventory;
        var catalog = GameStatusService.Instance.ItemCatalog;
        var allOwnedItems = inventory.GetAllItems();

        _giftableItems.Clear();
        foreach (var itemKVP in allOwnedItems)
        {
            if (itemKVP.Value <= 0) continue;
            ItemConfigData itemConfig = catalog.GetItemConfig(itemKVP.Key);
            if (itemConfig == null) continue;

            var rule = itemConfig.TargetingRule;
            bool isTargetTypeMatch = (rule.Category == TargetCategory.HeroineOnly ||
                                      rule.Category == TargetCategory.AnyCharacter);
            bool isHeroineAllowed = (rule.AllowedHeroineIDs == null ||
                                     rule.AllowedHeroineIDs.Count == 0 ||
                                     rule.AllowedHeroineIDs.Contains(_currentDisplayingHeroineID));

            if (isTargetTypeMatch && isHeroineAllowed) { _giftableItems.Add(itemConfig); }
        }

        int maxStart = Mathf.Max(0, _giftableItems.Count - _visibleSlots);
        _startIndex = Mathf.Clamp(_startIndex, 0, maxStart);
        RefreshVisibleSlots();
    }

    private void RefreshVisibleSlots()
    {
        for (int i = 0; i < _visibleSlots; i++)
        {
            if (i >= _visibleSlotButtons.Count) break;

            int itemIndex = _startIndex + i;
            BackpackItemButton currentButton = _visibleSlotButtons[i];

            // 永遠顯示(滿版),只差有沒有道具
            currentButton.gameObject.SetActive(true);

            if (itemIndex >= 0 && itemIndex < _giftableItems.Count)
            {
                // 有道具
                ItemConfigData itemToShow = _giftableItems[itemIndex];
                currentButton.Setup(itemToShow, this);

                bool isSelected = (_selectedGiftItem != null && itemToShow == _selectedGiftItem);
                currentButton.SetSelected(isSelected);
                if (isSelected) _selectedGiftButton = currentButton;
            }
            else
            {
                // 空格佔位
                currentButton.SetupAsEmpty();
            }
        }
        UpdateScrollButtons();

        
    }
    // ══════════════════════════════════════════
    //  互動(IItemButtonHost 實作)
    // ══════════════════════════════════════════

    /// <summary>
    /// 由 BackpackItemButton 點擊時回呼。
    /// 邏輯仿 BackpackUI:同一個按鈕再點一次 = 取消選中。
    /// </summary>
    public void OnItemSelected(ItemConfigData item, BackpackItemButton button)
    {
        if (_selectedGiftButton == button && button != null)
        {
            DeselectCurrent();
            return;
        }

        if (_selectedGiftButton != null)
            _selectedGiftButton.SetSelected(false);

        _selectedGiftItem = item;
        _selectedGiftButton = button;

        if (_selectedGiftButton != null)
            _selectedGiftButton.SetSelected(true);

        _sendButton.interactable = (_selectedGiftItem != null);
        RefreshTitleForSelectedItem();
    }

    private void DeselectCurrent()
    {
        if (_selectedGiftButton != null)
            _selectedGiftButton.SetSelected(false);

        _selectedGiftItem = null;
        _selectedGiftButton = null;
        _sendButton.interactable = false;
        ShowDefaultTitle();
    }

    private void OnSendButtonClicked()
    {
        if (_selectedGiftItem == null || string.IsNullOrEmpty(_currentDisplayingHeroineID)) return;

        var useService = GameStatusService.Instance.ItemUseService;
        GiftResult result = useService.GiveGift(_selectedGiftItem.ItemID, _currentDisplayingHeroineID);

        if (result.IsSuccess)
        {
            GameStatusService.Instance.Inventory.RemoveItem(_selectedGiftItem.ItemID, 1);
            HeroineReactionManager.Instance?.TriggerGiftReaction(_currentDisplayingHeroineID, _selectedGiftItem.ItemID);
            Hide();
        }
        else
        {
            if (!string.IsNullOrEmpty(result.RejectConversationTitle))
            {
                Hide();
                StoryManager.Instance.PlayConversation(result.RejectConversationTitle, DialogueMode.Interrupt);
            }
            else
            {
                Hide();
            }
        }
    }

    // ══════════════════════════════════════════
    //  捲動(整頁翻 + 無效時隱藏按鈕)
    // ══════════════════════════════════════════

    private void ScrollLeft()
    {
        if (_startIndex <= 0) return;
        _startIndex = Mathf.Max(0, _startIndex - _visibleSlots);
        RefreshVisibleSlots();
    }

    private void ScrollRight()
    {
        int maxStart = Mathf.Max(0, _giftableItems.Count - _visibleSlots);
        if (_startIndex >= maxStart) return;
        _startIndex = Mathf.Min(maxStart, _startIndex + _visibleSlots);
        RefreshVisibleSlots();
    }

    private void UpdateScrollButtons()
    {
        bool canScrollLeft = (_startIndex > 0);
        bool canScrollRight = (_startIndex + _visibleSlots < _giftableItems.Count);

        _scrollLeftButton.gameObject.SetActive(canScrollLeft);
        _scrollRightButton.gameObject.SetActive(canScrollRight);
    }
}