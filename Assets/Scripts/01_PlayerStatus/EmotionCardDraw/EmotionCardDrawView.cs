using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 情緒卡抽選表演 View（精簡版：僅等待時間 + Tachie 立繪變化）。
///
/// 表情一律以 Catalog 內填的 TachieExpressionConfig preset 名稱（ID）套用。
/// - 小/中抽選：套用 SmallDrawExpressionID → 等待 → 完成。
/// - 大抽選兩段：
///   第一段：套用 SmallDrawExpressionID（表演用情緒） → 等待。
///   第二段：套用 BigDrawExpressionID（真正結果） → 等待 → 完成。
/// </summary>
public class EmotionCardDrawView : MonoBehaviour
{
    [Header("Emotion Card Catalog")]
    [Tooltip("情緒卡對照表。讀取 TachieFacePreset。")]
    [SerializeField] private EmotionCardCatalog catalog;

    [Header("Tachie Group ID Override")]
    [Tooltip("若有填，會覆寫 Catalog 的 DefaultTachieGroupID。留空則用 Catalog 的設定。")]
    [SerializeField] private string tachieGroupIDOverride = "";

    [Header("Options")]
    [SerializeField, Min(0f)] private float completeHoldSeconds = 0.25f;

    private Coroutine currentRoutine;

    private string TachieGroupID =>
        !string.IsNullOrEmpty(tachieGroupIDOverride) ? tachieGroupIDOverride :
        catalog != null ? catalog.DefaultTachieGroupID : "Sister";

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>小抽選。</summary>
    public void PlaySmallDrawShow(string heroineID, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(SmallMediumDrawRoutine(finalResult, duration, onComplete));
    }

    /// <summary>中抽選（同小抽選，秒數不同）。</summary>
    public void PlayMediumDrawShow(string heroineID, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(SmallMediumDrawRoutine(finalResult, duration, onComplete));
    }

    /// <summary>大抽選（兩段式）。</summary>
    public void PlayBigDrawShow(string heroineID,
        HeroineEmotionCardType performanceEmotion, HeroineEmotionCardType finalResult,
        float phase1Duration, float phase2Duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(BigDrawRoutine(performanceEmotion, finalResult, phase1Duration, phase2Duration, onComplete));
    }

    /// <summary>
    /// 顯示情緒結果（目前僅等待後回呼，不做演出）。
    /// 保留 API 供 SequencerCommand 等外部呼叫。
    /// </summary>
    public void ShowEmotionResult(string heroineID, HeroineEmotionCardType emotion, float duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(EmotionResultRoutine(duration, onComplete));
    }

    // ─────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────

    private IEnumerator SmallMediumDrawRoutine(HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        ApplySmallDrawFace(finalResult);

        float holdSeconds = Mathf.Max(duration, completeHoldSeconds);
        yield return new WaitForSeconds(holdSeconds);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator BigDrawRoutine(
        HeroineEmotionCardType performanceEmotion, HeroineEmotionCardType finalResult,
        float phase1Duration, float phase2Duration, Action onComplete)
    {
        // ── 第一段：考慮中（表演用情緒的 SmallDrawFace）──
        ApplySmallDrawFace(performanceEmotion);

        if (phase1Duration > 0f)
            yield return new WaitForSeconds(phase1Duration);

        // ── 第二段：猶豫（真正結果的 BigDrawFace）──
        ApplyBigDrawFace(finalResult);

        float hold2 = Mathf.Max(phase2Duration, completeHoldSeconds);
        yield return new WaitForSeconds(hold2);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator EmotionResultRoutine(float duration, Action onComplete)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    // Tachie 切換
    // ─────────────────────────────────────────────────────────────

    private void ApplySmallDrawFace(HeroineEmotionCardType emotion)
    {
        ApplyDrawFace(catalog?.GetSmallDrawFace(emotion), emotion, "SmallDraw");
    }

    private void ApplyBigDrawFace(HeroineEmotionCardType emotion)
    {
        ApplyDrawFace(catalog?.GetBigDrawFace(emotion), emotion, "BigDraw");
    }

    /// <summary>
    /// 套用一組抽選臉：表情走 TachieController.ChangeExpression（吃 TachieExpressionConfig 的 preset 名稱），
    /// 身體走 TachieController.ChangeBody。兩者皆留空時不動該部位。皆套到目前 group（支援連動）。
    /// </summary>
    private void ApplyDrawFace(TachieFace face, HeroineEmotionCardType emotion, string label)
    {
        if (face == null)
        {
            Debug.LogWarning($"[EmotionCardDrawView] {label} face not set for: {emotion}");
            return;
        }

        bool hasExpression = !string.IsNullOrEmpty(face.ExpressionID);
        bool hasBody = !string.IsNullOrEmpty(face.Body);
        if (!hasExpression && !hasBody) return;

        var tc = TachieController.Instance;
        if (tc == null)
        {
            Debug.LogWarning("[EmotionCardDrawView] TachieController.Instance is null.");
            return;
        }

        // ★ 防護：立繪套用失敗（找不到 preset / 圖層 / 例外）只記錄，絕不讓表演協程中斷，
        //   否則 onComplete 不會被呼叫，EmotionDrawDone 不會發出，對話會永久卡住。
        try
        {
            if (hasExpression) tc.ChangeExpression(TachieGroupID, face.ExpressionID);
            if (hasBody) tc.ChangeBody(TachieGroupID, face.Body);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EmotionCardDrawView] {label} 套用表情/身體失敗（emotion={emotion}, " +
                           $"expr='{face.ExpressionID}', body='{face.Body}'）: {e}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────

    private void StopCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
    }
}
