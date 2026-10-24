using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TimeMapping", menuName = "Game/TimeMapping")]
public class TimeMappingSO : ScriptableObject
{
    [System.Serializable]
    public class SlotTimeInfo
    {
        [Tooltip("索引 0 代表剛進入 Slot。請填 24 小時制 HH:mm，例如 08:05。")]
        public List<string> miniTimes = new List<string>();
    }

    [System.Serializable]
    public class PhaseTimeInfo
    {
        [Tooltip("僅供識別用，例如：早上")]
        public string phaseName;

        [Header("多語系設定")]
        [Tooltip("Text Table 中的 Field Name，例如：Phase/Morning")]
        public string localizationKey;

        [Header("各 Slot 的 Mini 顯示時間")]
        public List<SlotTimeInfo> slots = new List<SlotTimeInfo>();
    }

    [Header("時段與 Slot 對應表")]
    public List<PhaseTimeInfo> phases = new List<PhaseTimeInfo>();

    [Header("視覺換日設定 (在此門檻之後顯示為隔天)")]
    [Tooltip("當前 Phase 大於此值，或 Phase 相等且 Slot 大於等於此值時，UI 顯示天數+1")]
    public int dayRolloverPhase = 3;
    public int dayRolloverSlot = 1;

    /// <summary>
    /// 取得 Mini 對應的時間字串；未設定或格式錯誤時顯示 --:--。
    /// </summary>
    public string GetTimeDisplay(int phaseIndex, int slotInPhase, int miniInSlot)
    {
        return TryGetClockTime(phaseIndex, slotInPhase, miniInSlot, out int hour, out int minute)
            ? $"{hour:D2}:{minute:D2}" : "--:--";
    }

    /// <summary>供 UI 取得時、分數字。查詢不會補建或更動設定。</summary>
    public bool TryGetClockTime(int phaseIndex, int slotInPhase, int miniInSlot, out int hour, out int minute)
    {
        hour = minute = 0;
        if (phases == null || phaseIndex < 0 || phaseIndex >= phases.Count) return false;
        var slots = phases[phaseIndex]?.slots;
        int slotIndex = slotInPhase - 1;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Count) return false;
        var times = slots[slotIndex]?.miniTimes;
        if (times == null || miniInSlot < 0 || miniInSlot >= times.Count) return false;
        return TryParseClockTime(times[miniInSlot], out hour, out minute);
    }

    public static bool TryParseClockTime(string text, out int hour, out int minute)
    {
        hour = minute = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (text.Length != 5 || text[2] != ':' ||
            text[0] < '0' || text[0] > '9' || text[1] < '0' || text[1] > '9' ||
            text[3] < '0' || text[3] > '9' || text[4] < '0' || text[4] > '9') return false;
        int h = (text[0] - '0') * 10 + text[1] - '0';
        int m = (text[3] - '0') * 10 + text[4] - '0';
        if (h > 23 || m > 59) return false;
        hour = h;
        minute = m;
        return true;
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
