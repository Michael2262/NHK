using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class TachieController : MonoBehaviour
{
    // Singleton Instance單例
    public static TachieController Instance { get; private set; }

    [Header("角色清單 (手動拉入場景中的 Actor)")]
    public List<TachieActor> actorList = new List<TachieActor>();

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
    }

    private TachieActor GetActor(string id)
    {
        if (actorDict.TryGetValue(id, out TachieActor actor)) return actor;
        Debug.LogError($"[TachieController] 找不到角色: {id}");
        return null;
    }

    // --- API 介面 ---

    // 1. 指定角色出現/消失
    public void SetVisibility(string id, bool isVisible, float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        GetActor(id)?.Fade(isVisible ? 1f : 0f, dur);
    }

    // 2. 細分面部控制 API

    // 換眉毛
    public void ChangeEyebrow(string id, string name)
    {
        GetActor(id)?.ChangeEyebrow(name);
    }

    // 換眼睛
    public void ChangeEye(string id, string name)
    {
        GetActor(id)?.ChangeEye(name);
    }

    // 換嘴巴
    public void ChangeMouth(string id, string name)
    {
        GetActor(id)?.ChangeMouth(name);
    }

    // 換腮紅
    public void ChangeBlush(string id, string name)
    {
        GetActor(id)?.ChangeBlush(name);
    }

    // 換其他特徵 (如汗水、青筋、眼鏡等)
    public void ChangeOther(string id, string name)
    {
        GetActor(id)?.ChangeOther(name);
    }

    // 換最上層特徵 (如眼鏡、帽子等覆蓋在臉部上方的物件)
    public void ChangeAbove(string id, string name)
    {
        GetActor(id)?.ChangeAbove(name);
    }

    // 一次性更換表情組合的便捷方法
    public void ChangeFullFace(string id, string eye, string mouth, string eyebrow = "Normal", string blush = "None")
    {
        var actor = GetActor(id);
        if (actor != null)
        {
            actor.ChangeEye(eye);
            actor.ChangeMouth(mouth);
            actor.ChangeEyebrow(eyebrow);
            actor.ChangeBlush(blush);
        }
    }

    // 3. 換身體
    public void ChangeBody(string id, string bodyName)
    {
        GetActor(id)?.ChangeBody(bodyName);
    }

    // 4. 移動 X 值
    public void MoveToX(string id, float xValue, float duration = -1)
    {
        float dur = duration < 0 ? defaultMoveDuration : duration;
        GetActor(id)?.MoveX(xValue, dur);
    }

    // 5. 回原位 (X=0)
    public void MoveToCenter(string id, float duration = -1)
    {
        MoveToX(id, 0, duration);
    }

    // 6. 到右側位
    public void MoveToRight(string id, float duration = -1)
    {
        MoveToX(id, rightPosX, duration);
    }

    // 7. 到左側位
    public void MoveToLeft(string id, float duration = -1)
    {
        MoveToX(id, leftPosX, duration);
    }

    // 8. 全部消失並回到原位
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