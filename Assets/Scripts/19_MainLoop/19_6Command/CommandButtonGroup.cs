using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

/// <summary>
/// 機率性觸發結果按鈕群組。
///
/// 職責：
///   - 管理所有註冊的 CommandButton。
///   - 當任一機率按鈕被點擊時，鎖定群組內所有按鈕。
///   - 驅動共用的時間條 UI（Image.fillAmount 或 Slider）。
///   - 時間條結束後依照成功率擲骰，觸發對應的成功 / 失敗事件。
///   - 時間條結束後解鎖所有按鈕。
///
/// 使用方式：
///   1. 場景中放一個 GameObject 掛此腳本。
///   2. 指定 progressBarImage（填充式 Image）或 progressBarSlider（Slider）。
///   3. 各 CommandButton 的 group 欄位指向此物件。
///   4. 按鈕的 Button.OnClick() 掛 CommandButton.OnButtonClicked()。
/// </summary>
public class CommandButtonGroup : MonoBehaviour
{
    [Header("共用時間條 UI")]
    [Tooltip("填充式 Image（fillAmount 0→1）。與 Slider 二選一。")]
    public Image progressBarImage;

    [Tooltip("Slider（value 0→1）。與 Image 二選一。")]
    public Slider progressBarSlider;

    [Tooltip("啟用後時間條從中間同時往左右展開（使用 RectTransform 寬度）。progressBarImage 的 Pivot 請設為 (0.5, 0.5)。")]
    public bool fillFromCenter = false;

    [Tooltip("時間條的父物件。跑條時自動顯示，結束後自動隱藏。若未指定則不控制顯隱。")]
    public GameObject progressBarRoot;

    [Header("思考動畫")]
    [Tooltip("思考氣泡的父物件。跑條時自動顯示，結束後自動隱藏。若未指定則不控制。")]
    public GameObject thinkingRoot;

    [Tooltip("AnimatorEmotionController 的 ID。會透過 AnimatorEmotionController.Get() 查找，" +
             "並呼叫 Think() / Stop() 控制動畫。")]
    public string thinkingControllerId = "MiniGame";

    [Header("結果文字")]
    [Tooltip("顯示成功或失敗文字的 TMP 元件。")]
    public TMP_Text resultText;

    [Tooltip("成功時的多語系 Key。")]
    public string successLocalizationKey = "System.Succese";

    [Tooltip("失敗時的多語系 Key。")]
    public string failLocalizationKey = "System.Failed";

    [Header("結果停留")]
    [Tooltip("時間條跑完後，結果文字與時間條一起停留的秒數，之後才消失並解鎖。")]
    [Min(0f)]
    public float delayAfterBar = 1.5f;

    [Header("群組全域事件（可選）")]
    [Tooltip("任一按鈕開始跑時間條時觸發。")]
    public UnityEvent onBarStarted;

    [Tooltip("時間條結束且判定完畢後觸發（無論成功失敗）。")]
    public UnityEvent onBarFinished;

    [Header("按鈕顯示條件")]
    [Tooltip("設定群組中需要依照 Flag 條件顯示/隱藏的按鈕。" +
             "此列表獨立於按鈕的自動註冊機制，確保被隱藏的按鈕不會丟失參考。")]
    public List<ConditionalButton> conditionalButtons;

    [Header("跑條時半透明")]
    [Tooltip("時間條跑條期間要變半透明的 CanvasGroup。若未指定則不控制。")]
    public CanvasGroup dimCanvasGroup;

    [Tooltip("跑條期間的透明度。")]
    [Range(0f, 1f)]
    public float dimAlpha = 0.4f;

    [Tooltip("透明度漸變的秒數。")]
    [Min(0f)]
    public float dimFadeDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool isLocked;
    [SerializeField] private int registeredButtonCount;

    private readonly List<CommandButton> _buttons = new List<CommandButton>();
    private Coroutine _barCoroutine;

    private void Awake()
    {
        SetProgressValue(0f);
        SetProgressBarVisible(false);
        SetThinkingVisible(false);
        HideResultText();
    }

    // ─────────────────────────────────────────────
    // 註冊 / 取消註冊
    // ─────────────────────────────────────────────

    public void Register(CommandButton button)
    {
        if (!_buttons.Contains(button))
        {
            _buttons.Add(button);
            registeredButtonCount = _buttons.Count;
        }
    }

    public void Unregister(CommandButton button)
    {
        _buttons.Remove(button);
        registeredButtonCount = _buttons.Count;
    }

    // ─────────────────────────────────────────────
    // 鎖定狀態
    // ─────────────────────────────────────────────

    /// <summary>
    /// 群組目前是否鎖定中（時間條正在跑）。
    /// </summary>
    public bool IsLocked => isLocked;

    private void SetLocked(bool locked)
    {
        isLocked = locked;
        SetAllButtonsInteractable(!locked);
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (var btn in _buttons)
        {
            if (btn == null) continue;
            var uiButton = btn.GetComponent<Button>();
            if (uiButton != null)
                uiButton.interactable = interactable;
        }
    }

    // ─────────────────────────────────────────────
    // 按鈕點擊處理
    // ─────────────────────────────────────────────

    /// <summary>
    /// 由 CommandButton 呼叫。
    /// </summary>
    internal void HandleButtonClicked(CommandButton button)
    {
        if (isLocked)
        {
            Debug.Log($"[CommandButtonGroup] 群組鎖定中，忽略 {button.gameObject.name} 的點擊。");
            return;
        }

        // 1. 立刻觸發點擊事件
        button.FireClickEvents();

        // 2. 如果是機率按鈕 → 跑時間條
        if (button.IsChanceButton)
        {
            _barCoroutine = StartCoroutine(RunProgressBar(button));
        }
        // 普通按鈕：只觸發 onClick，不跑時間條，不鎖定
    }

    // ─────────────────────────────────────────────
    // 時間條 Coroutine
    // ─────────────────────────────────────────────

    private IEnumerator RunProgressBar(CommandButton button)
    {
        // 鎖定群組
        SetLocked(true);

        float duration = button.GetCurrentBarDuration();
        button.AdvanceClickCount();
        bool useThinking = duration >= 1f;

        // 顯示時間條，條件性顯示思考動畫
        SetProgressBarVisible(true);
        if (useThinking) SetThinkingVisible(true);
        SetProgressValue(0f);
        SetDim(true);

        onBarStarted?.Invoke();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetProgressValue(t);
            yield return null;
        }

        SetProgressValue(1f);

        // 時間條結束，停止思考動畫
        if (useThinking) SetThinkingVisible(false);

        // 判定成功 / 失敗
        float chance = button.chanceProvider.CalculateSuccessChance();
        float roll = Random.value; // 0~1
        bool success = roll <= chance;

        Debug.Log($"[CommandButtonGroup] {button.gameObject.name}: " +
                  $"chance={chance:P1}, roll={roll:F3}, result={(success ? "SUCCESS" : "FAIL")}");

        // 顯示結果文字（多語系查表）
        ShowResultText(success);

        if (success)
            button.FireSuccessEvents();
        else
            button.FireFailEvents();

        onBarFinished?.Invoke();

        // 結果停留一段時間，讓玩家看到結果
        if (delayAfterBar > 0f)
            yield return new WaitForSeconds(delayAfterBar);

        // 隱藏時間條與結果文字
        SetProgressBarVisible(false);
        HideResultText();
        SetDim(false);

        // 解鎖群組
        SetLocked(false);

        _barCoroutine = null;
    }

    // ─────────────────────────────────────────────
    // 時間條 UI 操作
    // ─────────────────────────────────────────────

    private void SetProgressValue(float value)
    {
        if (progressBarImage != null)
        {
            if (fillFromCenter)
            {
                // 從中間往兩邊展開：X 軸縮放 0→1，Pivot (0.5, 0.5) 確保從中心擴張
                var scale = progressBarImage.rectTransform.localScale;
                scale.x = value;
                progressBarImage.rectTransform.localScale = scale;
            }
            else
            {
                progressBarImage.fillAmount = value;
            }
        }

        if (progressBarSlider != null)
            progressBarSlider.value = value;
    }

    private void SetProgressBarVisible(bool visible)
    {
        if (progressBarRoot != null)
            progressBarRoot.SetActive(visible);
    }

    private void SetThinkingVisible(bool visible)
    {
        if (thinkingRoot != null)
            thinkingRoot.SetActive(visible);

        var controller = AnimatorEmotionController.Get(thinkingControllerId);
        if (controller != null)
        {
            if (visible)
                controller.Think();
            else
                controller.Stop();
        }
    }

    // ─────────────────────────────────────────────
    // 結果文字
    // ─────────────────────────────────────────────

    private void ShowResultText(bool success)
    {
        if (resultText == null) return;

        string key = success ? successLocalizationKey : failLocalizationKey;
        string localized = DialogueManager.GetLocalizedText(key);

        // 查不到就直接顯示 key
        if (string.IsNullOrEmpty(localized))
            localized = key;

        resultText.text = localized;
        resultText.gameObject.SetActive(true);
    }

    private void HideResultText()
    {
        if (resultText == null) return;
        resultText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 半透明控制
    // ─────────────────────────────────────────────

    private void SetDim(bool dim, bool instant = false)
    {
        if (dimCanvasGroup == null) return;

        // 中斷前一次的漸變
        dimCanvasGroup.DOKill();

        float target = dim ? dimAlpha : 1f;

        if (!instant && dimFadeDuration > 0f)
            dimCanvasGroup.DOFade(target, dimFadeDuration).SetEase(Ease.OutQuad);
        else
            dimCanvasGroup.alpha = target;
    }

    // ─────────────────────────────────────────────
    // 公共工具
    // ─────────────────────────────────────────────

    /// <summary>
    /// 強制中斷目前的時間條（例如場景切換時）。
    /// 不會觸發成功或失敗事件。
    /// </summary>
    public void CancelBar()
    {
        if (_barCoroutine != null)
        {
            StopCoroutine(_barCoroutine);
            _barCoroutine = null;
        }

        SetProgressBarVisible(false);
        SetThinkingVisible(false);
        HideResultText();
        SetDim(false, instant: true);
        SetLocked(false);
    }

    /// <summary>
    /// 根據 ProgressFlag 條件更新按鈕的顯示/隱藏狀態。
    /// 適合在面板開啟時呼叫，或掛到 UnityEvent。
    /// </summary>
    public void UpdateButtonConditions()
    {
        if (conditionalButtons == null || conditionalButtons.Count == 0) return;

        ProgressFlagModel flags = GameStatusService.Instance?.ProgressFlags;
        if (flags == null)
        {
            Debug.LogWarning("[CommandButtonGroup] 找不到 ProgressFlagModel，無法檢查按鈕條件。");
            return;
        }

        foreach (var entry in conditionalButtons)
        {
            if (entry.buttonObject == null) continue;

            bool show;

            if (entry.isDefault)
            {
                show = true;
            }
            else
            {
                show = entry.requiredFlag != null && flags.Contains(entry.requiredFlag.FlagID);
            }

            entry.buttonObject.SetActive(show);
        }
    }

    private void OnEnable()
    {
        UpdateButtonConditions();
    }

    private void OnDisable()
    {
        CancelBar();
    }
}