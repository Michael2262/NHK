using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 【指示器框控制器】
/// 
/// 管理一群 Indicator。當「所有 Indicator 都是關閉狀態」時,關掉自己的框;
/// 只要任一 Indicator 打開,框就打開。
/// 
/// 掛在「框」物件本身,會自動掃描底下 (children) 的所有 Indicator。
/// Indicator 不需要知道框的存在 — 透過事件解耦。
/// </summary>
public class IndicatorFrameController : MonoBehaviour
{
    public enum ToggleMode
    {
        ImageEnabled,       // 只切換 Image.enabled
        GameObjectActive    // 切換整個 GameObject
    }

    [Header("--- 框的顯示控制 ---")]
    [Tooltip("要被切換開關的框圖片 (可空,若空則改用 GameObjectActive 切 self)")]
    public Image frameImage;

    [Tooltip("切換模式")]
    public ToggleMode toggleMode = ToggleMode.GameObjectActive;

    [Header("--- 掃描範圍 ---")]
    [Tooltip("true = 僅掃描直接子物件上的 Indicator\n" +
             "false = 掃描所有層級的子物件 (含孫層)")]
    public bool directChildrenOnly = false;

    [Tooltip("手動指定要監聽的 Indicator 清單。\n" +
             "留空則自動掃描子物件;填入後會以這個清單為準 (override)")]
    public List<Indicator> manualIndicators = new List<Indicator>();

    [Header("--- Debug ---")]
    [SerializeField, Tooltip("Readonly:目前追蹤中的 Indicator 數量")]
    private int _trackedCount;

    [SerializeField, Tooltip("Readonly:目前框是否顯示")]
    private bool _isFrameVisible;

    // 實際追蹤中的 Indicator (包含已訂閱事件的)
    private readonly List<Indicator> _tracked = new List<Indicator>();

    // ==========================================================
    // 生命週期
    // ==========================================================

    private void OnEnable()
    {
        RescanAndSubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeAll();
    }

    // ==========================================================
    // 掃描與訂閱
    // ==========================================================

    /// <summary>
    /// 重新掃描底下的 Indicator,並訂閱它們的事件。
    /// 外部若動態新增/刪除 Indicator,可以手動呼叫這個。
    /// </summary>
    public void RescanAndSubscribe()
    {
        UnsubscribeAll();
        _tracked.Clear();

        // 優先用手動指定的清單
        if (manualIndicators != null && manualIndicators.Count > 0)
        {
            foreach (var ind in manualIndicators)
            {
                if (ind != null) _tracked.Add(ind);
            }
        }
        else
        {
            // 自動掃描
            if (directChildrenOnly)
            {
                foreach (Transform child in transform)
                {
                    var ind = child.GetComponent<Indicator>();
                    if (ind != null) _tracked.Add(ind);
                }
            }
            else
            {
                // 包含所有子孫
                var inds = GetComponentsInChildren<Indicator>(includeInactive: true);
                _tracked.AddRange(inds);
            }
        }

        // 訂閱事件
        foreach (var ind in _tracked)
        {
            ind.OnVisibilityChanged += HandleIndicatorChanged;
        }

        _trackedCount = _tracked.Count;
    }

    private void UnsubscribeAll()
    {
        foreach (var ind in _tracked)
        {
            if (ind != null)
                ind.OnVisibilityChanged -= HandleIndicatorChanged;
        }
    }

    private void HandleIndicatorChanged(Indicator who, bool nowVisible)
    {
        Refresh();
    }

    // ==========================================================
    // 核心刷新
    // ==========================================================

    /// <summary>
    /// 重新判斷框是否該顯示,並套用。
    /// </summary>
    public void Refresh()
    {
        bool anyVisible = false;
        foreach (var ind in _tracked)
        {
            if (ind != null && ind.IsVisible)
            {
                anyVisible = true;
                break;
            }
        }

        _isFrameVisible = anyVisible;
        ApplyFrameVisibility(anyVisible);
    }

    private void ApplyFrameVisibility(bool show)
    {
        switch (toggleMode)
        {
            case ToggleMode.ImageEnabled:
                if (frameImage != null && frameImage.enabled != show)
                    frameImage.enabled = show;
                break;

            case ToggleMode.GameObjectActive:
                // 切自己的 GameObject 有個陷阱 — 自己被 disable 就收不到事件
                // 所以如果要切 self,建議切子物件 (frameImage.gameObject),或是多套一層 wrapper
                if (frameImage != null)
                {
                    if (frameImage.gameObject.activeSelf != show)
                        frameImage.gameObject.SetActive(show);
                }
                else
                {
                    // 沒指定 Image 時,也不建議切自己,會導致失聯
                    // 不做事,並在 Debug 時提示
                }
                break;
        }
    }

    // ==========================================================
    // Debug
    // ==========================================================

    [ContextMenu("Debug/重新掃描並刷新")]
    public void DebugRescanAndRefresh()
    {
        RescanAndSubscribe();
        Refresh();

        int visible = 0;
        foreach (var ind in _tracked)
            if (ind != null && ind.IsVisible) visible++;

        Debug.Log($"[IndicatorFrameController] 追蹤 {_tracked.Count} 個 Indicator,其中 {visible} 個顯示中。框狀態: {_isFrameVisible}", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
        {
            RescanAndSubscribe();
            Refresh();
        }
    }
#endif
}
