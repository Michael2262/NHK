using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NHK 版主角狀態門檻觸發器。
/// 原檔名保留，但用途改為監測 Stress / LifePower / SocialFear / Dependency。
/// 
/// 主要用途：
/// 1. Stress 進入危險 / 崩潰時觸發事件。
/// 2. LifePower 低於門檻時觸發事件。
/// 3. SocialFear 高於門檻時觸發事件。
/// 4. Dependency 高於門檻時觸發事件。
/// 
/// 一次性事件預設「場景內只觸發一次」，可由 ResetAllTriggers() 重置。
/// </summary>
public class ProtagonistStatusDepletionTrigger : MonoBehaviour
{
    [Header("Stress 門檻")]
    [SerializeField] private int _stressCriticalThreshold = ProtagonistStatusModel.STRESS_CRITICAL_THRESHOLD;
    [SerializeField] private int _stressCollapseThreshold = ProtagonistStatusModel.STRESS_COLLAPSE_THRESHOLD;
    [SerializeField] private UnityEvent _onStressCritical;
    [SerializeField] private UnityEvent _onStressCollapsed;

    [Header("LifePower 門檻")]
    [SerializeField] private int _lifePowerLowThreshold = ProtagonistStatusModel.LIFE_VERY_LOW_MAX;
    [SerializeField] private UnityEvent _onLifePowerLow;

    [Header("SocialFear 門檻")]
    [SerializeField] private int _socialFearHighThreshold = ProtagonistStatusModel.SOCIAL_FEAR_HIGH_THRESHOLD;
    [SerializeField] private UnityEvent _onSocialFearHigh;

    [Header("Dependency 門檻")]
    [SerializeField] private int _dependencyHighThreshold = ProtagonistStatusModel.DEPENDENCY_HIGH_THRESHOLD;
    [SerializeField] private UnityEvent _onDependencyHigh;

    private ProtagonistStatusModel _model;
    private bool _stressCriticalTriggered;
    private bool _stressCollapsedTriggered;
    private bool _lifePowerLowTriggered;
    private bool _socialFearHighTriggered;
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
        _model.OnSocialFearChanged += HandleSocialFearChanged;
        _model.OnDependencyChanged += HandleDependencyChanged;
    }

    private void Unsubscribe()
    {
        if (_model == null) return;
        _model.OnStressChanged -= HandleStressChanged;
        _model.OnLifePowerChanged -= HandleLifePowerChanged;
        _model.OnSocialFearChanged -= HandleSocialFearChanged;
        _model.OnDependencyChanged -= HandleDependencyChanged;
    }

    private void MarkAlreadySatisfiedAsTriggered()
    {
        _stressCriticalTriggered = _model.Stress >= _stressCriticalThreshold;
        _stressCollapsedTriggered = _model.Stress >= _stressCollapseThreshold;
        _lifePowerLowTriggered = _model.LifePower <= _lifePowerLowThreshold;
        _socialFearHighTriggered = _model.SocialFear >= _socialFearHighThreshold;
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

    private void HandleSocialFearChanged(int delta)
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

        if (!_stressCollapsedTriggered && _model.Stress >= _stressCollapseThreshold)
        {
            _stressCollapsedTriggered = true;
            _onStressCollapsed?.Invoke();
            return;
        }

        if (!_stressCriticalTriggered && _model.Stress >= _stressCriticalThreshold)
        {
            _stressCriticalTriggered = true;
            _onStressCritical?.Invoke();
        }

        if (!_lifePowerLowTriggered && _model.LifePower <= _lifePowerLowThreshold)
        {
            _lifePowerLowTriggered = true;
            _onLifePowerLow?.Invoke();
        }

        if (!_socialFearHighTriggered && _model.SocialFear >= _socialFearHighThreshold)
        {
            _socialFearHighTriggered = true;
            _onSocialFearHigh?.Invoke();
        }

        if (!_dependencyHighTriggered && _model.Dependency >= _dependencyHighThreshold)
        {
            _dependencyHighTriggered = true;
            _onDependencyHigh?.Invoke();
        }
    }

    public void ResetAllTriggers()
    {
        _stressCriticalTriggered = false;
        _stressCollapsedTriggered = false;
        _lifePowerLowTriggered = false;
        _socialFearHighTriggered = false;
        _dependencyHighTriggered = false;
    }

    public void ResetStressCriticalTrigger() => _stressCriticalTriggered = false;
    public void ResetStressCollapsedTrigger() => _stressCollapsedTriggered = false;
    public void ResetLifePowerLowTrigger() => _lifePowerLowTriggered = false;
    public void ResetSocialFearHighTrigger() => _socialFearHighTriggered = false;
    public void ResetDependencyHighTrigger() => _dependencyHighTriggered = false;
}
