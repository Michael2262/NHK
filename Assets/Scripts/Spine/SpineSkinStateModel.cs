using System;
using System.Collections.Generic;

/// <summary>保存各接收器的外觀選擇；不持有場景物件，也不依賴轉場 Queue。</summary>
public sealed class SpineSkinStateModel
{
    private readonly Dictionary<string, string> selections = new Dictionary<string, string>(StringComparer.Ordinal);

    // 只有明確要求立即套用時發送；讀檔由 OnGameStatusLoaded 統一刷新。
    public event Action<string> OnApplyRequested;

    public bool TryGetPreset(string receiverId, out string presetId)
    {
        presetId = null;
        return !string.IsNullOrWhiteSpace(receiverId) && selections.TryGetValue(receiverId, out presetId);
    }

    public bool SetPreset(string receiverId, string presetId, bool applyImmediately = false)
    {
        if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(presetId)) return false;
        selections[receiverId] = presetId;
        if (applyImmediately) OnApplyRequested?.Invoke(receiverId);
        return true;
    }

    public bool ClearPreset(string receiverId, bool applyImmediately = false)
    {
        if (string.IsNullOrWhiteSpace(receiverId)) return false;
        selections.Remove(receiverId);
        if (applyImmediately) OnApplyRequested?.Invoke(receiverId);
        return true;
    }

    public void NewGame() => selections.Clear();

    public SpineSkinStateSaveData ToSaveData()
    {
        var data = new SpineSkinStateSaveData();
        foreach (var pair in selections)
        {
            data.ReceiverIDs.Add(pair.Key);
            data.PresetIDs.Add(pair.Value);
        }
        return data;
    }

    public void LoadFromSaveData(SpineSkinStateSaveData data)
    {
        // 舊存檔缺少欄位時也必須清掉前一局的狀態。
        selections.Clear();
        if (data?.ReceiverIDs == null || data.PresetIDs == null) return;
        int count = Math.Min(data.ReceiverIDs.Count, data.PresetIDs.Count);
        for (int i = 0; i < count; i++)
            SetPreset(data.ReceiverIDs[i], data.PresetIDs[i]);
    }
}
