using System;
using System.Collections.Generic;

/// <summary>槽位存檔中的外觀選擇；兩份清單使用相同索引配對。</summary>
[Serializable]
public sealed class SpineSkinStateSaveData
{
    public List<string> ReceiverIDs = new List<string>();
    public List<string> PresetIDs = new List<string>();
}
