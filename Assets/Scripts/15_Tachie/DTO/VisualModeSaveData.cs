using System;
using System.Collections.Generic;

/// <summary>
/// 視覺 mode 的存檔資料容器（Tachie 各角色 body mode + 全域 BG/CG mode）。
/// Tachie 部分沿用「平行 List」pattern（JSON 無法直接還原 Dictionary）：
/// BodyModeActorIDs[i] 對應 BodyModeValues[i]。
/// </summary>
[Serializable]
public class VisualModeSaveData
{
    public List<string> BodyModeActorIDs = new List<string>();   // key：角色 ID
    public List<string> BodyModeValues = new List<string>();     // value：mode 名（與上面同 index 對齊）

    public string BgMode = "Default";
    public string CgMode = "Default";
}
