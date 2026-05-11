using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NHK 版主角數值檢查器。
/// 用途：在 UI 按鈕或事件觸發前，檢查主角數值 / 金錢 / 技能點 / 時間是否符合條件。
/// 注意：本腳本只做「檢查」與 Money / SkillPoints 的扣除。
/// Stress / LifePower / Sociality / Dependency 的實際增減，建議交給專門的數值變化腳本或事件本身處理。
/// </summary>
[AddComponentMenu("Game/UI/Protagonist Value Router")]
public class ProtagonistValueRouter : MonoBehaviour
{
    public enum CheckType
    {
        None,
        CheckMoney,
        CheckSkillPoints,
        CheckStressAtMost,
        CheckLifePowerAtLeast,
        CheckSocialityAtLeast,
        CheckDependencyAtLeast
    }

    [Header("數值檢查設定")]
    [Tooltip("選擇要檢查的主角數值類型。")]
    public CheckType checkType = CheckType.None;

    [Tooltip("檢查門檻。Money / SkillPoints 表示需求量；Stress 表示不可超過；LifePower / Sociality / Dependency 表示至少需要。")]
    public int amount = 1;

    [Header("時間檢查設定")]
    [Tooltip("是否同時檢查剩餘時間？")]
    public bool checkTime = false;

    [Tooltip("需要消耗的時段數量。")]
    public int timeAmount = 1;

    [Header("結果事件")]
    [Tooltip("當所有檢查條件皆通過時觸發。")]
    public UnityEvent onSuccess;

    [Tooltip("當數值條件不通過時觸發。")]
    public UnityEvent onFailure;

    [Tooltip("當數值足夠，但時間不足（會跨日）時觸發。")]
    public UnityEvent onTimeFailure;

    public enum CheckResult
    {
        Success,
        ValueFail,
        TimeFail
    }

    /// <summary>
    /// 純檢查，回傳結果，不觸發任何 UnityEvent。
    /// </summary>
    public CheckResult Check()
    {
        var service = GameStatusService.Instance;
        if (service == null || service.Protagonist == null)
        {
            Debug.LogError("[ProtagonistValueRouter] GameStatusService 或 Protagonist 尚未初始化。");
            return CheckResult.ValueFail;
        }

        var p = service.Protagonist;
        var t = service.Time;

        if (!CheckValue(p))
            return CheckResult.ValueFail;

        if (checkTime)
        {
            if (t == null)
            {
                Debug.LogError("[ProtagonistValueRouter] TimeSystemModel 尚未初始化，無法檢查時間。");
                return CheckResult.TimeFail;
            }

            if (!t.CanAdvanceWithinDay(timeAmount))
                return CheckResult.TimeFail;
        }

        return CheckResult.Success;
    }

    /// <summary>
    /// 綁定在 Unity Button 的 OnClick()。
    /// </summary>
    public void Trigger()
    {
        CheckResult result = Check();

        switch (result)
        {
            case CheckResult.Success:
                onSuccess?.Invoke();
                break;

            case CheckResult.TimeFail:
                onTimeFailure?.Invoke();
                break;

            default:
                onFailure?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 實際扣除資源與時間。
    /// Money / SkillPoints 會扣除；其他主角狀態只檢查不扣除。
    /// Stress / LifePower / Sociality / Dependency 的變化，請由事件或其他數值變化腳本處理。
    /// </summary>
    public void ExecuteDeduction()
    {
        var service = GameStatusService.Instance;
        if (service == null || service.Protagonist == null)
        {
            Debug.LogError("[ProtagonistValueRouter] GameStatusService 或 Protagonist 尚未初始化，無法扣除。");
            return;
        }

        var p = service.Protagonist;
        var t = service.Time;

        switch (checkType)
        {
            case CheckType.CheckMoney:
                p.TryReduceMoney(amount);
                break;

            case CheckType.CheckSkillPoints:
                p.TryReduceSkillPoints(amount);
                break;
        }

        if (checkTime && timeAmount > 0)
        {
            if (t != null)
                t.TryAdvanceTime(timeAmount);
            else
                Debug.LogError("[ProtagonistValueRouter] TimeSystemModel 尚未初始化，無法推進時間。");
        }

        Debug.Log($"[ProtagonistValueRouter] ExecuteDeduction 完成 - 檢查類型: {checkType}, 數量/門檻: {amount}, 時間: {(checkTime ? timeAmount.ToString() : "無")}");
    }

    private bool CheckValue(ProtagonistStatusModel p)
    {
        int threshold = Mathf.Max(0, amount);

        switch (checkType)
        {
            case CheckType.None:
                return true;

            case CheckType.CheckMoney:
                return p.CanReduceMoney(threshold);

            case CheckType.CheckSkillPoints:
                return p.CanReduceSkillPoints(threshold);

            case CheckType.CheckStressAtMost:
                return p.Stress <= threshold;

            case CheckType.CheckLifePowerAtLeast:
                return p.LifePower >= threshold;

            case CheckType.CheckSocialityAtLeast:
                return p.Sociality >= threshold;

            case CheckType.CheckDependencyAtLeast:
                return p.Dependency >= threshold;

            default:
                return false;
        }
    }
}
