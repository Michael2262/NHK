using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 按鈕端行動觸發器。
/// 可用於：
/// 1. 白天 1 生活行動：enableOutcomeResult = false，跑完後 onCompleteEvents。
/// 2. 白天 2 復歸行動：enableOutcomeResult = true，跑完後顯示成功/失敗，並分別呼叫 onSuccessEvents / onFailureEvents。
///
/// 注意：
/// - 此腳本只負責跑條、顯示成功/失敗、分流 UnityEvent。
/// - 成功率可以由 Inspector 直接填 successChance，也可以由 RecoveryActionChanceProvider 在執行前代入。
/// </summary>
public class ActionOverlayTrigger : MonoBehaviour
{
    [Header("行動設定")]
    [Tooltip("此動作需要花費的演出時間")]
    public float duration = 2.0f;

    [Tooltip("要顯示的插圖")]
    public Sprite actionSprite;

    [Tooltip("在地化文本的 Key，例如：Action_Cooking")]
    public string localizationKey;

    [Header("結果判定設定")]
    [Tooltip("是否在跑條結束後判定成功/失敗。白天 2 復歸行動建議打開。")]
    public bool enableOutcomeResult = false;

    [Tooltip("成功率。0 = 必定失敗，1 = 必定成功。若有 RecoveryActionChanceProvider，執行前會被覆寫。")]
    [Range(0f, 1f)] public float successChance = 0.5f;

    [Tooltip("成功率來源。復歸行動可指定 RecoveryActionChanceProvider，由它依主角數值計算成功率。")]
    public RecoveryActionChanceProvider chanceProvider;

    [Tooltip("Execute() 時是否自動從 chanceProvider 重新取得成功率。")]
    public bool autoApplyChanceProviderOnExecute = true;

    [Tooltip("成功時顯示的文字或在地化 Key。")]
    public string successLocalizationKey = "Result.Success";

    [Tooltip("失敗時顯示的文字或在地化 Key。")]
    public string failureLocalizationKey = "Result.Failure";

    [Tooltip("成功/失敗文字顯示秒數。")]
    public float resultHoldSeconds = 2.0f;

    [Header("完成後觸發")]
    [Tooltip("無成功/失敗判定時，跑條完成後觸發。若有成功/失敗判定，則在成功/失敗事件後也會觸發。")]
    public UnityEvent onCompleteEvents;

    [Header("成功 / 失敗後觸發")]
    public UnityEvent onSuccessEvents;
    public UnityEvent onFailureEvents;

    [Header("開始時觸發")]
    public UnityEvent onStartEvents;

    public bool LastResultWasSuccess { get; private set; }

    private void Reset()
    {
        // 方便掛在同一顆按鈕上時自動抓取。
        chanceProvider = GetComponent<RecoveryActionChanceProvider>();
    }

    /// <summary>
    /// 公開方法：呼叫此方法來啟動過場。
    /// </summary>
    public void Execute()
    {
        if (ActionOverlayManager.Instance == null)
        {
            Debug.LogError("場景中找不到 ActionOverlayManager！");
            return;
        }

        if (!enableOutcomeResult)
        {
            ActionOverlayManager.Instance.TriggerAction(
                duration,
                actionSprite,
                localizationKey,
                () => onCompleteEvents?.Invoke(),
                () => onStartEvents?.Invoke());
            return;
        }

        RefreshSuccessChanceFromProvider();

        bool isSuccess = RollSuccess();
        LastResultWasSuccess = isSuccess;

        ActionOverlayManager.Instance.TriggerActionWithResult(
            duration,
            actionSprite,
            localizationKey,
            isSuccess,
            successLocalizationKey,
            failureLocalizationKey,
            resultHoldSeconds,
            success =>
            {
                if (success) onSuccessEvents?.Invoke();
                else onFailureEvents?.Invoke();

                onCompleteEvents?.Invoke();
            },
            () => onStartEvents?.Invoke());
    }

    /// <summary>
    /// 外部可直接指定結果，用於之後若你想讓復歸行動使用加權結果表。
    /// </summary>
    public void ExecuteWithForcedResult(bool success)
    {
        if (ActionOverlayManager.Instance == null)
        {
            Debug.LogError("場景中找不到 ActionOverlayManager！");
            return;
        }

        LastResultWasSuccess = success;

        ActionOverlayManager.Instance.TriggerActionWithResult(
            duration,
            actionSprite,
            localizationKey,
            success,
            successLocalizationKey,
            failureLocalizationKey,
            resultHoldSeconds,
            result =>
            {
                if (result) onSuccessEvents?.Invoke();
                else onFailureEvents?.Invoke();

                onCompleteEvents?.Invoke();
            },
            () => onStartEvents?.Invoke());
    }

    public void SetSuccessChance(float chance)
    {
        successChance = Mathf.Clamp01(chance);
    }

    /// <summary>
    /// 手動從 Provider 更新成功率。若 Button.OnClick 想明確分兩步，可先呼叫此方法再 Execute()。
    /// </summary>
    public void RefreshSuccessChanceFromProvider()
    {
        if (!autoApplyChanceProviderOnExecute) return;
        if (chanceProvider == null) return;

        SetSuccessChance(chanceProvider.CalculateSuccessChance());
    }

    private bool RollSuccess()
    {
        return Random.value <= successChance;
    }

    [ContextMenu("測試觸發動作")]
    private void TestTrigger()
    {
        Execute();
    }

    [ContextMenu("測試成功")]
    private void TestSuccess()
    {
        ExecuteWithForcedResult(true);
    }

    [ContextMenu("測試失敗")]
    private void TestFailure()
    {
        ExecuteWithForcedResult(false);
    }
}
