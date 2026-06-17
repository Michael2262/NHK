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
    [Tooltip("Resources 下的資料夾路徑，例如 BGCG（其下需有 BG / CG 子資料夾）")]
    public string resourceFolderPath = "BGCG";

    [Header("Mode（服裝 / 變體）設定")]
    [Tooltip("BG 的額外 mode 子資料夾名（放在 BGCG/BG/<mode>/ 下）。根目錄檔案自動視為 Default，不需填。")]
    public List<string> bgModes = new List<string>();
    [Tooltip("CG 的額外 mode 子資料夾名（放在 BGCG/CG/<mode>/ 下）。根目錄檔案自動視為 Default，不需填。")]
    public List<string> cgModes = new List<string>();

    public const string DEFAULT_MODE = "Default";

    // 子資料夾名（BGCG/BG、BGCG/CG）
    private const string BG_SUBFOLDER = "BG";
    private const string CG_SUBFOLDER = "CG";

    // mode -> (去前綴後的 key -> Sprite)
    private Dictionary<string, Dictionary<string, Sprite>> bgModeDict;
    private Dictionary<string, Dictionary<string, Sprite>> cgModeDict;

    // 目前所在 mode（換衣服時用 SetCGMode / SetBGMode 切換）
    public string CurrentBGMode { get; private set; } = DEFAULT_MODE;
    public string CurrentCGMode { get; private set; } = DEFAULT_MODE;

    // 目前畫面上顯示中的 id（供切 mode 時用同一個 id 重抓圖）
    private string currentBGName;
    private string currentCGName;

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
    /// 從 Resources/{resourceFolderPath}/BG、/CG 自動載入所有 Sprite，並依 mode（子資料夾）分桶。
    /// 檔名以 BG_ 開頭 → 背景，去除 "BG_" 前綴作為 key。
    /// 檔名以 CG_ 開頭 → 插圖，去除 "CG_" 前綴作為 key。
    /// 子資料夾名（cgModes / bgModes 所列）= mode 名；直接放在 BG/、CG/ 根目錄的檔案視為 Default mode。
    /// 因此同一個 id 可在不同 mode 子資料夾各放一張，對話腳本用同一個 id 也能依目前 mode 換圖。
    /// </summary>
    private void LoadSpritesFromResources()
    {
        bgModeDict = new Dictionary<string, Dictionary<string, Sprite>>(System.StringComparer.OrdinalIgnoreCase);
        cgModeDict = new Dictionary<string, Dictionary<string, Sprite>>(System.StringComparer.OrdinalIgnoreCase);

        LoadScope(BG_SUBFOLDER, "BG_", bgModes, bgModeDict);
        LoadScope(CG_SUBFOLDER, "CG_", cgModes, cgModeDict);

        Debug.Log($"[CGController] 自動載入完成 — " +
                  $"BG: {CountSprites(bgModeDict)} 張 / {bgModeDict.Count} mode；" +
                  $"CG: {CountSprites(cgModeDict)} 張 / {cgModeDict.Count} mode");
    }

    /// <summary>
    /// 載入單一範圍（BG 或 CG）的所有 mode。
    /// 先載入各宣告的 mode 子資料夾，記下其 Sprite；剩下沒被任何 mode 收走的（= 根目錄檔案）歸 Default。
    /// （Resources.LoadAll 會回傳相同的 Sprite 實例，故可用參照比對來扣除 mode 檔案。）
    /// </summary>
    private void LoadScope(string subFolder, string prefix, List<string> modes,
                           Dictionary<string, Dictionary<string, Sprite>> modeDict)
    {
        string basePath = $"{resourceFolderPath}/{subFolder}";
        var claimed = new HashSet<Sprite>();

        // 1. 各 mode 子資料夾
        if (modes != null)
        {
            foreach (string mode in modes)
            {
                if (string.IsNullOrEmpty(mode)) continue;
                if (string.Equals(mode, DEFAULT_MODE, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[CGController] '{DEFAULT_MODE}' 為保留 mode（根目錄自動歸入），不需列在 {subFolder}Modes。");
                    continue;
                }

                Sprite[] sprites = Resources.LoadAll<Sprite>($"{basePath}/{mode}");
                if (sprites.Length == 0)
                    Debug.LogWarning($"[CGController] mode 子資料夾 '{basePath}/{mode}' 沒有任何 Sprite。");

                var dict = GetOrCreateModeDict(modeDict, mode);
                foreach (Sprite s in sprites)
                {
                    claimed.Add(s);
                    RegisterSprite(dict, s, prefix, mode);
                }
            }
        }

        // 2. Default = 整個範圍遞迴載入後，扣掉已被 mode 收走的
        var defaultDict = GetOrCreateModeDict(modeDict, DEFAULT_MODE);
        foreach (Sprite s in Resources.LoadAll<Sprite>(basePath))
        {
            if (claimed.Contains(s)) continue;
            RegisterSprite(defaultDict, s, prefix, DEFAULT_MODE);
        }
    }

    private void RegisterSprite(Dictionary<string, Sprite> dict, Sprite sprite, string prefix, string mode)
    {
        string fileName = sprite.name; // Unity 自動去除副檔名
        if (!fileName.StartsWith(prefix)) return; // 不是該前綴 → 忽略

        string key = fileName.Substring(prefix.Length);
        if (dict.ContainsKey(key))
        {
            Debug.LogWarning($"[CGController] mode '{mode}' 重複的名稱: {key} (來自 {fileName})");
            return;
        }
        dict[key] = sprite;
    }

    private Dictionary<string, Sprite> GetOrCreateModeDict(
        Dictionary<string, Dictionary<string, Sprite>> modeDict, string mode)
    {
        if (!modeDict.TryGetValue(mode, out var dict))
        {
            dict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
            modeDict[mode] = dict;
        }
        return dict;
    }

    private int CountSprites(Dictionary<string, Dictionary<string, Sprite>> modeDict)
    {
        int total = 0;
        foreach (var dict in modeDict.Values) total += dict.Count;
        return total;
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
        currentBGName = bgName;
    }

    public void HideBG(float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        bgGroupA.DOKill();
        bgGroupB.DOKill();
        bgGroupA.DOFade(0, dur);
        bgGroupB.DOFade(0, dur);
        isBGActive = false;
        currentBGName = null;
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
        currentBGName = bgName;
    }

    /// <summary>
    /// 切換背景 mode（例如日／夜）。若目前有背景，立刻用同一個 id 在新 mode 重抓圖。
    /// 新 mode 找不到該 id 時，GetBGSprite 會自動退回 Default 的同名圖。
    /// </summary>
    public void SetBGMode(string mode, float duration = -1)
    {
        if (string.IsNullOrEmpty(mode)) return;
        if (bgModeDict == null || !bgModeDict.ContainsKey(mode))
        {
            Debug.LogWarning($"[CGController] 沒有 BG mode '{mode}'，維持 '{CurrentBGMode}'。");
            return;
        }
        if (string.Equals(mode, CurrentBGMode, System.StringComparison.OrdinalIgnoreCase)) return;

        CurrentBGMode = mode;
        if (isBGActive && !string.IsNullOrEmpty(currentBGName))
            SwitchBG(currentBGName, 1f, duration);
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
        currentCGName = cgName;
    }

    public void HideCG(float duration = -1)
    {
        float dur = duration < 0 ? defaultFadeDuration : duration;
        cgGroupA.DOKill();
        cgGroupB.DOKill();
        cgGroupA.DOFade(0, dur);
        cgGroupB.DOFade(0, dur).OnComplete(() => ResetZoom(0));
        isCGActive = false;
        currentCGName = null;
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
        currentCGName = cgName;
    }

    /// <summary>
    /// 切換插圖 mode（例如換衣服）。若目前有插圖，立刻用同一個 id 在新 mode 重抓圖，
    /// 對話腳本不必重寫 ShowCG/SwitchCG。新 mode 找不到該 id 時自動退回 Default 的同名圖。
    /// </summary>
    public void SetCGMode(string mode, float duration = -1)
    {
        if (string.IsNullOrEmpty(mode)) return;
        if (cgModeDict == null || !cgModeDict.ContainsKey(mode))
        {
            Debug.LogWarning($"[CGController] 沒有 CG mode '{mode}'，維持 '{CurrentCGMode}'。");
            return;
        }
        if (string.Equals(mode, CurrentCGMode, System.StringComparison.OrdinalIgnoreCase)) return;

        CurrentCGMode = mode;
        if (isCGActive && !string.IsNullOrEmpty(currentCGName))
            SwitchCG(currentCGName, 1f, duration);
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
        return ResolveSprite(bgModeDict, CurrentBGMode, name, "背景");
    }

    private Sprite GetCGSprite(string name)
    {
        return ResolveSprite(cgModeDict, CurrentCGMode, name, "插圖");
    }

    /// <summary>
    /// 依 mode 解析 Sprite：先找目前 mode；找不到退回 Default 的同名圖；都沒有才警告。
    /// （某個 mode 沒幫某張圖畫變體很正常，這時顯示 Default 原版即可。）
    /// </summary>
    private Sprite ResolveSprite(Dictionary<string, Dictionary<string, Sprite>> modeDict,
                                 string mode, string name, string label)
    {
        if (modeDict == null)
        {
            Debug.LogWarning($"[CGController] {label}尚未載入，無法取得: {name}");
            return null;
        }

        // 1. 目前 mode
        if (modeDict.TryGetValue(mode, out var dict) && dict.TryGetValue(name, out Sprite sprite))
            return sprite;

        // 2. 退回 Default 的同名圖
        if (!string.Equals(mode, DEFAULT_MODE, System.StringComparison.OrdinalIgnoreCase) &&
            modeDict.TryGetValue(DEFAULT_MODE, out var defaultDict) &&
            defaultDict.TryGetValue(name, out Sprite defaultSprite))
        {
            return defaultSprite;
        }

        string registered = modeDict.TryGetValue(mode, out var d) ? string.Join(", ", d.Keys) : "(無)";
        Debug.LogWarning($"[CGController] 找不到{label}圖片: {name} (mode: {mode}，該 mode 已註冊: {registered})");
        return null;
    }
}