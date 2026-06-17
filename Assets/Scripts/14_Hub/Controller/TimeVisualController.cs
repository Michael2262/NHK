using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TimePhaseVisualRule
{
    [Header("Editor (易讀性)")]
    [Tooltip("此視覺的描述名稱，純粹為了 Inspector 易讀性。例如：「白天-室內」、「假日-夜晚」")]
    public string visualName;

    public int phaseIndex;
    [Tooltip("此視覺在哪種日期生效。EveryDay=平日假日皆套用；WeekdayOnly/WeekendOnly=僅平日/僅假日。\n" +
             "同一 phase 可同時放 EveryDay 與假日專屬版，系統會優先挑專屬版，沒有才 fallback 到 EveryDay。")]
    public ScheduleDayType dayType = ScheduleDayType.EveryDay;
    public GameObject objectToActivate;
}

public class TimeVisualController : MonoBehaviour, ISceneReadyHandler
{
    [Header("--- 環境視覺 ---")]
    public List<TimePhaseVisualRule> timeVisualRules;

    private GameStatusService _service;

    void OnEnable()
    {
        if (GameStatusService.Instance?.TimeManager != null)
        {
            GameStatusService.Instance.TimeManager.OnPhaseChanged += RefreshVisuals;
            GameStatusService.Instance.TimeManager.OnDayPassed += RefreshVisuals;
        }
    }

    void OnDisable()
    {
        if (GameStatusService.Instance?.TimeManager != null)
        {
            GameStatusService.Instance.TimeManager.OnPhaseChanged -= RefreshVisuals;
            GameStatusService.Instance.TimeManager.OnDayPassed -= RefreshVisuals;
        }
    }

    public IEnumerator OnSceneReady()
    {
        _service = GameStatusService.Instance;
        if (_service == null) yield break;

        RefreshVisuals();
        yield return null;
    }

    private void RefreshVisuals()
    {
        if (_service == null) _service = GameStatusService.Instance;

        // 先全部關閉
        foreach (var rule in timeVisualRules)
            if (rule.objectToActivate != null) rule.objectToActivate.SetActive(false);

        // 依今天是平日/假日挑選對應視覺：先找專屬版，沒有才 fallback 到 EveryDay。
        int currentPhase = _service.Time.CurrentPhaseIndex;
        ScheduleDayType specific = _service.Time.IsWeekend
            ? ScheduleDayType.WeekendOnly
            : ScheduleDayType.WeekdayOnly;

        TimePhaseVisualRule activeRule =
            timeVisualRules.Find(r => r.phaseIndex == currentPhase && r.dayType == specific)
            ?? timeVisualRules.Find(r => r.phaseIndex == currentPhase && r.dayType == ScheduleDayType.EveryDay);

        if (activeRule?.objectToActivate != null)
            activeRule.objectToActivate.SetActive(true);
    }
}