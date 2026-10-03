using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 單一場景計時工具。設定在首次使用時建立快照，執行期間請勿修改清單。
/// 停用或隨場景卸載時清空全部倒數及佇列；不使用 DontDestroyOnLoad。
/// </summary>
[AddComponentMenu("NHK/Scene Tools/Scene Timer Controller")]
public sealed class SceneTimerController : MonoBehaviour
{
    [Serializable]
    public sealed class BehaviorDefinition
    {
        [Tooltip("本 Controller 內唯一，區分大小寫，不可空白或含前後空格。")]
        public string ID;
        public TimerId Timer = TimerId.Main;
        [Min(0.001f)] public float Duration = 10f;
        [Tooltip("Interrupt：取代目前工作；Skip：忙碌時忽略；Queue：排隊；Priority：插至隊首；Parallel：獨立倒數，不使用所屬 Timer。")]
        public TimerStartMode StartMode = TimerStartMode.Interrupt;
        public UnityEvent OnCompleted = new UnityEvent();
    }

    [SerializeField] private List<BehaviorDefinition> behaviors = new List<BehaviorDefinition>();
    [Header("EvaluateTimer / EvaluateID 的判斷結果")]
    [SerializeField] private UnityEvent onCheckTrue = new UnityEvent();
    [SerializeField] private UnityEvent onCheckFalse = new UnityEvent();

    private SceneTimerModel model;
    private bool initializationAttempted;

    public bool IsInitialized => model != null;
    public bool InitializationFailed => initializationAttempted && model == null;

    /// <summary>只讀取已建立的 Model，不因編輯器查看而提早初始化設定。</summary>
    public bool TryGetTimerSnapshot(TimerId id, out SceneTimerModel.TimerSnapshot snapshot)
    {
        snapshot = null;
        return model != null && model.TryGetTimerSnapshot(id, out snapshot);
    }

    public bool TryGetParallelSnapshot(string id, out SceneTimerModel.TimerSnapshot snapshot)
    {
        snapshot = null;
        return model != null && model.TryGetParallelSnapshot(id, out snapshot);
    }

    private void Awake() { EnsureInitialized(); }
    private void Update() { if (EnsureInitialized()) model.Tick(Time.deltaTime); }
    private void OnDisable() { model?.CancelAllTimers(); }
    private void OnDestroy() { model?.CancelAllTimers(); }

    public void StartTimer(string behaviorID)
    {
        if (!isActiveAndEnabled || !ValidateID(behaviorID)) return;
        model.StartTimer(behaviorID);
    }

    public bool CheckTimer() { return CheckTimer(TimerId.Main); }
    public bool CheckTimer(int timerNumber) { return CheckTimer((TimerId)timerNumber); }
    public bool CheckTimer(string timerName)
    {
        TimerId id;
        return TryParseTimer(timerName, out id) && CheckTimer(id);
    }
    public bool CheckTimer(TimerId id)
    {
        return ValidateTimer(id) && EnsureInitialized() && model.CheckTimer(id);
    }
    public bool CheckID(string behaviorID)
    {
        return ValidateID(behaviorID) && model.CheckID(behaviorID);
    }

    // UnityEvent 不能直接接回傳 bool 的方法，提供對應的事件分支入口。
    public void EvaluateTimer() { EvaluateTimer("Main"); }
    public void EvaluateTimer(string timerName) { InvokeSafely(CheckTimer(timerName) ? onCheckTrue : onCheckFalse); }
    public void EvaluateID(string behaviorID) { InvokeSafely(CheckID(behaviorID) ? onCheckTrue : onCheckFalse); }

    public void PauseTimer() { PauseTimer(TimerId.Main); }
    public void PauseTimer(int timerNumber) { PauseTimer((TimerId)timerNumber); }
    public void PauseTimer(string timerName)
    {
        TimerId id;
        if (TryParseTimer(timerName, out id)) PauseTimer(id);
    }
    public void PauseTimer(TimerId id)
    {
        if (isActiveAndEnabled && ValidateTimer(id) && EnsureInitialized()) model.PauseTimer(id);
    }

    public void CancelTimer() { CancelTimer(TimerId.Main); }
    public void CancelTimer(int timerNumber) { CancelTimer((TimerId)timerNumber); }
    public void CancelTimer(string timerName)
    {
        TimerId id;
        if (TryParseTimer(timerName, out id)) CancelTimer(id);
    }
    public void CancelTimer(TimerId id)
    {
        if (ValidateTimer(id) && EnsureInitialized()) model.CancelTimer(id);
    }
    public void CancelAllTimers() { model?.CancelAllTimers(); }

    /// <summary>取消執行中或排隊中的指定行為，包含 Parallel；不觸發完成事件。</summary>
    public void CancelID(string behaviorID)
    {
        if (ValidateID(behaviorID)) model.CancelID(behaviorID);
    }

    private bool EnsureInitialized()
    {
        if (initializationAttempted) return model != null;
        initializationAttempted = true;
        try
        {
            var definitions = new List<SceneTimerModel.Behavior>();
            if (behaviors == null) throw new ArgumentException("行為清單不可為空參照。");
            foreach (BehaviorDefinition entry in behaviors)
            {
                if (entry == null) throw new ArgumentException("行為設定不可為空。");
                UnityEvent completed = entry.OnCompleted;
                definitions.Add(new SceneTimerModel.Behavior(entry.ID, entry.Timer, entry.Duration,
                    entry.StartMode, () => InvokeSafely(completed)));
            }
            model = new SceneTimerModel(definitions);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError("[SceneTimerController] 設定錯誤：" + exception.Message, this);
        }
        return model != null;
    }

    private bool ValidateID(string id)
    {
        if (!EnsureInitialized()) return false;
        if (model.ContainsID(id)) return true;
        Debug.LogWarning("[SceneTimerController] 找不到行為 ID：" + id, this);
        return false;
    }

    private bool ValidateTimer(TimerId id)
    {
        if (Enum.IsDefined(typeof(TimerId), id)) return true;
        Debug.LogWarning("[SceneTimerController] 未定義的計時器編號：" + (int)id, this);
        return false;
    }

    private bool TryParseTimer(string name, out TimerId id)
    {
        if (string.IsNullOrWhiteSpace(name)) { id = TimerId.Main; return true; }
        // 禁止 Enum.TryParse 接受「Main, Emo」這類組合名稱。
        if (name.IndexOf(',') < 0 && Enum.TryParse(name.Trim(), false, out id)
            && Enum.IsDefined(typeof(TimerId), id)) return true;
        id = TimerId.Main;
        Debug.LogWarning("[SceneTimerController] 找不到計時器名稱或編號：" + name, this);
        return false;
    }

    private void InvokeSafely(UnityEvent callback)
    {
        try { callback?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
