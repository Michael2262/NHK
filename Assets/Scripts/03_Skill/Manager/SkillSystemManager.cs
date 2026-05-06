using System.Collections.Generic;
using UnityEngine;

// 職責：管理技能系統的狀態查詢、解鎖邏輯以及效果應用。
public enum SkillStatus { Locked, Learnable, Learned }

public class SkillSystemManager
{
    private readonly ProtagonistStatusModel _pStatus;
    private readonly ProtagonistSkillModel _pSkill;
    private readonly SkillConfig _config;
    private readonly ProgressFlagModel _pFlags;

    public SkillSystemManager(ProtagonistStatusModel pStatus, ProtagonistSkillModel pSkill, SkillConfig config, ProgressFlagModel pFlags)
    {
        _pStatus = pStatus;
        _pSkill = pSkill;
        _config = config;
        _pFlags = pFlags;
    }

    public SkillStatus GetSkillStatus(string skillID)
    {
        SkillData skill = _config.GetSkillData(skillID);
        if (skill == null) return SkillStatus.Locked;

        if (_pSkill.IsSkillUnlocked(skillID)) return SkillStatus.Learned;

        foreach (SkillData preRequisite in skill.Prerequisites)
        {
            if (!_pSkill.IsSkillUnlocked(preRequisite.SkillID)) return SkillStatus.Locked;
        }

        // 既有欄位 RequiredLevel 暫作「解鎖天數」使用。
        if (_pStatus.Day >= skill.RequiredLevel && _pStatus.SkillPoints >= skill.Cost)
            return SkillStatus.Learnable;

        return SkillStatus.Locked;
    }

    public bool TryUnlockSkill(string skillID)
    {
        SkillStatus status = GetSkillStatus(skillID);
        if (status == SkillStatus.Learnable)
        {
            SkillData skill = _config.GetSkillData(skillID);
            if (_pStatus.TryReduceSkillPoints(skill.Cost))
            {
                _pSkill.UnlockSkill(skillID);
                ApplySkillEffect(skill);
                Debug.Log($"[SkillSystem] 成功學習：{skill.DisplayName}");
                return true;
            }
        }
        Debug.LogWarning($"[SkillSystem] 學習失敗：{skillID}。狀態：{status}");
        return false;
    }

    private void ApplySkillEffect(SkillData skill)
    {
        if (skill == null) return;

        switch (skill.EffectType)
        {
            // 舊版攻擊 / 防禦效果在 NHK 中不再使用。
            // 為避免舊 SkillData 失效，暫時轉成較保守的主角狀態補正。
            case SkillEffectType.PermanentAttackIncrease:
                _pStatus.AddLifePower(skill.EffectValue);
                Debug.Log($"[SkillSystem] 舊 PermanentAttackIncrease 已轉換為 LifePower +{skill.EffectValue}");
                break;

            case SkillEffectType.PermanentDefenseIncrease:
                _pStatus.ReduceStress(skill.EffectValue);
                Debug.Log($"[SkillSystem] 舊 PermanentDefenseIncrease 已轉換為 Stress -{skill.EffectValue}");
                break;

            case SkillEffectType.UnlockNewAction:
                if (_pFlags != null && skill.EffectFlag != null)
                {
                    string flagIDToAdd = skill.EffectFlag.FlagID;
                    _pFlags.AddPersistentFlag(flagIDToAdd);
                    Debug.Log($"[SkillSystem] 觸發永久 Flag: {flagIDToAdd}");
                }
                else
                {
                    Debug.LogWarning($"[SkillSystem] 技能 {skill.SkillID} 類型為 UnlockNewAction，但 EffectFlag 未指定或 ProgressFlagModel 未注入。");
                }
                break;
        }
    }
}
