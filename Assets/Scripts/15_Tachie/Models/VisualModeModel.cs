using System.Collections.Generic;

/// <summary>
/// 視覺 mode 的真相來源（Model，純 C# 類別，由 GameStatusService 持有）。
///
/// 管理三種「呈現變體」狀態：
/// 1. Tachie 各角色的 body mode —— 以 override map 記錄（characterID → mode），
///    只記「被明確設定過」的角色；沒有 entry 的角色由 TachieActor 自己的預設 mode 處理。
/// 2. 全域 BG mode
/// 3. 全域 CG mode
///
/// 場景上的 TachieController / CGController 是「視圖」，切 mode 時寫回本 Model，
/// 並在場景初始化 / 讀檔（OnGameStatusLoaded）時從本 Model 讀回套用。
/// 這樣 mode 才能跨場景維持，並被存檔保存。
/// </summary>
public class VisualModeModel
{
    public const string DEFAULT_MODE = "Default";

    // characterID → mode（不分大小寫）
    private readonly Dictionary<string, string> _bodyModes =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

    public string BgMode { get; private set; } = DEFAULT_MODE;
    public string CgMode { get; private set; } = DEFAULT_MODE;

    // ==========================================================
    // Tachie body mode
    // ==========================================================

    public void SetBodyMode(string characterID, string mode)
    {
        if (string.IsNullOrEmpty(characterID) || string.IsNullOrEmpty(mode)) return;
        _bodyModes[characterID] = mode;
    }

    public bool TryGetBodyMode(string characterID, out string mode)
    {
        mode = null;
        if (string.IsNullOrEmpty(characterID)) return false;
        return _bodyModes.TryGetValue(characterID, out mode);
    }

    public void ClearBodyMode(string characterID)
    {
        if (string.IsNullOrEmpty(characterID)) return;
        _bodyModes.Remove(characterID);
    }

    // ==========================================================
    // 全域 BG / CG mode
    // ==========================================================

    public void SetBgMode(string mode)
    {
        if (!string.IsNullOrEmpty(mode)) BgMode = mode;
    }

    public void SetCgMode(string mode)
    {
        if (!string.IsNullOrEmpty(mode)) CgMode = mode;
    }

    // ==========================================================
    // 生命週期
    // ==========================================================

    /// <summary>開新遊戲：全部回歸預設。</summary>
    public void NewGame()
    {
        _bodyModes.Clear();
        BgMode = DEFAULT_MODE;
        CgMode = DEFAULT_MODE;
    }

    // ==========================================================
    // 存檔對接
    // ==========================================================

    public VisualModeSaveData ToSaveData()
    {
        var data = new VisualModeSaveData
        {
            BgMode = BgMode,
            CgMode = CgMode
        };

        foreach (var kvp in _bodyModes)
        {
            data.BodyModeActorIDs.Add(kvp.Key);
            data.BodyModeValues.Add(kvp.Value);
        }

        return data;
    }

    public void LoadFromSaveData(VisualModeSaveData data)
    {
        // null（舊存檔沒有此欄位）→ 視為全部預設
        if (data == null)
        {
            NewGame();
            return;
        }

        _bodyModes.Clear();

        if (data.BodyModeActorIDs != null && data.BodyModeValues != null &&
            data.BodyModeActorIDs.Count == data.BodyModeValues.Count)
        {
            for (int i = 0; i < data.BodyModeActorIDs.Count; i++)
            {
                string id = data.BodyModeActorIDs[i];
                string mode = data.BodyModeValues[i];
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(mode))
                    _bodyModes[id] = mode;
            }
        }
        else
        {
            UnityEngine.Debug.LogError("[VisualModeModel] 存檔損壞：BodyModeActorIDs 與 BodyModeValues 長度不匹配或為 null！");
        }

        BgMode = string.IsNullOrEmpty(data.BgMode) ? DEFAULT_MODE : data.BgMode;
        CgMode = string.IsNullOrEmpty(data.CgMode) ? DEFAULT_MODE : data.CgMode;
    }
}
