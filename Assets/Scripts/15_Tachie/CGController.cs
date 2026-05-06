using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class CGController : MonoBehaviour
{
    public static CGController Instance { get; private set; }

    [Header("背景控制 (A/B系統)")]
    public CanvasGroup bgGroupA;
    public Image bgImageA;
    public CanvasGroup bgGroupB;
    public Image bgImageB;
    private bool isUsingBGA = true;
    private bool isBGActive = false;

    [Header("插圖控制 (A/B系統 + 容器縮放)")]
    [Tooltip("請將 A 與 B 組件放於此容器內，縮放將對此容器進行")]
    public RectTransform cgContainer;
    public CanvasGroup cgGroupA;
    public Image cgImageA;
    public CanvasGroup cgGroupB;
    public Image cgImageB;
    private bool isUsingCGA = true;
    private bool isCGActive = false;

    [Header("自動載入設定")]
    [Tooltip("Resources 下的資料夾路徑，例如 BGCG")]
    public string resourceFolderPath = "BGCG";

    private Dictionary<string, Sprite> bgSpriteDict = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> cgSpriteDict = new Dictionary<string, Sprite>();

    [Header("全域設定")]
    public float defaultFadeDuration = 0.2f;

    [Header("震動設定")]
    public float defaultShakeStrength = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadSpritesFromResources();
        InitState();
    }

    /// <summary>
    /// 從 Resources/{resourceFolderPath} 自動載入所有 Sprite。
    /// 檔名以 BG_ 開頭 → 註冊為背景，去除 "BG_" 前綴作為 key。
    /// 檔名以 CG_ 開頭 → 註冊為插圖，去除 "CG_" 前綴作為 key。
    /// 其餘檔案忽略。Resources.LoadAll 會自動遞迴掃描子資料夾。
    /// </summary>
    private void LoadSpritesFromResources()
    {
        bgSpriteDict.Clear();
        cgSpriteDict.Clear();

        // Resources.LoadAll 會遞迴載入所有子資料夾中的 Sprite
        Sprite[] allSprites = Resources.LoadAll<Sprite>(resourceFolderPath);

        foreach (Sprite sprite in allSprites)
        {
            string fileName = sprite.name; // Unity 自動去除副檔名

            if (fileName.StartsWith("BG_"))
            {
                string key = fileName.Substring(3); // 去除 "BG_"
                if (!bgSpriteDict.ContainsKey(key))
                {
                    bgSpriteDict[key] = sprite;
                }
                else
                {
                    Debug.LogWarning($"[CGController] 重複的BG名稱: {key} (來自 {fileName})");
                }
            }
            else if (fileName.StartsWith("CG_"))
            {
                string key = fileName.Substring(3); // 去除 "CG_"
                if (!cgSpriteDict.ContainsKey(key))
                {
                    cgSpriteDict[key] = sprite;
                }
                else
                {
                    Debug.LogWarning($"[CGController] 重複的CG名稱: {key} (來自 {fileName})");
                }
            }
            // 不是 BG_ 或 CG_ 開頭的檔案自動忽略
        }

        Debug.Log($"[CGController] 自動載入完成 — BG: {bgSpriteDict.Count} 張, CG: {cgSpriteDict.Count} 張");
    }

    private void InitState()
    {
        bgGroupA.alpha = 0;
        bgGroupB.alpha = 0;
        cgGroupA.alpha = 0;
        cgGroupB.alpha = 0;

        isBGActive = false;
        isCGActive = false;
        isUsingBGA = true;
        isUsingCGA = true;
    }

    // ==========================================
    // 1. 背景系統 (BG System)
    // ==========================================

    public void ShowBG(string bgName, float targetAlpha = 1f, float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        Sprite s = GetBGSprite(bgName);
        if (s == null) return;

        bgImageA.sprite = s;
        bgGroupA.DOKill();
        bgGroupA.DOFade(targetAlpha, dur);

        isUsingBGA = true;
        isBGActive = true;
    }

    public void HideBG(float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        bgGroupA.DOKill();
        bgGroupB.DOKill();
        bgGroupA.DOFade(0, dur);
        bgGroupB.DOFade(0, dur);
        isBGActive = false;
    }

    public void SwitchBG(string bgName, float targetAlpha = 1f, float duration = -1)
    {
        if (!isBGActive)
        {
            ShowBG(bgName, targetAlpha, duration);
            return;
        }

        float dur = duration < 0 ? defaultFadeDuration : duration;
        Sprite s = GetBGSprite(bgName);
        if (s == null) return;

        CanvasGroup active = isUsingBGA ? bgGroupA : bgGroupB;
        CanvasGroup next = isUsingBGA ? bgGroupB : bgGroupA;
        Image nextImg = isUsingBGA ? bgImageB : bgImageA;

        nextImg.sprite = s;
        active.DOKill();
        next.DOKill();

        active.DOFade(0, dur);
        next.DOFade(targetAlpha, dur);

        isUsingBGA = !isUsingBGA;
    }

    public void FadeBGAlpha(float targetAlpha, float duration = 0.5f)
    {
        CanvasGroup active = isUsingBGA ? bgGroupA : bgGroupB;
        active.DOKill();
        active.DOFade(targetAlpha, duration);
    }

    // ==========================================
    // 2. 插圖系統 (CG System)
    // ==========================================

    public void ShowCG(string cgName, float targetAlpha = 1f, float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        Sprite s = GetCGSprite(cgName);
        if (s == null) return;

        cgImageA.sprite = s;
        cgGroupA.DOKill();
        cgGroupA.DOFade(targetAlpha, dur);

        isUsingCGA = true;
        isCGActive = true;
    }

    public void HideCG(float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        cgGroupA.DOKill();
        cgGroupB.DOKill();
        cgGroupA.DOFade(0, dur);
        cgGroupB.DOFade(0, dur).OnComplete(() => ResetZoom(0));
        isCGActive = false;
    }

    public void SwitchCG(string cgName, float targetAlpha = 1f, float duration = -1)
    {
        if (!isCGActive)
        {
            ShowCG(cgName, targetAlpha, duration);
            return;
        }

        float dur = duration < 0 ? defaultFadeDuration : duration;
        Sprite s = GetCGSprite(cgName);
        if (s == null) return;

        CanvasGroup active = isUsingCGA ? cgGroupA : cgGroupB;
        CanvasGroup next = isUsingCGA ? cgGroupB : cgGroupA;
        Image nextImg = isUsingCGA ? cgImageB : cgImageA;

        nextImg.sprite = s;
        active.DOKill();
        next.DOKill();

        active.DOFade(0, dur);
        next.DOFade(targetAlpha, dur);

        isUsingCGA = !isUsingCGA;
    }

    public void FadeCGAlpha(float targetAlpha, float duration = 0.5f)
    {
        CanvasGroup active = isUsingCGA ? cgGroupA : cgGroupB;
        active.DOKill();
        active.DOFade(targetAlpha, duration);
    }

    // ==========================================
    // 3. 縮放與聚焦系統 (Zoom System)
    // ==========================================

    public void ZoomIn(GameObject target, float scale = 1.5f, float duration = 1.0f)
    {
        ApplyZoomEffect(target, scale, duration);
    }

    public void ZoomOut(GameObject target, float scale = 0.5f, float duration = 1.0f)
    {
        ApplyZoomEffect(target, scale, duration);
    }

    public void ResetZoom(float duration = 1.0f)
    {
        cgContainer.DOKill();
        if (duration <= 0)
        {
            cgContainer.localScale = Vector3.one;
            cgContainer.anchoredPosition = Vector2.zero;
            cgContainer.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            cgContainer.DOScale(1f, duration).SetEase(Ease.OutCubic);
            cgContainer.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic);
        }
    }

    public void MoveFocusTo(GameObject target, float duration = 1.0f)
    {
        if (target == null) return;
        Vector2 targetPivot = CalculatePivotFromTarget(target);

        Vector2 size = cgContainer.rect.size;
        Vector2 deltaPivot = cgContainer.pivot - targetPivot;
        Vector3 deltaPos = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * cgContainer.localScale.x;

        cgContainer.DOKill();
        cgContainer.DOAnchorPos(cgContainer.anchoredPosition + (Vector2)deltaPos, duration).SetEase(Ease.InOutQuad);
        cgContainer.pivot = targetPivot;
    }

    // ==========================================
    // 4. 震動
    // ==========================================

    public void ShakeBG(float duration = 0.5f, float strength = -1)
    {
        float s = strength < 0 ? defaultShakeStrength : strength;
        bgGroupA.transform.DOKill(true);
        bgGroupB.transform.DOKill(true);
        bgGroupA.transform.DOShakePosition(duration, s);
        bgGroupB.transform.DOShakePosition(duration, s);
    }

    public void ShakeCG(float duration = 0.5f, float strength = -1)
    {
        float s = strength < 0 ? defaultShakeStrength : strength;
        cgContainer.DOKill(true);
        cgContainer.DOShakePosition(duration, s);
    }

    // --- 內部輔助方法 ---

    private void ApplyZoomEffect(GameObject target, float scale, float duration)
    {
        if (target == null || cgContainer == null) return;

        Vector2 newPivot = CalculatePivotFromTarget(target);
        SetPivotKeepPosition(cgContainer, newPivot);

        cgContainer.DOKill();
        cgContainer.DOScale(scale, duration).SetEase(Ease.OutCubic);
    }

    private Vector2 CalculatePivotFromTarget(GameObject target)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(cgContainer,
            RectTransformUtility.WorldToScreenPoint(null, target.transform.position), null, out localPoint);

        float px = (localPoint.x / cgContainer.rect.width) + cgContainer.pivot.x;
        float py = (localPoint.y / cgContainer.rect.height) + cgContainer.pivot.y;
        return new Vector2(px, py);
    }

    private void SetPivotKeepPosition(RectTransform rect, Vector2 pivot)
    {
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = rect.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rect.localScale.x;
        rect.pivot = pivot;
        rect.anchoredPosition -= (Vector2)deltaPosition;
    }

    private Sprite GetBGSprite(string name)
    {
        if (bgSpriteDict.TryGetValue(name, out Sprite sprite))
            return sprite;

        Debug.LogWarning($"[CGController] 找不到背景圖片: {name} (已註冊: {string.Join(", ", bgSpriteDict.Keys)})");
        return null;
    }

    private Sprite GetCGSprite(string name)
    {
        if (cgSpriteDict.TryGetValue(name, out Sprite sprite))
            return sprite;

        Debug.LogWarning($"[CGController] 找不到插圖圖片: {name} (已註冊: {string.Join(", ", cgSpriteDict.Keys)})");
        return null;
    }
}