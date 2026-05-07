using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 連動群組資料結構：同一群組內的角色，外觀操作會自動同步。
/// </summary>
[System.Serializable]
public class TachieLinkGroup
{
    [Tooltip("群組名稱，方便辨識用")]
    public string groupName;
    [Tooltip("群組內所有角色的 ID")]
    public List<string> memberIDs = new List<string>();
}

public class TachieController : MonoBehaviour
{
    // Singleton Instance單例
    public static TachieController Instance { get; private set; }

    [Header("角色清單 (手動拉入場景中的 Actor)")]
    public List<TachieActor> actorList = new List<TachieActor>();

    [Header("連動群組設定")]
    [Tooltip("同一組內的角色，外觀類操作 (Body/Eye/Mouth 等) 會自動同步")]
    public List<TachieLinkGroup> linkGroups = new List<TachieLinkGroup>();

    [Header("全域位置設定")]
    [Tooltip("角色到左側時的 X 座標")]
    public float leftPosX = -500f;
    [Tooltip("角色到右側時的 X 座標")]
    public float rightPosX = 500f;

    [Header("全域時間設定")]
    public float defaultFadeDuration = 0.4f;
    public float defaultMoveDuration = 0.5f;

    // 內部查詢字典：Key = characterID, Value = TachieActor(不分大小寫)
    private Dictionary<string, TachieActor> actorDict = new Dictionary<string, TachieActor>(System.StringComparer.OrdinalIgnoreCase);

    // 連動查詢字典：Key = groupName, Value = 群組內所有角色ID列表
    private Dictionary<string, List<string>> groupDict = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);



    private void Awake()
    {
        // 單例防呆邏輯
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("發現多個 TachieController，已刪除多餘的實例。");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 如果你的立繪系統需要跨場景保留，可以解開下一行代碼
        // DontDestroyOnLoad(gameObject);

        InitActors();
    }

    private void InitActors()
    {
        actorDict.Clear();
        foreach (var actor in actorList)
        {
            if (actor == null) continue;

            if (!actorDict.ContainsKey(actor.characterID))
            {
                actorDict.Add(actor.characterID, actor);
                actor.canvasGroup.alpha = 0;// 防呆：初始將 Alpha 設為 0
            }
            else
            {
                Debug.LogWarning($"重複的角色 ID: {actor.characterID}，請檢查 Inspector 設定");
            }
        }

        // 建立群組查詢表：Key = groupName, Value = 成員ID列表
        groupDict.Clear();
        foreach (var group in linkGroups)
        {
            if (string.IsNullOrEmpty(group.groupName) || group.memberIDs == null || group.memberIDs.Count < 2) continue;

            if (groupDict.ContainsKey(group.groupName))
            {
                Debug.LogWarning($"[TachieController] 重複的群組名稱: {group.groupName}");
                continue;
            }

            // 確保 groupName 不會與任何角色 ID 衝突
            if (actorDict.ContainsKey(group.groupName))
            {
                Debug.LogError($"[TachieController] 群組名稱 '{group.groupName}' 與角色 ID 衝突，請更換名稱！");
                continue;
            }

            groupDict.Add(group.groupName, new List<string>(group.memberIDs));
        }
    }

    private TachieActor GetActor(string id)
    {
        if (actorDict.TryGetValue(id, out TachieActor actor)) return actor;
        Debug.LogError($"[TachieController] 找不到角色: {id}");
        return null;
    }

    /// <summary>
    /// 解析 ID：如果是群組名稱，回傳群組內所有成員 ID；如果是角色 ID，只回傳該角色自己。
    /// </summary>
    private List<string> ResolveIDs(string id)
    {
        // 優先查群組
        if (groupDict.TryGetValue(id, out var members))
            return members;

        // 否則當作單一角色 ID
        return null;
    }

    /// <summary>
    /// 對指定 ID 執行外觀操作。
    /// - 若 ID 是群組名稱 → 對群組內所有成員執行（連動）
    /// - 若 ID 是角色 ID → 只對該角色執行（不連動）
    /// 連動角色若找不到對應素材名稱，TachieActor 內部會 LogWarning 並跳過，不會報錯。
    /// </summary>
    private void ExecuteAppearance(string id, System.Action<string> operation)
    {
        var groupMembers = ResolveIDs(id);
        if (groupMembers != null)
        {
            // 是群組 → 對所有成員執行
            foreach (var memberID in groupMembers)
                operation(memberID);
        }
        else
        {
            // 是單一角色
            operation(id);
        }
    }

    // ========== API 介面 ==========

    // --- 1. 顯示/隱藏 (不連動) ---
    public void SetVisibility(string id, bool isVisible, float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        GetActor(id)?.Fade(isVisible ? 1f : 0f, dur);
    }

    // --- 2. 細分面部控制 API (有連動) ---

    // 換眉毛
    public void ChangeEyebrow(string id, string name)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeEyebrow(name));
    }

    // 換眼睛
    public void ChangeEye(string id, string name)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeEye(name));
    }

    // 換嘴巴
    public void ChangeMouth(string id, string name)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeMouth(name));
    }

    // 換腮紅
    public void ChangeBlush(string id, string name)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeBlush(name));
    }

    // 換其他特徵 (如汗水、青筋、眼鏡等)
    public void ChangeOther(string id, string name)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeOther(name));
    }

    // 換最上層特徵 (如眼鏡、帽子等覆蓋在臉部上方的物件)
    public void ChangeAbove(string id, string name)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeAbove(name));
    }

    // 一次性更換表情組合的便捷方法 (有連動)
    public void ChangeFullFace(string id, string eye, string mouth, string eyebrow = "Normal", string blush = "None")
    {
        ExecuteAppearance(id, linkId =>
        {
            var actor = GetActor(linkId);
            if (actor != null)
            {
                actor.ChangeEye(eye);
                actor.ChangeMouth(mouth);
                actor.ChangeEyebrow(eyebrow);
                actor.ChangeBlush(blush);
            }
        });
    }

    // --- 3. 換身體 (有連動) ---
    public void ChangeBody(string id, string bodyName)
    {
        ExecuteAppearance(id, linkId => GetActor(linkId)?.ChangeBody(bodyName));
    }

    // --- 4. 移動與位置控制 (不連動) ---

    // 移動 X 值
    public void MoveToX(string id, float xValue, float duration = -1)
    {
        float dur = duration < 0 ? defaultMoveDuration : duration;
        GetActor(id)?.MoveX(xValue, dur);
    }

    // 回原位 (X=0)
    public void MoveToCenter(string id, float duration = -1)
    {
        MoveToX(id, 0, duration);
    }

    // 到右側位
    public void MoveToRight(string id, float duration = -1)
    {
        MoveToX(id, rightPosX, duration);
    }

    // 到左側位
    public void MoveToLeft(string id, float duration = -1)
    {
        MoveToX(id, leftPosX, duration);
    }

    // --- 5. 全部消失並回到原位 (不連動，因為本來就是全部角色) ---
    public void ClearAll(float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        foreach (var actor in actorDict.Values)
        {
            actor.Fade(0, dur);
            actor.MoveX(0, dur);
        }
    }
}