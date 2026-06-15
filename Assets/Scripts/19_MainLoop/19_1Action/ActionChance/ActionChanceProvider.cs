using UnityEngine;
using TMPro;

/// <summary>
/// 行動成功率計算器（抽象基底類別）。
///
/// 職責：
/// - 從 ProtagonistStatusModel 計算成功率（由子類別實作公式）。
/// - 將成功率代入 ActionOverlayTrigger。
/// - （可選）將成功率顯示到 TextMeshPro 文字元件上。
///
/// 用法：
/// - 每種行動各自繼承此類別，override CalculateChance() 填入自己的公式。
/// - 掛在按鈕 / 行動物件上，ActionOverlayTrigger 會自動抓取。
/// - 任何需要機率判定的按鈕都可以使用此基底。
/// </summary>
public abstract class ActionChanceProvider : MonoBehaviour
{
    [Tooltip("若指定，ApplyChanceToTrigger() 會把成功率寫入此 Trigger。未指定時會抓同物件上的 ActionOverlayTrigger。")]
    public ActionOverlayTrigger targetTrigger;

    [Header("成功率顯示")]
    [Tooltip("指定後會自動將成功率寫成 'X%' 顯示。未指定則不顯示。")]
    public TMP_Text chanceDisplay;

    [Tooltip("啟用時自動刷新顯示。若 GameStatusService 可能還沒初始化完，可關閉此選項改用手動呼叫 RefreshDisplay()。")]
    public bool refreshOnEnable = true;

    [Header("通用限制")]
    [Range(0f, 1f)] public float minChance = 0.1f;
    [Range(0f, 1f)] public float maxChance = 0.9f;

    [Header("Debug")]
    [SerializeField, Range(0f, 1f)] protected float lastCalculatedChance;
    [SerializeField] protected string lastDebugSummary;

    protected virtual void Reset()
    {
        targetTrigger = GetComponent<ActionOverlayTrigger>();
    }

    protected virtual void OnEnable()
    {
        SubscribeToStatusChanges();

        // 訂閱「遊戲數據載入 / 重置完畢」事件。
        // 進場時 OnEnable 先跑（此時模型多半還是載入前的舊值），
        // 之後讀檔管線才套用存檔並觸發 OnGameStatusLoaded，
        // 屆時再重新計算一次，確保第一次進場顯示正確。
        if (GameStatusService.Instance != null)
            GameStatusService.Instance.OnGameStatusLoaded += OnGameStatusLoaded;

        if (refreshOnEnable && chanceDisplay != null)
        {
            CalculateSuccessChance();
        }
    }

    protected virtual void OnDisable()
    {
        UnsubscribeFromStatusChanges();

        if (GameStatusService.Instance != null)
            GameStatusService.Instance.OnGameStatusLoaded -= OnGameStatusLoaded;
    }

    /// <summary>
    /// 遊戲數據載入 / 重置完畢後重新計算並刷新顯示。
    /// </summary>
    private void OnGameStatusLoaded()
    {
        if (chanceDisplay == null) return;
        CalculateSuccessChance();
    }

    private void SubscribeToStatusChanges()
    {
        ProtagonistStatusModel protagonist = GameStatusService.Instance?.Protagonist;
        if (protagonist == null) return;

        protagonist.OnStressChanged += OnStatusValueChanged;
        protagonist.OnLifePowerChanged += OnStatusValueChanged;
        protagonist.OnSocialityChanged += OnStatusValueChanged;
        protagonist.OnDependencyChanged += OnStatusValueChanged;
    }

    private void UnsubscribeFromStatusChanges()
    {
        ProtagonistStatusModel protagonist = GameStatusService.Instance?.Protagonist;
        if (protagonist == null) return;

        protagonist.OnStressChanged -= OnStatusValueChanged;
        protagonist.OnLifePowerChanged -= OnStatusValueChanged;
        protagonist.OnSocialityChanged -= OnStatusValueChanged;
        protagonist.OnDependencyChanged -= OnStatusValueChanged;
    }

    private void OnStatusValueChanged(int delta)
    {
        if (chanceDisplay == null) return;
        CalculateSuccessChance();
    }

    /// <summary>
    /// 計算目前成功率。回傳值為 0～1，已 Clamp。
    /// </summary>
    public float CalculateSuccessChance()
    {
        ProtagonistStatusModel protagonist = GameStatusService.Instance?.Protagonist;
        if (protagonist == null)
        {
            Debug.LogWarning($"{GetType().Name}: 找不到 GameStatusService.Instance.Protagonist，使用 fallback。", this);
            lastCalculatedChance = Mathf.Clamp(GetFallbackChance(), minChance, maxChance);
            lastDebugSummary = "Fallback: No protagonist model.";
            UpdateChanceDisplay(lastCalculatedChance);
            return lastCalculatedChance;
        }

        float chance = CalculateChance(protagonist);
        chance = Mathf.Clamp(chance, minChance, maxChance);

        lastCalculatedChance = chance;
        lastDebugSummary = BuildDebugSummary(protagonist, chance);
        UpdateChanceDisplay(chance);
        return chance;
    }

    /// <summary>
    /// 子類別必須實作：根據主角數值計算原始成功率（尚未 Clamp）。
    /// </summary>
    protected abstract float CalculateChance(ProtagonistStatusModel protagonist);

    /// <summary>
    /// 當找不到 ProtagonistStatusModel 時的預設成功率。子類別可 override。
    /// </summary>
    protected virtual float GetFallbackChance() => 0.5f;

    /// <summary>
    /// Debug 摘要。子類別可 override 加入更多資訊。
    /// </summary>
    protected virtual string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        return $"{GetType().Name}: chance={chance:P0}, Stress={p.Stress}, LifePower={p.LifePower}, " +
               $"Sociality={p.Sociality}, Dependency={p.Dependency}";
    }

    /// <summary>
    /// 更新 chanceDisplay 文字。若未指定 chanceDisplay 則跳過。
    /// </summary>
    private void UpdateChanceDisplay(float chance)
    {
        if (chanceDisplay == null) return;
        chanceDisplay.text = $"{Mathf.RoundToInt(chance * 100)}%";
    }

    /// <summary>
    /// 重新計算成功率並更新顯示文字。
    /// 適合掛到 Button.OnClick 或在畫面開啟時呼叫，讓玩家看到目前成功率。
    /// </summary>
    public void RefreshDisplay()
    {
        CalculateSuccessChance();
    }

    /// <summary>
    /// 將計算後的成功率寫入 targetTrigger。
    /// 可掛到 Button.OnClick，放在 ActionOverlayTrigger.Execute() 前。
    /// </summary>
    public void ApplyChanceToTrigger()
    {
        ActionOverlayTrigger trigger = targetTrigger != null ? targetTrigger : GetComponent<ActionOverlayTrigger>();
        if (trigger == null)
        {
            Debug.LogWarning($"{GetType().Name}: 找不到 ActionOverlayTrigger，無法代入成功率。", this);
            return;
        }

        trigger.SetSuccessChance(CalculateSuccessChance());
    }

    /// <summary>
    /// 提供給其他腳本直接指定目標。
    /// </summary>
    public void ApplyChanceToTrigger(ActionOverlayTrigger trigger)
    {
        if (trigger == null)
        {
            Debug.LogWarning($"{GetType().Name}: 傳入的 ActionOverlayTrigger 為 null。", this);
            return;
        }

        trigger.SetSuccessChance(CalculateSuccessChance());
    }

    [ContextMenu("Debug Calculate Chance")]
    private void DebugCalculateChance()
    {
        float chance = CalculateSuccessChance();
        Debug.Log(lastDebugSummary + $" ({chance})", this);
    }
}