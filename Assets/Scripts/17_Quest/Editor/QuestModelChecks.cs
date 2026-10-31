#if UNITY_EDITOR
using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>開發驗證工具；僅建立暫存 SO 與獨立 Model，不修改遊戲進度或任何資產。</summary>
public static class QuestModelChecks
{
    [MenuItem("Tools/Quest/開發工具/執行核心自我檢查")]
    public static void Run()
    {
        int failed = 0;
        Check("獨立啟停、每日限制、完成與已發獎紀錄", CheckProgress, ref failed);
        Check("存檔往返、切槽、新遊戲與查詢隔離", CheckSave, ref failed);
        Check("第一版進度轉換與第二版存檔格式", CheckMigration, ref failed);
        Check("無效資料保護與事件重入", CheckInvalidData, ref failed);
        Check("SO 名稱識別與資產引用操作", CheckAssetReferences, ref failed);
        Check("缺少定義的舊任務保留、隔離與存檔往返", CheckMissingDefinitions, ref failed);
        Check("扣回次數、每日限制與已發獎紀錄保存", CheckDecrease, ref failed);
        Check("固定獎勵順序、實際數值、重入與讀檔", CheckRewards, ref failed);
        Check("指定領獎、停用任務、重複領取與失敗隔離", CheckSpecifiedReward, ref failed);
        Check("無效獎勵不部分發放也不跳過", CheckInvalidRewards, ref failed);
        Check("Flag 獎勵存活週期與關閉行為", CheckFlagRewardLifetimes, ref failed);
        Check("已啟動未完成查詢與無效資料", CheckActiveIncomplete, ref failed);
        Check("追蹤名單排序、同日保留、撤下不補位與刷新", CheckTrackerSelection, ref failed);
        Check("追蹤項目貼紙、每日勾選與完成顯示資料", CheckTrackerPresentation, ref failed);
        if (failed == 0) Debug.Log("[Quest 自我檢查] 14 組全部通過；未修改遊戲中的 Model 或任何資產。");
        else Debug.LogError($"[Quest 自我檢查] {failed} 組失敗，請查看前面的錯誤。");
    }

    private static void Check(string name, Action test, ref int failed)
    {
        try { test(); }
        catch (Exception exception)
        {
            failed++;
            Debug.LogError($"[Quest 自我檢查] {name}：{exception}");
        }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void CheckProgress()
    {
        using (var f = new Fixture())
        {
            var model = f.Model;
            Expect(model.TryRecordAction("A") == QuestResult.NotActive, "未啟動不可計次。");
            Expect(model.TryMarkRewarded("A") == QuestResult.NotCompleted, "未完成不可記為已發獎。");
            Expect(model.GetProgress("B").IsActive, "另一筆任務應套用初始啟動設定。");
            model.SetActive("A", true);
            Expect(model.TryRecordAction("A", out bool completed) == QuestResult.Success && !completed, "第一回不應完成。");
            model.SetActive("A", false);
            model.SetActive("A", true);
            Expect(model.TryRecordAction("A") == QuestResult.DailyLimit, "重新啟用不能繞過每日限制。");
            Expect(model.TryRecordAction("B") == QuestResult.Success, "其他任務應能獨立計次。");
            Expect(model.TryRecordAction("B") == QuestResult.Success, "無每日限制的任務可同日多次計次。");
            f.Day++;
            Expect(model.TryRecordAction("A", out completed) == QuestResult.Success && completed, "跨日達標應完成。");
            Expect(model.GetProgress("A").IsActive && !model.GetProgress("A").IsRewarded, "完成不自動停用或發獎。");
            Expect(model.TryRecordAction("A") == QuestResult.AlreadyCompleted, "完成後不可加次。");
            model.SetActive("A", false);
            Expect(model.TryMarkRewarded("A") == QuestResult.Success, "完成後即使停用仍可記錄已發獎。");
            Expect(model.TryMarkRewarded("A") == QuestResult.AlreadyRewarded, "不可重複標記發獎。");
        }
    }

    private static void CheckSave()
    {
        using (var f = new Fixture())
        {
            f.Model.SetActive("A", true);
            f.Model.TryRecordAction("A");
            string json = JsonConvert.SerializeObject(f.Model.ToSaveData());
            int changes = 0;
            f.Model.OnChanged += () => changes++;
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(json));
            Expect(changes == 0, "讀檔不得逐筆通知。");
            Expect(f.Model.GetProgress("A").CurrentCount == 1 && f.Model.CanRecordAction("A") == QuestResult.DailyLimit, "讀檔需保留進度與當日限制。");
            f.Model.GetProgress("A").CurrentCount = 99;
            f.Model.ToSaveData().QuestDataList[0].IsRewarded = true;
            Expect(f.Model.GetProgress("A").CurrentCount == 1 && !f.Model.GetProgress("A").IsRewarded, "查詢及存檔必須回傳複本。");
            f.Day++;
            f.Model.TryRecordAction("A");
            f.Model.TryMarkRewarded("A");
            var saved = f.Model.ToSaveData();
            f.Model.NewGame();
            Expect(!f.Model.GetProgress("A").IsActive && !f.Model.GetProgress("A").IsRewarded, "新遊戲需清除舊狀態。");
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(JsonConvert.SerializeObject(saved)));
            Expect(f.Model.GetProgress("A").IsCompleted && f.Model.GetProgress("A").IsRewarded, "完成及發獎紀錄必須往返保存。");
            f.Model.LoadFromSaveData(null);
            Expect(f.Model.GetProgress("A").CurrentCount == 0 && f.Model.GetProgress("B").IsActive, "無 Quest 資料的存檔需套用初始設定。");
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(json));
            Expect(!f.Model.GetProgress("A").IsRewarded, "切換到較早存檔不能殘留已發獎狀態。");
        }
    }

    private static void CheckMigration()
    {
        using (var f = new Fixture())
        {
            const string oldJson = "{'Version':1,'ActiveQuestIDs':['A'],'QuestIDs':['A'],'QuestDataList':[{'AssignedCycle':1,'CurrentCount':1,'IsCompleted':false,'LastPerformedDay':1,'UncreditedStickers':4}],'ClaimedRewardIDs':['OldReward']}";
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(oldJson));
            Expect(f.Model.LoadError == null && f.Model.GetProgress("A").IsActive, "第一版啟用清單需轉換。");
            Expect(f.Model.GetProgress("A").CurrentCount == 1 && f.Model.CanRecordAction("A") == QuestResult.DailyLimit, "第一版計次與每日紀錄需保留。");
            Expect(!f.Model.GetProgress("A").IsRewarded, "不能把舊門檻領獎推測成每筆已發獎。");
            var json = JsonConvert.SerializeObject(f.Model.ToSaveData());
            Expect(!json.Contains("ActiveQuestIDs") && !json.Contains("UncreditedStickers") && !json.Contains("ClaimedRewardIDs"), "新存檔不可再寫舊批次、貼紙、門檻領獎欄位。");
            Expect(f.Model.ToSaveData().Version == 2, "轉換後應使用第二版。");
        }
    }

    private static void CheckInvalidData()
    {
        using (var f = new Fixture())
        {
            var broken = f.Model.ToSaveData();
            broken.QuestDataList.RemoveAt(0);
            f.Model.LoadFromSaveData(broken);
            Expect(f.Model.LoadError != null && f.Model.SetActive("A", true) == QuestResult.InvalidSaveData, "不一致資料需阻擋操作。");
            Expect(f.Model.GetProgress("A") == null && f.Model.ToSaveData().QuestDataList.Count == 1, "不可顯示假進度或覆寫收到的資料。");
            f.Model.NewGame();
            var invalidReward = f.Model.ToSaveData();
            invalidReward.QuestDataList[0].IsRewarded = true;
            f.Model.LoadFromSaveData(invalidReward);
            Expect(f.Model.LoadError != null, "已發獎卻從未成功計次的資料應拒絕。");
            f.Model.NewGame();
            var nested = QuestResult.Success;
            f.Model.OnChanged += () => nested = f.Model.TryRecordAction("B");
            f.Model.TryRecordAction("B");
            Expect(nested == QuestResult.Busy && f.Model.GetProgress("B").CurrentCount == 1, "事件重入不得重複計次。");
            f.B.name = f.A.name;
            var duplicate = new QuestModel(f.Database, () => f.Day);
            Expect(duplicate.ConfigurationError != null && duplicate.SetActive("A", true) == QuestResult.InvalidConfiguration, "重複 ID 應阻擋。");
        }
        var empty = new QuestModel(null, () => 1);
        Expect(empty.SetActive("A", true) == QuestResult.NotConfigured, "未配置資料庫應明確回報。");
    }

    private static void CheckAssetReferences()
    {
        using (var f = new Fixture())
        {
            Expect(f.Model.SetActive(f.A, true) == QuestResult.Success, "SO 引用應能啟動任務。");
            Expect(f.Model.CanRecordAction(f.A) == QuestResult.Success, "SO 引用應能查詢計次條件。");
            Expect(f.Model.TryRecordAction(f.A, out bool completed) == QuestResult.Success && !completed, "SO 引用應能計次。");
            Expect(f.Model.GetProgress("A").CurrentCount == 1, "SO 與 ID 操作必須共用同一筆進度。");
            f.Day++;
            Expect(f.Model.TryRecordAction(f.A) == QuestResult.Success, "SO 引用應保留每日限制後的正常計次。");
            Expect(f.Model.CanMarkRewarded(f.A) == QuestResult.Success && f.Model.TryMarkRewarded(f.A) == QuestResult.Success,
                "SO 引用應能記錄已發獎。");
            var saved = f.Model.ToSaveData();
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(JsonConvert.SerializeObject(saved)));
            Expect(f.Model.GetProgress(f.A).IsRewarded && saved.QuestIDs.Contains(f.A.name), "存讀檔應以 SO 名稱對應狀態。");
            QuestDefinition missing = null;
            Expect(f.Model.TryRecordAction(missing) == QuestResult.UnknownQuest && f.Model.GetProgress(missing) == null,
                "未指定 SO 不應擲出例外或操作其他任務。");
            f.A.name = "A_Renamed";
            var renamed = new QuestModel(f.Database, () => f.Day);
            Expect(renamed.GetDefinition("A") == null && renamed.GetDefinition("A_Renamed") != null,
                "新建 Model 應使用改名後的 ID。");
            renamed.LoadFromSaveData(saved);
            Expect(renamed.LoadError == null && renamed.ToSaveData().QuestIDs.Contains("A") && renamed.GetProgress("A") == null,
                "舊名稱無法對應時應保留存檔但略過使用，不可阻擋其他任務。");
            var initial = renamed.GetProgress("A_Renamed");
            Expect(!initial.IsActive && initial.CurrentCount == 0 && !initial.IsCompleted && !initial.IsRewarded
                && initial.LastPerformedDay == -1, "改名後的新 ID 不可繼承舊進度，應套用初始設定。");
            Expect(renamed.SetActive("A_Renamed", true) == QuestResult.Success
                && renamed.TryRecordAction("A_Renamed") == QuestResult.Success, "改名後的新任務應可啟動與計次。");
        }
    }

    private static void CheckMissingDefinitions()
    {
        using (var f = new Fixture())
        {
            f.Model.SetActive("A", true);
            f.Model.TryRecordAction("A");
            var saved = f.Model.ToSaveData();
            saved.QuestIDs.Add("Removed");
            saved.QuestDataList.Add(new QuestProgressSaveData
            {
                IsActive = true, CurrentCount = 8, IsCompleted = true, IsRewarded = false, LastPerformedDay = 1
            });
            string originalRecord = JsonConvert.SerializeObject(saved.QuestDataList[2]);
            int changes = 0;
            f.Model.OnChanged += () => changes++;
            f.Model.LoadFromSaveData(saved);
            Expect(f.Model.LoadError == null && changes == 0, "缺少定義不可阻擋讀檔，也不可逐筆通知。");
            Expect(f.Model.GetProgress("A").CurrentCount == 1 && f.Model.CanRecordAction("A") == QuestResult.DailyLimit,
                "有效任務的進度及每日限制應保留。");
            Expect(f.Model.GetProgress("Removed") == null && f.Model.SetActive("Removed", true) == QuestResult.UnknownQuest,
                "缺少定義的任務不可透過查詢或啟停繼續使用。");
            Expect(string.Join(",", f.Model.GetActiveQuestIDs()) == "A,B" && !f.Model.HasUnclaimedRewards,
                "舊任務不可混入啟動清單或待領獎勵。");
            var tracker = new QuestTrackerModel(f.Model);
            tracker.RefreshTasks(f.Day, 3);
            Expect(TrackerIDs(tracker) == "A,B", "追蹤 UI 應略過舊任務。");
            Expect(f.Model.TryRecordAction("B") == QuestResult.Success, "其他任務應可繼續計次。");
            var roundTrip = JsonConvert.DeserializeObject<QuestSaveData>(JsonConvert.SerializeObject(f.Model.ToSaveData()));
            Expect(JsonConvert.SerializeObject(roundTrip.QuestDataList[roundTrip.QuestIDs.IndexOf("Removed")]) == originalRecord,
                "再次存檔必須完整保留舊任務紀錄。");
            f.Model.LoadFromSaveData(roundTrip);
            Expect(f.Model.LoadError == null && f.Model.GetProgress("B").CurrentCount == 1,
                "包含舊任務的存檔再次載入後，有效進度仍應可用。");

            var broken = roundTrip.Copy();
            broken.QuestIDs.Add("Removed");
            broken.QuestDataList.Add(broken.QuestDataList[2].Copy());
            f.Model.LoadFromSaveData(broken);
            Expect(f.Model.LoadError != null, "缺少定義也不可放行重複 ID。");
            broken = roundTrip.Copy();
            broken.QuestIDs[2] = null;
            f.Model.LoadFromSaveData(broken);
            Expect(f.Model.LoadError != null, "缺少定義也不可放行空 ID。");
            broken = roundTrip.Copy();
            broken.QuestDataList[2] = null;
            f.Model.LoadFromSaveData(broken);
            Expect(f.Model.LoadError != null, "缺少定義也不可放行空紀錄。");

            const string legacy = "{'Version':1,'ActiveQuestIDs':['Removed'],'QuestIDs':['Removed'],'QuestDataList':[{'CurrentCount':8,'IsCompleted':true,'LastPerformedDay':1}]}";
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(legacy));
            var migrated = f.Model.ToSaveData();
            Expect(f.Model.LoadError == null && migrated.Version == 2 && migrated.QuestDataList[0].IsActive
                && !migrated.QuestDataList[0].IsRewarded && f.Model.GetProgress("Removed") == null,
                "第一版缺少定義的任務仍須轉換啟動紀錄並隔離使用。");
            Expect(f.Model.GetProgress("B").IsActive && f.Model.SetActive("A", true) == QuestResult.Success,
                "舊存檔未包含的任務應套用初始狀態且可啟動。");
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(JsonConvert.SerializeObject(migrated)));
            Expect(f.Model.LoadError == null, "轉換後含舊任務的第二版存檔應可再次載入。");
        }
    }

    private static void CheckDecrease()
    {
        using (var f = new Fixture())
        {
            int changes = 0;
            f.Model.OnChanged += () => changes++;
            Expect(f.Model.TryDecreaseSuccessCount(f.A) == QuestResult.Success
                && f.Model.GetProgress(f.A).CurrentCount == 0 && changes == 0, "零次不能扣為負數或廣播變更。");
            Expect(f.Model.TryDecreaseSuccessCount("Missing") == QuestResult.UnknownQuest, "未知 ID 不可修改資料。");
            f.Model.SetActive(f.A, true);
            f.Model.TryRecordAction(f.A);
            f.Model.TryDecreaseSuccessCount(f.A);
            Expect(f.Model.GetProgress(f.A).CurrentCount == 0 && f.Model.CanRecordAction(f.A) == QuestResult.DailyLimit,
                "扣回次數不能解除當日限制。");
            f.Day++;
            f.Model.TryRecordAction(f.A);
            f.Day++;
            f.Model.TryRecordAction(f.A);
            f.Model.TryMarkRewarded(f.A);
            f.Model.SetActive(f.A, false);
            int day = f.Model.GetProgress(f.A).LastPerformedDay;
            int before = changes;
            f.Model.TryDecreaseSuccessCount("A");
            var record = f.Model.GetProgress(f.A);
            Expect(record.CurrentCount == 1 && !record.IsCompleted && !record.IsActive && record.IsRewarded
                && record.LastPerformedDay == day && changes == before + 1,
                "扣回已完成任務需更新完成狀態、通知一次，並保留啟動、日期及已發獎紀錄。");
            f.Model.TryDecreaseSuccessCount(f.A);
            var saved = JsonConvert.SerializeObject(f.Model.ToSaveData());
            f.Model.NewGame();
            f.Model.LoadFromSaveData(JsonConvert.DeserializeObject<QuestSaveData>(saved));
            Expect(f.Model.LoadError == null && f.Model.GetProgress(f.A).CurrentCount == 0
                && f.Model.GetProgress(f.A).IsRewarded && !f.Model.GetProgress(f.A).IsCompleted,
                "已發獎後扣到零的進度必須能存讀檔。");
            f.Model.SetActive(f.A, true);
            f.Day++;
            f.Model.TryRecordAction(f.A);
            f.Day++;
            f.Model.TryRecordAction(f.A);
            Expect(f.Model.TryMarkRewarded(f.A) == QuestResult.AlreadyRewarded, "再次達標不可重新標記已發獎。");

            QuestResult nested = QuestResult.Success;
            f.Model.OnChanged += () => nested = f.Model.TryDecreaseSuccessCount(f.A);
            f.Model.TryDecreaseSuccessCount(f.A);
            Expect(nested == QuestResult.Busy && f.Model.GetProgress(f.A).CurrentCount == 1, "事件重入不可連續扣回。");
        }
    }

    private static void CheckRewards()
    {
        var flag = ScriptableObject.CreateInstance<ProgressFlagDefinition>();
        flag.name = "QuestRewardFlag";
        try
        {
            using (var f = new Fixture((a, b) =>
            {
                a.name = "Z"; // 故意與字母排序及完成順序相反。
                b.name = "A";
                a.RequiredCount = b.RequiredCount = 1;
                a.OncePerDay = false;
                a.InitiallyActive = true;
                a.Rewards.StickerDelta = 5;
                a.Rewards.Heroines.Add(new QuestHeroineReward { HeroineID = "H", Mood = -2, Trust = 7, Libido = 10 });
                a.Rewards.Flags.Add(new QuestFlagReward { Flag = flag, Enabled = true });
                b.Rewards.StickerDelta = -3;
                b.Rewards.Flags.Add(new QuestFlagReward { Flag = flag, Enabled = false });
            }))
            {
                var protagonist = new ProtagonistStatusModel();
                var heroine = new HeroineStatusModel(new HeroineStat { ID = "H" }, null);
                var flags = new ProgressFlagModel();
                var service = new QuestRewardService(f.Model, protagonist, id => id == "H" ? heroine : null, flags);
                Expect(!service.HasUnclaimedRewards, "未完成不應有待領獎勵。");
                f.Model.TryRecordAction(f.B);
                f.Model.TryRecordAction(f.A);
                f.A.Rewards.StickerDelta = 999; // 執行期快照不能被 SO 修改影響。
                QuestResult nested = QuestResult.Success;
                protagonist.OnChanged += () => nested = service.ClaimNextReward();
                Expect(service.TryClaimNextReward(out string claimed) == QuestResult.Success && claimed == "Z",
                    "應領資料庫第一筆，不依字母或完成時間排序。");
                Expect(nested == QuestResult.Busy && service.HasUnclaimedRewards, "數值事件重入不可領取下一筆。");
                Expect(protagonist.StickerCount == 5,
                    "獎勵需增加持有貼紙，並使用建立時設定。");
                Expect(heroine.Mood == 3 && heroine.Trust == 7 && heroine.Libido == 10 && heroine.LibidoIncreasedToday
                    && heroine.Excitement == 0 && flags.Contains(flag.FlagID), "應套用三項正式數值與 Flag，不改興奮。");
                Expect(f.Model.GetProgress(f.A).IsRewarded && !f.Model.GetProgress(f.A).IsActive,
                    "預設領取後標記並停用。");
                var saved = JsonConvert.DeserializeObject<QuestSaveData>(JsonConvert.SerializeObject(f.Model.ToSaveData()));
                f.Model.NewGame();
                f.Model.LoadFromSaveData(saved);
                Expect(service.TryClaimNextReward(out claimed, false) == QuestResult.Success && claimed == "A",
                    "讀檔後應跳過已領取，領下一筆。");
                Expect(protagonist.StickerCount == 2 && !flags.Contains(flag.FlagID) && f.Model.GetProgress(f.B).IsActive,
                    "負數貼紙與關閉 Flag 應生效，關閉自動停用則保留啟動。");
                Expect(!service.HasUnclaimedRewards && service.ClaimNextReward() == QuestResult.NoUnclaimedReward,
                    "領完後應為 false，多呼叫不重複發獎。");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(flag); }
    }

    private static void CheckSpecifiedReward()
    {
        using (var f = new Fixture((a, b) =>
        {
            a.RequiredCount = b.RequiredCount = 1;
            a.InitiallyActive = true;
            a.Rewards.StickerDelta = 5;
            b.Rewards.StickerDelta = 8;
        }))
        {
            var protagonist = new ProtagonistStatusModel();
            var service = new QuestRewardService(f.Model, protagonist, null, new ProgressFlagModel());
            f.Model.TryRecordAction("A");
            Expect(service.ClaimReward("B") == QuestResult.NotCompleted, "指定未完成任務不可改領已完成的 A。");
            foreach (string id in new[] { null, "", " ", "Missing" })
                Expect(service.ClaimReward(id) == QuestResult.UnknownQuest, "空白或未知 ID 不可改領其他任務。");
            Expect(protagonist.StickerCount == 0 && !f.Model.GetProgress("A").IsRewarded,
                "指定領獎失敗不能影響其他待領任務。");

            f.Model.TryRecordAction("B");
            f.Model.SetActive("B", false);
            QuestResult nested = QuestResult.Success;
            protagonist.OnChanged += () => nested = service.ClaimReward("A");
            Expect(service.ClaimReward("B") == QuestResult.Success && protagonist.StickerCount == 8,
                "可指定清單後方已停用、已完成的 B，不受 A 排序影響。");
            Expect(f.Model.GetProgress("B").IsRewarded && !f.Model.GetProgress("B").IsActive
                && !f.Model.GetProgress("A").IsRewarded && nested == QuestResult.Busy,
                "成功需標記已領取並停用，事件重入不可領取 A。");
            Expect(service.ClaimReward("B") == QuestResult.AlreadyRewarded && protagonist.StickerCount == 8,
                "重複指定不得再次發獎或改領 A。");
            Expect(service.TryClaimNextReward(out string claimed) == QuestResult.Success && claimed == "A"
                && protagonist.StickerCount == 13 && !f.Model.GetProgress("A").IsActive,
                "指定領獎後，依序領獎仍應領取剩餘第一筆並停用。");
        }
        using (var f = new Fixture((a, b) =>
        {
            a.RequiredCount = b.RequiredCount = 1;
            a.InitiallyActive = true;
            a.Rewards.StickerDelta = 5;
            b.Rewards.StickerDelta = 8;
            b.Rewards.Heroines.Add(new QuestHeroineReward { HeroineID = "Missing", Trust = 3 });
        }))
        {
            var protagonist = new ProtagonistStatusModel();
            var service = new QuestRewardService(f.Model, protagonist, null, new ProgressFlagModel());
            f.Model.TryRecordAction("A");
            f.Model.TryRecordAction("B");
            Expect(service.ClaimReward("B") == QuestResult.InvalidReward && service.LastError != null
                && protagonist.StickerCount == 0 && !f.Model.GetProgress("B").IsRewarded
                && f.Model.GetProgress("B").IsActive && !f.Model.GetProgress("A").IsRewarded,
                "指定獎勵設定無效時不可部分發獎、標記、停用或改領 A。");
            Expect(service.ClaimReward("A", false) == QuestResult.Success && service.LastError == null
                && protagonist.StickerCount == 5 && f.Model.GetProgress("A").IsActive,
                "另一筆仍可獨立領取，並支援保留啟動狀態。");
        }
    }

    private static void CheckInvalidRewards()
    {
        using (var f = new Fixture((a, b) =>
        {
            a.RequiredCount = b.RequiredCount = 1;
            a.OncePerDay = false;
            a.InitiallyActive = true;
            a.Rewards.StickerDelta = 5;
            a.Rewards.Heroines.Add(new QuestHeroineReward { HeroineID = "H", Trust = 3 });
            b.Rewards.StickerDelta = 8;
        }))
        {
            var protagonist = new ProtagonistStatusModel();
            HeroineStatusModel heroine = null;
            var service = new QuestRewardService(f.Model, protagonist, id => heroine, new ProgressFlagModel());
            f.Model.TryRecordAction(f.A);
            f.Model.TryRecordAction(f.B);
            Expect(service.ClaimNextReward() == QuestResult.InvalidReward && service.HasUnclaimedRewards
                && protagonist.StickerCount == 0 && !f.Model.GetProgress(f.A).IsRewarded
                && !f.Model.GetProgress(f.B).IsRewarded, "角色缺失時不能部分發獎或跳過第一筆。");
            heroine = new HeroineStatusModel(new HeroineStat { ID = "H" }, null);
            Expect(service.ClaimNextReward() == QuestResult.Success && heroine.Trust == 3,
                "角色重新建立後，領取應解析目前正式 Model。");
        }
        using (var f = new Fixture((a, b) =>
        {
            b.RequiredCount = 1;
            b.Rewards.StickerDelta = 4;
            b.Rewards.Flags.Add(new QuestFlagReward());
        }))
        {
            var protagonist = new ProtagonistStatusModel();
            var service = new QuestRewardService(f.Model, protagonist, null, new ProgressFlagModel());
            f.Model.TryRecordAction(f.B);
            Expect(service.ClaimNextReward() == QuestResult.InvalidReward && protagonist.StickerCount == 0
                && !f.Model.GetProgress(f.B).IsRewarded, "未指定 Flag 時整筆保留未領取。");
        }
    }

    private static void CheckFlagRewardLifetimes()
    {
        var flag = ScriptableObject.CreateInstance<ProgressFlagDefinition>();
        flag.name = "LifetimeReward";
        try
        {
            Expect(new QuestFlagReward().Lifetime == FlagLifetime.Persistent, "舊設定需沿用永久預設。");
            foreach (FlagLifetime lifetime in Enum.GetValues(typeof(FlagLifetime)))
            using (var f = new Fixture((a, b) =>
            {
                a.RequiredCount = b.RequiredCount = 1;
                a.OncePerDay = false;
                a.InitiallyActive = true;
                a.Rewards.Flags.Add(new QuestFlagReward { Flag = flag, Lifetime = lifetime });
                // 關閉時忽略週期，即使殘留無效列舉也不應阻擋移除。
                b.Rewards.Flags.Add(new QuestFlagReward { Flag = flag, Enabled = false, Lifetime = (FlagLifetime)999 });
            }))
            {
                var flags = new ProgressFlagModel();
                var service = new QuestRewardService(f.Model, null, null, flags);
                f.A.Rewards.Flags[0].Lifetime = (FlagLifetime)999;
                f.Model.TryRecordAction(f.A);
                Expect(service.ClaimNextReward() == QuestResult.Success, $"{lifetime} 應使用建立時的週期設定。");
                bool delayed = lifetime == FlagLifetime.NextDayAdd || lifetime == FlagLifetime.NextDayOnly;
                Expect(flags.Contains(flag.FlagID) == !delayed, $"{lifetime} 的立即／隔日生效時機不正確。");
                // 可存檔的週期需在往返後仍按原規則生效；Scene 依既有規則不保存。
                if (lifetime != FlagLifetime.Scene)
                    flags.LoadFromSaveData(JsonConvert.DeserializeObject<ProgressFlagSaveData>(JsonConvert.SerializeObject(flags.ToSaveData())));
                switch (lifetime)
                {
                    case FlagLifetime.Scene: flags.OnSceneChanged(); break;
                    case FlagLifetime.UntilNextSlot: flags.ClearSlotFlags(); break;
                    case FlagLifetime.UntilNextPhase: flags.ClearPhaseFlags(); break;
                    case FlagLifetime.UntilNextDay: flags.HandleDayPassed(); break;
                    default:
                        flags.HandleDayPassed();
                        Expect(flags.Contains(flag.FlagID), $"{lifetime} 跨日後應生效。");
                        flags.HandleDayPassed();
                        break;
                }
                bool remains = lifetime == FlagLifetime.Persistent || lifetime == FlagLifetime.NextDayAdd;
                Expect(flags.Contains(flag.FlagID) == remains, $"{lifetime} 的清除時機不正確。");
                flags.AddFlag(flag.FlagID, lifetime);
                f.Model.TryRecordAction(f.B);
                Expect(service.ClaimNextReward() == QuestResult.Success && !flags.Contains(flag.FlagID), "關閉需忽略週期並移除 Flag。");
                flags.HandleDayPassed();
                Expect(!flags.Contains(flag.FlagID), "關閉也須取消隔日預約。");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(flag); }
    }

    private static void CheckActiveIncomplete()
    {
        using (var f = new Fixture())
        {
            Expect(f.Model.HasActiveIncompleteQuests, "已啟動且未完成的 B 應符合。");
            f.Model.SetActive(f.B, false);
            Expect(!f.Model.HasActiveIncompleteQuests, "未啟動或停用的未完成任務不能算入。");
            f.Model.SetActive(f.B, true);
            f.Model.TryRecordAction(f.B);
            f.Model.TryRecordAction(f.B);
            f.Model.TryRecordAction(f.B);
            Expect(!f.Model.HasActiveIncompleteQuests && f.Model.HasUnclaimedRewards,
                "已完成即使未領獎且仍啟動，也不能算未完成。");
            f.Model.TryDecreaseSuccessCount(f.B);
            Expect(f.Model.HasActiveIncompleteQuests, "扣回次數變成未完成時應立即更新查詢。");
            var saved = f.Model.ToSaveData();
            f.Model.NewGame();
            f.Model.LoadFromSaveData(saved);
            Expect(f.Model.HasActiveIncompleteQuests, "查詢應使用讀檔後狀態。");
            saved.QuestDataList[0].CurrentCount = -1;
            f.Model.LoadFromSaveData(saved);
            Expect(!f.Model.HasActiveIncompleteQuests, "無效存檔不能回報有未完成任務。");
        }
        Expect(!new QuestModel(null, () => 1).HasActiveIncompleteQuests, "未設定資料庫應回傳 false。");
        var database = ScriptableObject.CreateInstance<QuestDatabase>();
        try { Expect(!new QuestModel(database, () => 1).HasActiveIncompleteQuests, "空資料庫應回傳 false。"); }
        finally { UnityEngine.Object.DestroyImmediate(database); }
    }

    private static void CheckTrackerSelection()
    {
        var database = ScriptableObject.CreateInstance<QuestDatabase>();
        try
        {
            foreach (string id in new[] { "Z", "A", "M", "B" })
            {
                var task = ScriptableObject.CreateInstance<QuestDefinition>();
                task.name = id;
                task.RequiredCount = 1;
                task.InitiallyActive = true;
                database.Tasks.Add(task);
            }
            var quests = new QuestModel(database, () => 1);
            var tracker = new QuestTrackerModel(quests);
            tracker.RefreshTasks(1, 3);
            Expect(TrackerIDs(tracker) == "Z,A,M", "初次只取資料庫順序的前三筆，不按名稱排序。");
            quests.SetActive("Z", false);
            tracker.UpdateProgress(1, 3);
            Expect(TrackerIDs(tracker) == "Z,A,M", "同一天停用後仍需保留。");
            quests.TryRecordAction("A");
            tracker.UpdateProgress(1, 3);
            Expect(TrackerIDs(tracker) == "Z,A,M" && tracker.GetItems()[1].IsCompleted, "完成應留在原位置並顯示完成狀態。");
            quests.TryMarkRewarded("A");
            tracker.UpdateProgress(1, 3);
            Expect(TrackerIDs(tracker) == "Z,M", "領獎應撤下，但不能補入 B。");
            // 同一天關閉再打開 UI 只同步進度，不能重新挑選。
            tracker.UpdateProgress(1, 3);
            Expect(TrackerIDs(tracker) == "Z,M", "同日同步不能撤掉停用項目或自動補位。");
            tracker.RefreshTasks(1, 3);
            Expect(TrackerIDs(tracker) == "M,B", "手動刷新才移除停用項目並補入後面的任務，已領獎不再加入。");
            quests.SetActive("Z", true);
            tracker.UpdateProgress(1, 3);
            Expect(TrackerIDs(tracker) == "M,B", "新啟動的任務也需等到刷新才加入。");
            tracker.UpdateProgress(2, 3);
            Expect(TrackerIDs(tracker) == "Z,M,B", "換日應依資料庫順序重新挑選。");
            tracker.UpdateProgress(2, 1);
            Expect(TrackerIDs(tracker) == "Z", "調整上限應生效。");
            tracker.UpdateProgress(2, 0);
            Expect(tracker.GetItems().Length == 0, "上限零應清空顯示。");
            quests.NewGame();
            quests.TryRecordAction("Z");
            tracker.RefreshTasks(2, 3);
            Expect(tracker.GetItems()[0].IsCompleted, "刷新也應納入啟動中已完成、未領獎的任務。");
            var saved = quests.ToSaveData();
            saved.QuestDataList[0].IsActive = false;
            quests.LoadFromSaveData(saved);
            tracker.RefreshTasks(2, 3);
            Expect(TrackerIDs(tracker) == "A,M,B", "同日讀檔後強制刷新應採用新存檔狀態。");
            saved.QuestDataList[0].CurrentCount = -1;
            quests.LoadFromSaveData(saved);
            tracker.UpdateProgress(2, 3);
            Expect(tracker.GetItems().Length == 0, "無效資料不能殘留舊項目。");
        }
        finally
        {
            foreach (var task in database.Tasks) UnityEngine.Object.DestroyImmediate(task);
            UnityEngine.Object.DestroyImmediate(database);
        }
    }

    private static string TrackerIDs(QuestTrackerModel tracker)
        => string.Join(",", Array.ConvertAll(tracker.GetItems(), item => item.QuestID));

    private static void CheckTrackerPresentation()
    {
        using (var f = new Fixture((a, b) =>
        {
            a.InitiallyActive = true;
            a.TitleTextKey = "Quest.Title.A";
            a.Rewards.StickerDelta = 3;
            b.Rewards.StickerDelta = 0;
        }))
        {
            var tracker = new QuestTrackerModel(f.Model);
            tracker.RefreshTasks(f.Day, 3);
            var item = tracker.GetItems()[0];
            Expect(item.ShowStickerCount && item.ShowSticker && item.StickerCount == 3 && item.TitleTextKey == "Quest.Title.A"
                && item.ShowDailyProgress && !item.PerformedToday && item.CurrentCount == 0 && item.RequiredCount == 2,
                "初始顯示資料不正確。");
            var other = tracker.GetItems()[1];
            Expect(!other.ShowSticker && !other.ShowStickerCount && !other.ShowDailyProgress && !other.PerformedToday,
                "零貼紙與非每日任務應隱藏對應元件。");
            f.Model.TryRecordAction(f.A);
            tracker.UpdateProgress(f.Day, 3);
            Expect(tracker.GetItems()[0].PerformedToday && tracker.GetItems()[0].CurrentCount == 1, "當日計次需顯示勾選與進度。");
            f.Day++;
            tracker.UpdateProgress(f.Day, 3);
            Expect(!tracker.GetItems()[0].PerformedToday, "跨日應清除當日勾選。");
            f.Model.TryRecordAction(f.A);
            Expect(tracker.GetItems()[0].IsCompleted && tracker.GetItems()[0].PerformedToday, "達標時應顯示 Finish 與當日勾選。");
            f.Model.TryDecreaseSuccessCount(f.A);
            Expect(!tracker.GetItems()[0].IsCompleted && tracker.GetItems()[0].PerformedToday,
                "扣回次數應恢復未完成顯示，但不能抹掉當日已執行紀錄。");
        }
        foreach (int stickers in new[] { -2, 1 })
        using (var f = new Fixture((a, b) => b.Rewards.StickerDelta = stickers))
        {
            var tracker = new QuestTrackerModel(f.Model);
            tracker.RefreshTasks(f.Day, 3);
            var item = tracker.GetItems()[0];
            Expect(item.ShowSticker && item.ShowStickerCount == (stickers == 1),
                "一張貼紙也應顯示數量，負數維持隱藏數量；只有 0 才隱藏插圖。");
        }
    }

    private sealed class Fixture : IDisposable
    {
        public readonly QuestDefinition A = ScriptableObject.CreateInstance<QuestDefinition>();
        public readonly QuestDefinition B = ScriptableObject.CreateInstance<QuestDefinition>();
        public readonly QuestDatabase Database = ScriptableObject.CreateInstance<QuestDatabase>();
        public readonly QuestModel Model;
        public int Day = 1;

        public Fixture(Action<QuestDefinition, QuestDefinition> configure = null)
        {
            A.name = "A";
            A.RequiredCount = 2;
            A.OncePerDay = true;
            B.name = "B";
            B.RequiredCount = 3;
            B.InitiallyActive = true;
            configure?.Invoke(A, B);
            Database.Tasks.Add(A);
            Database.Tasks.Add(B);
            Model = new QuestModel(Database, () => Day);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(A);
            UnityEngine.Object.DestroyImmediate(B);
            UnityEngine.Object.DestroyImmediate(Database);
        }
    }
}
#endif
