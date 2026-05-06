using System;
using System.Collections.Generic;

// 職責：保存主角已解鎖的技能列表。這是存檔的關鍵數據。
public class ProtagonistSkillModel
{
    // 使用 HashSet 儲存，因為它查找效率高（只需要知道是否有該技能）
    private readonly HashSet<string> _unlockedSkillIDs = new HashSet<string>();

    // ───── 事件宣告 ─────
    public event Action<string> OnSkillUnlocked; // 告知 UI 或其他系統有新技能解鎖

    // ───── 方法 ─────

    /// <summary>
    /// 檢查主角是否已擁有某技能。
    /// </summary>
    public bool IsSkillUnlocked(string skillID)
    {
        return _unlockedSkillIDs.Contains(skillID);
    }

    /// <summary>
    /// 將新技能加入已解鎖列表。這個方法應由 SkillSystemManager 呼叫。
    /// </summary>
    public void UnlockSkill(string skillID)
    {
        if (_unlockedSkillIDs.Add(skillID)) // HashSet.Add() 返回 true 表示成功添加
        {
            OnSkillUnlocked?.Invoke(skillID);
            // Debug.Log($"[SkillModel] Skill unlocked: {skillID}");
        }
    }

    // ───── NewGame Reset ─────
    public void NewGame()
    {
        _unlockedSkillIDs.Clear();
        // 遊戲開始時，如果主角有基礎技能，可以放在這裡初始化
    }

    // ───── 存檔與讀檔方法 ─────

    /// <summary>
    /// 導出 Model 的當前狀態至 SkillSaveData 結構。
    /// </summary>
    public SkillSaveData ToSaveData()
    {
        return new SkillSaveData
        {
            // 將 HashSet 內容複製到 List，以便序列化
            UnlockedSkillIDs = new List<string>(_unlockedSkillIDs)
        };
    }

    /// <summary>
    /// 從存檔數據中恢復 Model 的狀態。
    /// </summary>
    public void LoadFromSaveData(SkillSaveData data)
    {
        if (data == null || data.UnlockedSkillIDs == null)
        {
            _unlockedSkillIDs.Clear(); // 如果數據無效，則清空
            return;
        }

        // 1. 清除舊數據
        _unlockedSkillIDs.Clear();

        // 2. 載入新數據
        foreach (string skillID in data.UnlockedSkillIDs)
        {
            _unlockedSkillIDs.Add(skillID);
            // 備註：這裡不觸發 OnSkillUnlocked 事件，因為這不是一個新的解鎖行為，只是數據恢復。
        }

        // 可以在這裡加入一個 OnSkillsLoaded 事件，通知 UI 刷新整個技能樹介面。
    }
}