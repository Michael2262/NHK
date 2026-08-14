using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TeaseGame 動作閘門（一個場景一個，跑條核心）。
///
/// 玩家成功操作一個觸碰區時，透過 TryBegin 進來：
///   - 若正在跑條（忙碌中）→ 回傳 false，這次操作無效丟棄。
///   - 若空閒 → 發 onStart（觸碰當下的表演）、開始跑條；時間到發 onComplete（女主角反應），
///     期間鎖住其他所有操作。
///
/// 跑條時長：TryBegin 傳入 > 0 的值可覆寫這一次，否則用 defaultDuration。
/// Progress（0..1）供跑條 UI（TeaseProgressBar）讀取。
/// </summary>
public class TeaseActionGate : MonoBehaviour
{
    /// <summary>場景內單例。小遊戲場景卸載時自動清空。</summary>
    public static TeaseActionGate Instance { get; private set; }

    [Header("跑條")]
    [Tooltip("預設跑條時長（秒）。TryBegin 未指定（傳 ≤ 0）時使用。")]
    [SerializeField] private float defaultDuration = 1f;

    /// <summary>是否正在跑條（忙碌中）。</summary>
    public bool IsBusy { get; private set; }

    /// <summary>目前跑條進度 0..1；空閒時為 0。</summary>
    public float Progress { get; private set; }

    private Coroutine _running;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[TeaseActionGate] 場上已有一個實例，銷毀重複的 {name}。", this);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 嘗試開始一次動作。忙碌中回傳 false（本次操作無效）。
    /// duration ≤ 0 時使用 defaultDuration。
    /// </summary>
    public bool TryBegin(float duration, UnityEvent onStart, UnityEvent onComplete)
    {
        if (IsBusy) return false;

        float d = duration > 0f ? duration : defaultDuration;
        _running = StartCoroutine(RunAction(d, onStart, onComplete));
        return true;
    }

    private IEnumerator RunAction(float duration, UnityEvent onStart, UnityEvent onComplete)
    {
        IsBusy = true;
        Progress = 0f;

        onStart?.Invoke();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Progress = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        Progress = 1f;
        onComplete?.Invoke();

        IsBusy = false;
        Progress = 0f;
        _running = null;
    }

    /// <summary>
    /// 強制中斷目前跑條（例如媽媽突然進來）。不會發 onComplete。
    /// </summary>
    public void Cancel()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        IsBusy = false;
        Progress = 0f;
    }
}
