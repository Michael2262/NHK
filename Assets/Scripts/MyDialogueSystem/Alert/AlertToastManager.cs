using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 系統公告（Toast）堆疊管理器。
///
/// 取代 Pixel Crushers 單一 Alert 面板「所有公告共用一個外框」的行為：
/// 每一則公告都獨立 Instantiate 一個 toast prefab，堆進帶 VerticalLayoutGroup 的容器中，
/// 到期後各自淡出銷毀。
///
/// 行為：
///   - 新公告出現在「最下面」，往下依序排（舊的在上、新的在下）。
///   - 即使同一幀內一齊觸發，也會用 <see cref="staggerInterval"/> 拉開一點時間差逐一冒出。
///   - 同時最多顯示 <see cref="maxVisible"/> 個，超過就把最舊的擠掉。
///
/// 掛法（Editor）：放在存放公告的 Canvas 底下，把 toast prefab 與容器參照拖進來。
/// 沒掛這個管理器的場景，StoryManager 會自動 fallback 回原生 DialogueManager.ShowAlert。
/// </summary>
public class AlertToastManager : MonoBehaviour
{
    private static AlertToastManager _instance;

    public static AlertToastManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<AlertToastManager>();
            return _instance;
        }
    }

    [Header("參照")]
    [Tooltip("單一公告的 prefab（需掛 AlertToast 元件）")]
    [SerializeField] private AlertToast toastPrefab;

    [Tooltip("容納 toast 的容器（建議掛 VerticalLayoutGroup + ContentSizeFitter）")]
    [SerializeField] private RectTransform container;

    [Header("堆疊行為")]
    [Tooltip("同時最多顯示幾個，超過就把最舊的擠掉")]
    [SerializeField] private int maxVisible = 5;

    [Tooltip("同一批公告之間逐一冒出的時間差（秒）。0 = 同一幀全部一起出現")]
    [SerializeField] private float staggerInterval = 0.12f;

    // 等待冒出的公告（實作逐一交錯出現）
    private readonly Queue<PendingToast> _pending = new Queue<PendingToast>();

    // 目前在畫面上的 toast，index 0 = 最舊（供上限管理擠掉最舊者）
    private readonly List<AlertToast> _active = new List<AlertToast>();

    private Coroutine _releaseRoutine;

    // 到期後等待「錯開淡出」的佇列，讓消失跟出現共用同一個 staggerInterval（逐一消失而非一起消失）
    private readonly Queue<AlertToast> _dismissPending = new Queue<AlertToast>();
    private Coroutine _dismissRoutine;

    private struct PendingToast
    {
        public string message;
        public float duration;
    }

    // 自我註冊：不論與 StoryManager 誰先生成、在哪個場景，只要這顆被啟用就登記為當前實例。
    // 這樣 StoryManager 不必依賴生成順序，也不必每次都 FindFirstObjectByType。
    private void OnEnable()
    {
        _instance = this;
    }

    private void OnDisable()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// 顯示一則公告。會排進佇列，依 staggerInterval 逐一冒出。
    /// </summary>
    public void Show(string message, float duration)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (toastPrefab == null || container == null)
        {
            // 參照沒設好時的保底：直接走原生 Alert，避免整個公告系統靜默失效
            Debug.LogWarning("[AlertToastManager] 未設定 toastPrefab / container，改用原生 DialogueManager.ShowAlert");
            PixelCrushers.DialogueSystem.DialogueManager.ShowAlert(message, duration);
            return;
        }

        _pending.Enqueue(new PendingToast { message = message, duration = duration });

        if (staggerInterval <= 0f)
        {
            // 不需要時間差：立即全部冒出
            while (_pending.Count > 0) SpawnNext();
        }
        else if (_releaseRoutine == null)
        {
            _releaseRoutine = StartCoroutine(ReleaseRoutine());
        }
    }

    private IEnumerator ReleaseRoutine()
    {
        // 第一則立刻出現，之後每隔 staggerInterval 冒出一則。
        // ★ 生成後「無條件」等一個間隔再處理下一則（即使當下佇列已空）：
        //   公告是一則一則進來的，若佇列一空就結束協程，同一幀稍後進來的公告會各自立刻生成，
        //   staggerInterval 等於沒作用。讓協程持續活著，才能把後續公告錯開。
        while (_pending.Count > 0)
        {
            SpawnNext();
            yield return new WaitForSecondsRealtime(staggerInterval);
        }
        _releaseRoutine = null;
    }

    private void SpawnNext()
    {
        if (_pending.Count == 0) return;
        PendingToast p = _pending.Dequeue();
        SpawnToast(p.message, p.duration);
    }

    private void SpawnToast(string message, float duration)
    {
        AlertToast toast = Instantiate(toastPrefab, container);
        toast.gameObject.SetActive(true); // prefab 模板可能是關著的（當模板用），clone 出來要主動啟用
        // 不呼叫 SetAsFirstSibling：VerticalLayoutGroup 下 Instantiate 預設排在最後，
        // 所以新公告會出現在「最下面」，往下依序排（舊的在上、新的在下）。
        _active.Add(toast);
        toast.Init(this, message, duration);

        // 超過上限：把最舊、還在顯示中的那則提前淡出。
        // 注意：只算「還在顯示」的（未離場也未淡出完成）；已淡出但保留佔位的 clone 不列入計算，
        //       也不在這裡銷毀 —— 統一等整批結束再清，維持不跑位。
        while (CountShowing() > maxVisible)
        {
            AlertToast oldest = FindOldestShowing();
            if (oldest == null) break;
            oldest.PlayFadeOut();
        }
    }

    /// <summary>目前「還在顯示中」的公告數（未離場、未淡出完成）。</summary>
    private int CountShowing()
    {
        int n = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            AlertToast t = _active[i];
            if (t != null && !t.IsDismissing && !t.IsFinished) n++;
        }
        return n;
    }

    /// <summary>找出最舊、還在顯示中的公告（供上限管理提前淡出）。</summary>
    private AlertToast FindOldestShowing()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            AlertToast t = _active[i];
            if (t != null && !t.IsDismissing && !t.IsFinished) return t;
        }
        return null;
    }

    /// <summary>
    /// toast 停留時間到、請求消失。管理器把它排進淡出佇列，
    /// 依 staggerInterval 逐一叫它們淡出，讓「消失」跟「出現」有一樣的時間差。
    /// </summary>
    public void RequestDismiss(AlertToast toast)
    {
        if (toast == null) return;

        if (staggerInterval <= 0f)
        {
            // 不需要時間差：直接淡出
            toast.PlayFadeOut();
            return;
        }

        _dismissPending.Enqueue(toast);
        if (_dismissRoutine == null)
            _dismissRoutine = StartCoroutine(DismissRoutine());
    }

    private IEnumerator DismissRoutine()
    {
        // 第一則立刻淡出，之後每隔 staggerInterval 淡出一則。
        // ★ 同 ReleaseRoutine：淡出一則後無條件等一個間隔，讓協程持續活著，
        //   才能把「同時到期」的多則公告逐一錯開淡出。
        while (_dismissPending.Count > 0)
        {
            AlertToast toast = _dismissPending.Dequeue();
            if (toast != null) toast.PlayFadeOut();
            yield return new WaitForSecondsRealtime(staggerInterval);
        }
        _dismissRoutine = null;
    }

    /// <summary>
    /// 某則 toast 淡出完成時回報。這裡「不」立即移除/銷毀它，
    /// 而是檢查是否「整批都已結束」（沒有待生成、待淡出、也沒有還沒淡完的），
    /// 若是就一次清除全部 clone —— 此時畫面上全都是 alpha 0，銷毀不會造成任何跳動或跑位。
    /// </summary>
    public void NotifyFinished(AlertToast toast)
    {
        TryCleanupAll();
    }

    private void TryCleanupAll()
    {
        // 還有沒生出來的、還在等淡出的 → 這批還沒結束
        if (_pending.Count > 0 || _dismissPending.Count > 0) return;

        // 還有任何一則沒淡出完成 → 先不清
        for (int i = 0; i < _active.Count; i++)
        {
            AlertToast t = _active[i];
            if (t != null && !t.IsFinished) return;
        }

        // 全部淡出完成 → 一次清掉所有 clone，容器回到空狀態
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i] != null) _active[i].DestroySelf();
        }
        _active.Clear();
    }
}
