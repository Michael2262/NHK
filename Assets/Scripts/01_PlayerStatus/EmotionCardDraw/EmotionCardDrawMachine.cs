using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// NHK 情緒卡抽選模式。
/// Small：小抽選，有表演，結果為目前主導情緒。
/// Medium：中抽選，有表演，結果邏輯同大抽選。
/// Big：大抽選，有表演，兩段式（表演用情緒 + 真正結果）。
/// FakeBig：造假大抽選，有表演，結果由外部指定。
/// *WithoutShow：無表演版本。
/// </summary>
public enum EmotionDrawMode
{
    Small,
    Medium,
    Big,
    SmallWithoutShow,
    MediumWithoutShow,
    BigWithoutShow,
    FakeBig,
    FakeBigWithoutShow
}

/// <summary>
/// 情緒卡抽選結果。
/// </summary>
[Serializable]
public class EmotionDrawResult
{
    public HeroineEmotionCardType ResultEmotion;
    public EmotionDrawMode DrawMode;
    public bool HasShow;
    public string HeroineID;
    public List<HeroineEmotionCardType> ShowSequence = new List<HeroineEmotionCardType>();

    [Tooltip("是否曾要求造假指定結果。")]
    public bool FakeRequested;

    [Tooltip("造假是否成功。若指定情緒不在卡池中，會是 false，並退回普通大抽選。")]
    public bool FakeSucceeded;

    [Tooltip("造假要求指定的情緒。只有 FakeRequested=true 時有意義。")]
    public HeroineEmotionCardType RequestedFakeEmotion;

    /// <summary>
    /// 大抽選第一段表演用的情緒（僅供表演，不影響實際結果）。
    /// </summary>
    public HeroineEmotionCardType PerformanceEmotion;

    public EmotionDrawResult(
        HeroineEmotionCardType resultEmotion,
        EmotionDrawMode drawMode,
        bool hasShow,
        List<HeroineEmotionCardType> showSequence = null,
        string heroineID = null,
        bool fakeRequested = false,
        bool fakeSucceeded = false,
        HeroineEmotionCardType requestedFakeEmotion = default,
        HeroineEmotionCardType performanceEmotion = default)
    {
        ResultEmotion = resultEmotion;
        DrawMode = drawMode;
        HasShow = hasShow;
        ShowSequence = showSequence ?? new List<HeroineEmotionCardType>();
        HeroineID = heroineID;
        FakeRequested = fakeRequested;
        FakeSucceeded = fakeSucceeded;
        RequestedFakeEmotion = requestedFakeEmotion;
        PerformanceEmotion = performanceEmotion;
    }
}

/// <summary>
/// NHK 情緒卡抽選機（重構版：文字 + Tachie 表演）。
///
/// 責任：
/// 1. 保存目前要抽選的女主角 ID。
/// 2. 接收抽選請求。
/// 3. 決定抽選結果。
/// 4. 大抽選額外抽一個「表演用情緒」。
/// 5. 呼叫 EmotionCardDrawView 播放文字 + Tachie 表演。
/// 6. 等 View 表演完成後，把結果回傳。
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

    [Header("Fallback Show Settings")]
    [SerializeField, Min(0f)] private float smallDrawDuration = 0.45f;
    [SerializeField, Min(0f)] private float mediumDrawDuration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawPhase1Duration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawPhase2Duration = 1.5f;

    [Header("Optional Config Override")]
    [Tooltip("若指定，秒數會優先使用 HeroineStatusConfig 的設定。")]
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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
    // Public entry points：使用目前 currentHeroineID
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(Action<EmotionDrawResult> onComplete) => StartDraw(currentHeroineID, EmotionDrawMode.Small, onComplete);
    public void StartMediumDraw(Action<EmotionDrawResult> onComplete) => StartDraw(currentHeroineID, EmotionDrawMode.Medium, onComplete);
    public void StartBigDraw(Action<EmotionDrawResult> onComplete) => StartDraw(currentHeroineID, EmotionDrawMode.Big, onComplete);
    public void StartFakeBigDraw(HeroineEmotionCardType fakeResult, Action<EmotionDrawResult> onComplete) => StartFakeBigDraw(currentHeroineID, fakeResult, true, onComplete);

    public EmotionDrawResult DrawSmallWithoutShow() => DrawSmallWithoutShow(currentHeroineID);
    public EmotionDrawResult DrawMediumWithoutShow() => DrawMediumWithoutShow(currentHeroineID);
    public EmotionDrawResult DrawBigWithoutShow() => DrawBigWithoutShow(currentHeroineID);
    public EmotionDrawResult DrawFakeBigWithoutShow(HeroineEmotionCardType fakeResult) => DrawFakeBigWithoutShow(currentHeroineID, fakeResult);

    public void StartDraw(EmotionDrawMode mode, Action<EmotionDrawResult> onComplete) => StartDraw(currentHeroineID, mode, onComplete);

    // ─────────────────────────────────────────────────────────────
    // Public entry points：直接傳 HeroineStatusModel
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(HeroineStatusModel heroine, Action<EmotionDrawResult> onComplete) => StartDraw(heroine, EmotionDrawMode.Small, onComplete);
    public void StartMediumDraw(HeroineStatusModel heroine, Action<EmotionDrawResult> onComplete) => StartDraw(heroine, EmotionDrawMode.Medium, onComplete);
    public void StartBigDraw(HeroineStatusModel heroine, Action<EmotionDrawResult> onComplete) => StartDraw(heroine, EmotionDrawMode.Big, onComplete);

    public void StartFakeBigDraw(HeroineStatusModel heroine, HeroineEmotionCardType fakeResult, bool hasShow, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(heroine, hasShow ? EmotionDrawMode.FakeBig : EmotionDrawMode.FakeBigWithoutShow, fakeResult, onComplete);
    }

    public EmotionDrawResult DrawSmallWithoutShow(HeroineStatusModel heroine)
    {
        HeroineEmotionCardType result = GetSmallDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.SmallWithoutShow, false, null, heroine?.HeroineID);
    }

    public EmotionDrawResult DrawMediumWithoutShow(HeroineStatusModel heroine)
    {
        HeroineEmotionCardType result = GetBigDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.MediumWithoutShow, false, null, heroine?.HeroineID);
    }

    public EmotionDrawResult DrawBigWithoutShow(HeroineStatusModel heroine)
    {
        HeroineEmotionCardType result = GetBigDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.BigWithoutShow, false, null, heroine?.HeroineID);
    }

    public EmotionDrawResult DrawFakeBigWithoutShow(HeroineStatusModel heroine, HeroineEmotionCardType fakeResult)
    {
        bool fakeSucceeded = CanForceBigDraw(heroine, fakeResult);
        HeroineEmotionCardType result = fakeSucceeded ? fakeResult : GetBigDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.FakeBigWithoutShow, false, null,
            heroine?.HeroineID, true, fakeSucceeded, fakeResult);
    }

    public void StartDraw(HeroineStatusModel heroine, EmotionDrawMode mode, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(heroine, mode, null, onComplete);
    }

    // ─────────────────────────────────────────────────────────────
    // Public entry points：用 heroineID
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(string heroineID, Action<EmotionDrawResult> onComplete) => StartDraw(heroineID, EmotionDrawMode.Small, onComplete);
    public void StartMediumDraw(string heroineID, Action<EmotionDrawResult> onComplete) => StartDraw(heroineID, EmotionDrawMode.Medium, onComplete);
    public void StartBigDraw(string heroineID, Action<EmotionDrawResult> onComplete) => StartDraw(heroineID, EmotionDrawMode.Big, onComplete);

    public void StartFakeBigDraw(string heroineID, HeroineEmotionCardType fakeResult, bool hasShow, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(GetHeroineByID(heroineID), hasShow ? EmotionDrawMode.FakeBig : EmotionDrawMode.FakeBigWithoutShow, fakeResult, onComplete);
    }

    public EmotionDrawResult DrawSmallWithoutShow(string heroineID) => DrawSmallWithoutShow(GetHeroineByID(heroineID));
    public EmotionDrawResult DrawMediumWithoutShow(string heroineID) => DrawMediumWithoutShow(GetHeroineByID(heroineID));
    public EmotionDrawResult DrawBigWithoutShow(string heroineID) => DrawBigWithoutShow(GetHeroineByID(heroineID));
    public EmotionDrawResult DrawFakeBigWithoutShow(string heroineID, HeroineEmotionCardType fakeResult) => DrawFakeBigWithoutShow(GetHeroineByID(heroineID), fakeResult);

    public void StartDraw(string heroineID, EmotionDrawMode mode, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(GetHeroineByID(heroineID), mode, null, onComplete);
    }

    // ─────────────────────────────────────────────────────────────
    // Draw result decision
    // ─────────────────────────────────────────────────────────────

    public bool CanForceBigDraw(string heroineID, HeroineEmotionCardType fakeResult) => CanForceBigDraw(GetHeroineByID(heroineID), fakeResult);

    public bool CanForceBigDraw(HeroineStatusModel heroine, HeroineEmotionCardType fakeResult)
    {
        return heroine != null && heroine.GetCardCount(fakeResult) > 0;
    }

    private HeroineEmotionCardType GetSmallDrawResult(HeroineStatusModel heroine)
    {
        return heroine != null ? heroine.GetDominantEmotion() : HeroineEmotionCardType.Angry;
    }

    private HeroineEmotionCardType GetBigDrawResult(HeroineStatusModel heroine)
    {
        if (heroine == null) return HeroineEmotionCardType.Angry;

        IReadOnlyList<HeroineEmotionCardSaveData> deck = heroine.EmotionDeck;
        if (deck == null || deck.Count == 0) return heroine.GetDominantEmotion();

        int index = UnityEngine.Random.Range(0, deck.Count);
        return deck[index].Type;
    }

    /// <summary>
    /// 大抽選第一段表演用的情緒：從卡池隨機抽一張（共用中小抽選的資料庫）。
    /// </summary>
    private HeroineEmotionCardType GetPerformanceEmotion(HeroineStatusModel heroine)
    {
        return GetBigDrawResult(heroine);
    }

    private void StartDrawInternal(HeroineStatusModel heroine, EmotionDrawMode mode, HeroineEmotionCardType? fakeResult, Action<EmotionDrawResult> onComplete)
    {
        if (heroine == null)
        {
            Debug.LogWarning($"[EmotionCardDrawMachine] StartDraw failed: heroine is null. currentHeroineID={currentHeroineID}");
            bool hasShowOnNull = mode == EmotionDrawMode.Small || mode == EmotionDrawMode.Medium || mode == EmotionDrawMode.Big || mode == EmotionDrawMode.FakeBig;
            onComplete?.Invoke(new EmotionDrawResult(HeroineEmotionCardType.Angry, mode, hasShowOnNull, null, currentHeroineID));
            return;
        }

        if (currentDrawRoutine != null)
        {
            StopCoroutine(currentDrawRoutine);
            currentDrawRoutine = null;
            isDrawing = false;
        }

        switch (mode)
        {
            case EmotionDrawMode.SmallWithoutShow:
                onComplete?.Invoke(DrawSmallWithoutShow(heroine));
                break;
            case EmotionDrawMode.MediumWithoutShow:
                onComplete?.Invoke(DrawMediumWithoutShow(heroine));
                break;
            case EmotionDrawMode.BigWithoutShow:
                onComplete?.Invoke(DrawBigWithoutShow(heroine));
                break;
            case EmotionDrawMode.FakeBigWithoutShow:
                onComplete?.Invoke(DrawFakeBigWithoutShow(heroine, fakeResult ?? HeroineEmotionCardType.Angry));
                break;
            case EmotionDrawMode.Small:
            case EmotionDrawMode.Medium:
            case EmotionDrawMode.Big:
            case EmotionDrawMode.FakeBig:
                currentDrawRoutine = StartCoroutine(DrawRoutine(heroine, mode, fakeResult, onComplete));
                break;
            default:
                Debug.LogWarning($"[EmotionCardDrawMachine] Unsupported draw mode: {mode}");
                onComplete?.Invoke(DrawSmallWithoutShow(heroine));
                break;
        }
    }

    private IEnumerator DrawRoutine(HeroineStatusModel heroine, EmotionDrawMode mode, HeroineEmotionCardType? fakeResult, Action<EmotionDrawResult> onComplete)
    {
        isDrawing = true;

        bool fakeRequested = mode == EmotionDrawMode.FakeBig;
        HeroineEmotionCardType requestedFakeEmotion = fakeResult ?? HeroineEmotionCardType.Angry;
        bool fakeSucceeded = false;
        HeroineEmotionCardType finalResult;

        // 先決定最終結果
        if (mode == EmotionDrawMode.FakeBig)
        {
            fakeSucceeded = CanForceBigDraw(heroine, requestedFakeEmotion);
            finalResult = fakeSucceeded ? requestedFakeEmotion : GetBigDrawResult(heroine);
        }
        else if (mode == EmotionDrawMode.Big || mode == EmotionDrawMode.Medium)
        {
            finalResult = GetBigDrawResult(heroine);
        }
        else
        {
            finalResult = GetSmallDrawResult(heroine);
        }

        // 大抽選額外抽一個表演用情緒
        bool isBigStyle = mode == EmotionDrawMode.Big || mode == EmotionDrawMode.FakeBig;
        HeroineEmotionCardType performanceEmotion = isBigStyle ? GetPerformanceEmotion(heroine) : finalResult;

        EmotionDrawResult result = new EmotionDrawResult(
            finalResult, mode, true, null,
            heroine.HeroineID,
            fakeRequested, fakeSucceeded, requestedFakeEmotion,
            performanceEmotion);

        // 播放表演
        if (drawView != null)
        {
            bool completed = false;

            if (isBigStyle)
            {
                // 大抽選：兩段式
                drawView.PlayBigDrawShow(
                    heroine.HeroineID,
                    performanceEmotion,
                    finalResult,
                    BigDrawPhase1Duration,
                    BigDrawPhase2Duration,
                    () => completed = true);
            }
            else if (mode == EmotionDrawMode.Medium)
            {
                drawView.PlayMediumDrawShow(heroine.HeroineID, finalResult, MediumDrawDuration, () => completed = true);
            }
            else
            {
                drawView.PlaySmallDrawShow(heroine.HeroineID, finalResult, SmallDrawDuration, () => completed = true);
            }

            while (!completed) yield return null;
        }
        else
        {
            // 沒有 View 時，純等待
            float duration;
            if (isBigStyle)
                duration = BigDrawPhase1Duration + BigDrawPhase2Duration;
            else if (mode == EmotionDrawMode.Medium)
                duration = MediumDrawDuration;
            else
                duration = SmallDrawDuration;

            if (duration > 0f) yield return new WaitForSeconds(duration);
        }

        isDrawing = false;
        currentDrawRoutine = null;
        onComplete?.Invoke(result);
    }

    // ─────────────────────────────────────────────────────────────
    // Show sequence generation（保留向下相容，但新表演不使用 sequence）
    // ─────────────────────────────────────────────────────────────

    public List<HeroineEmotionCardType> GenerateSmallShowSequence(HeroineStatusModel heroine, HeroineEmotionCardType finalResult)
    {
        return new List<HeroineEmotionCardType> { finalResult };
    }

    public List<HeroineEmotionCardType> GenerateMediumShowSequence(HeroineStatusModel heroine, HeroineEmotionCardType finalResult)
    {
        return new List<HeroineEmotionCardType> { finalResult };
    }

    public List<HeroineEmotionCardType> GenerateBigShowSequence(HeroineStatusModel heroine, HeroineEmotionCardType finalResult)
    {
        return new List<HeroineEmotionCardType> { finalResult };
    }

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