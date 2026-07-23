using System;
using UnityEngine;

// ==========================================================
// 大冒險流程效果：結束大冒險 / 標記地點通關 / 設進度旗標
// 「結束時機」與「通關」都做成效果，由設計者掛在牌上（通常是 ForcedDraw 的 Boss 牌）。
// ==========================================================

/// <summary>
/// 結束這趟大冒險。通常掛在最終牌的「成功效果」。
/// 可選：一併把目前正在進行的 Dungeon 標記為已通關。
/// </summary>
[Serializable]
public class AdvEndAdventureEffect : AdventureEffect
{
    [Tooltip("結束時是否把「目前 Dungeon」標記為已通關（設其 ClearedFlag 的 persistent 旗標）")]
    public bool MarkCurrentDungeonCleared = true;

    public override void Apply(AdventureContext ctx)
    {
        if (ctx.Run == null) return;
        if (MarkCurrentDungeonCleared) ctx.Run.MarkCurrentDungeonCleared();
        ctx.Run.EndAdventure(AdventureEndReason.ClearedByCard);
    }
}

/// <summary>
/// 把「指定的」冒險地點標記為已通關（可以是非當前的 Dungeon，用來做連鎖解鎖）。
/// 留空 Dungeon = 標記目前正在進行的地點。
/// </summary>
[Serializable]
public class AdvMarkDungeonClearedEffect : AdventureEffect
{
    [Tooltip("要標記通關的地點。留空 = 目前正在進行的 Dungeon")]
    public AdventureDungeonData Dungeon;

    public override void Apply(AdventureContext ctx)
    {
        var dungeon = Dungeon != null ? Dungeon : ctx.Run?.Dungeon;
        if (dungeon == null || dungeon.ClearedFlag == null || ctx.ProgressFlags == null) return;
        ctx.ProgressFlags.AddPersistentFlag(dungeon.ClearedFlag.FlagID);
    }
}

/// <summary>
/// 播放一段對話（轉給 StoryManager）。
/// 可放在必有 / 成功 / 失敗任一清單，讓翻牌結果帶出劇情。
/// </summary>
[Serializable]
public class AdvPlayConversationEffect : AdventureEffect
{
    [Tooltip("要播放的 Pixel Crushers 對話 Title")]
    public string ConversationTitle;

    [Tooltip("播放模式：\n" +
             "・Queue      排隊，等目前對話結束後再播\n" +
             "・Interrupt  中斷目前對話，立刻播這段\n" +
             "・Skip       目前有對話在播就直接放棄這段\n" +
             "・Priority   依 StoryManager 的優先權規則處理")]
    public DialogueMode Mode = DialogueMode.Queue;

    public override void Apply(AdventureContext ctx)
    {
        if (string.IsNullOrEmpty(ConversationTitle)) return;

        if (StoryManager.Instance == null)
        {
            Debug.LogWarning($"[AdvPlayConversationEffect] 找不到 StoryManager，跳過對話 '{ConversationTitle}'");
            return;
        }

        StoryManager.Instance.PlayConversation(ConversationTitle, Mode);
    }
}

/// <summary>
/// 通用：設一個 persistent 進度旗標。可拿來觸發劇情、解鎖其他系統等。
/// </summary>
[Serializable]
public class AdvSetProgressFlagEffect : AdventureEffect
{
    [Tooltip("要開啟的 persistent 進度旗標定義檔")]
    public ProgressFlagDefinition Flag;

    public override void Apply(AdventureContext ctx)
    {
        if (Flag == null || ctx.ProgressFlags == null) return;
        ctx.ProgressFlags.AddPersistentFlag(Flag.FlagID);
    }
}
