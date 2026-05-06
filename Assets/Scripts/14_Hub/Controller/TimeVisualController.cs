using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TimePhaseVisualRule
{
    public int phaseIndex;
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

        // 開啟當前 phase 對應的物件
        int currentPhase = _service.Time.CurrentPhaseIndex;
        TimePhaseVisualRule activeRule = timeVisualRules.Find(r => r.phaseIndex == currentPhase);
        if (activeRule?.objectToActivate != null)
            activeRule.objectToActivate.SetActive(true);
    }
}