using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class HeroineSpawnRule
{
    public string activityState;
    public GameObject characterObjectToShow;
    public List<GameObject> objectsToClose;
}

[System.Serializable]
public class RiskSpawnRule
{
    public string inspectionTypeID;
    public GameObject riskObjectToShow;
    public List<GameObject> objectsToClose;
}

[System.Serializable]
public class TimePhaseVisual
{
    public int phaseIndex;
    public GameObject objectToActivate;
    public List<CanvasGroup> canvasGroupsToActivate;
}

/// <summary>
/// 場景的角色佈置與環境視覺控制器。
/// 
/// 【v3 改動】
/// - 不再實作 ISceneReadyHandler（初始化改由 Task_InitHubController 在 Coordinator 管線中觸發）
/// - 原本的 OnSceneReady() 邏輯移至公開的 Initialize() 方法
/// - OnEnable/OnDisable 的事件訂閱、持續監聽等邏輯完全不變
/// 
/// 【v4 改動】
/// - TimePhaseVisual 新增 canvasGroupsToActivate 欄位
/// - 環境視覺除了 GameObject 開關，也支援 CanvasGroup 的開關
///   （關閉時 alpha=0、interactable=false、blocksRaycasts=false；開啟時反之）
/// </summary>
public class HubController : MonoBehaviour
{
    [Header("--- 地點設定 ---")]
    [SerializeField] private string myLocationID = "SET_THIS_IN_INSPECTOR";

    [Header("--- 舞台劇本 ---")]
    public List<HeroineSpawnRule> heroineRules;
    public List<RiskSpawnRule> riskRules;

    [Header("--- 環境視覺 ---")]
    public List<TimePhaseVisual> timeVisualRules;

    private GameStatusService _service;

    private void Awake()
    {
        _service = GameStatusService.Instance;
    }

    void OnEnable()
    {
        if (GameStatusService.Instance != null && GameStatusService.Instance.Scenario != null)
        {
            GameStatusService.Instance.Scenario.OnHeroineMoved += HandleHeroineMoved;
            GameStatusService.Instance.Scenario.OnRiskMoved += HandleRiskMoved;
            GameStatusService.Instance.Scenario.OnScenarioRecalculated += RefreshVisuals;
        }

        if (GameStatusService.Instance.TimeManager != null)
        {
            GameStatusService.Instance.TimeManager.OnPhaseChanged += HandlePhaseChanged;
            GameStatusService.Instance.TimeManager.OnDayPassed += HandlePhaseChanged;
        }
    }

    void OnDisable()
    {
        if (GameStatusService.Instance != null && GameStatusService.Instance.Scenario != null)
        {
            GameStatusService.Instance.Scenario.OnHeroineMoved -= HandleHeroineMoved;
            GameStatusService.Instance.Scenario.OnRiskMoved -= HandleRiskMoved;
            GameStatusService.Instance.Scenario.OnScenarioRecalculated -= RefreshVisuals;
        }

        if (GameStatusService.Instance.TimeManager != null)
        {
            GameStatusService.Instance.TimeManager.OnPhaseChanged -= HandlePhaseChanged;
            GameStatusService.Instance.TimeManager.OnDayPassed -= HandlePhaseChanged;
        }
    }

    private void HandleHeroineMoved(string hID, string oldLoc, string newLoc, string newAct)
    {
        if (oldLoc == myLocationID || newLoc == myLocationID) RefreshVisuals();
    }

    private void HandleRiskMoved(string agentID, string oldLoc, string newLoc, string actionID)
    {
        Debug.Log($"[Debug] 收到風險移動通知: {agentID} 從 {oldLoc} 到 {newLoc}。當前場景 ID 是: {myLocationID}");
        if (oldLoc == myLocationID || newLoc == myLocationID)
        {
            Debug.Log($"[Hub] 偵測到風險角色 {agentID} 移動，重新整理場景視覺。");
            RefreshVisuals();
        }
    }

    private void HandlePhaseChanged()
    {
        Debug.Log($"[HubController] 偵測到時間階段切換，重新整理場景視覺。");
        RefreshVisuals();
    }

    // ============================================================
    // 【v3 改動】公開初始化方法，由 Task_InitHubController 呼叫
    // ============================================================

    /// <summary>
    /// 初始化此 HubController（原本的 OnSceneReady 邏輯）。
    /// 由 Coordinator 管線中的 Task_InitHubController 在適當時機呼叫。
    /// </summary>
    public IEnumerator Initialize()
    {
        _service = GameStatusService.Instance;
        if (_service == null) yield break;

        if (string.IsNullOrEmpty(myLocationID) || myLocationID == "SET_THIS_IN_INSPECTOR")
        {
            Debug.LogError($"[HubController] 請在 Inspector 設定正確的 myLocationID！", this);
            yield break;
        }

        RefreshVisuals();
        yield return null;
    }

    // ============================================================
    // 核心顯示邏輯
    // ============================================================

    private void RefreshVisuals()
    {
        if (_service == null) _service = GameStatusService.Instance;

        //Debug.Log($"<color=orange>[HubController] RefreshVisuals 被呼叫,當前 CurrentPhaseIndex = {_service.Time.CurrentPhaseIndex}</color>");

        HideAllSceneObjects();

        var scenarioModel = _service.Scenario;
        if (scenarioModel.AllLocationStates.TryGetValue(myLocationID, out var myState))
        {
            foreach (var hData in myState.Heroines)
            {
                HeroineSpawnRule rule = heroineRules.Find(r => r.activityState == hData.Activity);
                if (rule != null)
                {
                    if (rule.characterObjectToShow != null)
                        rule.characterObjectToShow.SetActive(true);

                    if (rule.objectsToClose != null)
                    {
                        foreach (var obj in rule.objectsToClose)
                            if (obj != null) obj.SetActive(false);
                    }
                }
            }

            foreach (var risk in myState.Risks)
            {
                RiskSpawnRule rule = riskRules.Find(r => r.inspectionTypeID == risk.inspectionTypeID);
                if (rule != null)
                {
                    if (rule.riskObjectToShow != null)
                        rule.riskObjectToShow.SetActive(true);

                    if (rule.objectsToClose != null)
                    {
                        foreach (var obj in rule.objectsToClose)
                            if (obj != null) obj.SetActive(false);
                    }
                }
            }
        }

        UpdateEnvironmentVisuals(_service.Time.CurrentPhaseIndex);
    }

    private void HideAllSceneObjects()
    {
        foreach (var rule in heroineRules)
        {
            if (rule.characterObjectToShow != null) rule.characterObjectToShow.SetActive(false);

            if (rule.objectsToClose != null)
            {
                foreach (var obj in rule.objectsToClose)
                    if (obj != null) obj.SetActive(true);
            }
        }

        foreach (var rule in riskRules)
        {
            if (rule.riskObjectToShow != null) rule.riskObjectToShow.SetActive(false);

            if (rule.objectsToClose != null)
            {
                foreach (var obj in rule.objectsToClose)
                    if (obj != null) obj.SetActive(true);
            }
        }

        foreach (var rule in timeVisualRules)
        {
            if (rule.objectToActivate != null)
                rule.objectToActivate.SetActive(false);

            if (rule.canvasGroupsToActivate != null)
            {
                foreach (var cg in rule.canvasGroupsToActivate)
                {
                    if (cg != null)
                    {
                        cg.alpha = 0f;
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                    }
                }
            }
        }
    }

    private void UpdateEnvironmentVisuals(int currentPhaseIndex)
    {
        TimePhaseVisual activeRule = timeVisualRules.Find(r => r.phaseIndex == currentPhaseIndex);
        if (activeRule != null)
        {
            if (activeRule.objectToActivate != null)
                activeRule.objectToActivate.SetActive(true);

            if (activeRule.canvasGroupsToActivate != null)
            {
                foreach (var cg in activeRule.canvasGroupsToActivate)
                {
                    if (cg != null)
                    {
                        cg.alpha = 1f;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
            }
        }
    }
}