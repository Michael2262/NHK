using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Dialogue System Sequencer 指令：透過 SpriteActor 替換圖片。
    ///
    /// 語法：SpriteActorSet(角色ID, 圖片名稱)
    ///
    /// 範例：
    ///   SpriteActorSet(Enemy01, Damaged)
    ///   SpriteActorSet(NPC_Shop, Happy)
    ///   SpriteActorSet(Door01, Open)
    ///   SpriteActorSet(Item, None)          → 清除圖片
    /// </summary>
    public class SequencerCommandSpriteActorSet : SequencerCommand
    {
        public void Awake()
        {
            string actorID = GetParameter(0);
            string spriteName = GetParameter(1);

            if (string.IsNullOrEmpty(actorID))
            {
                Debug.LogWarning("[SpriteActorSet] 缺少角色 ID（參數 0）。");
            }
            else
            {
                SpriteActor.Set(actorID, spriteName);
            }

            Stop();
        }
    }
}
