using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// (V2 - 統一規範版) 透過 SpriteRenderer 切換圖片。
/// 結構參考 SpineTogglePlayOnPress，支援 Reset 與 WatchOut 狀態。
/// </summary>
public class PicToggleOnPress : ConditionalPressReactionBase
{
    public enum TimeoutAction { DisableObject, SwitchToDefault }

    /*────────── Sprite 設定 ──────────*/
    [Header("Sprite Settings")]
    public SpriteRenderer targetRenderer;
    public Sprite spriteA;
    public Sprite spriteB;

    /*────────── Reset & WatchOut 設定 ──────────*/
    [Header("Reset & WatchOut")]
    [Tooltip("當按鍵正常放開 (Reset) 時顯示的圖片")]
    public Sprite resetSprite;

    [Tooltip("當被發現或突發中斷 (WatchOut) 時顯示的圖片")]
    public Sprite watchOutSprite;

    /*────────── Timeout 設定 ──────────*/
    [Header("Timeout Settings (延遲後行為)")]
    public float hideDelay = 1.0f;
    public TimeoutAction onTimeoutAction = TimeoutAction.DisableObject;
    [Tooltip("超時後若選擇 SwitchToDefault，則顯示此圖片")]
    public Sprite spriteDefault;

    [Header("FSM Event")]
    public string resetFsmEvent = "STOPHOVER";

    private bool _showNextA = true;
    private Coroutine _hideCoroutine;

    protected override void Awake()
    {
        base.Awake(); // 確保執行基類的 Awake
        if (!targetRenderer) targetRenderer = GetComponent<SpriteRenderer>();
    }


    /// <summary>
    /// 核心點擊/觸發邏輯：A/B 圖片切換
    /// </summary>
    public override void OnTouched()
    {
        if (!targetRenderer) return;

        StopHideTimer();

        // 確保物件顯示
        if (!targetRenderer.gameObject.activeSelf)
        {
            targetRenderer.gameObject.SetActive(true);
        }

        // 切換圖片
        targetRenderer.sprite = _showNextA ? spriteA : spriteB;
        _showNextA = !_showNextA;
    }

    /// <summary>
    /// 突發狀況處理：播放驚嚇/中斷圖片，並啟動隱藏計時
    /// </summary>
    public override void WatchOut()
    {
        // 核心：必須呼叫 base.WatchOut() 重置基類的計數與狀態
        base.WatchOut();

        if (targetRenderer != null)
        {
            if (watchOutSprite != null)
            {
                targetRenderer.sprite = watchOutSprite;
            }
            StartHideTimer();
        }
    }

    /// <summary>
    /// 正常釋放處理：顯示 Reset 圖片，重置切換順序，並啟動隱藏計時
    /// </summary>
    public override void ResetToOriginal()
    {
        // 核心：必須呼叫 base.ResetToOriginal() 清除基類的按壓計數
        base.ResetToOriginal();

        if (targetRenderer != null)
        {
            if (resetSprite != null)
            {
                targetRenderer.sprite = resetSprite;
            }
            StartHideTimer();
        }

        _showNextA = true; // 重置下一次從 A 開始
        SendFsmEvent(resetFsmEvent);
    }

    /*────────── 內部輔助邏輯 ──────────*/

    private void StartHideTimer()
    {
        StopHideTimer();
        if (gameObject.activeInHierarchy)
        {
            _hideCoroutine = StartCoroutine(WaitAndHideRoutine());
        }
        else
        {
            ApplyTimeoutEffect();
        }
    }

    private void StopHideTimer()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
    }

    private IEnumerator WaitAndHideRoutine()
    {
        yield return new WaitForSeconds(hideDelay);
        ApplyTimeoutEffect();
        _hideCoroutine = null;
    }

    private void ApplyTimeoutEffect()
    {
        if (targetRenderer == null) return;

        if (onTimeoutAction == TimeoutAction.DisableObject)
            targetRenderer.gameObject.SetActive(false);
        else if (spriteDefault != null)
            targetRenderer.sprite = spriteDefault;
    }
}