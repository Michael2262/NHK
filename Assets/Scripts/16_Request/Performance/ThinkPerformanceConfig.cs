using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Think 系列表演：從 EmotionCardCatalog 讀臉與秒數的兩段式立繪（不經過 EmotionCardDrawMachine）。
///
/// 三顆資產共用同一顆 Catalog，只差 phaseMode：
///   Think（Both）    → 第一段掂量臉 → 第二段猶豫臉
///   ThinkPhase1      → 只演第一段（SmallDrawFace + phase1 秒數）
///   ThinkPhase2      → 只演第二段（BigDrawFace  + phase2 秒數）
///
/// 情緒來源：呼叫端有帶情緒參數就用參數（如 Angry）；沒帶就用女主的「當前情緒」。
/// 所以 Request(sister, Think) 兩段都吃當前情緒，跟舊大抽選一致。
/// </summary>
[CreateAssetMenu(menuName = "NHK/Request Performance/Think", fileName = "Think")]
public class ThinkPerformanceConfig : RequestPerformanceConfig
{
    public enum PhaseMode { Both, Phase1, Phase2 }

    [Header("資料來源")]
    [Tooltip("臉與秒數都從這顆 Catalog 讀（Think / ThinkPhase1 / ThinkPhase2 共用同一顆）。")]
    [SerializeField] private EmotionCardCatalog catalog;

    [Tooltip("立繪切換用 groupID。留空 = 用 Catalog 的 DefaultTachieGroupID。")]
    [SerializeField] private string tachieGroupIDOverride = "";

    [Header("這顆資產演哪些階段")]
    [SerializeField] private PhaseMode phaseMode = PhaseMode.Both;

    public override void Play(MonoBehaviour host, string heroineID, bool pass, string[] args, Action onDone)
    {
        host.StartCoroutine(Run(heroineID, args, onDone));
    }

    private IEnumerator Run(string heroineID, string[] args, Action onDone)
    {
        if (catalog == null)
        {
            Debug.LogWarning("[ThinkPerformance] 未指定 EmotionCardCatalog，直接結束。");
            onDone?.Invoke();
            yield break;
        }

        HeroineEmotionCardType emotion = ResolveEmotion(heroineID, args);
        string group = string.IsNullOrEmpty(tachieGroupIDOverride)
            ? catalog.DefaultTachieGroupID
            : tachieGroupIDOverride;

        // 第一段：掂量臉（SmallDrawFace）
        if (phaseMode == PhaseMode.Both || phaseMode == PhaseMode.Phase1)
        {
            RequestTachieUtil.Apply(catalog.GetSmallDrawFace(emotion), group);
            if (catalog.Phase1Duration > 0f) yield return new WaitForSeconds(catalog.Phase1Duration);
        }

        // 第二段：猶豫臉（BigDrawFace）
        if (phaseMode == PhaseMode.Both || phaseMode == PhaseMode.Phase2)
        {
            RequestTachieUtil.Apply(catalog.GetBigDrawFace(emotion), group);
            if (catalog.Phase2Duration > 0f) yield return new WaitForSeconds(catalog.Phase2Duration);
        }

        onDone?.Invoke();
    }

    /// <summary>情緒：有帶參數就用參數，否則用女主的當前情緒（CurrentEmotion）。</summary>
    private HeroineEmotionCardType ResolveEmotion(string heroineID, string[] args)
    {
        string arg = (args != null && args.Length > 0) ? args[0] : null;
        if (!string.IsNullOrWhiteSpace(arg) && TryParseEmotion(arg, out var parsed))
            return parsed;

        var svc = GameStatusService.Instance;
        if (svc != null && svc.Heroines != null && !string.IsNullOrEmpty(heroineID)
            && svc.Heroines.TryGetValue(heroineID, out var heroine) && heroine != null)
            return heroine.CurrentEmotion;

        return HeroineEmotionCardType.Normal;
    }

    private static bool TryParseEmotion(string raw, out HeroineEmotionCardType emotion)
    {
        raw = raw.Trim();
        switch (raw)
        {
            case "普通": case "Normal": emotion = HeroineEmotionCardType.Normal; return true;
            case "生氣": case "Angry": emotion = HeroineEmotionCardType.Angry; return true;
            case "害羞": case "Shy": emotion = HeroineEmotionCardType.Shy; return true;
            case "擔心": case "Worried": emotion = HeroineEmotionCardType.Worried; return true;
            case "母性": case "Maternal": emotion = HeroineEmotionCardType.Maternal; return true;
            case "放鬆": case "Relaxed": emotion = HeroineEmotionCardType.Relaxed; return true;
            case "失望": case "Disappointed": emotion = HeroineEmotionCardType.Disappointed; return true;
        }
        if (Enum.TryParse(raw, true, out emotion)) return true;

        Debug.LogWarning($"[ThinkPerformance] 無法解析情緒：{raw}，改用當前情緒/預設。");
        emotion = HeroineEmotionCardType.Normal;
        return false;
    }
}
