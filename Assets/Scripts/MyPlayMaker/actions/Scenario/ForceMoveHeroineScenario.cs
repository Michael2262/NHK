using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Playmaker Action：強制移動女主角到指定地點並設定行為。
///
/// 對應 SequencerCommandScenario 裡的：
///     Scenario(Heroine, 角色ID, 地點ID, 行為名稱)
/// 例如：Scenario(Heroine, saya, Corridor, Saya_Standing)
///
/// 底層呼叫 ScenarioManager.Instance.ForceMoveHeroine(heroineID, locationID, activity)，
/// 該方法會透過 Model 的 ForceSetHeroineLocation 處理「先移除舊位置 → 放到新位置 → 發送 OnHeroineMoved 事件」。
///
/// 【使用方式】
/// 1. 填入 heroineID（例如 saya）
/// 2. 填入 targetLocationID（例如 Corridor）。留空 = 從地圖上移除該角色。
/// 3. 填入 activity（例如 Saya_Standing），對應該場景 HubController heroineRules 的 activityState
/// </summary>
[ActionCategory("Scenario")]
[Tooltip("強制移動女主角到指定地點並設定行為。等同 Sequencer 指令 Scenario(Heroine, 角色ID, 地點ID, 行為名稱)。")]
public class ForceMoveHeroineScenario : FsmStateAction
{
    [Header("目標設定")]
    [RequiredField]
    [Tooltip("女主角 ID（例如 saya、sister）")]
    public FsmString heroineID;

    [Tooltip("目標地點 ID（例如 Corridor）。留空 = 把她從地圖上移除。")]
    public FsmString targetLocationID;

    [Tooltip("行為名稱（例如 Saya_Standing），需對應該場景 HubController heroineRules 的 activityState")]
    public FsmString activity;

    [Header("結果事件（可選）")]
    [Tooltip("移動完成後發送的事件")]
    public FsmEvent finishedEvent;

    [Tooltip("找不到 ScenarioManager 或 heroineID 為空時發送的事件")]
    public FsmEvent errorEvent;

    public override void Reset()
    {
        heroineID = null;
        targetLocationID = null;
        activity = null;
        finishedEvent = null;
        errorEvent = null;
    }

    public override void OnEnter()
    {
        DoMove();
        Finish();
    }

    private void DoMove()
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning("[ForceMoveHeroineScenario] 場景中找不到 ScenarioManager 實例。");
            Fsm.Event(errorEvent);
            return;
        }

        if (heroineID == null || string.IsNullOrEmpty(heroineID.Value))
        {
            Debug.LogWarning("[ForceMoveHeroineScenario] heroineID 為空，已略過。");
            Fsm.Event(errorEvent);
            return;
        }

        string hID = heroineID.Value;
        string locID = targetLocationID != null ? targetLocationID.Value : null;
        string act = activity != null ? activity.Value : null;

        ScenarioManager.Instance.ForceMoveHeroine(hID, locID, act);

        Fsm.Event(finishedEvent);
    }
}
