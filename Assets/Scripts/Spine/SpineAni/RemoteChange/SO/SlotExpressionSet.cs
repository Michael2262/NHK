using System.Collections.Generic;
using UnityEngine;
using MySpineSystem;
using System;

[Serializable]
public class SlotSetting
{
    public ExpressionSlotType slotType; // 腮紅、流汗等
    public List<string> attachmentNames; // 該插槽在此情緒下的貼圖池

    public string GetRandomName()
    {
        if (attachmentNames == null || attachmentNames.Count == 0) return null;
        return attachmentNames[UnityEngine.Random.Range(0, attachmentNames.Count)];
    }
}

[CreateAssetMenu(fileName = "SlotSet_New", menuName = "Expression System/Multi-Slot Set")]
public class SlotExpressionSet : ScriptableObject
{
    public HeroineEmotionType emotion;
    public List<SlotSetting> slotSettings = new List<SlotSetting>();
}