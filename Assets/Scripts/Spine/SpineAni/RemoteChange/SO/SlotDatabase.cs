using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MySpineSystem;

[CreateAssetMenu(fileName = "SlotDatabase", menuName = "Expression System/Slot Database")]
public class SlotDatabase : ScriptableObject
{
    [UnityEngine.Tooltip("收納所有的情緒插槽設定檔")]
    public List<SlotExpressionSet> allSlotSets;

    /// <summary>
    /// 根據情緒獲取完整的 SlotExpressionSet 物件
    /// </summary>
    public SlotExpressionSet GetSet(HeroineEmotionType emotion)
    {
        if (allSlotSets == null) return null;

        // 搜尋並回傳符合該情緒的設定檔物件
        return allSlotSets.FirstOrDefault(s => s.emotion == emotion);
    }
}