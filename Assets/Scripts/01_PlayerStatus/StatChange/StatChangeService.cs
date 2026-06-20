using System.Collections.Generic;
using UnityEngine;

// ============================================================
// StatChangeService.cs
// ============================================================
// 「數值變化套組」執行核心（純 C# Model）。
//
// 職責：
//   1. 依 ID 從 StatChangePackageDatabase 取出套組。
//   2. 逐條把數值變化套到 GameStatusService.Instance 上的
//      Protagonist（Stress/LifePower/Sociality/Dependency）
//      與指定的 Heroine（Libido/Trust）。
//   3. 收集每筆「實際發生的 delta」成 ValueChangeRecord 回傳，
//      由呼叫端（SequencerCommandStatPackage 等）決定要不要報告。
//
// 不做任何 UI / 報告呼叫，維持 Model 純淨（CLAUDE.md 鐵則 5）。
//
// 由 GameStatusService.InitializeGameModels() 建立並持有。
// ============================================================

public class StatChangeService
{
    private readonly StatChangePackageDatabase _database;

    public StatChangeService(StatChangePackageDatabase database)
    {
        _database = database;
        if (_database == null)
        {
            Debug.LogWarning("[StatChangeService] 未提供 StatChangePackageDatabase，所有套組調用都會失敗。請在 GameStatusService 的 Inspector 拖入資料庫。");
        }
    }

    /// <summary>
    /// 套用指定 ID 的數值變化套組。
    /// </summary>
    /// <param name="packageID">套組 ID。</param>
    /// <param name="heroineID">女主角 ID。套組若含 Libido / Trust 變化時必填；純主角套組可省略。</param>
    /// <returns>實際發生變化（delta != 0）的記錄清單，供報告使用。永不為 null。</returns>
    public List<ValueChangeRecord> Apply(string packageID, string heroineID = null)
    {
        var records = new List<ValueChangeRecord>();

        if (_database == null)
        {
            Debug.LogWarning("[StatChangeService] 資料庫為 null，無法套用套組。");
            return records;
        }

        var entry = _database.GetEntry(packageID);
        if (entry == null)
        {
            Debug.LogWarning($"[StatChangeService] 找不到數值變化套組：{packageID}");
            return records;
        }

        var service = GameStatusService.Instance;
        if (service == null || service.Protagonist == null)
        {
            Debug.LogWarning("[StatChangeService] GameStatusService 或 Protagonist 尚未初始化，無法套用套組。");
            return records;
        }

        if (entry.ops == null) return records;

        // 女主角延後查找：只有真的有女主角項時才需要 heroineID。
        HeroineStatusModel heroine = null;
        bool heroineResolved = false;

        foreach (var op in entry.ops)
        {
            if (op == null) continue;

            if (op.IsHeroineStat)
            {
                if (!heroineResolved)
                {
                    heroine = ResolveHeroine(service, heroineID, packageID);
                    heroineResolved = true;
                }
                if (heroine == null) continue; // 找不到女主角，跳過所有女主角項（ResolveHeroine 已警告）

                int delta = ApplyHeroineOp(heroine, op);
                if (delta != 0)
                {
                    records.Add(BuildRecord(op.kind, delta, isHeroine: true, heroineNameKey: heroine.NameTextKey));
                }
            }
            else
            {
                int delta = ApplyProtagonistOp(service.Protagonist, op);
                if (delta != 0)
                {
                    records.Add(BuildRecord(op.kind, delta, isHeroine: false, heroineNameKey: null));
                }
            }
        }

        return records;
    }

    // ==========================================================
    // 內部：套用單筆變化，回傳實際 delta（套用後 - 套用前）
    // ==========================================================

    private static int ApplyProtagonistOp(ProtagonistStatusModel p, StatChangeOp op)
    {
        int before = GetProtagonistValue(p, op.kind);

        switch (op.kind)
        {
            case StatKind.Stress:
                if (op.op == StatOp.Set) p.SetStress(op.amount); else p.AddStress(op.amount);
                break;
            case StatKind.LifePower:
                if (op.op == StatOp.Set) p.SetLifePower(op.amount); else p.AddLifePower(op.amount);
                break;
            case StatKind.Sociality:
                if (op.op == StatOp.Set) p.SetSociality(op.amount); else p.AddSociality(op.amount);
                break;
            case StatKind.Dependency:
                if (op.op == StatOp.Set) p.SetDependency(op.amount); else p.AddDependency(op.amount);
                break;
            case StatKind.RoomMessLevel:
                if (op.op == StatOp.Set) p.SetRoomMessLevel(op.amount); else p.AddRoomMessLevel(op.amount);
                break;
            default:
                Debug.LogWarning($"[StatChangeService] ApplyProtagonistOp 收到非主角數值：{op.kind}");
                return 0;
        }

        return GetProtagonistValue(p, op.kind) - before;
    }

    private static int ApplyHeroineOp(HeroineStatusModel heroine, StatChangeOp op)
    {
        int before = GetHeroineValue(heroine, op.kind);

        switch (op.kind)
        {
            case StatKind.Libido:
                if (op.op == StatOp.Set) heroine.SetLibido(op.amount); else heroine.AddLibido(op.amount);
                break;
            case StatKind.Trust:
                if (op.op == StatOp.Set) heroine.SetTrust(op.amount); else heroine.AddTrust(op.amount);
                break;
            default:
                Debug.LogWarning($"[StatChangeService] ApplyHeroineOp 收到非女主角數值：{op.kind}");
                return 0;
        }

        return GetHeroineValue(heroine, op.kind) - before;
    }

    private static int GetProtagonistValue(ProtagonistStatusModel p, StatKind kind)
    {
        switch (kind)
        {
            case StatKind.Stress: return p.Stress;
            case StatKind.LifePower: return p.LifePower;
            case StatKind.Sociality: return p.Sociality;
            case StatKind.Dependency: return p.Dependency;
            case StatKind.RoomMessLevel: return p.RoomMessLevel;
            default: return 0;
        }
    }

    private static int GetHeroineValue(HeroineStatusModel heroine, StatKind kind)
    {
        switch (kind)
        {
            case StatKind.Libido: return heroine.Libido;
            case StatKind.Trust: return heroine.Trust;
            default: return 0;
        }
    }

    private static HeroineStatusModel ResolveHeroine(GameStatusService service, string heroineID, string packageID)
    {
        if (string.IsNullOrWhiteSpace(heroineID))
        {
            Debug.LogWarning($"[StatChangeService] 套組 {packageID} 含女主角數值，但調用時未提供 heroineID，已跳過女主角項。");
            return null;
        }

        if (service.Heroines == null || !service.Heroines.TryGetValue(heroineID, out var heroine) || heroine == null)
        {
            Debug.LogWarning($"[StatChangeService] 找不到女主角：{heroineID}（套組 {packageID}）。");
            return null;
        }

        return heroine;
    }

    private static ValueChangeRecord BuildRecord(StatKind kind, int delta, bool isHeroine, string heroineNameKey)
    {
        return new ValueChangeRecord
        {
            isHeroineResource = isHeroine,
            resourceTypeKey = kind.ToString(),  // 與 ValueChangeReporter 的 key 規則對齊（Stress/Libido/Trust…）
            finalAmount = delta,
            effectResult = ValueChangeResult.EffectResult.Normal,
            heroineNameKey = heroineNameKey
        };
    }
}
