using UnityEngine;
using HutongGames.PlayMaker; // 引用 PlayMaker
using MySpineSystem; // 為了 AnimationTrack enum
using Spine; // 為了 TrackEntry
using System.Collections.Generic;

/// <summary>
/// 播放模式：決定每個部位如何從動畫清單中選取動畫。
/// </summary>
public enum PlaybackMode
{
    /// <summary>完全隨機 (原始行為)</summary>
    Random,
    /// <summary>依序播放：從第一個到最後一個，再循環回第一個</summary>
    Sequential,
    /// <summary>半隨機：從第一個開始，每次跳 1~2 個，到底後循環回來</summary>
    SemiRandom
}

/// <summary>
/// 【表情部位控制器】
/// 負責監聽 FSM 的 "REMOTE" 事件，並在計數器歸零時，
/// 根據 FSM 的當前情緒，從 ExpressionDatabase 查詢對應的動畫名稱，
/// 最後命令 SpineAnimationController 播放動畫。
/// 
/// * 掛載位置：SpineAnimationController 的子物件上 (例如 Eyes_Logic)。
/// * FSM 需求：FSM 必須有一個 "REMOTE" 事件，並在收到此事件時，
///   使用 "Send Message" (Broadcast to Children) 呼叫本腳本的 "OnRemoteEvent" 函式。
/// </summary>
public class FacialPartController : MonoBehaviour
{
    [Header("1. 依賴組件 (Dependencies)")]
    [UnityEngine.Tooltip("拖入掛載 FSM 的那個 GameObject (例如 G1_女主角1PlayMakerFSM)")]
    public GameObject fsmOwnerObject; // 改成指定 GameObject

    [UnityEngine.Tooltip("我們要監聽的 FSM 的內部名稱 (Fsm Name)")]
    public string targetFsmName = "HeroineEmotionFSM"; // 指定 FSM 名稱

    [UnityEngine.Tooltip("指定要查詢哪一個表情資料庫 (ScriptableObject)")]
    public ExpressionDatabase expressionDatabase;

    [Header("2. 部位身份 (Part Identity)")]
    [UnityEngine.Tooltip("此腳本代表哪一個五官部位？這會影響它去資料庫的哪個清單查詢")]
    public FacialPartType partType; // Enum (Eyes, Mouth, ...)

    [Header("3. Spine 控制 (Spine Control)")]
    [UnityEngine.Tooltip("此部位要在 Spine 的哪一個 Track 上播放")]
    public AnimationTrack targetTrack; // Enum (Track_1, Track_2, ...)

    [UnityEngine.Tooltip("播放動畫時使用的清軌策略 (建議 KeepTrack)")]
    public SpineAnimationController.ClearMode clearMode = SpineAnimationController.ClearMode.KeepTrack;

    [UnityEngine.Tooltip("如果使用 ClearAfterDelay，要延遲幾秒 (使用-1會抓 SpineAnimationController 上的預設值)")]
    public float clearDelaySeconds = -1f;

    [Header("4. 隨機計數邏輯 (Random Counter)")]
    [UnityEngine.Tooltip("最少要聽到幾次 REMOTE 才會觸發")]
    public int minRemoteCount = 1;

    [UnityEngine.Tooltip("最多要聽到幾次 REMOTE 才會觸發")]
    public int maxRemoteCount = 5;

    [Header("5. 播放模式 (Playback Mode)")]
    [UnityEngine.Tooltip("選擇此部位的動畫播放模式：\n" +
        "Random = 完全隨機\n" +
        "Sequential = 從第一個依序播到最後一個再循環\n" +
        "SemiRandom = 從第一個開始，每次跳1~2個，到底後循環")]
    public PlaybackMode playbackMode = PlaybackMode.Random;

    // --- 私有變數 ---
    private PlayMakerFSM myOwnerFSM; // 儲存我們在 Start() 找到的 FSM 參考
    private SpineAnimationController spineController; // 儲存父層的 Spine 動畫控制器
    private int currentRemoteCounter; // 當前的隨機計數器

    // 播放索引追蹤 (用於 Sequential 和 SemiRandom 模式)
    // Key = 情緒類型, Value = 該情緒目前播到第幾個
    private Dictionary<HeroineEmotionType, int> playbackIndexMap = new Dictionary<HeroineEmotionType, int>();

    // 要在 FSM 中查詢的 Enum 變數的「字串名稱」
    private const string EMOTION_VARIABLE_NAME = "MyCurrentEmotion"; //

    void Start()
    {
        // --- 1. 檢查並獲取 FSM ---
        if (fsmOwnerObject == null)
        {
            Debug.LogError($"[FacialPartController] 在 {gameObject.name} 上的 'Fsm Owner Object' 欄位尚未指定！");
            this.enabled = false;
            return;
        }

        // 遍歷 fsmOwnerObject 上的所有 FSM，找出名稱相符的那一個
        PlayMakerFSM[] allFsms = fsmOwnerObject.GetComponents<PlayMakerFSM>();
        myOwnerFSM = null;
        foreach (PlayMakerFSM fsm in allFsms)
        {
            if (fsm.FsmName == targetFsmName)
            {
                myOwnerFSM = fsm; // 找到了！
                break;
            }
        }

        // 檢查是否成功找到
        if (myOwnerFSM == null)
        {
            Debug.LogError($"[FacialPartController] {partType} 在 '{fsmOwnerObject.name}' 上找不到 FSM Name 為 '{targetFsmName}' 的 FSM！");
            this.enabled = false;
            return;
        }

        // --- 2. 檢查資料庫 ---
        if (expressionDatabase == null)
        {
            Debug.LogError($"[FacialPartController] 在 {gameObject.name} 上的 'Expression Database' 欄位尚未指定！");
            this.enabled = false;
            return;
        }

        // --- 3. 獲取 Spine 控制器 ---
        spineController = GetComponentInParent<SpineAnimationController>();
        if (spineController == null)
        {
            Debug.LogError($"[FacialPartController] 在 {gameObject.name} 上找不到父層的 SpineAnimationController！");
            this.enabled = false;
            return;
        }

        // --- 4. 檢查 FSM 變數是否存在 (只在 Start 時檢查一次) ---
        FsmEnum fsmEnumVar = myOwnerFSM.FsmVariables.GetFsmEnum(EMOTION_VARIABLE_NAME);
        if (fsmEnumVar == null)
        {
            Debug.LogError($"[FacialPartController] {partType} 在 FSM '{myOwnerFSM.FsmName}' 上找不到名稱為 '{EMOTION_VARIABLE_NAME}' 的 [Enum] 變數！");
        }
        else if (fsmEnumVar.EnumType != typeof(HeroineEmotionType))
        {
            Debug.LogError($"[FacialPartController] {partType} 變數類型不匹配！ FSM '{myOwnerFSM.FsmName}' 中的 '{EMOTION_VARIABLE_NAME}' 類型是 [{fsmEnumVar.EnumType.FullName}]，但 C# 腳本需要的是 [HeroineEmotionType]。");
        }

        // --- 5. 初始化計數器 ---
        ResetCounter();
    }

    /// <summary>
    /// 【公開】由 FSM 的 "Send Message" Action 呼叫。
    /// 每當 FSM 收到 "REMOTE" 事件時，此函式就會被觸發。
    /// </summary>
    public void OnRemoteEvent()
    {
        //Debug.Log(gameObject.name + " 收到事件了！");
        // 計數器倒數
        currentRemoteCounter--;

        // 檢查計數器是否歸零
        if (currentRemoteCounter <= 0)
        {
            // 歸零，執行核心邏輯
            TriggerExpressionChange();
            // 重置計數器，等待下一次觸發
            ResetCounter();
        }
    }

    /// <summary>
    /// 【核心】觸發表情變更。
    /// 讀取 FSM 狀態 -> 查詢資料庫 -> 播放動畫。
    /// </summary>
    private void TriggerExpressionChange()
    {
        // --- 1. 讀取 FSM 變數 ---
        FsmEnum fsmEnumVar = myOwnerFSM.FsmVariables.GetFsmEnum(EMOTION_VARIABLE_NAME);
        HeroineEmotionType currentEmotion;

        // 安全檢查：確認變數存在且類型正確
        if (fsmEnumVar != null && fsmEnumVar.Value is HeroineEmotionType)
        {
            currentEmotion = (HeroineEmotionType)fsmEnumVar.Value;
        }
        else
        {
            currentEmotion = HeroineEmotionType.Idle;
        }

        // --- 2. 查詢資料庫，根據播放模式選取動畫 ---
        string animToPlay = GetAnimationByPlaybackMode(currentEmotion);

        if (string.IsNullOrEmpty(animToPlay))
        {
            // 查無動畫，悄悄地 return。
            return;
        }

        // --- 3. 播放動畫 ---
        spineController.PlayAnimation(
            targetTrack,
            animToPlay,
            clearMode,
            clearDelaySeconds
        );
    }

    /// <summary>
    /// 根據當前的 PlaybackMode，從資料庫的動畫清單中選取一個動畫。
    /// </summary>
    private string GetAnimationByPlaybackMode(HeroineEmotionType emotion)
    {
        // 先從資料庫取得該情緒 + 該部位的動畫清單
        List<string> animList = expressionDatabase.GetAnimationList(emotion, this.partType);

        if (animList == null || animList.Count == 0)
            return null;

        switch (playbackMode)
        {
            case PlaybackMode.Random:
                // 完全隨機 (與原始行為一致)
                return animList[UnityEngine.Random.Range(0, animList.Count)];

            case PlaybackMode.Sequential:
                {
                    // 取得目前索引 (預設 0)
                    int currentIndex = GetPlaybackIndex(emotion);
                    // 取出動畫
                    string anim = animList[currentIndex % animList.Count];
                    // 索引 +1，存回
                    SetPlaybackIndex(emotion, (currentIndex + 1) % animList.Count);
                    return anim;
                }

            case PlaybackMode.SemiRandom:
                {
                    // 取得目前索引 (預設 0)
                    int currentIndex = GetPlaybackIndex(emotion);
                    // 取出動畫
                    string anim = animList[currentIndex % animList.Count];
                    // 隨機跳 1 或 2 步
                    int step = UnityEngine.Random.Range(1, 3); // 1 或 2
                    SetPlaybackIndex(emotion, (currentIndex + step) % animList.Count);
                    return anim;
                }

            default:
                return animList[UnityEngine.Random.Range(0, animList.Count)];
        }
    }

    /// <summary>
    /// 取得某個情緒的目前播放索引。
    /// </summary>
    private int GetPlaybackIndex(HeroineEmotionType emotion)
    {
        if (playbackIndexMap.TryGetValue(emotion, out int index))
            return index;
        return 0;
    }

    /// <summary>
    /// 設定某個情緒的播放索引。
    /// </summary>
    private void SetPlaybackIndex(HeroineEmotionType emotion, int index)
    {
        playbackIndexMap[emotion] = index;
    }

    /// <summary>
    /// 重置隨機計數器
    /// </summary>
    private void ResetCounter()
    {
        currentRemoteCounter = Random.Range(minRemoteCount, maxRemoteCount + 1);
    }

    void OnDestroy() { }
}