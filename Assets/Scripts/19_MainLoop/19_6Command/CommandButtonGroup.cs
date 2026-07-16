using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 機率性觸發結果按鈕群組。
///
/// 職責：
///   - 管理所有註冊的 CommandButton。
///   - 當任一機率按鈕被點擊時，鎖定群組內所有按鈕。
///   - 透過 ProgressBarFrame 驅動共用的時間條 / 結果文字 / 結果音效。
///   - 時間條結束後依照成功率擲骰，觸發對應的成功 / 失敗事件。
///   - 時間條結束後解鎖所有按鈕。
///
/// 使用方式：
///   1. 場景中放一個 GameObject 掛此腳本。
///   2. 指定 progressBarFrame（掛著 ProgressBarFrame 的共用時間條物件）。
///   3. 各 CommandButton 的 group 欄位指向此物件。
///   4. 按鈕的 Button.OnClick() 掛 CommandButton.OnButtonClicked()。
/// </summary>
public class CommandButtonGroup : MonoBehaviour
{
    [Header("共用時間條外框")]
    [Tooltip("時間條 UI、結果文字、結果音效的集中設定（ProgressBarFrame）。" +
             "可被同場景多個 CommandButtonGroup 共用。")]
    public ProgressBarFrame progressBarFrame;

    [Header("結果停留")]
    [Tooltip("時間條跑完後，先播放音效並顯示成功/失敗文字，停留這個秒數後，" +
             "才執行按鈕的成功/失敗事件（FSM + UnityEvent），接著消失並解鎖。")]
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
        if (progressBarFrame == null)
        {
            Debug.LogWarning($"[CommandButtonGroup] {gameObject.name}: 未指定 progressBarFrame，" +
                             "時間條、結果文字與結果音效將不會顯示。", this);
        }
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

            // 跑條中換頁進來的新按鈕要同步鎖定；
            // 鎖定期間被藏起、解鎖後才重新顯示的按鈕則要解鎖回來。
            var uiButton = button.GetComponent<Button>();
            if (uiButton != null)
                uiButton.interactable = !isLocked;
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

        // 顯示時間條
        if (progressBarFrame != null)
        {
            progressBarFrame.SetVisible(true);
            progressBarFrame.SetProgressValue(0f);
        }
        SetDim(true);

        onBarStarted?.Invoke();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (progressBarFrame != null)
                progressBarFrame.SetProgressValue(t);
            yield return null;
        }

        if (progressBarFrame != null)
            progressBarFrame.SetProgressValue(1f);

        // 判定成功 / 失敗
        float chance = button.chanceProvider.CalculateSuccessChance();
        float roll = Random.value; // 0~1
        bool success = roll <= chance;

        Debug.Log($"<color=#FF1493>[CommandButtonGroup] {button.gameObject.name}：" +
                  $"這次成功率 {chance:P1}，骰到 {roll:F3}，結果是「{(success ? "成功" : "失敗")}」！</color>");

        // 先播放結果音效 + 顯示成功/失敗文字，讓玩家先看到、聽到結果
        if (progressBarFrame != null)
        {
            progressBarFrame.PlayResultSound(success);
            progressBarFrame.ShowResultText(success);
        }

        // 結果停留一段時間，讓玩家看到結果。
        // 此期間還不執行成功/失敗事件。
        if (delayAfterBar > 0f)
            yield return new WaitForSeconds(delayAfterBar);

        // 停留秒數結束後，才執行按鈕的成功/失敗事件（FSM + UnityEvent）
        if (success)
            button.FireSuccessEvents();
        else
            button.FireFailEvents();

        onBarFinished?.Invoke();

        // 隱藏時間條與結果文字
        if (progressBarFrame != null)
        {
            progressBarFrame.SetVisible(false);
            progressBarFrame.HideResultText();
        }
        SetDim(false);

        // 解鎖群組
        SetLocked(false);

        _barCoroutine = null;
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

        if (progressBarFrame != null)
        {
            progressBarFrame.SetVisible(false);
            progressBarFrame.HideResultText();
        }
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