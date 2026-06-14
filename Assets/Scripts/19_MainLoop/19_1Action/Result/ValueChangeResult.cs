using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NHK 版資源 / 狀態變動結果處理器。
/// 支援：Stress / LifePower / Sociality / Dependency / Money / SkillPoints / Time / Heroine resources / Flag 開關。
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
        Sociality,
        Dependency,
        Money,
        SkillPoints
    }

    // ==================================================
    // 女主角資源類型列舉
    // ==================================================
    public enum HeroineResourceType
    {
        Libido,
        Trust
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

        [Tooltip("基礎增加數量。若要降低 Stress 等，也可填負數。")]
        public int baseAmount = 0;

        [Tooltip("隨機效果設定")]
        public RandomEffectSettings randomEffect = new RandomEffectSettings();
    }

    [Serializable]
    public class HeroineResourceChangeItem
    {
        public HeroineResourceType resourceType = HeroineResourceType.Libido;
        public int amount = 0;
        public RandomEffectSettings randomEffect = new RandomEffectSettings();
    }

    // ==================================================
    // Flag 操作類型
    // ==================================================
    public enum FlagOperation
    {
        AddFlag,    // 開啟 Flag
        RemoveFlag  // 移除 Flag
    }

    [Serializable]
    public class FlagChangeItem
    {
        [Tooltip("要操作的 Flag 定義（拖曳 ProgressFlagDefinition 資源）")]
        public ProgressFlagDefinition flagDefinition;

        [Tooltip("操作類型：開啟 / 移除")]
        public FlagOperation operation = FlagOperation.AddFlag;

        [Tooltip("Flag 生命週期（僅 AddFlag 時生效；RemoveFlag 會清掉所有桶子）")]
        public FlagLifetime lifetime = FlagLifetime.Persistent;
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

    // ==================================================
    // 時間扣除單位
    // ==================================================
    public enum TimeDeductUnit
    {
        Slot,   // 以時段(Slot)為單位（原本的行為）
        Phase   // 以階段(Phase)為單位：填 1 = 跳至下一 Phase
    }

    [Header("4. 時間扣除")]
    [SerializeField] private bool deductTime = false;
    [Tooltip("扣除單位：Slot=逐格扣；Phase=以階段為單位，填 1 即跳至下一 Phase。\n注意：此單位僅作用於下方「手動時間量」；由 Router 帶入的時間一律以 Slot 計。")]
    [SerializeField] private TimeDeductUnit timeUnit = TimeDeductUnit.Slot;
    [SerializeField] private int manualTimeAmount = 0;

    [Header("5. 女主角資源變動")]
    [SerializeField] private string targetHeroineID = "";
    [SerializeField] private List<HeroineResourceChangeItem> heroineResourceChanges = new List<HeroineResourceChangeItem>();

    [Header("5.5 數值變化套組 (StatChangePackageDatabase)")]
    [Tooltip("以 ID 套用集中設定的數值變化套組（主角 + 女主角）。套組中的女主角項會套到上方的 targetHeroineID。可填多個，依序執行。")]
    [SerializeField] private List<string> statPackageIDs = new List<string>();

    [Header("6. Flag 開關（隱性，不報告）")]
    [SerializeField] private List<FlagChangeItem> flagChanges = new List<FlagChangeItem>();

    [Header("7. 報告設定")]
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
        ExecuteStatPackages();
        ExecuteFlagChanges();

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
        // Router 帶入的時間一律以 Slot 計，優先處理。
        if (sourceRouter != null && sourceRouter.checkTime)
        {
            int routerSlots = sourceRouter.timeAmount;
            if (routerSlots > 0)
            {
                if (t != null) t.TryAdvanceTime(routerSlots);
                else Debug.LogWarning("[ValueChangeResult] TimeSystemModel 尚未初始化，無法扣除時間。");
                Debug.Log($"[ValueChangeResult] 時間扣除(Slot, Router): {routerSlots}");
            }
            return;
        }

        // 手動時間扣除：依 timeUnit 決定以 Slot 或 Phase 為單位。
        if (deductTime && manualTimeAmount > 0)
        {
            if (t == null)
            {
                Debug.LogWarning("[ValueChangeResult] TimeSystemModel 尚未初始化，無法扣除時間。");
                return;
            }

            if (timeUnit == TimeDeductUnit.Phase)
            {
                t.TryAdvancePhases(manualTimeAmount);
                Debug.Log($"[ValueChangeResult] 時間扣除(Phase): {manualTimeAmount}");
            }
            else
            {
                t.TryAdvanceTime(manualTimeAmount);
                Debug.Log($"[ValueChangeResult] 時間扣除(Slot): {manualTimeAmount}");
            }
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

    private void ExecuteStatPackages()
    {
        if (statPackageIDs == null || statPackageIDs.Count == 0) return;

        var statService = GameStatusService.Instance?.StatChangeService;
        if (statService == null)
        {
            Debug.LogWarning("[ValueChangeResult] StatChangeService 尚未初始化，跳過數值變化套組。");
            return;
        }

        foreach (var id in statPackageIDs)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            // 套組中的女主角項會套到 targetHeroineID；回傳的記錄已內含 heroineNameKey。
            var records = statService.Apply(id, targetHeroineID);
            if (records != null && records.Count > 0)
            {
                _changeRecords.AddRange(records);
            }
            Debug.Log($"[ValueChangeResult] 套用數值變化套組: {id} (記錄數: {records?.Count ?? 0})");
        }
    }

    private void ExecuteFlagChanges()
    {
        if (flagChanges == null || flagChanges.Count == 0) return;

        var service = GameStatusService.Instance;
        var flagModel = service?.ProgressFlags;
        if (flagModel == null)
        {
            Debug.LogWarning("[ValueChangeResult] ProgressFlags 尚未初始化，跳過 Flag 操作。");
            return;
        }

        foreach (var item in flagChanges)
        {
            if (item == null || item.flagDefinition == null)
            {
                Debug.LogWarning("[ValueChangeResult] FlagChangeItem 缺少 flagDefinition，已跳過。");
                continue;
            }

            string key = item.flagDefinition.FlagID;
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[ValueChangeResult] flagDefinition 的 FlagID 為空，已跳過：{item.flagDefinition.name}");
                continue;
            }

            switch (item.operation)
            {
                case FlagOperation.AddFlag:
                    flagModel.AddFlag(key, item.lifetime);
                    Debug.Log($"[ValueChangeResult] Flag 開啟 - {key} (Lifetime: {item.lifetime})");
                    break;

                case FlagOperation.RemoveFlag:
                    flagModel.RemoveFlag(key);
                    Debug.Log($"[ValueChangeResult] Flag 移除 - {key}");
                    break;
            }
            // 注意：刻意不呼叫 RecordChange，Flag 操作為隱性，不進報告。
        }
    }

    private void ApplyHeroineChange(HeroineStatusModel heroine, HeroineResourceType type, int amount)
    {
        switch (type)
        {
            case HeroineResourceType.Libido:
                heroine.AddLibido(amount);
                break;
            case HeroineResourceType.Trust:
                heroine.AddTrust(amount);
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
            case ResourceType.Sociality:
                p.ReduceSociality(amount);
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
            case ResourceType.Sociality:
                p.AddSociality(amount);
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

    public void AddStatPackageID(string packageID)
    {
        if (string.IsNullOrWhiteSpace(packageID)) return;
        statPackageIDs.Add(packageID);
    }

    public void ClearStatPackageIDs() => statPackageIDs.Clear();

    public void AddFlagChangeItem(ProgressFlagDefinition definition, FlagOperation operation, FlagLifetime lifetime = FlagLifetime.Persistent)
    {
        if (definition == null) return;
        flagChanges.Add(new FlagChangeItem
        {
            flagDefinition = definition,
            operation = operation,
            lifetime = lifetime
        });
    }

    public void ClearFlagChangeItems() => flagChanges.Clear();

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