using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 職責：管理角色身上所有持續性的狀態效果 (Buff/Debuff)。
/// (已加入興奮度效果處理，並改用通用 Modifier 計算)
/// </summary>
public class ProtagonistStatusEffectModel
{
    // 內部類別 ActiveEffect 保持不變
    private class ActiveEffect
    {
        public StatusEffect Effect;
        public int RemainingDays;
    }

    private readonly List<ActiveEffect> _activeEffects = new List<ActiveEffect>(); //
    public event Action OnEffectsChanged; //

    // AddEffect 方法保持不變 (因為 OnApply/OnRemove 邏輯仍在 StatusEffect 子類中)
    public void AddEffect(StatusEffect newEffect, ProtagonistStatusModel target)
    {
        if (newEffect == null) return;
        var existing = _activeEffects.FirstOrDefault(e => e.Effect.EffectID == newEffect.EffectID);
        if (existing != null)
        {
            existing.RemainingDays = newEffect.DurationInDays;
            Debug.Log($"狀態效果 [{newEffect.DisplayName}] 的持續時間被重置。");
        }
        else
        {
            var activeEffect = new ActiveEffect { Effect = newEffect, RemainingDays = newEffect.DurationInDays };
            _activeEffects.Add(activeEffect);
            activeEffect.Effect.OnApply(target); // 觸發 OnApply
        }
        OnEffectsChanged?.Invoke(); // 通知總值需要重新計算
    }

    // HandleDayPassed 方法保持不變
    public void HandleDayPassed(ProtagonistStatusModel target)
    {
        bool hasChanged = false;
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effectInstance = _activeEffects[i];
            effectInstance.Effect.OnDayPassed(target); // 觸發 OnDayPassed
            if (effectInstance.Effect.DurationInDays > 0)
            {
                effectInstance.RemainingDays--;
                if (effectInstance.RemainingDays <= 0)
                {
                    effectInstance.Effect.OnRemove(target); // 觸發 OnRemove
                    _activeEffects.RemoveAt(i);
                    hasChanged = true;
                }
            }
        }
        if (hasChanged) OnEffectsChanged?.Invoke(); // 通知總值需要重新計算
    }

    // ▼▼▼【★ 核心修改 1：改寫 Modifier 方法 ★】▼▼▼
    // 不再需要檢查具體類型，直接呼叫 StatusEffect 上的虛擬方法

    /// <summary>
    /// 查詢當前所有攻擊力加成的總和。
    /// </summary>
    public int GetAttackModifier()
    {
        int totalBonus = 0;
        foreach (var activeEffect in _activeEffects)
        {
            // 直接呼叫基類的 GetAttackModifier()
            totalBonus += activeEffect.Effect.GetAttackModifier();
        }
        // 原始碼: 檢查 is StatusEffect_TemporaryStatBuff
        return totalBonus;
    }

    /// <summary>
    /// 查詢當前所有防禦力加成的總和。
    /// </summary>
    public int GetDefenseModifier()
    {
        int totalBonus = 0;
        foreach (var activeEffect in _activeEffects)
        {
            // 直接呼叫基類的 GetDefenseModifier()
            totalBonus += activeEffect.Effect.GetDefenseModifier();
        }
        // 原始碼: 檢查 is StatusEffect_TemporaryStatBuff
        return totalBonus;
    }

    /// <summary>
    /// 【★ 新增 ★】查詢當前所有興奮度等級加成的總和。
    /// </summary>
    public int GetExcitementModifier()
    {
        int totalBonus = 0;
        foreach (var activeEffect in _activeEffects)
        {
            // 直接呼叫基類的 GetExcitementModifier()
            totalBonus += activeEffect.Effect.GetExcitementModifier();
        }
        return totalBonus;
    }
    
    public void NewGame()
    {
        _activeEffects.Clear();
        OnEffectsChanged?.Invoke();
    }

    public ProtagonistStatusEffectSaveData ToSaveData()
    {
        var saveData = new ProtagonistStatusEffectSaveData();
        foreach (var activeEffect in _activeEffects)
        {
            saveData.ActiveEffects.Add(new ActiveStatusEffectSaveData
            {
                EffectID = activeEffect.Effect.EffectID,
                RemainingDays = activeEffect.RemainingDays
            });
        }
        return saveData;
    }

    public void LoadFromSaveData(ProtagonistStatusEffectSaveData data, StatusEffectDatabase effectDatabase)
    {
        _activeEffects.Clear();
        if (data?.ActiveEffects == null || effectDatabase == null)
        {
            OnEffectsChanged?.Invoke(); // 即使清空也要通知
            return;
        }

        foreach (var effectData in data.ActiveEffects)
        {
            StatusEffect effectAsset = effectDatabase.GetEffectByID(effectData.EffectID);
            if (effectAsset != null)
            {
                _activeEffects.Add(new ActiveEffect
                {
                    Effect = effectAsset,
                    RemainingDays = effectData.RemainingDays
                });
            }
            else
            {
                Debug.LogWarning($"讀取存檔時找不到 ID 為 [{effectData.EffectID}] 的狀態效果，可能已被移除。");
            }
        }
        OnEffectsChanged?.Invoke(); // 載入完成後通知
    }
    public List<(string name, int days)> GetActiveEffectInfoForUI()
    {
        var infoList = new List<(string name, int days)>();
        foreach (var activeEffect in _activeEffects)
        {
            infoList.Add((activeEffect.Effect.DisplayName, activeEffect.RemainingDays));
        }
        return infoList;
    }

}

