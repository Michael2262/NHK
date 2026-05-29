using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 機率性觸發結果按鈕。
///
/// 每個按鈕可以是「機率按鈕」（有 ActionChanceProvider）或「普通按鈕」（僅觸發點擊事件）。
/// 所有按鈕都必須註冊進同一個 CommandButtonGroup，
/// 當任一機率按鈕的時間條在跑時，群組內所有按鈕都會被鎖定。
///
/// 流程（機率按鈕）：
///   1. 玩家點擊 → 立刻觸發 onClickEvents + onClickFsm
///   2. 群組開始跑時間條（長度 = barDuration）
///   3. 時間條結束 → 依照成功率判定結果
///      - 成功 → 觸發 onSuccessEvents + onSuccessFsm
///      - 失敗 → 觸發 onFailEvents + onFailFsm
///
/// 流程（普通按鈕，chanceProvider == null）：
///   1. 玩家點擊 → 立刻觸發 onClickEvents + onClickFsm
///   2. 不走時間條，不做成功失敗判定。
/// </summary>
public class CommandButton : MonoBehaviour
{
    [Header("按鈕群組")]
    [Tooltip("此按鈕所屬的群組。群組管理鎖定狀態與共用時間條。")]
    public CommandButtonGroup group;

    [Header("機率來源（可選）")]
    [Tooltip("成功率提供者。若為 null，此按鈕為普通按鈕，點擊只觸發 onClick 事件。")]
    public ActionChanceProvider chanceProvider;

    [Header("時間條設定")]
    [Tooltip("時間條跑完所需的秒數。僅機率按鈕有效。")]
    [Min(0.1f)]
    public float barDuration = 2f;

    // ─────────────────────────────────────────────
    // 點擊瞬間
    // ─────────────────────────────────────────────
    [Header("點擊瞬間事件")]
    [Tooltip("點擊按鈕的瞬間觸發的 UnityEvent。")]
    public UnityEvent onClickEvents;

    [Tooltip("點擊瞬間要觸發的 FSM 事件。")]
    public FsmEventSender onClickFsm;

    // ─────────────────────────────────────────────
    // 成功
    // ─────────────────────────────────────────────
    [Header("成功事件")]
    [Tooltip("成功時觸發的 UnityEvent。")]
    public UnityEvent onSuccessEvents;

    [Tooltip("成功時要觸發的 FSM 事件。")]
    public FsmEventSender onSuccessFsm;

    // ─────────────────────────────────────────────
    // 失敗
    // ─────────────────────────────────────────────
    [Header("失敗事件")]
    [Tooltip("失敗時觸發的 UnityEvent。")]
    public UnityEvent onFailEvents;

    [Tooltip("失敗時要觸發的 FSM 事件。")]
    public FsmEventSender onFailFsm;

    private void OnEnable()
    {
        if (group != null)
            group.Register(this);
    }

    private void OnDisable()
    {
        if (group != null)
            group.Unregister(this);
    }

    /// <summary>
    /// 外部呼叫入口（掛到 Button.OnClick）。
    /// </summary>
    public void OnButtonClicked()
    {
        if (group == null)
        {
            Debug.LogWarning($"[CommandButton] {gameObject.name}: 未指定 group，無法執行。", this);
            return;
        }

        group.HandleButtonClicked(this);
    }

    // ─────────────────────────────────────────────
    // 由 Group 呼叫的內部方法
    // ─────────────────────────────────────────────

    /// <summary>
    /// 觸發點擊瞬間事件（UnityEvent + FSM）。由 Group 呼叫。
    /// </summary>
    internal void FireClickEvents()
    {
        onClickEvents?.Invoke();
        onClickFsm?.Send();
    }

    /// <summary>
    /// 觸發成功事件。由 Group 呼叫。
    /// </summary>
    internal void FireSuccessEvents()
    {
        onSuccessEvents?.Invoke();
        onSuccessFsm?.Send();

        // 通知 ChanceProvider 累加重複失敗率（如果有的話）
        if (chanceProvider is HeroineActionChanceProvider heroineProvider)
        {
            heroineProvider.NotifySuccess();
        }
    }

    /// <summary>
    /// 觸發失敗事件。由 Group 呼叫。
    /// </summary>
    internal void FireFailEvents()
    {
        onFailEvents?.Invoke();
        onFailFsm?.Send();
    }

    /// <summary>
    /// 此按鈕是否為機率按鈕（有 chanceProvider）。
    /// </summary>
    public bool IsChanceButton => chanceProvider != null;
}