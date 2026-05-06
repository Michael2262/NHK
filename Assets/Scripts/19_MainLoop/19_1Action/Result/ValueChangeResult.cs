using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NHK 版資源 / 狀態變動結果處理器。
/// 支援：Stress / LifePower / SocialFear / Dependency / Money / SkillPoints / Time / Heroine resources。
/// </summary>
[AddComponentMenu("Game/API/Value Change Result")]
public class ValueChangeResult : MonoBehaviour
{
    // ==================================================
    // 主角資源 / 狀態類型
    // ==================================================
    public enum ResourceType
    {
        Stress,
        LifePower,
        SocialFear,
        Dependency,
        Money,
        SkillPoints
    }

    // ==================================================
    // 女主角資源類型列舉
    // ==================================================
    public enum HeroineResourceType
    {
        LewdnessExp,
        AffinityExp,
        ExcitementExp
    }

    [Serializable]
    public class ResourceChangeItem
    {
        [Tooltip("資源 / 狀態類型")]
        public ResourceType resourceType = ResourceType.Money;

        [Tooltip("變動數量。扣除 / 減少請填正數，系統會依扣除流程處理。")]
        public int amount = 0;
    }

    [Serializable]
    public class RandomEffectSettings
    {
        [Tooltip("是否啟用隨機效果")]
        public bool enabled = false;

        [Header("好運效果")]
        public bool enableGood = true;
        [Range(0f, 1f)] public float goodChance = 0.1f;
        public float goodMultiplier = 1.5f;

        [Header("厄運效果")]
        public bool enableBad = true;
        [Range(0f, 1f)] public float badChance = 0.1f;
        public float badMultiplier = 0.5f;
    }

    [Serializable]
    public class ResourceGainItem
    {
        [Tooltip("資源 / 狀態類型")]
        public ResourceType resourceType = ResourceType.Money;

        [Tooltip("基礎增加數量。若要降低 Stress / SocialFear 等，也可填負數。")]
        public int baseAmount = 0;

        [Tooltip("隨機效果設定")]
        public RandomEffectSettings randomEffect = new RandomEffectSettings();
    }

    [Serializable]
    public class HeroineResourceChangeItem
    {
        public HeroineResourceType resourceType = HeroineResourceType.AffinityExp;
        public int amount = 0;
        public RandomEffectSettings randomEffect = new RandomEffectSettings();
    }

    public enum EffectResult
    {
        Normal,
        Good,
        Bad
    }

    [Header("1. 資源扣除 - 從 Router 讀取")]
    [SerializeField] private ProtagonistValueRouter sourceRouter;

    [Header("2. 資源扣除 - 主動設定")]
    [SerializeField] private List<ResourceChangeItem> manualDeductions = new List<ResourceChangeItem>();

    [Header("3. 資源 / 狀態增加 - 主動設定")]
    [SerializeField] private List<ResourceGainItem> resourceGains = new List<ResourceGainItem>();

    [Header("4. 時間扣除")]
    [SerializeField] private bool deductTime = false;
    [SerializeField] private int manualTimeAmount = 0;

    [Header("5. 女主角資源變動")]
    [SerializeField] private string targetHeroineID = "";
    [SerializeField] private List<HeroineResourceChangeItem> heroineResourceChanges = new List<HeroineResourceChangeItem>();

    [Header("6. 報告設定")]
    [SerializeField] private bool autoReport = true;

    [Header("事件回調")]
    public UnityEvent onGoodEffect;
    public UnityEvent onBadEffect;
    public UnityEvent onComplete;

    private EffectResult _lastEffectResult = EffectResult.Normal;
    private string _targetHeroineNameKey = "";
    private List<ValueChangeRecord> _changeRecords = new List<ValueChangeRecord>();

    public EffectResult LastEffectResult => _lastEffectResult;
    public string TargetHeroineNameKey => _targetHeroineNameKey;
    public List<ValueChangeRecord> LastChangeRecords => _changeRecords;

    public void Execute()
    {
        var service = GameStatusService.Instance;
        if (service == null || service.Protagonist == null)
        {
            Debug.LogError("[ValueChangeResult] GameStatusService 或 Protagonist 尚未初始化。");
            return;
        }

        var p = service.Protagonist;
        var t = service.Time;

        _lastEffectResult = EffectResult.Normal;
        _targetHeroineNameKey = "";
        _changeRecords.Clear();

        ExecuteDeductions(p);
        ExecuteTimeDeduction(t);
        ExecuteGains(p);
        ExecuteHeroineChanges();

        if (autoReport)
        {
            ValueChangeReporter.Report(_changeRecords);
        }

        onComplete?.Invoke();
        Debug.Log($"[ValueChangeResult] Execute 完成 - 效果結果: {_lastEffectResult}, 變動記錄數: {_changeRecords.Count}");
    }

    public void ShowReport()
    {
        ValueChangeReporter.Report(_changeRecords);
    }

    private (int finalAmount, EffectResult effect) ProcessRandomEffect(int baseAmount, RandomEffectSettings settings)
    {
        if (!settings.enabled || baseAmount == 0)
        {
            return (baseAmount, EffectResult.Normal);
        }

        bool canTriggerGood = settings.enableGood && settings.goodChance > 0;
        bool canTriggerBad = settings.enableBad && settings.badChance > 0;

        if (!canTriggerGood && !canTriggerBad)
        {
            return (baseAmount, EffectResult.Normal);
        }

        float roll = UnityEngine.Random.value;
        if (canTriggerGood && roll < settings.goodChance)
        {
            int finalAmount = Mathf.RoundToInt(baseAmount * settings.goodMultiplier);
            return (finalAmount, EffectResult.Good);
        }

        roll = UnityEngine.Random.value;
        if (canTriggerBad && roll < settings.badChance)
        {
            int finalAmount = Mathf.RoundToInt(baseAmount * settings.badMultiplier);
            return (finalAmount, EffectResult.Bad);
        }

        return (baseAmount, EffectResult.Normal);
    }

    private void ExecuteDeductions(ProtagonistStatusModel p)
    {
        HashSet<ResourceType> handledTypes = new HashSet<ResourceType>();

        if (sourceRouter != null)
        {
            if (TryConvertCheckTypeToResourceType(sourceRouter.checkType, out ResourceType routerType))
            {
                int routerAmount = Mathf.Max(0, sourceRouter.amount);
                ApplyDeduction(p, routerType, routerAmount);
                RecordChange(routerType.ToString(), -routerAmount, EffectResult.Normal, false, null);
                handledTypes.Add(routerType);
                Debug.Log($"[ValueChangeResult] 從 Router 扣除 - {routerType}: {routerAmount}");
            }
        }

        foreach (var item in manualDeductions)
        {
            if (handledTypes.Contains(item.resourceType))
            {
                Debug.Log($"[ValueChangeResult] 跳過主動扣除（已由 Router 處理）- {item.resourceType}");
                continue;
            }

            int amount = Mathf.Max(0, item.amount);
            ApplyDeduction(p, item.resourceType, amount);
            RecordChange(item.resourceType.ToString(), -amount, EffectResult.Normal, false, null);
            Debug.Log($"[ValueChangeResult] 主動扣除 - {item.resourceType}: {amount}");
        }
    }

    private void ExecuteTimeDeduction(TimeSystemModel t)
    {
        int timeToDeduct = 0;

        if (sourceRouter != null && sourceRouter.checkTime)
        {
            timeToDeduct = sourceRouter.timeAmount;
        }
        else if (deductTime && manualTimeAmount > 0)
        {
            timeToDeduct = manualTimeAmount;
        }

        if (timeToDeduct > 0)
        {
            if (t != null) t.TryAdvanceTime(timeToDeduct);
            else Debug.LogWarning("[ValueChangeResult] TimeSystemModel 尚未初始化，無法扣除時間。");
            Debug.Log($"[ValueChangeResult] 時間扣除: {timeToDeduct}");
        }
    }

    private void ExecuteHeroineChanges()
    {
        if (string.IsNullOrEmpty(targetHeroineID) || heroineResourceChanges.Count == 0)
            return;

        if (!GameStatusService.Instance.Heroines.TryGetValue(targetHeroineID, out var heroine))
        {
            Debug.LogWarning($"[ValueChangeResult] 找不到女主角: {targetHeroineID}");
            return;
        }

        _targetHeroineNameKey = heroine.NameTextKey;

        foreach (var item in heroineResourceChanges)
        {
            var (finalAmount, itemEffect) = ProcessRandomEffect(item.amount, item.randomEffect);
            TriggerRandomEffectEvents(itemEffect);

            if (finalAmount != 0)
            {
                ApplyHeroineChange(heroine, item.resourceType, finalAmount);
                RecordChange(item.resourceType.ToString(), finalAmount, itemEffect, true, _targetHeroineNameKey);
                Debug.Log($"[ValueChangeResult] 女主角資源變動 [{targetHeroineID}] - {item.resourceType}: {(finalAmount > 0 ? "+" : "")}{finalAmount} (效果: {itemEffect})");
            }
        }
    }

    private void ApplyHeroineChange(HeroineStatusModel heroine, HeroineResourceType type, int amount)
    {
        switch (type)
        {
            case HeroineResourceType.LewdnessExp:
                heroine.AddLewdnessExp(amount);
                break;
            case HeroineResourceType.AffinityExp:
                heroine.AddAffinityExp(amount);
                break;
            case HeroineResourceType.ExcitementExp:
                heroine.AddExcitementExp(amount);
                break;
        }
    }

    private void ExecuteGains(ProtagonistStatusModel p)
    {
        foreach (var item in resourceGains)
        {
            var (finalAmount, itemEffect) = ProcessRandomEffect(item.baseAmount, item.randomEffect);
            TriggerRandomEffectEvents(itemEffect);

            if (finalAmount != 0)
            {
                ApplyGain(p, item.resourceType, finalAmount);
                RecordChange(item.resourceType.ToString(), finalAmount, itemEffect, false, null);
                Debug.Log($"[ValueChangeResult] 資源 / 狀態變動 - {item.resourceType}: {(finalAmount > 0 ? "+" : "")}{finalAmount} (效果: {itemEffect})");
            }
        }
    }

    private void TriggerRandomEffectEvents(EffectResult effect)
    {
        if (effect == EffectResult.Good)
        {
            _lastEffectResult = EffectResult.Good;
            onGoodEffect?.Invoke();
        }
        else if (effect == EffectResult.Bad)
        {
            _lastEffectResult = EffectResult.Bad;
            onBadEffect?.Invoke();
        }
    }

    private void RecordChange(string typeKey, int amount, EffectResult effect, bool isHeroine, string heroineNameKey)
    {
        _changeRecords.Add(new ValueChangeRecord
        {
            isHeroineResource = isHeroine,
            resourceTypeKey = typeKey,
            finalAmount = amount,
            effectResult = effect,
            heroineNameKey = heroineNameKey
        });
    }

    private void ApplyDeduction(ProtagonistStatusModel p, ResourceType type, int amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case ResourceType.Stress:
                p.ReduceStress(amount);
                break;
            case ResourceType.LifePower:
                p.ReduceLifePower(amount);
                break;
            case ResourceType.SocialFear:
                p.ReduceSocialFear(amount);
                break;
            case ResourceType.Dependency:
                p.ReduceDependency(amount);
                break;
            case ResourceType.Money:
                p.TryReduceMoney(amount);
                break;
            case ResourceType.SkillPoints:
                p.TryReduceSkillPoints(amount);
                break;
        }
    }

    private void ApplyGain(ProtagonistStatusModel p, ResourceType type, int amount)
    {
        if (amount == 0) return;

        switch (type)
        {
            case ResourceType.Stress:
                p.AddStress(amount);
                break;
            case ResourceType.LifePower:
                p.AddLifePower(amount);
                break;
            case ResourceType.SocialFear:
                p.AddSocialFear(amount);
                break;
            case ResourceType.Dependency:
                p.AddDependency(amount);
                break;
            case ResourceType.Money:
                p.AddMoney(amount);
                break;
            case ResourceType.SkillPoints:
                p.AddSkillPoints(amount);
                break;
        }
    }

    private bool TryConvertCheckTypeToResourceType(ProtagonistValueRouter.CheckType checkType, out ResourceType resourceType)
    {
        switch (checkType)
        {
            case ProtagonistValueRouter.CheckType.CheckMoney:
                resourceType = ResourceType.Money;
                return true;
            case ProtagonistValueRouter.CheckType.CheckSkillPoints:
                resourceType = ResourceType.SkillPoints;
                return true;
            default:
                resourceType = ResourceType.Money;
                return false;
        }
    }

    public void SetSourceRouter(ProtagonistValueRouter router) => sourceRouter = router;
    public void ClearSourceRouter() => sourceRouter = null;

    public void AddGainItem(ResourceType type, int baseAmount, bool enableRandom = false,
                            float goodChance = 0.1f, float badChance = 0.1f,
                            float goodMult = 1.5f, float badMult = 0.5f,
                            bool enableGood = true, bool enableBad = true)
    {
        resourceGains.Add(new ResourceGainItem
        {
            resourceType = type,
            baseAmount = baseAmount,
            randomEffect = new RandomEffectSettings
            {
                enabled = enableRandom,
                enableGood = enableGood,
                goodChance = goodChance,
                goodMultiplier = goodMult,
                enableBad = enableBad,
                badChance = badChance,
                badMultiplier = badMult
            }
        });
    }

    public void ClearGainItems() => resourceGains.Clear();

    public void AddDeductionItem(ResourceType type, int amount)
    {
        manualDeductions.Add(new ResourceChangeItem
        {
            resourceType = type,
            amount = amount
        });
    }

    public void ClearDeductionItems() => manualDeductions.Clear();

    public void SetTargetHeroineID(string heroineID) => targetHeroineID = heroineID;
    public void ClearTargetHeroineID() => targetHeroineID = "";

    public void AddHeroineChangeItem(HeroineResourceType type, int amount, bool enableRandom = false,
                                      float goodChance = 0.1f, float badChance = 0.1f,
                                      float goodMult = 1.5f, float badMult = 0.5f,
                                      bool enableGood = true, bool enableBad = true)
    {
        heroineResourceChanges.Add(new HeroineResourceChangeItem
        {
            resourceType = type,
            amount = amount,
            randomEffect = new RandomEffectSettings
            {
                enabled = enableRandom,
                enableGood = enableGood,
                goodChance = goodChance,
                goodMultiplier = goodMult,
                enableBad = enableBad,
                badChance = badChance,
                badMultiplier = badMult
            }
        });
    }

    public void ClearHeroineChangeItems() => heroineResourceChanges.Clear();

    public void ExecuteHeroineChangesOnly()
    {
        _lastEffectResult = EffectResult.Normal;
        _targetHeroineNameKey = "";
        _changeRecords.Clear();
        ExecuteHeroineChanges();

        if (autoReport)
        {
            ValueChangeReporter.Report(_changeRecords);
        }

        onComplete?.Invoke();
        Debug.Log($"[ValueChangeResult] ExecuteHeroineChangesOnly 完成 - 效果結果: {_lastEffectResult}");
    }

    public void SetAutoReport(bool value) => autoReport = value;
}
