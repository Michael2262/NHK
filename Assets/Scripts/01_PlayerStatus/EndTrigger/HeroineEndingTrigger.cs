using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 女主角結局觸發器（通用版）
/// 監看指定 HeroineID 的開發度（LewdnessLevel）與親密度（BaseAffinityLevel），
/// 當條件達成時觸發 UnityEvent。
///
/// 一個結局掛一個此元件，3 種模式涵蓋常見的「單一條件」與「雙條件」：
///   - LewdnessReached：           LewdnessLevel >= lewdnessThreshold
///   - AffinityReached：           BaseAffinityLevel >= affinityThreshold
///   - LewdnessAndAffinityReached：兩者同時達標
///
/// 偵測方式：訂閱該女主角的 OnLewdnessChanged / OnAffinityChanged 事件，
///           事件來時讀取「當前數值」進行判斷（不依賴事件參數，因為原 Model 中
///           兩個事件的參數有時為 delta、有時為 level，不一致）。
///
/// 跨輪行為：
///   - 條件「達成的瞬間」才觸發；不會在進場時掃描當前狀態
///   - NewGame / LoadGame 後，會重設觸發旗標並重新訂閱新的 Heroine 實例
///     （StartNewGame 會重建整個 Heroines Dictionary，必須重新訂閱）
///   - 因此每一輪遊戲都是獨立的：上一輪已觸發的結局，這一輪只要再次達成條件就會再觸發
///
/// 排他機制（選填）：
///   - 在 skipIfAnyFlag 列表填入「會擋掉本結局」的 ProgressFlag 名稱
///   - 條件達成的瞬間會去檢查這些 Flag，若任一為 true（或數值 > 0），本次跳過
///   - 跳過時「不」消耗 _hasTriggered，所以阻擋 Flag 解除後條件再次達成仍會觸發
///   - 標準用法：在 onEndingTriggered 裡呼叫
///       GameStatusService.Instance.ProgressFlags.AddPersistentFlag("ENDING_X_DONE")
///     讓自己被其他結局排他
///
/// 使用方式：掛在不卸載的場景物件上，在 Inspector 設定：
///           1. heroineID（要監看哪一位女主角）
///           2. checkType（檢查模式）
///           3. 對應的門檻值
///           4. onEndingTriggered（達成時要做什麼）
/// </summary>
public class HeroineEndingTrigger : MonoBehaviour
{
    public enum HeroineCheckType
    {
        LewdnessReached,             // 開發度 >= 門檻
        AffinityReached,             // 親密度 >= 門檻
        LewdnessAndAffinityReached   // 開發度 + 親密度 同時達標
    }

    [Header("目標女主角")]
    [Tooltip("要監看的女主角 ID（對應 HeroineStatusConfig 中的 ID）。")]
    [SerializeField] private string heroineID;

    [Header("觸發條件")]
    [Tooltip("檢查模式：單一條件或雙條件同時達標。")]
    [SerializeField] private HeroineCheckType checkType = HeroineCheckType.LewdnessReached;

    [Tooltip("開發度門檻。當 checkType 包含 Lewdness 時生效。")]
    [SerializeField] private int lewdnessThreshold = 5;

    [Tooltip("親密度門檻。當 checkType 包含 Affinity 時生效。")]
    [SerializeField] private int affinityThreshold = 5;

    [Header("結局事件")]
    [Tooltip("條件達成時觸發。內容請在 Inspector 自行安排（場景切換、CG、對白等）。")]
    [SerializeField] private UnityEvent onEndingTriggered;

    [Header("排他設定（選填）")]
    [Tooltip("檢查 ProgressFlags：列表中只要任一 Flag 為 true（含數值 > 0），本次就跳過、不觸發 UnityEvent。\n" +
             "用途：避免結局互踩。例如此處拉入結局 A、B 的標記 Flag Asset，當 A 或 B 已觸發過時，C 就不會再觸發。\n" +
             "搭配方式：在 onEndingTriggered 裡呼叫 ProgressFlags.AddPersistentFlag(\"ENDING_X_DONE\") 來標記自己已觸發。")]
    [SerializeField] private ProgressFlagDefinition[] skipIfAnyFlag;

    private bool _hasTriggered = false;
    private HeroineStatusModel _subscribedHeroine; // 已訂閱的對象，方便解除訂閱

    private void Start()
    {
        var service = GameStatusService.Instance;
        if (service == null) return;

        SubscribeToCurrentHeroine();

        // 訂閱遊戲重開事件，重設 _hasTriggered 並重新訂閱（讀檔後 Heroines 可能被重建）
        service.OnGameStatusLoaded += OnGameReloaded;
    }

    private void OnDestroy()
    {
        var service = GameStatusService.Instance;
        if (service == null) return;

        UnsubscribeFromCurrentHeroine();
        service.OnGameStatusLoaded -= OnGameReloaded;
    }

    // ── 訂閱管理 ──

    private void SubscribeToCurrentHeroine()
    {
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning($"[HeroineEndingTrigger] heroineID 未設定，無法訂閱事件。物件：{name}");
            return;
        }

        var service = GameStatusService.Instance;
        if (service == null) return;

        if (!service.Heroines.TryGetValue(heroineID, out var heroine) || heroine == null)
        {
            Debug.LogWarning($"[HeroineEndingTrigger] 找不到 ID 為 '{heroineID}' 的女主角。請確認 HeroineStatusConfig 中已定義。");
            return;
        }

        // 依檢查模式只訂閱必要的事件
        switch (checkType)
        {
            case HeroineCheckType.LewdnessReached:
                heroine.OnLewdnessChanged += OnHeroineValueChanged;
                break;
            case HeroineCheckType.AffinityReached:
                heroine.OnAffinityChanged += OnHeroineValueChanged;
                break;
            case HeroineCheckType.LewdnessAndAffinityReached:
                heroine.OnLewdnessChanged += OnHeroineValueChanged;
                heroine.OnAffinityChanged += OnHeroineValueChanged;
                break;
        }

        _subscribedHeroine = heroine;
    }

    private void UnsubscribeFromCurrentHeroine()
    {
        if (_subscribedHeroine == null) return;

        // 解除全部可能的訂閱，避免漏掉
        _subscribedHeroine.OnLewdnessChanged -= OnHeroineValueChanged;
        _subscribedHeroine.OnAffinityChanged -= OnHeroineValueChanged;
        _subscribedHeroine = null;
    }

    // ── 事件回呼 ──

    private void OnHeroineValueChanged(int _)
    {
        // 不依賴事件參數，直接讀屬性當前值判斷
        CheckCondition();
    }

    private void OnGameReloaded()
    {
        // NewGame 或 LoadGame 時：先解除舊訂閱（Heroines 已被重建），重設旗標，重新訂閱
        UnsubscribeFromCurrentHeroine();
        _hasTriggered = false;
        SubscribeToCurrentHeroine();
        Debug.Log($"[HeroineEndingTrigger] 遊戲重載，重新訂閱女主角 '{heroineID}' 的事件。");
    }

    // ── 核心檢查 ──

    private void CheckCondition()
    {
        if (_hasTriggered) return;
        if (_subscribedHeroine == null) return;

        bool conditionMet = false;
        switch (checkType)
        {
            case HeroineCheckType.LewdnessReached:
                conditionMet = _subscribedHeroine.LewdnessLevel >= lewdnessThreshold;
                break;
            case HeroineCheckType.AffinityReached:
                conditionMet = _subscribedHeroine.BaseAffinityLevel >= affinityThreshold;
                break;
            case HeroineCheckType.LewdnessAndAffinityReached:
                conditionMet = _subscribedHeroine.LewdnessLevel >= lewdnessThreshold
                            && _subscribedHeroine.BaseAffinityLevel >= affinityThreshold;
                break;
        }

        if (conditionMet)
        {
            // 排他檢查：若任一 skip flag 為 true，這次直接跳過（不消耗 _hasTriggered，下次仍可再檢）
            string blockedBy = GetBlockingFlag();
            if (blockedBy != null)
            {
                Debug.Log($"[HeroineEndingTrigger] 條件已達成但被排他 Flag '{blockedBy}' 阻擋 - HeroineID={heroineID}, Mode={checkType}");
                return;
            }

            _hasTriggered = true;
            Debug.Log($"[HeroineEndingTrigger] 觸發女主角結局 - HeroineID={heroineID}, " +
                      $"Mode={checkType}, Lewdness={_subscribedHeroine.LewdnessLevel}, " +
                      $"Affinity={_subscribedHeroine.BaseAffinityLevel}");
            onEndingTriggered?.Invoke();
        }
    }

    /// <summary>
    /// 檢查 skipIfAnyFlag 列表中是否有任一 Flag 在 ProgressFlags 中為 true。
    /// 回傳：第一個被命中的 Flag ID（用於 log）；若無任何命中則回傳 null。
    /// </summary>
    private string GetBlockingFlag()
    {
        if (skipIfAnyFlag == null || skipIfAnyFlag.Length == 0) return null;

        var flags = GameStatusService.Instance?.ProgressFlags;
        if (flags == null) return null;

        foreach (var def in skipIfAnyFlag)
        {
            if (def == null) continue;
            string id = def.FlagID;
            if (string.IsNullOrEmpty(id)) continue;
            if (flags.Contains(id)) return id;
        }
        return null;
    }
}