using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// 將 RequestArchetype 的即時難度文字提供給 Dialogue System Lua。
/// 掛載於 GameStatusService 同一個 GameObject 上。
///
/// Lua 用法：
///   RequestDifficulty("JobInterview")
///   RequestDifficultyBonus("JobInterview", 10)
/// </summary>
public class RequestDifficultyLuaBridge : MonoBehaviour
{
    private const string ArchetypeResourcesFolder = "RequestRoll/";
    private const string DefaultConfigResourcesPath = "RequestRoll/RequestDifficultyDisplayConfig";

    [Header("難度文字設定")]
    [Tooltip("未指定時，會嘗試讀取 Resources/RequestRoll/RequestDifficultyDisplayConfig.asset。")]
    [SerializeField] private RequestDifficultyDisplayConfig displayConfig;

    private void Awake()
    {
        EnsureDisplayConfig();
    }

    private void OnEnable()
    {
        Lua.RegisterFunction(
            "RequestDifficulty",
            this,
            typeof(RequestDifficultyLuaBridge).GetMethod(nameof(RequestDifficulty)));

        Lua.RegisterFunction(
            "RequestDifficultyBonus",
            this,
            typeof(RequestDifficultyLuaBridge).GetMethod(nameof(RequestDifficultyBonus)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("RequestDifficulty");
        Lua.UnregisterFunction("RequestDifficultyBonus");
    }

    /// <summary>
    /// 取得指定 RequestArchetype 對應的本地化難度文字。
    /// Lua：RequestDifficulty("JobInterview")
    /// </summary>
    public string RequestDifficulty(string archetypeID)
    {
        return ResolveDifficultyText(archetypeID, 0);
    }

    /// <summary>
    /// 取得含本次臨時加減值的本地化難度文字。
    /// bonus 必須與 RequestRoll 使用的 bonus 相同，才能維持顯示與擲骰一致。
    /// Lua：RequestDifficultyBonus("JobInterview", 10)
    /// </summary>
    public string RequestDifficultyBonus(string archetypeID, double bonus)
    {
        return ResolveDifficultyText(archetypeID, (int)bonus);
    }

    private string ResolveDifficultyText(string archetypeID, int bonus)
    {
        string id = archetypeID?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[RequestDifficultyLuaBridge] archetypeID 為空，無法取得難度文字。", this);
            return string.Empty;
        }

        if (!EnsureDisplayConfig())
            return string.Empty;

        RequestArchetype archetype = Resources.Load<RequestArchetype>(ArchetypeResourcesFolder + id);
        if (archetype == null)
        {
            Debug.LogWarning(
                $"[RequestDifficultyLuaBridge] 找不到 Resources/{ArchetypeResourcesFolder}{id}.asset。",
                this);
            return string.Empty;
        }

        if (!RequestDifficultyEvaluator.IsSupportedProtagonistDriver(archetype.Driver))
        {
            Debug.LogWarning(
                $"[RequestDifficultyLuaBridge] RequestArchetype '{id}' 使用 {archetype.Driver}，" +
                "目前此 Lua 函數只支援主角 LifePower、Sociality、Dependency。",
                archetype);
            return string.Empty;
        }

        int driverValue = RequestRoller.ResolveDriverValue(archetype.Driver, string.Empty);
        float successRate = RequestDifficultyEvaluator.ComputeEffectiveSuccessRate(
            archetype,
            driverValue,
            bonus);

        string key = RequestDifficultyEvaluator.ResolveTextTableKey(displayConfig, successRate);
        return WrapBlue(Localize(key));
    }

    private bool EnsureDisplayConfig()
    {
        if (displayConfig != null)
            return true;

        displayConfig = Resources.Load<RequestDifficultyDisplayConfig>(DefaultConfigResourcesPath);
        if (displayConfig != null)
            return true;

        Debug.LogError(
            "[RequestDifficultyLuaBridge] 未指定 RequestDifficultyDisplayConfig，且找不到 " +
            $"Resources/{DefaultConfigResourcesPath}.asset。",
            this);
        return false;
    }

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        string text = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[RequestDifficultyLuaBridge] Text Table 找不到 Key: {key}", this);
            return key;
        }

        return text;
    }

    /// <summary>
    /// 與 StoryManager.TB 相同，使用 Dialogue System 的 em6 標記顯示藍色文字。
    /// </summary>
    private static string WrapBlue(string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : $"[em6]{text}[/em6]";
    }
}
