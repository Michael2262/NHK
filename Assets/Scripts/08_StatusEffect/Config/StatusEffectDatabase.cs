using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Game/Status Effects/Status Effect Database")]

//就像 ItemDatabase 一樣，我們需要一個地方來註冊所有的 StatusEffect ScriptableObject，這樣在讀取存檔時，才能根據ID找到對應的效果模組
public class StatusEffectDatabase : ScriptableObject
{
    public List<StatusEffect> AllStatusEffects = new List<StatusEffect>();

    public StatusEffect GetEffectByID(string effectID)
    {
        if (string.IsNullOrEmpty(effectID)) return null;
        return AllStatusEffects.FirstOrDefault(effect => effect.EffectID == effectID);
    }
}