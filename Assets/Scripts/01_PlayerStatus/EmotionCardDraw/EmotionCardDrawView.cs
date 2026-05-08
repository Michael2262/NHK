using System;
using System.Collections;
using UnityEngine;
using TMPro;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 情緒卡抽選表演 View（重構版：文字 + Tachie）。
///
/// 設計:
/// - 小/中抽選：顯示「{角色名} 考慮中…」+ 套用 SmallDrawFace 的完整 Tachie 表情。
/// - 大抽選分兩段：
///   第一段：「考慮中…」+ SmallDrawFace（表演用情緒）。
///   第二段：「十分猶豫…」+ BigDrawFace（真正結果情緒）。
/// - 抽完就結束，不顯示抽到哪個情緒。
///
/// Tachie 切換透過 TachieFacePreset.ApplyTo(groupID) 直接呼叫 TachieController。
/// </summary>
public class EmotionCardDrawView : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("建議指定子物件 root。若未指定,不會停用本 GameObject。")]
    [SerializeField] private GameObject root;

    [Tooltip("顯示「考慮中…」「十分猶豫…」等文字的 TextMeshPro。")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Emotion Card Catalog")]
    [Tooltip("情緒卡對照表。讀取 TachieFacePreset 和 Tachie groupID。")]
    [SerializeField] private EmotionCardCatalog catalog;

    [Header("Tachie Group ID Override")]
    [Tooltip("若有填，會覆寫 Catalog 的 DefaultTachieGroupID。留空則用 Catalog 的設定。")]
    [SerializeField] private string tachieGroupIDOverride = "";

    [Header("Text Table Keys")]
    [Tooltip("小/中抽選文字的 TextTable Key。內容例如「{0} 考慮中…」。")]
    [SerializeField] private string smallDrawTextKey = "Emotion.Select1";

    [Tooltip("大抽選第二段文字的 TextTable Key。內容例如「{0} 十分猶豫…」。")]
    [SerializeField] private string bigDrawTextKey = "Emotion.Select2";

    [Tooltip("情緒結果文字的 TextTable Key。內容例如「{0} 覺得 {1}」。")]
    [SerializeField] private string resultTextKey = "Emotion.Result";

    [Header("Options")]
    [SerializeField] private bool hideAfterComplete = true;
    [SerializeField, Min(0f)] private float completeHoldSeconds = 0.25f;

    [Tooltip("抽選結束後延遲多久才真正隱藏 root。這段時間內若有新的顯示請求進來，root 不會關閉，避免黑底閃爍。")]
    [SerializeField, Min(0f)] private float hideGraceSeconds = 0.15f;

    private Coroutine currentRoutine;
    private Coroutine delayedHideRoutine;

    private string TachieGroupID =>
        !string.IsNullOrEmpty(tachieGroupIDOverride) ? tachieGroupIDOverride :
        catalog != null ? catalog.DefaultTachieGroupID : "Sister";

    private void Awake()
    {
        SetVisible(false);
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 小抽選表演。
    /// </summary>
    public void PlaySmallDrawShow(string heroineID, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(SmallMediumDrawRoutine(heroineID, finalResult, duration, onComplete));
    }

    /// <summary>
    /// 中抽選表演（同小抽選，秒數不同）。
    /// </summary>
    public void PlayMediumDrawShow(string heroineID, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(SmallMediumDrawRoutine(heroineID, finalResult, duration, onComplete));
    }

    /// <summary>
    /// 大抽選表演（兩段式）。
    /// </summary>
    public void PlayBigDrawShow(string heroineID,
        HeroineEmotionCardType performanceEmotion, HeroineEmotionCardType finalResult,
        float phase1Duration, float phase2Duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(BigDrawRoutine(heroineID, performanceEmotion, finalResult, phase1Duration, phase2Duration, onComplete));
    }

    /// <summary>
    /// 單純顯示情緒結果文字：「{角色名} 覺得 {情緒名}」。
    /// 不切換 Tachie，不做抽選，純粹顯示文字一段時間後隱藏。
    /// 供 SequencerCommandEmotionResultMessage 等外部呼叫。
    /// </summary>
    public void ShowEmotionResult(string heroineID, HeroineEmotionCardType emotion, float duration, Action onComplete)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(EmotionResultRoutine(heroineID, emotion, duration, onComplete));
    }

    private IEnumerator EmotionResultRoutine(string heroineID, HeroineEmotionCardType emotion, float duration, Action onComplete)
    {
        SetVisible(true);

        ShowMessageWithEmotion(resultTextKey, heroineID, emotion);

        float holdSeconds = Mathf.Max(duration, completeHoldSeconds);
        yield return new WaitForSeconds(holdSeconds);

        // Emotion.Result 結束後直接消失，不走延遲隱藏
        SetVisible(false);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            // 有新的顯示請求，取消任何正在等待的延遲隱藏
            CancelDelayedHide();

            if (root != null)
                root.SetActive(true);
        }
        else
        {
            if (root != null)
                root.SetActive(false);

            if (messageText != null)
                messageText.text = string.Empty;
        }
    }

    /// <summary>
    /// 延遲隱藏：等 hideGraceSeconds 後才真正關閉 root。
    /// 如果在這段期間有新的 SetVisible(true) 進來，會取消隱藏，root 不閃。
    /// </summary>
    private void HideWithGrace()
    {
        CancelDelayedHide();
        delayedHideRoutine = StartCoroutine(DelayedHideRoutine());
    }

    private IEnumerator DelayedHideRoutine()
    {
        // 先清掉文字，但 root 還開著（黑底還在）
        if (messageText != null)
            messageText.text = string.Empty;

        if (hideGraceSeconds > 0f)
            yield return new WaitForSeconds(hideGraceSeconds);

        // grace 時間到了還沒被取消，真正關掉
        SetVisible(false);
        delayedHideRoutine = null;
    }

    private void CancelDelayedHide()
    {
        if (delayedHideRoutine != null)
        {
            StopCoroutine(delayedHideRoutine);
            delayedHideRoutine = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────

    private IEnumerator SmallMediumDrawRoutine(string heroineID, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        SetVisible(true);

        // 顯示「{角色名} 考慮中…」
        ShowMessage(smallDrawTextKey, heroineID);

        // 套用考慮表情
        ApplySmallDrawFace(finalResult);

        float holdSeconds = Mathf.Max(duration, completeHoldSeconds);
        yield return new WaitForSeconds(holdSeconds);

        if (hideAfterComplete) HideWithGrace();

        currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator BigDrawRoutine(string heroineID,
        HeroineEmotionCardType performanceEmotion, HeroineEmotionCardType finalResult,
        float phase1Duration, float phase2Duration, Action onComplete)
    {
        SetVisible(true);

        // ── 第一段：考慮中（用表演用情緒的 SmallDrawFace）──
        ShowMessage(smallDrawTextKey, heroineID);
        ApplySmallDrawFace(performanceEmotion);

        if (phase1Duration > 0f)
            yield return new WaitForSeconds(phase1Duration);

        // ── 第二段：十分猶豫（用真正結果的 BigDrawFace）──
        ShowMessage(bigDrawTextKey, heroineID);
        ApplyBigDrawFace(finalResult);

        float hold2 = Mathf.Max(phase2Duration, completeHoldSeconds);
        yield return new WaitForSeconds(hold2);

        if (hideAfterComplete) HideWithGrace();

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
    // Text helpers
    // ─────────────────────────────────────────────────────────────

    private void ShowMessage(string textTableKey, string heroineID)
    {
        if (messageText == null) return;

        string heroineName = GetHeroineName(heroineID);
        string template = DialogueManager.GetLocalizedText(textTableKey);

        if (string.IsNullOrEmpty(template))
            template = textTableKey;

        messageText.text = string.Format(template, heroineName);
    }

    private void ShowMessageWithEmotion(string textTableKey, string heroineID, HeroineEmotionCardType emotion)
    {
        if (messageText == null) return;

        string heroineName = GetHeroineName(heroineID);
        string emotionName = GetEmotionName(emotion);
        string template = DialogueManager.GetLocalizedText(textTableKey);

        if (string.IsNullOrEmpty(template))
            template = "{0} 覺得 {1}";

        messageText.text = string.Format(template, heroineName, emotionName);
    }

    private string GetEmotionName(HeroineEmotionCardType type)
    {
        if (catalog != null)
        {
            string textKey = catalog.GetEmotionNameTextKey(type);
            if (!string.IsNullOrEmpty(textKey))
            {
                string localized = DialogueManager.GetLocalizedText(textKey);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }
        }
        return type.ToString();
    }

    private string GetHeroineName(string heroineID)
    {
        if (GameStatusService.Instance == null || GameStatusService.Instance.Heroines == null)
            return heroineID;

        if (GameStatusService.Instance.Heroines.TryGetValue(heroineID, out var heroine) && heroine != null)
        {
            if (!string.IsNullOrEmpty(heroine.NameTextKey))
            {
                string localizedName = DialogueManager.GetLocalizedText(heroine.NameTextKey);
                if (!string.IsNullOrEmpty(localizedName))
                    return localizedName;
            }
            return heroine.Name;
        }

        return heroineID;
    }

    private void StopCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
    }
}