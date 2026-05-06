using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// (圖片切換版 - 延遲處理)
/// 觸發時：在 A/B 圖片間切換。
/// 放開時：先切換成「SpriteLeave」，等待 hideDelay 秒後執行指定動作 (關閉 或 換回預設圖)。
/// 若在等待期間再次觸發：取消倒數，直接回到 A/B 切換循環。
/// </summary>
[RequireComponent(typeof(Button))]
public class PicToggleOnHoverButton : ConditionalHoverButtonReactionBase
{
    // 定義倒數結束後的行為模式
    public enum TimeoutAction
    {
        DisableObject,      // 1. 時間到關閉物件 (原功能)
        SwitchToDefault     // 2. 時間到換成 spriteDefault (不關物件)
    }

    /*────────── Hover 設定 ──────────*/
    [Header("Hover Loop Settings")]
    [UnityEngine.Tooltip("在長按/AutoHover 狀態下，每隔幾秒切換一次圖片")]
    public float hoverLoopInterval = 0.5f;

    /*────────── 圖片設定 ──────────*/
    [Header("Sprite Settings")]
    [UnityEngine.Tooltip("要控制的 SpriteRenderer")]
    public SpriteRenderer targetRenderer;

    [UnityEngine.Tooltip("觸發時顯示圖片 A")]
    public Sprite spriteA;

    [UnityEngine.Tooltip("觸發時顯示圖片 B")]
    public Sprite spriteB;

    [UnityEngine.Tooltip("放開(Reset)當下顯示的過渡圖片")]
    public Sprite spriteLeave;

    [Header("Timeout Settings (延遲後行為)")]
    [UnityEngine.Tooltip("放開後，延遲幾秒才執行後續動作")]
    public float hideDelay = 1.0f;

    [UnityEngine.Tooltip("時間到要執行的動作：關閉物件 或 換成預設圖")]
    public TimeoutAction onTimeoutAction = TimeoutAction.DisableObject;

    [UnityEngine.Tooltip("如果選擇 SwitchToDefault，時間到會換成這張圖")]
    public Sprite spriteDefault;

    /*────────── Reset 設定 ──────────*/
    [Header("Reset")]
    [Tooltip("當重置(放手)瞬間發送的 FSM 事件")]
    public string resetFsmEvent = "STOPHOVER";

    // 內部狀態
    private bool _showNextA = true;
    // 用來追蹤下一次顯示的是 A 還是 B 圖
    private bool _isForceInterrupt = false;
    // 標記是否處於強制中斷狀態
    private Coroutine _hideCoroutine; // 儲存倒數計時的協程

    /*────────── Mono ──────────*/
    protected override void Awake()
    {
        base.Awake();
        if (!targetRenderer)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    /*────────── 實作 ConditionalHoverButtonReactionBase ──────────*/

    protected override float GetHoverInterval()
    {
        return hoverLoopInterval;
    }

    /// <summary>
    /// (2) 觸發反應：開啟物件並切換圖片
    /// </summary>
    public override void OnTouched()
    {
        if (!targetRenderer) return;

        

        // 如果有正在跑的「延遲倒數」，立刻取消它
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        // 1. 確保物件開啟
        if (!targetRenderer.gameObject.activeSelf)
        {
            targetRenderer.gameObject.SetActive(true);
        }

        // 2. 切換 A/B 圖片
        targetRenderer.sprite = _showNextA ? spriteA : spriteB;
        _showNextA = !_showNextA;

        // 3.已可點擊代表強制中斷結束
        _isForceInterrupt = false;
    }

    /// <summary>
    /// (3) 重置反應：切換成 NotHover 圖 -> 等待 -> 執行 TimeoutAction
    /// </summary>
    public override void ResetToOriginal()
    {
        // 1. 執行基底重置 (停止 Loop 計時)
        base.ResetToOriginal();

        if (targetRenderer != null)
        {
            // 2. 顯示「未觸發狀態」的圖片 (這是放開瞬間的反應)，如果是強制中斷中，不顯示此圖
            if (spriteLeave != null && _isForceInterrupt != true)
            {
                targetRenderer.sprite = spriteLeave;
            }

            // 3. 啟動延遲處理的協程
            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);

            // 只有當物件還是 Active 的時候才能跑協程
            if (gameObject.activeInHierarchy)
            {
                _hideCoroutine = StartCoroutine(WaitAndHideRoutine());
            }
            else
            {
                // 如果按鈕自己都被關了，且模式是關閉物件，就直接把目標關掉
                if (onTimeoutAction == TimeoutAction.DisableObject)
                {
                    targetRenderer.gameObject.SetActive(false);
                }
            }
        }

        // 4. 重置 A/B 切換順序
        _showNextA = true;

        // 5. 發送 FSM 事件
        SendFsmEvent(resetFsmEvent);
    }

    /// <summary>
    /// (4) 暫停
    /// </summary>
    public override void ForceInterruptRelease()
    {
        base.ResetToOriginal();

        // 標記為強制中斷
        _isForceInterrupt = true;

        if (targetRenderer != null)
        {
            if (spriteLeave != null)
            {
                targetRenderer.sprite = spriteDefault;
            }

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);

            if (gameObject.activeInHierarchy)
            {
                _hideCoroutine = StartCoroutine(WaitAndHideRoutine());
            }
            else
            {
                if (onTimeoutAction == TimeoutAction.DisableObject)
                {
                    targetRenderer.gameObject.SetActive(false);
                }
            }
        }
        _showNextA = true;
    }

    /// <summary>
    /// 延遲處理的協程
    /// </summary>
    private IEnumerator WaitAndHideRoutine()
    {
        // 等待指定時間
        yield return new WaitForSeconds(hideDelay);

        // 時間到：根據設定決定行為
        if (targetRenderer != null)
        {
            switch (onTimeoutAction)
            {
                case TimeoutAction.DisableObject:
                    // 選項 1: 關閉物件
                    targetRenderer.gameObject.SetActive(false);
                    break;

                case TimeoutAction.SwitchToDefault:
                    // 選項 2: 換成 Default 圖 (保持物件開啟)
                    if (spriteDefault != null)
                    {
                        targetRenderer.sprite = spriteDefault;
                    }
                    break;
            }
        }

        _hideCoroutine = null;
    }

    private void OnDisable()
    {
        _hideCoroutine = null;
    }
}