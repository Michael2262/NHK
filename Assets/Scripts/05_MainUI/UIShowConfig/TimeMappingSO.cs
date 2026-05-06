using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TimeMapping", menuName = "Game/TimeMapping")]
public class TimeMappingSO : ScriptableObject
{
    [System.Serializable]
    public class PhaseTimeInfo
    {
        [Tooltip("僅供識別用，例如：早上")]
        public string phaseName;

        [Header("多語系設定")]
        [Tooltip("Text Table 中的 Field Name，例如：Phase/Morning")]
        public string localizationKey;

        [Header("時段設定")]
        [Tooltip("該階段內每個 Slot 的顯示字串")]
        public List<string> slotTimes = new List<string>();
    }

    [Header("時段與 Slot 對應表")]
    public List<PhaseTimeInfo> phases = new List<PhaseTimeInfo>();

    [Header("視覺換日設定 (在此門檻之後顯示為隔天)")]
    [Tooltip("當前 Phase 大於此值，或 Phase 相等且 Slot 大於等於此值時，UI 顯示天數+1")]
    public int dayRolloverPhase = 3;
    public int dayRolloverSlot = 1;

    /// <summary>
    /// 取得對應的時間字串
    /// </summary>
    public string GetTimeDisplay(int phaseIndex, int slotInPhase)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count) return "--:--";

        // 注意：slotInPhase 如果是從 1 開始計算，這裡要 -1
        int listIndex = slotInPhase - 1;

        if (listIndex < 0 || listIndex >= phases[phaseIndex].slotTimes.Count)
            return "??:??";

        return phases[phaseIndex].slotTimes[listIndex];
    }

    /// <summary>
    /// 取得指定 Phase 的多語系 Key (用於 Pixel Crushers LocalizeUI)
    /// </summary>
    /// <param name="phaseIndex">階段索引 (0-based)</param>
    /// <returns>Text Table 的 Field Name，若無設定則回傳 phaseName</returns>
    public string GetPhaseLocalizationKey(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count)
            return string.Empty;

        var phase = phases[phaseIndex];

        // 如果有設定 localizationKey 就使用它，否則回傳 phaseName 作為 fallback
        if (!string.IsNullOrEmpty(phase.localizationKey))
            return phase.localizationKey;

        return phase.phaseName;
    }

    /// <summary>
    /// 判斷當前時間是否已經達到視覺上的「隔天」
    /// </summary>
    public bool ShouldShowNextDay(int currentPhase, int currentSlot)
    {
        if (currentPhase > dayRolloverPhase) return true;
        if (currentPhase == dayRolloverPhase && currentSlot >= dayRolloverSlot) return true;
        return false;
    }
}