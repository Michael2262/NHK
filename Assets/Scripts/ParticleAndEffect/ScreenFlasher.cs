using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 全螢幕閃光控制器（與轉場層 TransitionFader 完全獨立）。
///
/// 用途：
///   對話演出中「閃某顏色一下、閃完自動回到透明」的瞬間效果
///   （例：受傷紅閃、雷電白閃、警告閃爍）。
///
/// 特性：
///   1. 自帶 Canvas + Image，自我啟動（不需手動掛到場景，DontDestroyOnLoad）。
///   2. 單段動畫 0 → 峰值 alpha → 0，可帶峰值停留時間。
///   3. raycastTarget = false，不會擋到任何點擊。
///   4. sortingOrder 預設 998（低於 TransitionFader 的 999），
///      真正轉場壓幕時仍會蓋過閃光，避免互相干擾。
///   5. 重複呼叫會中斷前一次閃光、立即重啟。
/// </summary>
[DefaultExecutionOrder(-900)]
public class ScreenFlasher : MonoBehaviour
{
    public static ScreenFlasher Instance { get; private set; }

    [Header("基本設定")]
    [Tooltip("Canvas 排序層級（預設 998，低於 TransitionFader 的 999）")]
    [SerializeField] private int sortingOrder = 998;

    // 內部元件
    private Image _flashImage;
    private Canvas _canvas;
    private Coroutine _flashRoutine;

    // ============================================================
    // 自動啟動與單例初始化
    // ============================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("ScreenFlasher_Auto");
            go.AddComponent<ScreenFlasher>();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 以程式碼建立並設定所有需要的 UI 元件
    /// </summary>
    private void Initialize()
    {
        // 1. Canvas（螢幕空間覆蓋）
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        // 2. 全螢幕 Image
        var imageObject = new GameObject("FlashImage");
        imageObject.transform.SetParent(transform, false);
        _flashImage = imageObject.AddComponent<Image>();
        _flashImage.raycastTarget = false;

        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        // 3. 初始透明
        _flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    // ============================================================
    // 【核心 API】由 SequencerCommandFlash 等呼叫
    // ============================================================

    /// <summary>
    /// 閃一下指定顏色後自動回到透明。
    /// </summary>
    /// <param name="color">閃光顏色（alpha 會被 peakAlpha 取代）</param>
    /// <param name="peakAlpha">峰值不透明度 (0~1)</param>
    /// <param name="duration">淡入 + 淡出總時間（秒，不含 hold）</param>
    /// <param name="hold">在峰值停留的時間（秒，預設 0）</param>
    public void Flash(Color color, float peakAlpha, float duration, float hold = 0f)
    {
        if (_flashImage == null) return;

        peakAlpha = Mathf.Clamp01(peakAlpha);
        duration = Mathf.Max(0f, duration);
        hold = Mathf.Max(0f, hold);

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, duration, hold));
    }

    private IEnumerator FlashRoutine(Color color, float peakAlpha, float duration, float hold)
    {
        float half = duration * 0.5f;

        // 淡入：0 → 峰值
        yield return FadeAlpha(color, 0f, peakAlpha, half);

        // 峰值停留
        if (hold > 0f)
        {
            float t = 0f;
            while (t < hold)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 淡出：峰值 → 0
        yield return FadeAlpha(color, peakAlpha, 0f, half);

        SetColor(color, 0f);
        _flashRoutine = null;
    }

    private IEnumerator FadeAlpha(Color color, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetColor(color, to);
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            SetColor(color, Mathf.Lerp(from, to, progress));
            yield return null;
        }
        SetColor(color, to);
    }

    private void SetColor(Color color, float alpha)
    {
        _flashImage.color = new Color(color.r, color.g, color.b, alpha);
    }
}
