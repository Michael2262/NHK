using System;
using UnityEngine;

// ==========================================================
// 大冒險流程效果：結束大冒險 / 標記地點通關 / 設進度旗標
// 「結束時機」與「通關」都做成效果，由設計者掛在牌上（通常是 ForcedDraw 的 Boss 牌）。
// ==========================================================

/// <summary>
/// 中止這趟大冒險。
///
/// 注意：里程達標的「正常通關」已經是固定行為，不需要用這個效果。
/// 這支是給「提前中止」用的（例如某張牌代表意外，直接被迫結束）。
/// </summary>
[Serializable]
public class AdvEndAdventureEffect : AdventureEffect
{
    [Tooltip("結束時是否把「目前 Dungeon」標記為已通關（設其 ClearedFlag 的 persistent 旗標）。\n" +
             "一般的提前中止應該不勾")]
    public bool MarkCurrentDungeonCleared = false;

    public override void Apply(AdventureContext ctx)
    {
        if (ctx.Run == null) return;
        if (MarkCurrentDungeonCleared) ctx.Run.MarkCurrentDungeonCleared();
        ctx.Run.EndAdventure(AdventureEndReason.ByEffect);
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
/// 調用立繪變化：依指定女主角目前的情緒（CurrentEmotion），從其表情組隨機挑一個變體，
/// 把那組 Sequencer 命令字串交給對話系統的 Sequencer 執行（跟 TachieControl(...) 同一套 parser）。
/// </summary>
[Serializable]
public class AdvTachieEmotionEffect : AdventureEffect
{
    [Tooltip("要變化立繪的女主角表情組（SO 內含 HeroineID 與各情緒的變體）")]
    public AdventureTachieEmotionSet EmotionSet;

    public override void Apply(AdventureContext ctx)
    {
        if (EmotionSet == null)
        {
            Debug.LogWarning("[AdvTachieEmotion] EmotionSet 未指定，跳過。");
            return;
        }

        // 讀該女主角目前的情緒（找不到女主角就用 Normal 當預設）
        var emotion = HeroineEmotionCardType.Normal;
        var gss = GameStatusService.Instance;
        bool heroineFound = false;
        if (gss != null && gss.Heroines != null && !string.IsNullOrEmpty(EmotionSet.HeroineID)
            && gss.Heroines.TryGetValue(EmotionSet.HeroineID, out var heroine) && heroine != null)
        {
            emotion = heroine.CurrentEmotion;
            heroineFound = true;
        }

        string sequence = EmotionSet.PickVariant(emotion);

        // ── 診斷 log（確認流程後可刪）──
        Debug.Log($"[AdvTachieEmotion] HeroineID='{EmotionSet.HeroineID}' 找到女主角={heroineFound} " +
                  $"目前情緒={emotion} 挑到的Sequence={(string.IsNullOrWhiteSpace(sequence) ? "(空)" : sequence)}");

        if (string.IsNullOrWhiteSpace(sequence))
        {
            Debug.LogWarning($"[AdvTachieEmotion] 情緒 {emotion} 沒挑到任何變體，" +
                             "請確認 EmotionSet 裡有該情緒的 Entry 且 Variants 不為空（或第一筆有內容）。");
            return;
        }

        // {actor} → HeroineID，然後交給對話系統的 Sequencer 跑（同一套 parser）
        sequence = sequence.Replace("{actor}", EmotionSet.HeroineID);

        if (PixelCrushers.DialogueSystem.DialogueManager.Instance != null)
        {
            Debug.Log($"[AdvTachieEmotion] 送出 PlaySequence：{sequence}");
            PixelCrushers.DialogueSystem.DialogueManager.PlaySequence(sequence);
        }
        else
        {
            Debug.LogWarning("[AdvTachieEmotion] 找不到 DialogueManager，無法播放立繪 Sequence。");
        }
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
