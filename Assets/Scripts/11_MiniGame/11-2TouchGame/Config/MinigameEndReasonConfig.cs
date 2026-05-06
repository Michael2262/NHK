using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 小遊戲開發度計算設定表
///
/// 公式：開發度 exp = (基礎得分 + 額外得分) × 所有乘倍器相乘
///
/// 額外得分 = (OrgasmTimes    × orgasmTimesBonus)
///          + (ExcitedLv      × excitedLvBonus)
///          + (ShootTimes     × shootTimesBonus)
///          + (OverShootTimes × overShootTimesBonus)
///
/// 乘倍器 = (DangerScene       ? dangerSceneMultiplier      : 1)
///        × (ChallengeAccepted ? challengeAcceptedMultiplier : 1)
/// </summary>
[CreateAssetMenu(fileName = "MinigameEndReasonConfig", menuName = "Minigame/EndReason Config")]
public class MinigameEndReasonConfig : ScriptableObject
{
    // ═══════════════════════════════════════════════
    // 基礎結果得分
    // ═══════════════════════════════════════════════
    [System.Serializable]
    public class ReasonScoreEntry
    {
        public MinigameEndReason reason;
        [Tooltip("此結束理由對應的基礎得分")]
        public int score;
        [Tooltip("顯示名稱的多語系 Key（對應 Dialogue System Text Table）")]
        public string displayNameKey;
    }

    [Header("基礎結果得分（對應 MinigameEndReason）")]
    public List<ReasonScoreEntry> baseScoreEntries = new List<ReasonScoreEntry>
    {
        new ReasonScoreEntry { reason = MinigameEndReason.GirlLeft,      score = 50,  displayNameKey = "EndReason/GirlLeft" },
        new ReasonScoreEntry { reason = MinigameEndReason.OutOfStamina,  score = 30,  displayNameKey = "EndReason/OutOfStamina" },
        new ReasonScoreEntry { reason = MinigameEndReason.CaughtByGhost, score = 20,  displayNameKey = "EndReason/CaughtByGhost" },
        new ReasonScoreEntry { reason = MinigameEndReason.GirlOvergrown, score = 100, displayNameKey = "EndReason/GirlOvergrown" },
        new ReasonScoreEntry { reason = MinigameEndReason.ManualExit,    score = 40,  displayNameKey = "EndReason/ManualExit" },
        new ReasonScoreEntry { reason = MinigameEndReason.NextMiniGame,  score = 50,  displayNameKey = "EndReason/NextMiniGame" },
        new ReasonScoreEntry { reason = MinigameEndReason.Other,         score = 0,   displayNameKey = "EndReason/Other" },
    };

    // ═══════════════════════════════════════════════
    // 遊戲得分轉換
    // ═══════════════════════════════════════════════
    [Header("遊戲得分轉換")]
    [Tooltip("遊戲得分 (GameScore) 轉換為開發度經驗的比率。例如 0.1 代表 GameScore 的 1/10")]
    [Range(0.01f, 1f)]
    public float gameScoreToExpRatio = 0.1f;

    // ═══════════════════════════════════════════════
    // 額外加分項目
    // ═══════════════════════════════════════════════
    [Header("額外加分項目")]
    [Tooltip("高潮次數 (fsm_LocalOrgasmTimes) 每次加分")]
    public int orgasmTimesBonus = 2;

    [Tooltip("興奮 Lv (fsm_LocalExcitedLv) 每級加分")]
    public int excitedLvBonus = 5;

    [Tooltip("射精次數 (global_ShootTimes) 每次加分")]
    public int shootTimesBonus = 5;

    [Tooltip("超越極限射精次數 (global_OverShootTimes) 每次加分")]
    public int overShootTimesBonus = 10;

    // ═══════════════════════════════════════════════
    // 額外乘倍項目
    // ═══════════════════════════════════════════════
    [Header("額外乘倍項目")]
    [Tooltip("旁邊很危險 (global_DangerScene) 觸發時的乘倍")]
    public int dangerSceneMultiplier = 2;

    [Tooltip("接受邀約 (global_ChallengeAccepted) 觸發時的乘倍")]
    public int challengeAcceptedMultiplier = 2;

    // ═══════════════════════════════════════════════
    // 查詢 API
    // ═══════════════════════════════════════════════

    /// <summary>取得指定 Reason 的基礎得分</summary>
    public int GetBaseScore(MinigameEndReason reason)
    {
        foreach (var entry in baseScoreEntries)
            if (entry.reason == reason) return entry.score;

        Debug.LogWarning($"[MinigameEndReasonConfig] 找不到 Reason '{reason}' 的基礎得分，回傳 0。");
        return 0;
    }

    /// <summary>取得指定 Reason 的多語系顯示名稱</summary>
    public string GetDisplayName(MinigameEndReason reason)
    {
        foreach (var entry in baseScoreEntries)
        {
            if (entry.reason == reason)
            {
                if (string.IsNullOrEmpty(entry.displayNameKey))
                    return reason.ToString();

                string localized = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(entry.displayNameKey);
                if (string.IsNullOrEmpty(localized))
                {
                    Debug.LogWarning($"[MinigameEndReasonConfig] Text Table 找不到 Key: {entry.displayNameKey}，使用 enum 名稱。");
                    return reason.ToString();
                }
                return localized;
            }
        }
        return reason.ToString();
    }

    /// <summary>
    /// 計算本次完整的開發度 exp
    /// = (遊戲得分轉換 + 結束方式得分 + 額外加分，clamp ≥ 0) × 乘倍器
    /// 
    /// 結束方式得分可為負數，但加總後若低於 0 會被 clamp 到 0，避免被乘倍放大負值。
    /// </summary>
    public int CalculateLewdnessExp(
        MinigameEndReason reason,
        int gameScore,
        int excitedLv,
        int orgasmTimes,
        int shootTimes,
        int overShootTimes,
        bool dangerScene,
        bool challengeAccepted)
    {
        int gameScoreConverted = Mathf.RoundToInt(gameScore * gameScoreToExpRatio);
        int reasonScore = GetBaseScore(reason);

        int bonusScore = (orgasmTimes * orgasmTimesBonus)
                       + (excitedLv * excitedLvBonus)
                       + (shootTimes * shootTimesBonus)
                       + (overShootTimes * overShootTimesBonus);

        // 加總後 clamp ≥ 0（避免結束方式扣分後被乘倍放大負值）
        int subtotal = Mathf.Max(0, gameScoreConverted + reasonScore + bonusScore);

        int multiplier = (dangerScene ? dangerSceneMultiplier : 1)
                       * (challengeAccepted ? challengeAcceptedMultiplier : 1);

        int result = subtotal * multiplier;

        Debug.Log($"[MinigameEndReasonConfig] 開發度計算 | " +
                  $"遊戲得分:{gameScore}×{gameScoreToExpRatio}={gameScoreConverted} " +
                  $"+ 結束方式:{reasonScore} " +
                  $"+ 額外:{bonusScore} (高潮{orgasmTimes}×{orgasmTimesBonus} " +
                  $"Lv{excitedLv}×{excitedLvBonus} 射精{shootTimes}×{shootTimesBonus} " +
                  $"超射{overShootTimes}×{overShootTimesBonus}) " +
                  $"→ 小計(clamp≥0):{subtotal} " +
                  $"× 乘倍:{multiplier} (危險:{dangerScene} 邀約:{challengeAccepted}) → {result}");
        return result;
    }
}