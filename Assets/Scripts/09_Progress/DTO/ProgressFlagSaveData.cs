using System.Collections.Generic;

[System.Serializable]
public class ProgressFlagSaveData
{
    public HashSet<string> ActiveFlags = new HashSet<string>(); //  Persistent 永久標記

    public HashSet<string> SlotFlags = new HashSet<string>();  // UntilNextSlot
    public HashSet<string> PhaseFlags = new HashSet<string>(); // UntilNextPhase
    public HashSet<string> DayFlags = new HashSet<string>();   // UntilNextDay

    public List<VariableEntry> ActiveVariables = new List<VariableEntry>(); // 數值變數

    [System.Serializable]
    public class VariableEntry
    {
        public string Key;
        public int Value;
    }
}