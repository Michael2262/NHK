using UnityEngine;
using System.Collections;

/// <summary>
/// 存檔通知元件：訂閱 GameStatusService 的存檔事件，自動顯示對應通知。
/// 
/// 【v2 改動】
/// 不再需要被其他腳本直接引用。改為訂閱靜態事件，自動響應。
/// 不管是誰觸發存檔（DayTransitionUI、手動存檔按鈕、其他系統），通知都會出現。
/// </summary>
public class SaveNotification : MonoBehaviour
{
    [Header("通知物件（請各自掛 LocalizeUI）")]
    [Tooltip("自動存檔完成時顯示的 GameObject")]
    [SerializeField] private GameObject autoSaveNotification;

    [Tooltip("手動存檔完成時顯示的 GameObject")]
    [SerializeField] private GameObject manualSaveNotification;

    [Header("設定")]
    [Tooltip("通知顯示的持續時間（秒）")]
    [SerializeField] private float displayDuration = 1f;

    private Coroutine _hideCoroutine;

    private void OnEnable()
    {
        GameStatusService.OnAutoSaveCompleted += ShowAutoSave;
        GameStatusService.OnManualSaveCompleted += ShowManualSave;
    }

    private void OnDisable()
    {
        GameStatusService.OnAutoSaveCompleted -= ShowAutoSave;
        GameStatusService.OnManualSaveCompleted -= ShowManualSave;
    }

    /// <summary>
    /// 顯示自動存檔通知
    /// </summary>
    public void ShowAutoSave()
    {
        ShowNotification(autoSaveNotification);
    }

    /// <summary>
    /// 顯示手動存檔通知
    /// </summary>
    public void ShowManualSave()
    {
        ShowNotification(manualSaveNotification);
    }

    private void ShowNotification(GameObject target)
    {
        if (target == null) return;

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        HideAll();

        target.SetActive(true);
        _hideCoroutine = StartCoroutine(HideAfterDelay(target));
    }

    private IEnumerator HideAfterDelay(GameObject target)
    {
        yield return new WaitForSeconds(displayDuration);
        target.SetActive(false);
        _hideCoroutine = null;
    }

    private void HideAll()
    {
        if (autoSaveNotification != null) autoSaveNotification.SetActive(false);
        if (manualSaveNotification != null) manualSaveNotification.SetActive(false);
    }
}