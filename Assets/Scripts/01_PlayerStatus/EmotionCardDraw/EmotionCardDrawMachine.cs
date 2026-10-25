using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 情緒抽選結果模式（決定結果怎麼來的）。
/// </summary>
public enum EmotionResultMode
{
    /// <summary>結果 = 主導情緒（CurrentEmotion，獨立儲存值）。可能為 Normal，抽選臉由 Catalog 的 Normal Entry 提供。</summary>
    Current,

    /// <summary>結果 = 大宗情緒（DominantEmotion，卡池最多者）。</summary>
    [InspectorName("已退役：Bulk")]
    Bulk,

    /// <summary>結果 = 從卡池 10 取 1 隨機抽選。</summary>
    [InspectorName("已退役：Random")]
    Random,

    /// <summary>結果 = 直接指定，但指定的情緒必須存在於卡池中。失敗時退回 Random。</summary>
    [InspectorName("已退役：Fake")]
    Fake,

    /// <summary>結果 = 直接指定，不管卡池有沒有。</summary>
    Mock,

    /// <summary>結果 = 情緒鄰近抽選。依據 CurrentEmotion 限定可抽到的情緒，再從卡池中隨機。</summary>
    [InspectorName("已退役：Adjacent")]
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
/// - Current 結果 + Big 表演
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

    [Header("Emotion Card Catalog")]
    [Tooltip("情緒卡對照表。抽選表演的表情 ID / 身體從這裡讀。")]
    [SerializeField] private EmotionCardCatalog catalog;

    [Tooltip("Tachie 切換用的 groupID 覆寫。留空 = 用 Catalog 的 DefaultTachieGroupID。")]
    [SerializeField] private string tachieGroupIDOverride = "";

    [Header("Show Duration Settings")]
    [SerializeField, Min(0f)] private float smallDrawDuration = 0.45f;
    [SerializeField, Min(0f)] private float mediumDrawDuration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawPhase1Duration = 1.0f;
    [SerializeField, Min(0f)] private float bigDrawPhase2Duration = 1.5f;
    [SerializeField, Min(0f)] private float completeHoldSeconds = 0.25f;

    [Header("Optional Config Override")]
    [SerializeField] private HeroineStatusConfig heroineStatusConfig;

    private Coroutine currentDrawRoutine;
    private Coroutine currentResultRoutine;
    private bool isDrawing;

    public bool IsDrawing => isDrawing;
    public string CurrentHeroineID => currentHeroineID;

    private string TachieGroupID =>
        !string.IsNullOrEmpty(tachieGroupIDOverride) ? tachieGroupIDOverride :
        catalog != null ? catalog.DefaultTachieGroupID : "Sister";

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
    public void SetCatalog(EmotionCardCatalog value) => catalog = value;

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

    private EmotionDrawResult ResolveResult(HeroineStatusModel heroine, string heroineID,
        EmotionResultMode resultMode, EmotionShowMode showMode, HeroineEmotionCardType specifiedEmotion)
    {
        if (heroine == null || (resultMode != EmotionResultMode.Current && resultMode != EmotionResultMode.Mock))
        {
            Debug.LogWarning("[EmotionCardDrawMachine] 角色不存在或所選模式依賴已退役卡池；請改用 Current 或 Mock。", this);
            return null;
        }
        if (resultMode == EmotionResultMode.Mock && !Enum.IsDefined(typeof(HeroineEmotionCardType), specifiedEmotion))
            return null;
        var emotion = resultMode == EmotionResultMode.Current ? heroine.CurrentEmotion : specifiedEmotion;
        return new EmotionDrawResult
        {
            HeroineID = heroineID, ResultMode = resultMode, ShowMode = showMode,
            ResultEmotion = emotion, PerformanceEmotion = emotion,
            RequestedEmotion = specifiedEmotion,
            FakeRequested = resultMode == EmotionResultMode.Mock,
            FakeSucceeded = resultMode == EmotionResultMode.Mock
        };
    }

    // ─────────────────────────────────────────────────────────────
    // Internal draw flow
    // ─────────────────────────────────────────────────────────────

    private void StartDrawInternal(HeroineStatusModel heroine, string heroineID,
        EmotionResultMode resultMode, EmotionShowMode showMode,
        HeroineEmotionCardType specifiedEmotion, Action<EmotionDrawResult> onComplete)
    {
        var result = ResolveResult(heroine, heroineID, resultMode, showMode, specifiedEmotion);
        if (result == null) { onComplete?.Invoke(null); return; }
        if (currentDrawRoutine != null)
        {
            StopCoroutine(currentDrawRoutine);
            currentDrawRoutine = null;
            isDrawing = false;
        }
        if (showMode == EmotionShowMode.None) { onComplete?.Invoke(result); return; }
        currentDrawRoutine = StartCoroutine(DrawRoutine(heroine, result, onComplete));
    }

    private IEnumerator DrawRoutine(HeroineStatusModel heroine, EmotionDrawResult result, Action<EmotionDrawResult> onComplete)
    {
        isDrawing = true;

        switch (result.ShowMode)
        {
            case EmotionShowMode.Big:
                // 第一段：考慮中（表演用情緒的 SmallDrawFace）。
                ApplySmallDrawFace(result.PerformanceEmotion);
                if (BigDrawPhase1Duration > 0f)
                    yield return new WaitForSeconds(BigDrawPhase1Duration);

                // 第二段：猶豫（真正結果的 BigDrawFace）。
                ApplyBigDrawFace(result.ResultEmotion);
                yield return new WaitForSeconds(Mathf.Max(BigDrawPhase2Duration, completeHoldSeconds));
                break;

            case EmotionShowMode.Medium:
                ApplySmallDrawFace(result.ResultEmotion);
                yield return new WaitForSeconds(Mathf.Max(MediumDrawDuration, completeHoldSeconds));
                break;

            case EmotionShowMode.Small:
            default:
                ApplySmallDrawFace(result.ResultEmotion);
                yield return new WaitForSeconds(Mathf.Max(SmallDrawDuration, completeHoldSeconds));
                break;
        }

        isDrawing = false;
        currentDrawRoutine = null;
        onComplete?.Invoke(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 情緒結果等待（保留給 EmotionResultMessage；目前無額外立繪演出，僅維持等待語意）
    // ─────────────────────────────────────────────────────────────

    public void ShowEmotionResult(string heroineID, HeroineEmotionCardType emotion, float duration, Action onComplete)
    {
        if (currentResultRoutine != null)
        {
            StopCoroutine(currentResultRoutine);
            currentResultRoutine = null;
        }
        currentResultRoutine = StartCoroutine(EmotionResultRoutine(duration, onComplete));
    }

    private IEnumerator EmotionResultRoutine(float duration, Action onComplete)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        currentResultRoutine = null;
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    // Tachie 立繪套用（原 EmotionCardDrawView 的職責，內化到 Machine）
    // 表情走 TachieController.ChangeExpression（吃 TachieExpressionConfig preset 名稱），
    // 身體走 TachieController.ChangeBody。兩者皆留空則不動該部位。
    // ─────────────────────────────────────────────────────────────

    private void ApplySmallDrawFace(HeroineEmotionCardType emotion)
        => ApplyDrawFace(catalog?.GetSmallDrawFace(emotion), emotion, "SmallDraw");

    private void ApplyBigDrawFace(HeroineEmotionCardType emotion)
        => ApplyDrawFace(catalog?.GetBigDrawFace(emotion), emotion, "BigDraw");

    private void ApplyDrawFace(TachieFace face, HeroineEmotionCardType emotion, string label)
    {
        if (face == null)
        {
            Debug.LogWarning($"[EmotionCardDrawMachine] {label} face not set for: {emotion}");
            return;
        }

        bool hasExpression = !string.IsNullOrEmpty(face.ExpressionID);
        bool hasBody = !string.IsNullOrEmpty(face.Body);
        if (!hasExpression && !hasBody) return;

        var tc = TachieController.Instance;
        if (tc == null)
        {
            Debug.LogWarning("[EmotionCardDrawMachine] TachieController.Instance is null.");
            return;
        }

        // ★ 立繪套用失敗（找不到 preset / 圖層 / 例外）只記錄，絕不讓表演協程中斷，
        //   否則 onComplete 不會被呼叫，RequestDone / EmotionDrawDone 不會發出，對話會永久卡住。
        try
        {
            if (hasExpression) tc.ChangeExpression(TachieGroupID, face.ExpressionID);
            if (hasBody) tc.ChangeBody(TachieGroupID, face.Body);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EmotionCardDrawMachine] {label} 套用表情/身體失敗（emotion={emotion}, " +
                           $"expr='{face.ExpressionID}', body='{face.Body}'）: {e}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────

    private HeroineStatusModel GetHeroineByID(string heroineID)
    {
        return GameStatusService.Instance?.GetHeroine(heroineID);
    }
}
