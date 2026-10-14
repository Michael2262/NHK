using UnityEngine;
using MySpineSystem;
using System.Collections.Generic;

/// <summary>決定每個部位如何從動畫清單中選取動畫。</summary>
public enum PlaybackMode
{
    /// <summary>完全隨機。</summary>
    Random,
    /// <summary>依序播放，到底後循環。</summary>
    Sequential,
    /// <summary>從第一個開始，每次跳 1～2 個，到底後循環。</summary>
    SemiRandom
}

/// <summary>
/// 共用一份表情模式與資料庫，管理多個部位的 Spine 表情。
/// 每次 OnRemoteEvent 呼叫讓各部位獨立倒數；歸零時才抽選並播放。
/// 可掛在 SpineAnimationController 所在物件或其子物件上。
/// </summary>
public class FacialPartController : MonoBehaviour
{
    [System.Serializable]
    public class PartSettings
    {
        [Tooltip("用來查詢資料庫的部位。")]
        public FacialPartType partType;

        [Tooltip("此部位播放動畫的 Spine Track；不同部位建議使用不同 Track，避免互相覆蓋。")]
        public AnimationTrack targetTrack = AnimationTrack.Eye;

        [Tooltip("預設在動畫播放完成時清軌。")]
        public SpineAnimationController.ClearMode clearMode = SpineAnimationController.ClearMode.ClearOnComplete;

        [Tooltip("ClearAfterDelay 的延遲秒數；-1 使用 SpineAnimationController 的預設值。")]
        public float clearDelaySeconds = -1f;

        [Min(1), Tooltip("至少收到幾次 OnRemoteEvent 才觸發。")]
        public int minRemoteCount = 1;

        [Min(1), Tooltip("最多收到幾次 OnRemoteEvent 才觸發，包含此數值。")]
        public int maxRemoteCount = 5;

        [Tooltip("Random：完全隨機；Sequential：依序循環；SemiRandom：從第一個開始，每次跳 1～2 格。")]
        public PlaybackMode playbackMode = PlaybackMode.Random;
    }

    [Header("識別設定")]
    [SerializeField, Tooltip("啟用時自動註冊的唯一 ID，區分大小寫並忽略前後空白。執行中改 ID 請呼叫 SetControllerId；Inspector 修改後需重新啟用。")]
    private string controllerId;

    public string ControllerId => NormalizeId(controllerId);

    // 僅索引啟用中的場景元件；表情模式與播放狀態仍由各元件自己持有。
    private static readonly Dictionary<string, HashSet<FacialPartController>> registry =
        new Dictionary<string, HashSet<FacialPartController>>(System.StringComparer.Ordinal);
    private string registeredId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => registry.Clear();

    private static string NormalizeId(string id) => (id ?? string.Empty).Trim();

    private void OnEnable()
    {
        RegisterId();
    }

    private void OnDisable() => UnregisterId();
    private void OnDestroy() => UnregisterId();

    /// <summary>變更 ID，啟用中的元件會同步更新註冊。</summary>
    public void SetControllerId(string id)
    {
        UnregisterId();
        controllerId = NormalizeId(id);
        if (isActiveAndEnabled) RegisterId();
    }

    private void RegisterId()
    {
        string id = ControllerId;
        if (id.Length == 0)
        {
            Debug.LogWarning("[FacialPartController] ID 為空，無法透過 ID 呼叫；仍可直接呼叫元件方法。", this);
            return;
        }

        if (!registry.TryGetValue(id, out var controllers))
        {
            controllers = new HashSet<FacialPartController>();
            registry.Add(id, controllers);
        }
        controllers.RemoveWhere(item => item == null || !item.isActiveAndEnabled);
        controllers.Add(this);
        registeredId = id;
        if (controllers.Count > 1)
            Debug.LogWarning($"[FacialPartController] ID '{id}' 重複，該 ID 的呼叫暫停，直到僅剩一個啟用元件。", this);
    }

    private void UnregisterId()
    {
        if (registeredId == null) return;
        if (registry.TryGetValue(registeredId, out var controllers))
        {
            controllers.Remove(this);
            if (controllers.Count == 0) registry.Remove(registeredId);
        }
        registeredId = null;
    }

    /// <summary>取得唯一且啟用中的目標。找不到或 ID 重複時警告並回傳 false。</summary>
    public static bool TryGetById(string id, out FacialPartController controller)
    {
        controller = null;
        id = NormalizeId(id);
        if (id.Length > 0 && registry.TryGetValue(id, out var controllers))
        {
            controllers.RemoveWhere(item => item == null || !item.isActiveAndEnabled);
            if (controllers.Count == 1)
            {
                foreach (var item in controllers) controller = item;
                return true;
            }
            if (controllers.Count > 1)
            {
                Debug.LogWarning($"[FacialPartController] ID '{id}' 有多個啟用元件，略過呼叫。");
                return false;
            }
            registry.Remove(id);
        }
        Debug.LogWarning($"[FacialPartController] 找不到 ID '{id}' 的啟用元件。");
        return false;
    }

    /// <summary>依 ID 推進所有部位一次；目標尚未 Start 或依賴失效時不推進。</summary>
    public static bool NotifyRemote(string id)
    {
        if (!TryGetById(id, out var controller)) return false;
        if (!controller.initialized || controller.spineController == null || controller.expressionDatabase == null)
        {
            Debug.LogWarning($"[FacialPartController] ID '{NormalizeId(id)}' 尚未就緒，略過計數。", controller);
            return false;
        }
        controller.OnRemoteEvent();
        return true;
    }

    /// <summary>依 ID 更新共用表情模式，不會立即播放或重置倒數。</summary>
    public static bool SetFacialModeById(string id, HeroineEmotionType selectedFacialMode)
    {
        if (!TryGetById(id, out var controller)) return false;
        if (!System.Enum.IsDefined(typeof(HeroineEmotionType), selectedFacialMode))
        {
            Debug.LogWarning($"[FacialPartController] 無效的表情模式值：{selectedFacialMode}，維持原本表情模式。", controller);
            return false;
        }
        controller.SetFacialMode(selectedFacialMode);
        return true;
    }

    /// <summary>通知所有已註冊 ID；重複 ID 與未就緒目標略過。回傳成功通知數。</summary>
    public static int NotifyRemoteAll()
    {
        // 播放回呼可能停用元件，先複製 ID，避免註冊表於遍歷中被修改。
        var ids = new List<string>(registry.Keys);
        int count = 0;
        foreach (string id in ids)
            if (NotifyRemote(id)) count++;
        return count;
    }

    [Header("共用設定")]
    [Tooltip("所有部位共用的表情資料庫。")]
    public ExpressionDatabase expressionDatabase;

    [SerializeField, Tooltip("所有部位共用的表情模式。變更後於各部位下次計數歸零時使用，不寫入遊戲存檔。")]
    [UnityEngine.Serialization.FormerlySerializedAs("currentEmotion")]
    private HeroineEmotionType facialMode = HeroineEmotionType.Idle;

    [SerializeField, Tooltip("留空時從本物件或父層取得 SpineAnimationController。")]
    private SpineAnimationController spineController;

    [Header("部位清單")]
    [Tooltip("各項目獨立保存 Track、播放模式、隨機計數與播放進度。舊版各元件的部位設定需在 Editor 手動整合至此。")]
    public List<PartSettings> parts = new List<PartSettings>();

    public HeroineEmotionType FacialMode => facialMode;

    // 執行狀態不序列化，不共用於其他 Controller；停用後重新啟用時保留。
    private readonly Dictionary<PartSettings, PartPlaybackState> partStates =
        new Dictionary<PartSettings, PartPlaybackState>();
    private bool initialized;

    /// <summary>只更新共用表情模式，保留各部位目前倒數與各表情模式播放進度。</summary>
    public void SetFacialMode(HeroineEmotionType selectedFacialMode)
    {
        if (!System.Enum.IsDefined(typeof(HeroineEmotionType), selectedFacialMode))
        {
            Debug.LogWarning($"[FacialPartController] 無效的表情模式值：{selectedFacialMode}，維持原本表情模式。", this);
            return;
        }
        facialMode = selectedFacialMode;
    }

    // 供 UnityEvent 或 PlayMaker 的 Call Method / Send Message 直接呼叫。
    public void SetFacialModeIdle() => SetFacialMode(HeroineEmotionType.Idle);
    public void SetFacialModeSad() => SetFacialMode(HeroineEmotionType.Sad);
    public void SetFacialModeAngry() => SetFacialMode(HeroineEmotionType.Angry);
    public void SetFacialModeShy() => SetFacialMode(HeroineEmotionType.Shy);
    public void SetFacialModeSexy() => SetFacialMode(HeroineEmotionType.Sexy);
    public void SetFacialModeOrgasm() => SetFacialMode(HeroineEmotionType.Orgasm);

    private void Start()
    {
        if (expressionDatabase == null)
        {
            Debug.LogError("[FacialPartController] 尚未指定共用的 Expression Database。", this);
            enabled = false;
            return;
        }

        if (spineController == null)
            spineController = GetComponentInParent<SpineAnimationController>();
        if (spineController == null)
        {
            Debug.LogError("[FacialPartController] 找不到 SpineAnimationController。", this);
            enabled = false;
            return;
        }

        if (parts == null || parts.Count == 0)
            Debug.LogWarning("[FacialPartController] 部位清單為空，請在 Inspector 新增部位；舊版設定需手動整合。", this);

        partStates.Clear();
        if (parts != null)
        {
            foreach (var part in parts)
                if (part != null) GetPartState(part);
        }
        initialized = true;
    }

    /// <summary>
    /// 由 FSM、UnityEvent 或其他腳本呼叫一次，各部位各自倒數一次。
    /// 不讀取 FSM 變數；未到觸發次數的部位不播放。
    /// </summary>
    public void OnRemoteEvent()
    {
        if (!initialized || !isActiveAndEnabled || spineController == null ||
            expressionDatabase == null || parts == null) return;

        // 同一輪所有部位使用同一份表情模式，即使播放回呼中另有程式切換表情模式。
        HeroineEmotionType selectedFacialMode = facialMode;
        foreach (var part in parts)
        {
            if (part == null) continue;
            PartPlaybackState state = GetPartState(part);
            if (!state.CountRemote(part)) continue;

            // 資料庫或清單未設定時，只跳過該部位，其他部位繼續處理。
            var emotionSets = expressionDatabase.allEmotionSets;
            var emotionSet = emotionSets == null
                ? null
                : emotionSets.Find(set => set != null && set.emotion == selectedFacialMode);
            var animations = emotionSet == null ? null : emotionSet.GetListForPart(part.partType);
            string animation = state.SelectAnimation(animations, selectedFacialMode, part.playbackMode);
            if (string.IsNullOrEmpty(animation)) continue;

            spineController.PlayAnimation(part.targetTrack, animation, part.clearMode, part.clearDelaySeconds);
        }
    }

    private PartPlaybackState GetPartState(PartSettings part)
    {
        if (!partStates.TryGetValue(part, out var state))
        {
            state = new PartPlaybackState(part);
            partStates.Add(part, state);
        }
        return state;
    }

    /// <summary>單一部位的純 C# 執行狀態，集中處理計數與抽選，不持有場景元件。</summary>
    private sealed class PartPlaybackState
    {
        private int remainingCount;
        private readonly Dictionary<HeroineEmotionType, int> playbackIndices =
            new Dictionary<HeroineEmotionType, int>();

        public PartPlaybackState(PartSettings settings)
        {
            ResetCounter(settings);
        }

        public bool CountRemote(PartSettings settings)
        {
            remainingCount--;
            if (remainingCount > 0) return false;

            // 即使該表情模式沒有動畫，也重新抽取下一輪次數。
            ResetCounter(settings);
            return true;
        }

        public string SelectAnimation(List<string> animations, HeroineEmotionType selectedFacialMode, PlaybackMode mode)
        {
            if (animations == null || animations.Count == 0) return null;

            if (mode != PlaybackMode.Sequential && mode != PlaybackMode.SemiRandom)
                return animations[Random.Range(0, animations.Count)];

            playbackIndices.TryGetValue(selectedFacialMode, out int index);
            index %= animations.Count;
            string selected = animations[index];
            int step = mode == PlaybackMode.Sequential ? 1 : Random.Range(1, 3);
            playbackIndices[selectedFacialMode] = (index + step) % animations.Count;
            return selected;
        }

        private void ResetCounter(PartSettings settings)
        {
            // 防止非法範圍與 max + 1 溢位；正常範圍仍包含上下限。
            int min = Mathf.Clamp(settings.minRemoteCount, 1, int.MaxValue - 1);
            int max = Mathf.Clamp(settings.maxRemoteCount, min, int.MaxValue - 1);
            remainingCount = Random.Range(min, max + 1);
        }
    }
}
