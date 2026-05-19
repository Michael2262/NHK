using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NHK 版主角狀態門檻觸發器。
/// 原檔名保留，但用途改為監測 Stress / LifePower / Sociality / Dependency。
/// 
/// 主要用途：
/// 1. Stress 進入 High / Extreme 時觸發事件。
/// 2. LifePower 低於門檻時觸發事件。
/// 3. Sociality 低於門檻時觸發事件。
/// 4. Dependency 高於門檻時觸發事件。
/// 
/// 一次性事件預設「場景內只觸發一次」，可由 ResetAllTriggers() 重置。
/// </summary>
public class ProtagonistStatusDepletionTrigger : MonoBehaviour
{
    [Header("Stress 門檻")]
    [SerializeField] private int _stressHighThreshold = ProtagonistStatusModel.STRESS_HIGH_THRESHOLD;
    [SerializeField] private int _stressExtremeThreshold = ProtagonistStatusModel.STRESS_EXTREME_THRESHOLD;
    [SerializeField] private UnityEvent _onStressHigh;
    [SerializeField] private UnityEvent _onStressExtreme;

    [Header("LifePower 門檻")]
    [Tooltip("生活力低於此值時觸發（即處於 Low 分級）。")]
    [SerializeField] private int _lifePowerLowThreshold = ProtagonistStatusModel.LIFE_MEDIUM_THRESHOLD;
    [SerializeField] private UnityEvent _onLifePowerLow;

    [Header("Sociality 門檻")]
    [Tooltip("社會性低於此值時觸發（即處於 Low 分級）。")]
    [SerializeField] private int _socialityLowThreshold = ProtagonistStatusModel.SOCIALITY_MEDIUM_THRESHOLD;
    [SerializeField] private UnityEvent _onSocialityLow;

    [Header("Dependency 門檻")]
    [SerializeField] private int _dependencyHighThreshold = ProtagonistStatusModel.DEPENDENCY_HIGH_THRESHOLD;
    [SerializeField] private UnityEvent _onDependencyHigh;

    private ProtagonistStatusModel _model;
    private bool _stressHighTriggered;
    private bool _stressExtremeTriggered;
    private bool _lifePowerLowTriggered;
    private bool _socialityLowTriggered;
    private bool _dependencyHighTriggered;

    private void Start()
    {
        _model = GameStatusService.Instance?.Protagonist;

        if (_model == null)
        {
            Debug.LogError($"[{nameof(ProtagonistStatusDepletionTrigger)}] 無法取得 ProtagonistStatusModel！", this);
            enabled = false;
            return;
        }

        Subscribe();
        MarkAlreadySatisfiedAsTriggered();
    }

    private void OnEnable()
    {
        if (_model != null) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        Unsubscribe();
        _model.OnStressChanged += HandleStressChanged;
        _model.OnLifePowerChanged += HandleLifePowerChanged;
        _model.OnSocialityChanged += HandleSocialityChanged;
        _model.OnDependencyChanged += HandleDependencyChanged;
    }

    private void Unsubscribe()
    {
        if (_model == null) return;
        _model.OnStressChanged -= HandleStressChanged;
        _model.OnLifePowerChanged -= HandleLifePowerChanged;
        _model.OnSocialityChanged -= HandleSocialityChanged;
        _model.OnDependencyChanged -= HandleDependencyChanged;
    }

    private void MarkAlreadySatisfiedAsTriggered()
    {
        _stressHighTriggered = _model.Stress >= _stressHighThreshold;
        _stressExtremeTriggered = _model.Stress >= _stressExtremeThreshold;
        _lifePowerLowTriggered = _model.LifePower < _lifePowerLowThreshold;
        _socialityLowTriggered = _model.Sociality < _socialityLowThreshold;
        _dependencyHighTriggered = _model.Dependency >= _dependencyHighThreshold;
    }

    private void HandleStressChanged(int delta)
    {
        TryInvokeIfConditionsMet();
    }

    private void HandleLifePowerChanged(int delta)
    {
        TryInvokeIfConditionsMet();
    }

    private void HandleSocialityChanged(int delta)
    {
        TryInvokeIfConditionsMet();
    }

    private void HandleDependencyChanged(int delta)
    {
        TryInvokeIfConditionsMet();
    }

    /// <summary>
    /// 主動補檢查所有條件。已經觸發過的不會重複觸發。
    /// </summary>
    public void TryInvokeIfConditionsMet()
    {
        if (_model == null) return;

        if (!_stressExtremeTriggered && _model.Stress >= _stressExtremeThreshold)
        {
            _stressExtremeTriggered = true;
            _onStressExtreme?.Invoke();
            return;
        }

        if (!_stressHighTriggered && _model.Stress >= _stressHighThreshold)
        {
            _stressHighTriggered = true;
            _onStressHigh?.Invoke();
        }

        if (!_lifePowerLowTriggered && _model.LifePower < _lifePowerLowThreshold)
        {
            _lifePowerLowTriggered = true;
            _onLifePowerLow?.Invoke();
        }

        if (!_socialityLowTriggered && _model.Sociality < _socialityLowThreshold)
        {
            _socialityLowTriggered = true;
            _onSocialityLow?.Invoke();
        }

        if (!_dependencyHighTriggered && _model.Dependency >= _dependencyHighThreshold)
        {
            _dependencyHighTriggered = true;
            _onDependencyHigh?.Invoke();
        }
    }

    public void ResetAllTriggers()
    {
        _stressHighTriggered = false;
        _stressExtremeTriggered = false;
        _lifePowerLowTriggered = false;
        _socialityLowTriggered = false;
        _dependencyHighTriggered = false;
    }

    public void ResetStressHighTrigger() => _stressHighTriggered = false;
    public void ResetStressExtremeTrigger() => _stressExtremeTriggered = false;
    public void ResetLifePowerLowTrigger() => _lifePowerLowTriggered = false;
    public void ResetSocialityLowTrigger() => _socialityLowTriggered = false;
    public void ResetDependencyHighTrigger() => _dependencyHighTriggered = false;
}