using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 情緒抽選結果模式（決定結果怎麼來的）。
/// </summary>
public enum EmotionResultMode
{
    /// <summary>結果 = 主導情緒（CurrentEmotion，獨立儲存值）。</summary>
    Current,

    /// <summary>結果 = 大宗情緒（DominantEmotion，卡池最多者）。</summary>
    Bulk,

    /// <summary>結果 = 從卡池 10 取 1 隨機抽選。</summary>
    Random,

    /// <summary>結果 = 直接指定，但指定的情緒必須存在於卡池中。失敗時退回 Random。</summary>
    Fake,

    /// <summary>結果 = 直接指定，不管卡池有沒有。</summary>
    Mock,

    /// <summary>結果 = 情緒鄰近抽選。依據 CurrentEmotion 限定可抽到的情緒，再從卡池中隨機。</summary>
    Adjacent
}

/// <summary>
/// 情緒抽選表演模式（決定怎麼演）。
/// </summary>
public enum EmotionShowMode
{
    /// <summary>無表演。</summary>
    None,

    /// <summary>小抽選表演：「考慮中…」+ SmallDrawFace。</summary>
    Small,

    /// <summary>中抽選表演：同小抽選，秒數較長。</summary>
    Medium,

    /// <summary>大抽選表演：兩段式（考慮中 + 十分猶豫）。</summary>
    Big
}

/// <summary>
/// 情緒卡抽選結果。
/// </summary>
[Serializable]
public class EmotionDrawResult
{
    public HeroineEmotionCardType ResultEmotion;
    public EmotionResultMode ResultMode;
    public EmotionShowMode ShowMode;
    public string HeroineID;

    /// <summary>Fake 模式：是否要求了指定結果。</summary>
    public bool FakeRequested;

    /// <summary>Fake 模式：指定是否成功（卡池有該情緒）。</summary>
    public bool FakeSucceeded;

    /// <summary>Fake/Mock 模式：被指定的情緒。</summary>
    public HeroineEmotionCardType RequestedEmotion;

    /// <summary>大抽選第一段表演用的情緒。</summary>
    public HeroineEmotionCardType PerformanceEmotion;
}

/// <summary>
/// NHK 情緒卡抽選機（重構版）。
///
/// 結果模式（EmotionResultMode）與表演模式（EmotionShowMode）完全解耦。
/// 呼叫端可以任意組合，例如：
/// - Random 結果 + Big 表演
/// - Current 結果 + None 表演
/// - Mock 指定結果 + Small 表演
/// </summary>
public class EmotionCardDrawMachine : MonoBehaviour
{
    public static EmotionCardDrawMachine Instance { get; private set; }

    [Header("Singleton")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Heroine Context")]
    [Tooltip("目前抽選用女主角 ID。")]
    [SerializeField] private string currentHeroineID;

    [Header("Global Draw View")]
    [Tooltip("全域抽選演出 View。")]
    [SerializeField] private EmotionCardDrawView drawView;

    [Header("Show Duration Settings")]
    [SerializeField, Min(0f)] private float smallDrawDuration = 0.45f;
    [SerializeField, Min(0f)] private float mediumDrawDuration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawPhase1Duration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawPhase2Duration = 1.5f;

    [Header("Optional Config Override")]
    [SerializeField] private HeroineStatusConfig heroineStatusConfig;

    private Coroutine currentDrawRoutine;
    private bool isDrawing;

    public bool IsDrawing => isDrawing;
    public string CurrentHeroineID => currentHeroineID;
    public EmotionCardDrawView CurrentDrawView => drawView;

    private float SmallDrawDuration => heroineStatusConfig != null ? heroineStatusConfig.SmallDrawDuration : smallDrawDuration;
    private float MediumDrawDuration => mediumDrawDuration;
    private float BigDrawPhase1Duration => bigDrawPhase1Duration;
    private float BigDrawPhase2Duration => heroineStatusConfig != null ? heroineStatusConfig.BigDrawDuration : bigDrawPhase2Duration;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────────────────────
    // Heroine context / View 設定
    // ─────────────────────────────────────────────────────────────

    public void SetCurrentHeroineID(string heroineID) => currentHeroineID = heroineID;
    public void ClearCurrentHeroineID() => currentHeroineID = string.Empty;
    public void SetDrawView(EmotionCardDrawView view) => drawView = view;

    // ─────────────────────────────────────────────────────────────
    // 統一入口
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 統一抽選入口。結果模式與表演模式完全獨立。
    /// </summary>
    /// <param name="heroineID">女主角 ID。</param>
    /// <param name="resultMode">結果怎麼決定。</param>
    /// <param name="showMode">表演怎麼演。</param>
    /// <param name="specifiedEmotion">Fake/Mock 模式下指定的情緒。其他模式忽略。</param>
    /// <param name="onComplete">完成後回呼。</param>
    public void StartDraw(string heroineID, EmotionResultMode resultMode, EmotionShowMode showMode,
        HeroineEmotionCardType specifiedEmotion, Action<EmotionDrawResult> onComplete)
    {
        var heroine = GetHeroineByID(heroineID);
        StartDrawInternal(heroine, heroineID, resultMode, showMode, specifiedEmotion, onComplete);
    }

    /// <summary>使用 currentHeroineID 的簡化版。</summary>
    public void StartDraw(EmotionResultMode resultMode, EmotionShowMode showMode,
        HeroineEmotionCardType specifiedEmotion, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(currentHeroineID, resultMode, showMode, specifiedEmotion, onComplete);
    }

    /// <summary>無指定情緒的簡化版（Fake/Mock 以外的模式）。</summary>
    public void StartDraw(string heroineID, EmotionResultMode resultMode, EmotionShowMode showMode,
        Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroineID, resultMode, showMode, default, onComplete);
    }

    // ─────────────────────────────────────────────────────────────
    // 向下相容入口（保留舊的方法名稱）
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(string heroineID, Action<EmotionDrawResult> onComplete)
        => StartDraw(heroineID, EmotionResultMode.Current, EmotionShowMode.Small, default, onComplete);

    public void StartMediumDraw(string heroineID, Action<EmotionDrawResult> onComplete)
        => StartDraw(heroineID, EmotionResultMode.Current, EmotionShowMode.Medium, default, onComplete);

    public void StartBigDraw(string heroineID, Action<EmotionDrawResult> onComplete)
        => StartDraw(heroineID, EmotionResultMode.Current, EmotionShowMode.Big, default, onComplete);

    public void StartFakeBigDraw(string heroineID, HeroineEmotionCardType fakeResult, bool hasShow, Action<EmotionDrawResult> onComplete)
        => StartDraw(heroineID, EmotionResultMode.Fake, hasShow ? EmotionShowMode.Big : EmotionShowMode.None, fakeResult, onComplete);

    public EmotionDrawResult DrawWithoutShow(string heroineID, EmotionResultMode resultMode, HeroineEmotionCardType specifiedEmotion = default)
    {
        var heroine = GetHeroineByID(heroineID);
        return ResolveResult(heroine, heroineID, resultMode, EmotionShowMode.None, specifiedEmotion);
    }

    public EmotionDrawResult DrawSmallWithoutShow(string heroineID) => DrawWithoutShow(heroineID, EmotionResultMode.Current);
    public EmotionDrawResult DrawMediumWithoutShow(string heroineID) => DrawWithoutShow(heroineID, EmotionResultMode.Current);
    public EmotionDrawResult DrawBigWithoutShow(string heroineID) => DrawWithoutShow(heroineID, EmotionResultMode.Current);
    public EmotionDrawResult DrawFakeBigWithoutShow(string heroineID, HeroineEmotionCardType fakeResult)
        => DrawWithoutShow(heroineID, EmotionResultMode.Fake, fakeResult);

    // ─────────────────────────────────────────────────────────────
    // Result resolution
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得有效的主導情緒。若 CurrentEmotion 為 Normal，改用 DominantEmotion。
    /// </summary>
    private HeroineEmotionCardType GetEffectiveCurrentEmotion(HeroineStatusModel heroine)
    {
        if (heroine == null) return HeroineEmotionCardType.Angry;

        var current = heroine.CurrentEmotion;
        if (current == HeroineEmotionCardType.Normal)
            return heroine.GetDominantEmotion();

        return current;
    }

    private EmotionDrawResult ResolveResult(HeroineStatusModel heroine, string heroineID,
        EmotionResultMode resultMode, EmotionShowMode showMode, HeroineEmotionCardType specifiedEmotion)
    {
        var result = new EmotionDrawResult
        {
            ResultMode = resultMode,
            ShowMode = showMode,
            HeroineID = heroineID
        };

        switch (resultMode)
        {
            case EmotionResultMode.Current:
                // ★ Normal 透通：CurrentEmotion 為 Normal 時改用 DominantEmotion
                result.ResultEmotion = GetEffectiveCurrentEmotion(heroine);
                break;

            case EmotionResultMode.Bulk:
                result.ResultEmotion = heroine != null ? heroine.GetDominantEmotion() : HeroineEmotionCardType.Angry;
                break;

            case EmotionResultMode.Random:
                result.ResultEmotion = GetRandomFromDeck(heroine);
                break;

            case EmotionResultMode.Fake:
                result.FakeRequested = true;
                result.RequestedEmotion = specifiedEmotion;
                bool canFake = heroine != null && heroine.GetCardCount(specifiedEmotion) > 0;
                result.FakeSucceeded = canFake;
                result.ResultEmotion = canFake ? specifiedEmotion : GetRandomFromDeck(heroine);
                break;

            case EmotionResultMode.Mock:
                result.FakeRequested = true;
                result.RequestedEmotion = specifiedEmotion;
                result.FakeSucceeded = true;
                result.ResultEmotion = specifiedEmotion;
                break;

            case EmotionResultMode.Adjacent:
                result.ResultEmotion = GetAdjacentRandomFromDeck(heroine);
                break;

            default:
                result.ResultEmotion = GetEffectiveCurrentEmotion(heroine);
                break;
        }

        // 大抽選表演需要額外一個表演用情緒
        if (showMode == EmotionShowMode.Big)
            result.PerformanceEmotion = GetRandomFromDeck(heroine);
        else
            result.PerformanceEmotion = result.ResultEmotion;

        return result;
    }

    /// <summary>
    /// 從卡池隨機抽一張。防禦性過濾掉 Normal（理論上卡池不該有）。
    /// </summary>
    private HeroineEmotionCardType GetRandomFromDeck(HeroineStatusModel heroine)
    {
        if (heroine == null) return HeroineEmotionCardType.Angry;

        IReadOnlyList<HeroineEmotionCardSaveData> deck = heroine.EmotionDeck;
        if (deck == null || deck.Count == 0) return heroine.GetDominantEmotion();

        // ★ 防禦性過濾：排除 Normal 卡
        var validCards = new List<HeroineEmotionCardSaveData>();
        foreach (var card in deck)
        {
            if (card.Type != HeroineEmotionCardType.Normal)
                validCards.Add(card);
        }

        // 如果過濾後為空（整副牌都是 Normal，不該發生），退回 DominantEmotion
        if (validCards.Count == 0) return heroine.GetDominantEmotion();

        int index = UnityEngine.Random.Range(0, validCards.Count);
        return validCards[index].Type;
    }

    /// <summary>
    /// 情緒鄰近抽選：依據有效主導情緒取得可抽到的情緒清單，
    /// 再從卡池中只抽這些情緒。若卡池中沒有任何鄰近情緒的卡，退回有效主導情緒。
    /// </summary>
    private HeroineEmotionCardType GetAdjacentRandomFromDeck(HeroineStatusModel heroine)
    {
        if (heroine == null) return HeroineEmotionCardType.Angry;

        // ★ Normal 透通：用有效主導情緒作為鄰近中心點
        var effectiveCurrent = GetEffectiveCurrentEmotion(heroine);
        var allowed = GetAdjacentEmotions(effectiveCurrent);

        IReadOnlyList<HeroineEmotionCardSaveData> deck = heroine.EmotionDeck;
        if (deck == null || deck.Count == 0) return effectiveCurrent;

        // 篩選卡池中屬於鄰近情緒的卡（同時排除 Normal）
        var candidates = new List<HeroineEmotionCardSaveData>();
        foreach (var card in deck)
        {
            if (card.Type != HeroineEmotionCardType.Normal && allowed.Contains(card.Type))
                candidates.Add(card);
        }

        if (candidates.Count == 0) return effectiveCurrent;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index].Type;
    }

    /// <summary>
    /// 取得指定主導情緒的鄰近情緒清單（含自身）。
    /// Normal 不應該出現在這裡（呼叫前應已透通），但 default 分支會安全處理。
    /// </summary>
    private static HashSet<HeroineEmotionCardType> GetAdjacentEmotions(HeroineEmotionCardType current)
    {
        switch (current)
        {
            case HeroineEmotionCardType.Angry:
                return new HashSet<HeroineEmotionCardType>
                {
                    HeroineEmotionCardType.Angry,
                    HeroineEmotionCardType.Worried,
                    HeroineEmotionCardType.Disappointed
                };

            case HeroineEmotionCardType.Worried:
                return new HashSet<HeroineEmotionCardType>
                {
                    HeroineEmotionCardType.Worried,
                    HeroineEmotionCardType.Maternal,
                    HeroineEmotionCardType.Angry
                };

            case HeroineEmotionCardType.Maternal:
                return new HashSet<HeroineEmotionCardType>
                {
                    HeroineEmotionCardType.Maternal,
                    HeroineEmotionCardType.Worried,
                    HeroineEmotionCardType.Relaxed,
                    HeroineEmotionCardType.Shy
                };

            case HeroineEmotionCardType.Relaxed:
                return new HashSet<HeroineEmotionCardType>
                {
                    HeroineEmotionCardType.Relaxed,
                    HeroineEmotionCardType.Shy,
                    HeroineEmotionCardType.Maternal
                };

            case HeroineEmotionCardType.Shy:
                return new HashSet<HeroineEmotionCardType>
                {
                    HeroineEmotionCardType.Shy,
                    HeroineEmotionCardType.Relaxed,
                    HeroineEmotionCardType.Maternal,
                    HeroineEmotionCardType.Angry
                };

            case HeroineEmotionCardType.Disappointed:
                return new HashSet<HeroineEmotionCardType>
                {
                    HeroineEmotionCardType.Disappointed,
                    HeroineEmotionCardType.Angry,
                    HeroineEmotionCardType.Worried
                };

            default:
                // Normal 不該走到這裡，但安全起見回傳空集合避免抽到自身
                return new HashSet<HeroineEmotionCardType>();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Internal draw flow
    // ─────────────────────────────────────────────────────────────

    private void StartDrawInternal(HeroineStatusModel heroine, string heroineID,
        EmotionResultMode resultMode, EmotionShowMode showMode,
        HeroineEmotionCardType specifiedEmotion, Action<EmotionDrawResult> onComplete)
    {
        if (heroine == null)
        {
            Debug.LogWarning($"[EmotionCardDrawMachine] StartDraw failed: heroine is null. heroineID={heroineID}");
            var fallback = new EmotionDrawResult
            {
                ResultEmotion = HeroineEmotionCardType.Angry,
                ResultMode = resultMode,
                ShowMode = showMode,
                HeroineID = heroineID
            };
            onComplete?.Invoke(fallback);
            return;
        }

        if (currentDrawRoutine != null)
        {
            StopCoroutine(currentDrawRoutine);
            currentDrawRoutine = null;
            isDrawing = false;
        }

        EmotionDrawResult result = ResolveResult(heroine, heroineID, resultMode, showMode, specifiedEmotion);

        if (showMode == EmotionShowMode.None)
        {
            onComplete?.Invoke(result);
            return;
        }

        currentDrawRoutine = StartCoroutine(DrawRoutine(heroine, result, onComplete));
    }

    private IEnumerator DrawRoutine(HeroineStatusModel heroine, EmotionDrawResult result, Action<EmotionDrawResult> onComplete)
    {
        isDrawing = true;

        if (drawView != null)
        {
            bool completed = false;

            switch (result.ShowMode)
            {
                case EmotionShowMode.Big:
                    drawView.PlayBigDrawShow(
                        result.HeroineID,
                        result.PerformanceEmotion,
                        result.ResultEmotion,
                        BigDrawPhase1Duration,
                        BigDrawPhase2Duration,
                        () => completed = true);
                    break;

                case EmotionShowMode.Medium:
                    drawView.PlayMediumDrawShow(result.HeroineID, result.ResultEmotion, MediumDrawDuration, () => completed = true);
                    break;

                case EmotionShowMode.Small:
                default:
                    drawView.PlaySmallDrawShow(result.HeroineID, result.ResultEmotion, SmallDrawDuration, () => completed = true);
                    break;
            }

            while (!completed) yield return null;
        }
        else
        {
            float duration;
            switch (result.ShowMode)
            {
                case EmotionShowMode.Big: duration = BigDrawPhase1Duration + BigDrawPhase2Duration; break;
                case EmotionShowMode.Medium: duration = MediumDrawDuration; break;
                default: duration = SmallDrawDuration; break;
            }
            if (duration > 0f) yield return new WaitForSeconds(duration);
        }

        isDrawing = false;
        currentDrawRoutine = null;
        onComplete?.Invoke(result);
    }

    // ─────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────

    private HeroineStatusModel GetHeroineByID(string heroineID)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("[EmotionCardDrawMachine] GameStatusService.Instance is null.");
            return null;
        }

        if (GameStatusService.Instance.Heroines == null || GameStatusService.Instance.Heroines.Count == 0)
        {
            Debug.LogWarning("[EmotionCardDrawMachine] GameStatusService.Heroines is empty.");
            return null;
        }

        if (!string.IsNullOrEmpty(heroineID) && GameStatusService.Instance.Heroines.TryGetValue(heroineID, out var heroine))
            return heroine;

        if (!string.IsNullOrEmpty(heroineID))
            Debug.LogWarning($"[EmotionCardDrawMachine] HeroineID not found: {heroineID}. Fallback to first heroine.");

        return GameStatusService.Instance.Heroines.Values.FirstOrDefault();
    }
}
