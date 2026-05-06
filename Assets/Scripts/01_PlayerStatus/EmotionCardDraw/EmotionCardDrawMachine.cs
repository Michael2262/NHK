using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// NHK 情緒卡抽選模式。
/// Small：小抽選，有表演，結果為目前主導情緒，動畫直接顯示結果。
/// Medium：中抽選，有表演，結果邏輯同大抽選，但使用中抽選專用表演秒數與張數。
/// Big：大抽選，有表演，結果為目前情緒卡池中隨機 1 張。
/// SmallWithoutShow：無表演小抽選。
/// MediumWithoutShow：無表演中抽選。
/// BigWithoutShow：無表演大抽選。
/// FakeBig：造假大抽選，有表演，結果由外部指定；若卡池沒有指定情緒，會自動退回普通大抽選。
/// FakeBigWithoutShow：造假大抽選，無表演；若卡池沒有指定情緒，會自動退回普通無表演大抽選。
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
/// 事件系統收到結果後，再自行決定劇情分歧、數值變化、晚 +1 / 晚 +2 是否開啟。
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

    public EmotionDrawResult(
        HeroineEmotionCardType resultEmotion,
        EmotionDrawMode drawMode,
        bool hasShow,
        List<HeroineEmotionCardType> showSequence = null,
        string heroineID = null,
        bool fakeRequested = false,
        bool fakeSucceeded = false,
        HeroineEmotionCardType requestedFakeEmotion = default)
    {
        ResultEmotion = resultEmotion;
        DrawMode = drawMode;
        HasShow = hasShow;
        ShowSequence = showSequence ?? new List<HeroineEmotionCardType>();
        HeroineID = heroineID;
        FakeRequested = fakeRequested;
        FakeSucceeded = fakeSucceeded;
        RequestedFakeEmotion = requestedFakeEmotion;
    }
}

/// <summary>
/// NHK 情緒卡抽選機。
///
/// 責任：
/// 1. 保存目前要抽選的女主角 ID。
/// 2. 接收抽選請求。
/// 3. 決定小抽選 / 大抽選 / 造假大抽選結果。
/// 4. 需要表演時，呼叫 EmotionCardDrawView 播放抽選表演。
/// 5. 等 View 表演完成後，把結果回傳給事件系統。
///
/// 注意：
/// - 本類不修改 HeroineStatusModel 的卡池。
/// - 本類不修改主角數值。
/// - 本類不決定劇情分歧。
/// - View 只負責表演，結果在表演開始前已決定。
/// </summary>
public class EmotionCardDrawMachine : MonoBehaviour
{
    public static EmotionCardDrawMachine Instance { get; private set; }

    [Header("Singleton")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Heroine Context")]
    [Tooltip("目前抽選用女主角 ID。不修改 ID 時，抽選機會依照此 ID 抽卡。")]
    [SerializeField] private string currentHeroineID;

    [Header("Global Draw View")]
    [Tooltip("全域抽選演出 View。所有有表演抽選預設使用此 View。")]
    [SerializeField] private EmotionCardDrawView drawView;

    [Header("Fallback Show Settings")]
    [SerializeField, Min(0f)] private float smallDrawDuration = 0.45f;
    [SerializeField, Min(0f)] private float mediumDrawDuration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawDuration = 2.0f;
    [SerializeField, Min(1)] private int smallDrawFlipCount = 1;
    [SerializeField, Min(1)] private int mediumDrawFlipCount = 6;
    [SerializeField, Min(1)] private int bigDrawFlipCount = 12;

    [Header("Optional Config Override")]
    [Tooltip("若指定，抽選表演秒數與翻牌數會優先使用 HeroineStatusConfig 的設定。")]
    [SerializeField] private HeroineStatusConfig heroineStatusConfig;

    private Coroutine currentDrawRoutine;
    private bool isDrawing;

    public bool IsDrawing => isDrawing;
    public string CurrentHeroineID => currentHeroineID;
    public EmotionCardDrawView CurrentDrawView => drawView;

    private float SmallDrawDuration => heroineStatusConfig != null ? heroineStatusConfig.SmallDrawDuration : smallDrawDuration;
    private float MediumDrawDuration => mediumDrawDuration;
    private float BigDrawDuration => heroineStatusConfig != null ? heroineStatusConfig.BigDrawDuration : bigDrawDuration;
    private int SmallDrawFlipCount => 1;
    private int MediumDrawFlipCount => Mathf.Max(1, mediumDrawFlipCount);
    private int BigDrawFlipCount => heroineStatusConfig != null ? Mathf.Max(1, heroineStatusConfig.BigDrawFlipCount) : Mathf.Max(1, bigDrawFlipCount);

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

    public void SetCurrentHeroineID(string heroineID)
    {
        currentHeroineID = heroineID;
    }

    public void ClearCurrentHeroineID()
    {
        currentHeroineID = string.Empty;
    }

    public void SetDrawView(EmotionCardDrawView view)
    {
        drawView = view;
    }

    // ─────────────────────────────────────────────────────────────
    // Public entry points：使用目前 currentHeroineID
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(Action<EmotionDrawResult> onComplete)
    {
        StartDraw(currentHeroineID, EmotionDrawMode.Small, onComplete);
    }

    public void StartMediumDraw(Action<EmotionDrawResult> onComplete)
    {
        StartDraw(currentHeroineID, EmotionDrawMode.Medium, onComplete);
    }

    public void StartBigDraw(Action<EmotionDrawResult> onComplete)
    {
        StartDraw(currentHeroineID, EmotionDrawMode.Big, onComplete);
    }

    public void StartFakeBigDraw(HeroineEmotionCardType fakeResult, Action<EmotionDrawResult> onComplete)
    {
        StartFakeBigDraw(currentHeroineID, fakeResult, true, onComplete);
    }

    public EmotionDrawResult DrawSmallWithoutShow()
    {
        return DrawSmallWithoutShow(currentHeroineID);
    }

    public EmotionDrawResult DrawMediumWithoutShow()
    {
        return DrawMediumWithoutShow(currentHeroineID);
    }

    public EmotionDrawResult DrawBigWithoutShow()
    {
        return DrawBigWithoutShow(currentHeroineID);
    }

    public EmotionDrawResult DrawFakeBigWithoutShow(HeroineEmotionCardType fakeResult)
    {
        return DrawFakeBigWithoutShow(currentHeroineID, fakeResult);
    }

    public void StartDraw(EmotionDrawMode mode, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(currentHeroineID, mode, onComplete);
    }

    // ─────────────────────────────────────────────────────────────
    // Public entry points：直接傳 HeroineStatusModel
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(HeroineStatusModel heroine, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroine, EmotionDrawMode.Small, onComplete);
    }

    public void StartMediumDraw(HeroineStatusModel heroine, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroine, EmotionDrawMode.Medium, onComplete);
    }

    public void StartBigDraw(HeroineStatusModel heroine, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroine, EmotionDrawMode.Big, onComplete);
    }

    public void StartFakeBigDraw(HeroineStatusModel heroine, HeroineEmotionCardType fakeResult, bool hasShow, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(heroine, hasShow ? EmotionDrawMode.FakeBig : EmotionDrawMode.FakeBigWithoutShow, fakeResult, onComplete);
    }

    public EmotionDrawResult DrawSmallWithoutShow(HeroineStatusModel heroine)
    {
        HeroineEmotionCardType result = GetSmallDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.SmallWithoutShow, false, null, heroine != null ? heroine.HeroineID : null);
    }

    public EmotionDrawResult DrawMediumWithoutShow(HeroineStatusModel heroine)
    {
        HeroineEmotionCardType result = GetBigDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.MediumWithoutShow, false, null, heroine != null ? heroine.HeroineID : null);
    }

    public EmotionDrawResult DrawBigWithoutShow(HeroineStatusModel heroine)
    {
        HeroineEmotionCardType result = GetBigDrawResult(heroine);
        return new EmotionDrawResult(result, EmotionDrawMode.BigWithoutShow, false, null, heroine != null ? heroine.HeroineID : null);
    }

    public EmotionDrawResult DrawFakeBigWithoutShow(HeroineStatusModel heroine, HeroineEmotionCardType fakeResult)
    {
        bool fakeSucceeded = CanForceBigDraw(heroine, fakeResult);
        HeroineEmotionCardType result = fakeSucceeded ? fakeResult : GetBigDrawResult(heroine);
        return new EmotionDrawResult(
            result,
            EmotionDrawMode.FakeBigWithoutShow,
            false,
            null,
            heroine != null ? heroine.HeroineID : null,
            true,
            fakeSucceeded,
            fakeResult);
    }

    public void StartDraw(HeroineStatusModel heroine, EmotionDrawMode mode, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(heroine, mode, null, onComplete);
    }

    // ─────────────────────────────────────────────────────────────
    // Public entry points：用 heroineID 從 GameStatusService 取多女主角資料
    // ─────────────────────────────────────────────────────────────

    public void StartSmallDraw(string heroineID, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroineID, EmotionDrawMode.Small, onComplete);
    }

    public void StartMediumDraw(string heroineID, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroineID, EmotionDrawMode.Medium, onComplete);
    }

    public void StartBigDraw(string heroineID, Action<EmotionDrawResult> onComplete)
    {
        StartDraw(heroineID, EmotionDrawMode.Big, onComplete);
    }

    public void StartFakeBigDraw(string heroineID, HeroineEmotionCardType fakeResult, bool hasShow, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(GetHeroineByID(heroineID), hasShow ? EmotionDrawMode.FakeBig : EmotionDrawMode.FakeBigWithoutShow, fakeResult, onComplete);
    }

    public EmotionDrawResult DrawSmallWithoutShow(string heroineID)
    {
        return DrawSmallWithoutShow(GetHeroineByID(heroineID));
    }

    public EmotionDrawResult DrawMediumWithoutShow(string heroineID)
    {
        return DrawMediumWithoutShow(GetHeroineByID(heroineID));
    }

    public EmotionDrawResult DrawBigWithoutShow(string heroineID)
    {
        return DrawBigWithoutShow(GetHeroineByID(heroineID));
    }

    public EmotionDrawResult DrawFakeBigWithoutShow(string heroineID, HeroineEmotionCardType fakeResult)
    {
        return DrawFakeBigWithoutShow(GetHeroineByID(heroineID), fakeResult);
    }

    public void StartDraw(string heroineID, EmotionDrawMode mode, Action<EmotionDrawResult> onComplete)
    {
        StartDrawInternal(GetHeroineByID(heroineID), mode, null, onComplete);
    }

    // ─────────────────────────────────────────────────────────────
    // Draw result decision
    // ─────────────────────────────────────────────────────────────

    public bool CanForceBigDraw(string heroineID, HeroineEmotionCardType fakeResult)
    {
        return CanForceBigDraw(GetHeroineByID(heroineID), fakeResult);
    }

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

        // 重要：有表演抽選必須先決定最終結果，再播放表演。
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

        bool isMediumStyle = mode == EmotionDrawMode.Medium;
        bool isBigStyle = mode == EmotionDrawMode.Big || mode == EmotionDrawMode.FakeBig;
        List<HeroineEmotionCardType> sequence = isBigStyle
            ? GenerateBigShowSequence(heroine, finalResult)
            : isMediumStyle
                ? GenerateMediumShowSequence(heroine, finalResult)
                : GenerateSmallShowSequence(heroine, finalResult);

        EmotionDrawResult result = new EmotionDrawResult(
            finalResult,
            mode,
            true,
            sequence,
            heroine.HeroineID,
            fakeRequested,
            fakeSucceeded,
            requestedFakeEmotion);

        if (drawView != null)
        {
            bool completed = false;
            if (isBigStyle)
            {
                drawView.PlayBigDrawShow(sequence, finalResult, BigDrawDuration, () => completed = true);
            }
            else if (isMediumStyle)
            {
                drawView.PlayMediumDrawShow(sequence, finalResult, MediumDrawDuration, () => completed = true);
            }
            else
            {
                drawView.PlaySmallDrawShow(sequence, finalResult, SmallDrawDuration, () => completed = true);
            }

            while (!completed) yield return null;
        }
        else
        {
            float duration = isBigStyle ? BigDrawDuration : isMediumStyle ? MediumDrawDuration : SmallDrawDuration;
            if (duration > 0f) yield return new WaitForSeconds(duration);
        }

        isDrawing = false;
        currentDrawRoutine = null;
        onComplete?.Invoke(result);
    }

    // ─────────────────────────────────────────────────────────────
    // Show sequence generation
    // ─────────────────────────────────────────────────────────────

    public List<HeroineEmotionCardType> GenerateSmallShowSequence(HeroineStatusModel heroine, HeroineEmotionCardType finalResult)
    {
        return new List<HeroineEmotionCardType> { finalResult };
    }

    public List<HeroineEmotionCardType> GenerateMediumShowSequence(HeroineStatusModel heroine, HeroineEmotionCardType finalResult)
    {
        return GenerateShowSequenceFromDeck(heroine, finalResult, MediumDrawFlipCount);
    }

    public List<HeroineEmotionCardType> GenerateBigShowSequence(HeroineStatusModel heroine, HeroineEmotionCardType finalResult)
    {
        return GenerateShowSequenceFromDeck(heroine, finalResult, BigDrawFlipCount);
    }

    private List<HeroineEmotionCardType> GenerateShowSequenceFromDeck(HeroineStatusModel heroine, HeroineEmotionCardType finalResult, int flipCount)
    {
        List<HeroineEmotionCardType> sequence = new List<HeroineEmotionCardType>();
        flipCount = Mathf.Max(1, flipCount);

        IReadOnlyList<HeroineEmotionCardSaveData> deck = heroine != null ? heroine.EmotionDeck : null;
        if (deck != null && deck.Count > 0)
        {
            for (int i = 0; i < flipCount - 1; i++)
            {
                int index = UnityEngine.Random.Range(0, deck.Count);
                sequence.Add(deck[index].Type);
            }
        }
        else
        {
            HeroineEmotionCardType[] allTypes = GetAllEmotionCardTypes();
            for (int i = 0; i < flipCount - 1; i++)
            {
                sequence.Add(allTypes[UnityEngine.Random.Range(0, allTypes.Length)]);
            }
        }

        sequence.Add(finalResult);
        return sequence;
    }

    private HeroineEmotionCardType[] GetAllEmotionCardTypes()
    {
        return (HeroineEmotionCardType[])Enum.GetValues(typeof(HeroineEmotionCardType));
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
        {
            return heroine;
        }

        if (!string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning($"[EmotionCardDrawMachine] HeroineID not found: {heroineID}. Fallback to first heroine.");
        }

        return GameStatusService.Instance.Heroines.Values.FirstOrDefault();
    }
}
