#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>只使用暫存 SO 與獨立 Model；不修改資產或遊戲進度。</summary>
public static class HeroineFlagScheduleChecks
{
    [MenuItem("Tools/Heroine Flag Schedule/執行核心自我檢查")]
    public static void Run()
    {
        int failed = 0;
        Check("單次排他切換與通知一致性", CheckSwitch, ref failed);
        Check("Flag 唯一還原來源與舊快照忽略", CheckRestore, ref failed);
        Check("零個 Flag 合法離場、多個 Flag 仍拒絕", CheckInvalidState, ref failed);
        Check("必填 Flag、重複 ID 與跨角色重複 Flag", CheckValidation, ref failed);
        Check("新遊戲預設與批次生命週期", CheckLifecycle, ref failed);
        Check("永久桶、時效桶、預約與重入", CheckAtomicFlags, ref failed);
        Check("指定活動、同組切換與抽選時機", CheckActivities, ref failed);
        Check("活動清單驗證與舊資料相容", CheckActivityValidation, ref failed);
        Check("指定 Flag 的抽選次數與無效呼叫", CheckFlagActivation, ref failed);
        Check("離場原子更新、存讀檔與重新出場", CheckDeactivateAndRestore, ref failed);
        if (failed == 0) Debug.Log("[Flag 行程檢查] 10 組全部通過，未修改資產或遊戲進度。");
        else Debug.LogError($"[Flag 行程檢查] {failed} 組失敗，請查看前面的錯誤。");
    }

    private static void Check(string name, Action test, ref int failed)
    {
        try { test(); }
        catch (Exception ex) { failed++; Debug.LogError($"[Flag 行程檢查] {name}：{ex}"); }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void CheckSwitch()
    {
        using (var f = new Fixture())
        {
            f.Start();
            f.Model.InitializeNewGame();
            f.Scenario.AddHeroineToLocation("旁人房", "other", "休息");
            var risk = new RiskAction();
            f.Scenario.AddRiskToLocation("家人房", risk);
            f.Flags.AddPersistentFlag("無關劇情");
            int notifications = 0;
            f.Scenario.OnScenarioRecalculated += () => notifications++;
            f.Flags.OnFlagChanged += (id, value) =>
            {
                if (id != f.FlagA.FlagID && id != f.FlagB.FlagID) return;
                Expect(!f.Flags.Contains(f.FlagA.FlagID) && f.Flags.Contains(f.FlagB.FlagID), "通知時整組 Flag 必須已完成切換。");
                Expect(f.At("B"), "Flag 訂閱者必須同時看到新的角色位置。");
            };
            Expect(f.Model.TryActivateActivity("sister", "活動B"), "應可用活動 ID 指定行程並開啟對應 Flag。");
            Expect(f.Model.GetActiveRuleID("sister") == "B" && notifications == 1, "切換應只刷新一次。");
            Expect(f.Model.TryActivateActivity("sister", "活動B") && notifications == 1, "重複指定同一活動應冪等。");
            Expect(f.Flags.Contains("無關劇情") && f.Scenario.GetState("旁人房").Heroines.Count == 1
                && ReferenceEquals(f.Scenario.GetState("家人房").Risks[0], risk), "不得更動旁人、家人或無關 Flag。");
            Expect(!f.Model.TryActivate("sister", "不存在") && f.At("B") && f.Flags.Contains(f.FlagB.FlagID), "無效目標不能造成部分切換。");
            f.Scenario.ReplaceHeroineState("sister", "演出", "臨時");
            f.Flags.AddPersistentFlag("另一個無關Flag");
            f.Model.FlushPending();
            Expect(f.At("演出"), "無關 Flag 不應中斷臨時演出。");
            f.Model.Refresh();
            Expect(f.At("B"), "明確刷新必須依 Flag 回到正式行程。");
        }
    }

    private static void CheckRestore()
    {
        using (var f = new Fixture())
        {
            f.Config.rules[1].activityIDs = new List<string> { "B_指定", "B_還原" };
            f.Start(count => count - 1);
            f.Model.InitializeNewGame();
            Expect(f.Model.TryActivateActivity("sister", "B_指定") && f.ActivityAt("B") == "B_指定", "指定活動不可抽選。");
            var flags = JsonConvert.DeserializeObject<ProgressFlagSaveData>(JsonConvert.SerializeObject(f.Flags.ToSaveData()));
            string scenarioJson = JsonConvert.SerializeObject(f.Scenario.ToSaveData());
            Expect(!scenarioJson.Contains("FlagScheduleSelections"), "新存檔不應保存另一份抽選 ID。");
            var scene = JsonConvert.DeserializeObject<CurrentScenarioSaveData>(scenarioJson);
            // 模擬第一版存檔殘留衝突的選擇，以及臨時演出的位置快照。
            scene.FlagScheduleHeroineIDs = new List<string> { "sister" };
            scene.FlagScheduleSelections = new List<HeroineFlagScheduleSelection>
                { new HeroineFlagScheduleSelection { RuleID = "A", CandidateSignature = "舊資料" } };
            scene.AllLocationStates.Clear();
            scene.AllLocationStates["舊位置"] = new LocationState
                { Heroines = new List<HeroineStateData> { new HeroineStateData { HeroineID = "sister", Activity = "演出" } } };
            int notifications = 0;
            f.Scenario.OnScenarioRecalculated += () => notifications++;
            f.Model.BeginRestore();
            f.Flags.LoadFromSaveData(flags);
            f.Scenario.LoadFromSaveData(scene);
            f.Model.FlushPending();
            Expect(notifications == 0 && f.At("舊位置"), "還原期間不能通知半完成狀態。");
            f.Model.CompleteRestore();
            Expect(f.At("B") && f.Model.GetActiveRuleID("sister") == "B", "讀檔只能依 Flag，不依行程 ID 或舊地點。");
            Expect(f.ActivityAt("B") == "B_還原" && f.Flags.Contains(f.FlagB.FlagID), "讀檔可以抽到同一行程內的另一個活動。");
        }
    }

    private static void CheckInvalidState()
    {
        using (var f = new Fixture())
        {
            f.Start();
            f.Model.InitializeNewGame();
            f.Model.BeginRestore();
            f.Flags.LoadFromSaveData(null);
            f.Model.CompleteRestore();
            Expect(f.Model.GetActiveRuleID("sister") == null && !f.At("A"), "缺少 Flag 不能自動使用新遊戲預設行程。");
            Expect(f.Warnings.Count == 0 && f.Model.Describe("sister").Contains("離場"), "零個 Flag 應顯示正常離場，不可警告或存取空行程。");
            f.Model.PrepareForSave();
            f.Flags.AddPersistentFlag(f.FlagA.FlagID);
            f.Flags.AddPersistentFlag(f.FlagB.FlagID);
            f.Model.FlushPending();
            Expect(f.Model.GetActiveRuleID("sister") == null && !f.At("A") && !f.At("B"), "多個 Flag 同時 ON 不能猜測優先順序。");
            Expect(f.Warnings.Count > 0, "多個 Flag 的異常仍須提示。");
            Expect(f.Flags.Contains(f.FlagA.FlagID) && f.Flags.Contains(f.FlagB.FlagID), "異常讀取不可偷偷更改 Flag。");
            bool rejected = false;
            try { f.Model.PrepareForSave(); } catch (InvalidOperationException) { rejected = true; }
            Expect(rejected, "異常行程不可保存為正常進度。");
            Expect(f.Model.TryActivate("sister", "A") && !f.Flags.Contains(f.FlagB.FlagID), "明確切換可修復多 ON 狀態。");
        }
    }

    private static void CheckValidation()
    {
        using (var f = new Fixture())
        {
            f.Config.rules[1].stateFlag = null;
            f.Start();
            Expect(!f.Model.TryActivate("sister", "A") && !f.Flags.Contains(f.FlagA.FlagID), "任何行程缺少必填 Flag 時整組不得部分執行。");
            Expect(!f.Model.TryDeactivate("sister"), "設定錯誤不可誤判為正常離場。");
            bool rejected = false;
            try { f.Model.PrepareForSave(); } catch (InvalidOperationException) { rejected = true; }
            Expect(rejected, "允許離場後，錯誤設定仍須阻止存檔。");
        }
        using (var f = new Fixture())
        {
            f.Config.rules[1].ruleID = "A";
            f.Start();
            Expect(!f.Model.TryActivate("sister", "A"), "重複行程 ID 必須拒絕。");
        }
        using (var f = new Fixture())
        {
            var other = ScriptableObject.CreateInstance<HeroineFlagScheduleConfig>();
            try
            {
                other.heroineID = "mom";
                other.rules.Add(new HeroineFlagScheduleRule { ruleID = "M", stateFlag = f.FlagA, locationID = "M", activityIDs = new List<string> { "活動M" } });
                using (var model = new HeroineFlagScheduleModel(f.Flags, f.Scenario, new[] { "sister", "mom" }, new[] { f.Config, other }))
                {
                    Expect(!model.TryActivate("sister", "B") && !model.TryActivate("mom", "M"), "跨角色共用 Flag 必須封鎖兩邊。");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(other); }
        }
        using (var f = new Fixture())
        {
            f.Config.rules[1].locationID = "";
            f.Start();
            Expect(!f.Model.TryActivateActivity("sister", "活動B") && !f.Flags.Contains(f.FlagB.FlagID), "每個行程必須指定地點。");
        }
    }

    private static void CheckLifecycle()
    {
        using (var f = new Fixture())
        {
            f.Start();
            f.Flags.AddPersistentFlag(f.FlagB.FlagID);
            f.Model.InitializeNewGame();
            Expect(f.Model.GetActiveRuleID("sister") == "A" && !f.Flags.Contains(f.FlagB.FlagID), "新遊戲須排他啟用明確預設行程。");
            f.Model.BeginBatch();
            f.Model.BeginBatch();
            f.Flags.SetExclusivePersistentFlag(new[] { f.FlagA.FlagID, f.FlagB.FlagID }, f.FlagB.FlagID);
            f.Model.EndBatch();
            Expect(f.At("A"), "巢狀批次未結束不可更新畫面。");
            f.Model.EndBatch();
            Expect(f.At("B"), "批次結束後依最終 Flag 更新。");
            f.Model.BeginBatch();
            f.Model.BeginRestore();
            f.Flags.NewGame();
            f.Scenario.NewGame();
            f.Model.InitializeNewGame();
            f.Model.CompleteRestore();
            Expect(!f.Model.IsSuspended && f.At("A"), "新遊戲應清除上一局批次並套用預設。");
        }
    }

    private static void CheckAtomicFlags()
    {
        using (var f = new Fixture())
        {
            f.Flags.AddSceneFlag(f.FlagA.FlagID);
            f.Flags.AddNextDayOnlyFlag(f.FlagA.FlagID);
            f.Flags.SetValue(f.FlagB.FlagID, 3);
            f.Start();
            bool reentryRejected = false;
            f.Flags.OnFlagChanged += (id, value) =>
            {
                Expect(!f.Flags.Contains(f.FlagA.FlagID) && f.Flags.Contains(f.FlagB.FlagID), "不能通知互斥寫入中間態。");
                reentryRejected = !f.Model.TryActivate("sister", "A");
            };
            Expect(f.Model.TryActivate("sister", "B") && reentryRejected, "Flag 通知中的重入切換必須被拒絕。");
            Expect(f.Flags.IsPersistentFlag(f.FlagB.FlagID) && f.Flags.GetValue(f.FlagB.FlagID) == 0, "目標須正規化為永久布林。");
            f.Flags.ClearSceneFlags();
            f.Flags.ClearSlotFlags();
            f.Flags.HandleDayPassed();
            Expect(!f.Flags.Contains(f.FlagA.FlagID) && f.Flags.Contains(f.FlagB.FlagID), "跨日及清理不得重新開啟舊行程。");
        }
    }

    private static void CheckActivities()
    {
        using (var f = new Fixture())
        {
            f.Config.rules[1].activityIDs = new List<string> { "B_1", "B_2", "B_3" };
            int draws = 0;
            f.Start(count => { draws++; return count - 1; });
            f.Model.InitializeNewGame();
            f.Flags.OnFlagChanged += (id, value) =>
            {
                if (id == f.FlagA.FlagID || id == f.FlagB.FlagID)
                    Expect(f.ActivityAt("B") == "B_1" && !f.Flags.Contains(f.FlagA.FlagID)
                        && f.Flags.Contains(f.FlagB.FlagID), "Flag 通知必須看到指定活動及完整互斥狀態。");
            };
            Expect(f.Model.TryActivateActivity("sister", "B_1") && draws == 0, "指定活動不應消耗隨機抽選。");
            Expect(f.Model.TryActivateActivity("sister", "B_2") && f.ActivityAt("B") == "B_2", "同一行程也能明確切換另一個活動。");
            Expect(!f.Model.TryActivateActivity("sister", "不存在") && f.ActivityAt("B") == "B_2", "無效活動不能更動現有活動。");
            Expect(!f.Model.TryActivateActivity("不存在", "B_2") && f.ActivityAt("B") == "B_2", "未知角色不能更動現有角色。");
            f.Flags.AddPersistentFlag("無關Flag");
            f.Model.FlushPending();
            f.Model.PrepareForSave();
            Expect(draws == 0 && f.ActivityAt("B") == "B_2", "一般刷新佇列、存檔與無關 Flag 不應重抽。");
            f.Model.Refresh();
            Expect(draws == 1 && f.ActivityAt("B") == "B_3" && f.Flags.Contains(f.FlagB.FlagID), "明確依 Flag 刷新應從同一行程抽選。");
            Expect(f.Model.TryActivateActivity("sister", "B_1") && f.ActivityAt("B") == "B_1" && draws == 1, "刷新後仍可固定指定活動。");
        }
    }

    private static void CheckActivityValidation()
    {
        using (var f = new Fixture())
        {
            f.Config.rules[1].activityIDs = new List<string> { "活動A" };
            f.Start();
            Expect(!f.Model.TryActivateActivity("sister", "活動A") && !f.At("A") && !f.Flags.Contains(f.FlagA.FlagID), "跨行程重複活動不可猜測所屬 Flag。");
        }
        foreach (var invalid in new[] { new List<string>(), new List<string> { " " }, new List<string> { "B", "B" } })
        {
            using (var f = new Fixture())
            {
                f.Config.rules[1].activityState = "";
                f.Config.rules[1].activityIDs = invalid;
                f.Start();
                Expect(!f.Model.TryActivateActivity("sister", "活動A"), "空清單、空 ID 或組內重複 ID 都必須阻止部分切換。");
            }
        }
        using (var f = new Fixture())
        {
            f.Config.rules[1].activityIDs = new List<string> { "新活動B" };
            f.Start();
            Expect(f.Model.TryActivateActivity("sister", "活動A"), "舊單一活動必須仍可使用。");
            Expect(!f.Model.TryActivateActivity("sister", "活動B") && f.Model.TryActivateActivity("sister", "新活動B"), "已有清單時不能混入舊活動。");
        }
        using (var f = new Fixture())
        {
            var other = ScriptableObject.CreateInstance<HeroineFlagScheduleConfig>();
            var flag = ScriptableObject.CreateInstance<ProgressFlagDefinition>();
            try
            {
                flag.name = "另一角色行程";
                other.heroineID = "mom";
                other.rules.Add(new HeroineFlagScheduleRule { ruleID = "M", stateFlag = flag, locationID = "M", activityIDs = new List<string> { "活動A" } });
                using (var model = new HeroineFlagScheduleModel(f.Flags, f.Scenario, new[] { "sister", "mom" }, new[] { f.Config, other }))
                {
                    Expect(model.TryActivateActivity("sister", "活動A") && model.TryActivateActivity("mom", "活動A"), "不同角色允許使用同名活動。");
                    Expect(f.Flags.Contains(f.FlagA.FlagID) && f.Flags.Contains(flag.FlagID) && f.At("A"), "另一角色的活動切換不得關閉本角色 Flag。");
                    Expect(model.TryDeactivate("mom") && !f.Flags.Contains(flag.FlagID)
                        && f.Flags.Contains(f.FlagA.FlagID) && f.At("A"), "離場只能關閉指定角色的行程 Flag。");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(flag); }
        }
    }

    private static void CheckFlagActivation()
    {
        using (var f = new Fixture())
        {
            f.Config.rules[1].activityIDs = new List<string> { "B_1", "B_2" };
            int draws = 0;
            f.Start(count => draws++ % count);
            f.Model.InitializeNewGame();
            Expect(f.Model.TryActivateFlag("sister", f.FlagB.FlagID) && f.ActivityAt("B") == "B_1"
                && draws == 1 && !f.Flags.Contains(f.FlagA.FlagID), "指定 Flag 應排他啟用並只抽選一次。");
            Expect(f.Model.TryActivateFlag("sister", f.FlagB.FlagID) && f.ActivityAt("B") == "B_2"
                && draws == 2, "重複呼叫相同 Flag 必須重新抽選一次。");
            Expect(!f.Model.TryActivateFlag("sister", "B") && !f.Model.TryActivateFlag("sister", "不存在")
                && !f.Model.TryActivateFlag("sister", "") && !f.Model.TryActivateFlag("不存在", f.FlagB.FlagID)
                && draws == 2 && f.ActivityAt("B") == "B_2" && f.Flags.Contains(f.FlagB.FlagID),
                "Flag 專用入口不能誤用行程 ID、空字串或其他無效目標，失敗不得改動狀態。");
            f.Model.FlushPending();
            f.Model.PrepareForSave();
            Expect(draws == 2, "一般刷新佇列與存檔不能再次抽選。");
            Expect(f.Model.TryActivateFlag("sister", f.FlagA.FlagID) && f.ActivityAt("A") == "活動A"
                && !f.Flags.Contains(f.FlagB.FlagID), "只有一個活動的 Flag 仍須正常啟用。");
        }
    }

    private static void CheckDeactivateAndRestore()
    {
        using (var f = new Fixture())
        {
            f.Start();
            f.Model.InitializeNewGame();
            f.Scenario.AddHeroineToLocation("旁人房", "other", "休息");
            var risk = new RiskAction();
            f.Scenario.AddRiskToLocation("家人房", risk);
            f.Flags.AddPersistentFlag("無關劇情");
            Expect(!f.Model.TryDeactivate("不存在") && f.At("A"), "未知角色離場不得改動原角色。");
            f.Model.BeginBatch();
            Expect(!f.Model.TryDeactivate("sister") && !f.Model.TryActivateFlag("sister", f.FlagB.FlagID)
                && f.At("A"), "批次更新期間必須拒絕立即切換。");
            f.Model.EndBatch();

            // 混合永久、時效、數值、預約資料；離場須清理完整群組。
            f.Flags.AddSceneFlag(f.FlagA.FlagID);
            f.Flags.AddNextDayAddFlag(f.FlagA.FlagID);
            f.Flags.AddNextDayOnlyFlag(f.FlagB.FlagID);
            f.Flags.SetValue(f.FlagB.FlagID, 3);
            Expect(!f.Flags.ClearExclusiveFlags(new[] { f.FlagA.FlagID, "" }) && f.Flags.Contains(f.FlagA.FlagID),
                "無效群組須先完整驗證，不能部分清除。");
            Expect(!f.Flags.SetExclusivePersistentFlag(new[] { f.FlagA.FlagID }, null) && f.Flags.Contains(f.FlagA.FlagID),
                "原切換 API 的空目標仍須拒絕，不能意外變成離場。");
            int notifications = 0;
            bool reentryRejected = false;
            f.Scenario.OnScenarioRecalculated += () => notifications++;
            Action<string, bool> observe = (id, value) =>
            {
                if (id != f.FlagA.FlagID && id != f.FlagB.FlagID) return;
                Expect(!f.Flags.Contains(f.FlagA.FlagID) && !f.Flags.Contains(f.FlagB.FlagID)
                    && !f.At("A") && !f.At("B"), "Flag 通知時，所有行程 Flag 與角色位置必須已一併清除。");
                reentryRejected = !f.Model.TryActivateFlag("sister", f.FlagA.FlagID) && !f.Model.TryDeactivate("sister");
            };
            f.Flags.OnFlagChanged += observe;
            Expect(f.Model.TryDeactivate("sister") && reentryRejected && notifications == 1, "離場須刷新畫面一次，並阻止通知中重入。");
            f.Flags.OnFlagChanged -= observe;
            Expect(f.Model.TryDeactivate("sister") && notifications == 1, "重複離場不應重複刷新。");
            Expect(f.Flags.Contains("無關劇情") && f.Scenario.GetState("旁人房").Heroines.Count == 1
                && ReferenceEquals(f.Scenario.GetState("家人房").Risks[0], risk), "離場不能更動其他角色、家人與劇情 Flag。");
            f.Flags.ClearSceneFlags();
            f.Flags.ClearSlotFlags();
            f.Flags.HandleDayPassed();
            f.Model.Refresh();
            Expect(!f.Flags.Contains(f.FlagA.FlagID) && !f.Flags.Contains(f.FlagB.FlagID)
                && f.Flags.GetValue(f.FlagB.FlagID) == 0 && !f.At("A") && !f.At("B"), "跨日或刷新不得恢復離場前的行程。");
            f.Model.PrepareForSave();
            var flags = JsonConvert.DeserializeObject<ProgressFlagSaveData>(JsonConvert.SerializeObject(f.Flags.ToSaveData()));
            // 模擬存檔中的舊活動位置快照；Flag 全關才是讀檔依據。
            f.Scenario.ReplaceHeroineState("sister", "舊位置", "舊活動");
            var scene = JsonConvert.DeserializeObject<CurrentScenarioSaveData>(JsonConvert.SerializeObject(f.Scenario.ToSaveData()));
            int beforeRestore = notifications;
            f.Model.BeginRestore();
            f.Flags.LoadFromSaveData(flags);
            f.Scenario.LoadFromSaveData(scene);
            f.Model.FlushPending();
            Expect(notifications == beforeRestore && f.At("舊位置"), "離場讀檔期間不能提前通知畫面。");
            f.Model.CompleteRestore();
            Expect(notifications == beforeRestore + 1 && !f.At("舊位置") && !f.At("A")
                && !f.Flags.Contains(f.FlagA.FlagID) && !f.Flags.Contains(f.FlagB.FlagID)
                && f.Model.Describe("sister").Contains("離場"), "讀檔必須清除舊活動並維持離場，不套用新遊戲預設。");
            f.Model.PrepareForSave();
            Expect(f.Model.TryActivateFlag("sister", f.FlagB.FlagID) && f.At("B"), "離場後可透過 Flag 重新出場。");
            Expect(f.Model.TryDeactivate("sister") && f.Model.TryActivateActivity("sister", "活動A") && f.At("A"),
                "離場後也可指定活動重新出場。");
        }
    }

    private sealed class Fixture : IDisposable
    {
        public readonly ProgressFlagModel Flags = new ProgressFlagModel();
        public readonly CurrentScenarioModel Scenario = new CurrentScenarioModel();
        public readonly HeroineFlagScheduleConfig Config = ScriptableObject.CreateInstance<HeroineFlagScheduleConfig>();
        public readonly ProgressFlagDefinition FlagA = ScriptableObject.CreateInstance<ProgressFlagDefinition>();
        public readonly ProgressFlagDefinition FlagB = ScriptableObject.CreateInstance<ProgressFlagDefinition>();
        public readonly List<string> Warnings = new List<string>();
        public HeroineFlagScheduleModel Model;

        public Fixture()
        {
            Config.heroineID = "sister";
            Config.defaultRuleID = "A";
            FlagA.name = "行程A"; FlagB.name = "行程B";
            Config.rules.Add(new HeroineFlagScheduleRule { ruleID = "A", ruleName = "A", stateFlag = FlagA, locationID = "A", activityState = "活動A" });
            Config.rules.Add(new HeroineFlagScheduleRule { ruleID = "B", ruleName = "B", stateFlag = FlagB, locationID = "B", activityState = "活動B" });
        }
        public void Start(Func<int, int> pickIndex = null) => Model = new HeroineFlagScheduleModel(Flags, Scenario, new[] { "sister" }, new[] { Config }, Warnings.Add, pickIndex);
        public bool At(string room) => Scenario.GetState(room)?.Heroines.Exists(h => h.HeroineID == "sister") == true;
        public string ActivityAt(string room) => Scenario.GetState(room)?.Heroines.Find(h => h.HeroineID == "sister")?.Activity;
        public void Dispose()
        {
            Model?.Dispose();
            UnityEngine.Object.DestroyImmediate(Config);
            UnityEngine.Object.DestroyImmediate(FlagA);
            UnityEngine.Object.DestroyImmediate(FlagB);
        }
    }
}
#endif
