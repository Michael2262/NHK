using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// NHK 版壓力預覽觸發器。
/// 保留舊檔名 / class 名，避免原 Inspector 綁定失效。
/// 原 StaminaPreview 介面現在用來預覽 Stress 變化：正數 = 壓力上升，負數 = 壓力下降。
/// </summary>
public class StaminaPreviewHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("數值來源設定")]
    [SerializeField] private bool useRouter = false;

    [Header("自訂數值")]
    [Tooltip("預覽的壓力變化量：正數 = 壓力上升，負數 = 壓力下降")]
    [SerializeField] private int previewDelta = 0;

    private ProtagonistValueRouter _router;
    private bool _isHovering = false;

    private void Awake()
    {
        _router = GetComponent<ProtagonistValueRouter>();
        if (useRouter && _router == null)
        {
            Debug.LogWarning($"[StaminaPreviewHoverTrigger] {gameObject.name} 找不到 ProtagonistValueRouter，改用自訂數值。");
            useRouter = false;
        }
    }

    private void OnDisable()
    {
        if (_isHovering)
        {
            HidePreview();
            _isHovering = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        ShowPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        HidePreview();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _isHovering = false;
        HidePreview();
    }

    private int GetPreviewDelta()
    {
        // 新版 ProtagonistValueRouter 的 amount 多數是「門檻」不是「變化量」，不適合自動推斷。
        // 需要預覽時請由 IxMenuService 或 Inspector 明確設定 previewDelta。
        return previewDelta;
    }

    private void ShowPreview()
    {
        int delta = GetPreviewDelta();
        if (delta == 0) return;
        LobbyUI_V2.Instance?.ShowStaminaPreview(delta);
    }

    private void HidePreview()
    {
        LobbyUI_V2.Instance?.HideStaminaPreview();
    }

    public void SetPreviewDelta(int delta)
    {
        previewDelta = delta;
    }

    public void SetUseRouter(bool value)
    {
        useRouter = value;
        if (useRouter && _router == null)
        {
            _router = GetComponent<ProtagonistValueRouter>();
            if (_router == null)
            {
                Debug.LogWarning($"[StaminaPreviewHoverTrigger] {gameObject.name} 找不到 ProtagonistValueRouter，保持使用自訂數值。");
                useRouter = false;
            }
        }
    }
}
