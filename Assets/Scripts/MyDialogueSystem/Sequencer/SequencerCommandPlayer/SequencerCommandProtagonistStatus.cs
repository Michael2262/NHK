// ============================================================
// SequencerCommandProtagonistStatus.cs
// ============================================================
// 集中存放主角核心數值與資源變化的 Sequencer Commands。
//
// 共通用法:
//   <Command>(amount)                 // 只變更數值，不顯示通知
//   <Command>(amount, alert)          // 變更後彈出 ValueChangeReporter alert
//   <Command>(amount, type)           // 變更後在 Subtitle 顯示變化
//
// 範例:
//   AddStress(10, alert)
//   ReduceLifePower(5, type)
//   SetMoney(0)
//   AddMoney(100, type)
//
// 包含的指令:
//   Money:       AddMoney / ReduceMoney / SetMoney
//   SkillPoints: AddSkillPoints / ReduceSkillPoints / SetSkillPoints
//   Stress:      AddStress / ReduceStress / SetStress
//   LifePower:   AddLifePower / ReduceLifePower / SetLifePower
//   SocialFear:  AddSocialFear / ReduceSocialFear / SetSocialFear
//   Dependency:  AddDependency / ReduceDependency / SetDependency
// ============================================================

using UnityEngine;
using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    // ========================================================
    // 共用基底 / 工具
    // ========================================================
    internal static class ProtagonistStatusSequencerUtil
    {
        public static ProtagonistStatusModel GetProtagonist(string commandName)
        {
            if (GameStatusService.Instance == null || GameStatusService.Instance.Protagonist == null)
            {
                Debug.LogWarning($"Dialogue System: {commandName} 指令找不到 GameStatusService 或 Protagonist 實例。");
                return null;
            }
            return GameStatusService.Instance.Protagonist;
        }

        /// <summary>
        /// 依 reportMode 字串送出 ValueChangeReporter 通知。
        /// reportMode: "alert" / "type" / 空字串 / "false" / 其他
        /// </summary>
        public static void Report(string reportMode, string resourceTypeKey, int actualDelta, string commandName)
        {
            if (actualDelta == 0) return;
            if (string.IsNullOrEmpty(reportMode)) return;
            if (reportMode == "false") return;

            var records = new List<ValueChangeRecord>
            {
                new ValueChangeRecord
                {
                    isHeroineResource = false,
                    resourceTypeKey = resourceTypeKey,
                    finalAmount = actualDelta,
                    effectResult = ValueChangeResult.EffectResult.Normal,
                    heroineNameKey = null
                }
            };

            switch (reportMode)
            {
                case "alert":
                    ValueChangeReporter.Report(records);
                    break;
                case "type":
                    ValueChangeReporter.ReportToSubtitle(records);
                    break;
                default:
                    Debug.LogWarning($"[{commandName}] 未知的 reportMode: '{reportMode}'");
                    break;
            }
        }
    }

    /// <summary>
    /// 共用基底：把 Add / Reduce / Set 的樣板抽出來。
    /// 子類別只需要提供「資源 Key」、「目前值取得」、「套用變化」三件事。
    /// </summary>
    public abstract class SequencerCommandProtagonistResourceBase : SequencerCommand
    {
        protected abstract string ResourceTypeKey { get; }
        protected abstract string CommandName { get; }

        /// <summary>讀取套用前的數值。</summary>
        protected abstract int GetCurrentValue(ProtagonistStatusModel p);

        /// <summary>
        /// 套用變化。傳入第 0 個參數 (int)，回傳實際發生的 delta。
        /// </summary>
        protected abstract int ApplyChange(ProtagonistStatusModel p, int parameter);

        public void Awake()
        {
            int parameter = GetParameterAsInt(0);
            string reportMode = GetParameter(1, string.Empty).Trim().ToLower();

            var p = ProtagonistStatusSequencerUtil.GetProtagonist(CommandName);
            if (p != null)
            {
                int actualDelta = ApplyChange(p, parameter);
                ProtagonistStatusSequencerUtil.Report(reportMode, ResourceTypeKey, actualDelta, CommandName);
            }

            Stop();
        }
    }

    // ========================================================
    // Money
    // ========================================================
    /// <summary>
    /// 用法: AddMoney(amount) / AddMoney(amount, alert) / AddMoney(amount, type)
    /// </summary>
    public class SequencerCommandAddMoney : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Money";
        protected override string CommandName => "AddMoney";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Money;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Money;
            p.AddMoney(parameter);
            return p.Money - prev;
        }
    }

    /// <summary>
    /// 用法: ReduceMoney(amount) / ReduceMoney(amount, alert) / ReduceMoney(amount, type)
    /// </summary>
    public class SequencerCommandReduceMoney : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Money";
        protected override string CommandName => "ReduceMoney";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Money;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.Money;
            p.AddMoney(-amount);
            return p.Money - prev;
        }
    }

    /// <summary>
    /// 用法: SetMoney(value) / SetMoney(value, alert) / SetMoney(value, type)
    /// 報告的 delta 為「設定後 - 設定前」。
    /// </summary>
    public class SequencerCommandSetMoney : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Money";
        protected override string CommandName => "SetMoney";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Money;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Money;
            p.SetMoney(parameter);
            return p.Money - prev;
        }
    }

    // ========================================================
    // SkillPoints
    // ========================================================
    public class SequencerCommandAddSkillPoints : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "SkillPoints";
        protected override string CommandName => "AddSkillPoints";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.SkillPoints;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.SkillPoints;
            p.AddSkillPoints(parameter);
            return p.SkillPoints - prev;
        }
    }

    public class SequencerCommandReduceSkillPoints : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "SkillPoints";
        protected override string CommandName => "ReduceSkillPoints";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.SkillPoints;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.SkillPoints;
            p.AddSkillPoints(-amount);
            return p.SkillPoints - prev;
        }
    }

    public class SequencerCommandSetSkillPoints : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "SkillPoints";
        protected override string CommandName => "SetSkillPoints";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.SkillPoints;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.SkillPoints;
            p.SetSkillPoints(parameter);
            return p.SkillPoints - prev;
        }
    }

    // ========================================================
    // Stress (壓力)
    // ========================================================
    public class SequencerCommandAddStress : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Stress";
        protected override string CommandName => "AddStress";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Stress;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Stress;
            p.AddStress(parameter);
            return p.Stress - prev;
        }
    }

    public class SequencerCommandReduceStress : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Stress";
        protected override string CommandName => "ReduceStress";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Stress;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.Stress;
            p.ReduceStress(amount);
            return p.Stress - prev;
        }
    }

    public class SequencerCommandSetStress : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Stress";
        protected override string CommandName => "SetStress";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Stress;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Stress;
            p.SetStress(parameter);
            return p.Stress - prev;
        }
    }

    // ========================================================
    // LifePower (生活力)
    // ========================================================
    public class SequencerCommandAddLifePower : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "LifePower";
        protected override string CommandName => "AddLifePower";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.LifePower;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.LifePower;
            p.AddLifePower(parameter);
            return p.LifePower - prev;
        }
    }

    public class SequencerCommandReduceLifePower : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "LifePower";
        protected override string CommandName => "ReduceLifePower";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.LifePower;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.LifePower;
            p.ReduceLifePower(amount);
            return p.LifePower - prev;
        }
    }

    public class SequencerCommandSetLifePower : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "LifePower";
        protected override string CommandName => "SetLifePower";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.LifePower;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.LifePower;
            p.SetLifePower(parameter);
            return p.LifePower - prev;
        }
    }

    // ========================================================
    // SocialFear (社會恐懼)
    // ========================================================
    public class SequencerCommandAddSocialFear : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "SocialFear";
        protected override string CommandName => "AddSocialFear";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.SocialFear;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.SocialFear;
            p.AddSocialFear(parameter);
            return p.SocialFear - prev;
        }
    }

    public class SequencerCommandReduceSocialFear : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "SocialFear";
        protected override string CommandName => "ReduceSocialFear";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.SocialFear;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.SocialFear;
            p.ReduceSocialFear(amount);
            return p.SocialFear - prev;
        }
    }

    public class SequencerCommandSetSocialFear : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "SocialFear";
        protected override string CommandName => "SetSocialFear";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.SocialFear;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.SocialFear;
            p.SetSocialFear(parameter);
            return p.SocialFear - prev;
        }
    }

    // ========================================================
    // Dependency (依賴度)
    // ========================================================
    public class SequencerCommandAddDependency : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Dependency";
        protected override string CommandName => "AddDependency";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Dependency;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Dependency;
            p.AddDependency(parameter);
            return p.Dependency - prev;
        }
    }

    public class SequencerCommandReduceDependency : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Dependency";
        protected override string CommandName => "ReduceDependency";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Dependency;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.Dependency;
            p.ReduceDependency(amount);
            return p.Dependency - prev;
        }
    }

    public class SequencerCommandSetDependency : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Dependency";
        protected override string CommandName => "SetDependency";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Dependency;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Dependency;
            p.SetDependency(parameter);
            return p.Dependency - prev;
        }
    }
}
