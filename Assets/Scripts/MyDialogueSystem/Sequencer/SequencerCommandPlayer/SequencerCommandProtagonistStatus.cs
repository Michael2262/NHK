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
//   Sociality:  AddSociality / ReduceSociality / SetSociality
//   Dependency:  AddDependency / ReduceDependency / SetDependency
//   RoomMess:    AddRoomMess / ReduceRoomMess / SetRoomMess     (房間髒亂度 0~25)
//   BodyDirty:   AddBodyDirty / ReduceBodyDirty / SetBodyDirty  (身體髒污度 0~25)
//   ShootTimes:  AddShootTimes / ReduceShootTimes
//   BadHealthy:  BadHealthy(on) / BadHealthy(off)
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
    // Sociality (社會性)
    // ========================================================
    public class SequencerCommandAddSociality : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Sociality";
        protected override string CommandName => "AddSociality";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Sociality;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Sociality;
            p.AddSociality(parameter);
            return p.Sociality - prev;
        }
    }

    public class SequencerCommandReduceSociality : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Sociality";
        protected override string CommandName => "ReduceSociality";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Sociality;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.Sociality;
            p.ReduceSociality(amount);
            return p.Sociality - prev;
        }
    }

    public class SequencerCommandSetSociality : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "Sociality";
        protected override string CommandName => "SetSociality";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.Sociality;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.Sociality;
            p.SetSociality(parameter);
            return p.Sociality - prev;
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

    // ========================================================
    // RoomMess（房間髒亂度 0~25）
    // ResourceTypeKey 用 "RoomMessLevel"，與 ResourceType 列舉一致。
    // ========================================================
    public class SequencerCommandAddRoomMess : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "RoomMessLevel";
        protected override string CommandName => "AddRoomMess";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.RoomMessLevel;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.RoomMessLevel;
            p.AddRoomMessLevel(parameter);
            return p.RoomMessLevel - prev;
        }
    }

    public class SequencerCommandReduceRoomMess : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "RoomMessLevel";
        protected override string CommandName => "ReduceRoomMess";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.RoomMessLevel;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.RoomMessLevel;
            p.ReduceRoomMessLevel(amount);
            return p.RoomMessLevel - prev;
        }
    }

    public class SequencerCommandSetRoomMess : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "RoomMessLevel";
        protected override string CommandName => "SetRoomMess";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.RoomMessLevel;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.RoomMessLevel;
            p.SetRoomMessLevel(parameter);
            return p.RoomMessLevel - prev;
        }
    }

    // ========================================================
    // BodyDirty（身體髒污度 0~25）
    // ResourceTypeKey 用 "BodyDirtyLevel"，與 ResourceType 列舉一致。
    // ========================================================
    public class SequencerCommandAddBodyDirty : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "BodyDirtyLevel";
        protected override string CommandName => "AddBodyDirty";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.BodyDirtyLevel;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.BodyDirtyLevel;
            p.AddBodyDirtyLevel(parameter);
            return p.BodyDirtyLevel - prev;
        }
    }

    public class SequencerCommandReduceBodyDirty : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "BodyDirtyLevel";
        protected override string CommandName => "ReduceBodyDirty";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.BodyDirtyLevel;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.BodyDirtyLevel;
            p.ReduceBodyDirtyLevel(amount);
            return p.BodyDirtyLevel - prev;
        }
    }

    public class SequencerCommandSetBodyDirty : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "BodyDirtyLevel";
        protected override string CommandName => "SetBodyDirty";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.BodyDirtyLevel;

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.BodyDirtyLevel;
            p.SetBodyDirtyLevel(parameter);
            return p.BodyDirtyLevel - prev;
        }
    }

    // ========================================================
    // ShootTimes（射精次數）
    // ========================================================

    /// <summary>
    /// 用法: AddShootTimes(amount) / AddShootTimes(amount, alert)
    /// 無上限，可以一直加。
    /// </summary>
    public class SequencerCommandAddShootTimes : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "ShootTimes";
        protected override string CommandName => "AddShootTimes";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.CheckShootTimes();

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int prev = p.CheckShootTimes();
            p.AddShootTimes(parameter);
            return p.CheckShootTimes() - prev;
        }
    }

    /// <summary>
    /// 用法: ReduceShootTimes(amount) / ReduceShootTimes(amount, alert)
    /// 呼叫 TryReduceShootTimes，若失敗（已達下限）則 delta 為 0。
    /// </summary>
    public class SequencerCommandReduceShootTimes : SequencerCommandProtagonistResourceBase
    {
        protected override string ResourceTypeKey => "ShootTimes";
        protected override string CommandName => "ReduceShootTimes";
        protected override int GetCurrentValue(ProtagonistStatusModel p) => p.CheckShootTimes();

        protected override int ApplyChange(ProtagonistStatusModel p, int parameter)
        {
            int amount = Math.Max(0, parameter);
            int prev = p.CheckShootTimes();
            p.TryReduceShootTimes(amount);
            return p.CheckShootTimes() - prev;
        }
    }

    // ========================================================
    // BadHealthy（狀態不好）
    // ========================================================

    /// <summary>
    /// 用法: BadHealthy(on) / BadHealthy(off)
    /// 也接受 true/false、add/remove。
    /// 開啟期間：增加壓力再 +1、減少壓力少 -1。換日自動解除。
    /// </summary>
    public class SequencerCommandBadHealthy : SequencerCommand
    {
        public void Awake()
        {
            string mode = GetParameter(0, string.Empty).Trim().ToLower();

            var p = ProtagonistStatusSequencerUtil.GetProtagonist("BadHealthy");
            if (p != null)
            {
                switch (mode)
                {
                    case "on":
                    case "true":
                    case "add":
                        p.SetBadHealthy(true);
                        break;
                    case "off":
                    case "false":
                    case "remove":
                        p.SetBadHealthy(false);
                        break;
                    default:
                        Debug.LogWarning($"Dialogue System: BadHealthy 未知參數 '{mode}'，請使用 on / off。");
                        break;
                }
            }

            Stop();
        }
    }
}