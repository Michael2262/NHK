using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 情緒卡抽選表演 View（精簡版：僅等待時間 + Tachie 立繪變化）。
///
/// - 小/中抽選：套用 SmallDrawFace → 等待 → 完成。
/// - 大抽選兩段：
///   第一段：套用 SmallDrawFace（表演用情緒） → 等待。
///   第二段：套用 BigDrawFace（真正結果） → 等待 → 完成。
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
        if (catalog == null) return;

        TachieFacePreset face = catalog.GetSmallDrawFace(emotion);
        if (face != null)
            face.ApplyTo(TachieGroupID);
        else
            Debug.LogWarning($"[EmotionCardDrawView] SmallDrawFace not found for: {emotion}");
    }

    private void ApplyBigDrawFace(HeroineEmotionCardType emotion)
    {
        if (catalog == null) return;

        TachieFacePreset face = catalog.GetBigDrawFace(emotion);
        if (face != null)
            face.ApplyTo(TachieGroupID);
        else
            Debug.LogWarning($"[EmotionCardDrawView] BigDrawFace not found for: {emotion}");
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
