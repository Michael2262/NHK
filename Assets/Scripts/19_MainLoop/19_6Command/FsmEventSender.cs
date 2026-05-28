using UnityEngine;
using Tooltip = UnityEngine.TooltipAttribute;
using HutongGames.PlayMaker;

/// <summary>
/// 可序列化的 FSM 事件發送器。
/// 用於在 TimedActionButton 的各階段（點擊 / 成功 / 失敗）觸發指定的 PlayMaker FSM。
///
/// 使用方式：
///   在 Inspector 中指定 fsmOwner（掛有 PlayMakerFSM 的 GameObject）、
///   fsmName（目標 FSM 名稱）與 eventName（要發送的事件名稱）。
///   然後呼叫 Send() 即可。
/// </summary>
[System.Serializable]
public class FsmEventSender
{
    [Tooltip("掛有目標 PlayMakerFSM 的 GameObject。若為空則使用自身。")]
    public GameObject fsmOwner;

    [Tooltip("目標 FSM 的名稱（在 PlayMaker 編輯器 FSM 頁籤最上面可以看到）。")]
    public string fsmName = "";

    [Tooltip("要發送給 FSM 的事件名稱。")]
    public string eventName = "TRIGGER";

    private PlayMakerFSM _cachedFsm;
    private bool _searched;

    /// <summary>
    /// 發送事件到指定的 FSM。
    /// </summary>
    public void Send()
    {
        if (fsmOwner == null) return;

        if (!_searched)
        {
            _searched = true;
            var fsms = fsmOwner.GetComponents<PlayMakerFSM>();
            foreach (var f in fsms)
            {
                if (f.FsmName == fsmName)
                {
                    _cachedFsm = f;
                    break;
                }
            }

            if (_cachedFsm == null)
            {
                Debug.LogError(
                    $"[FsmEventSender] 在 {fsmOwner.name} 上找不到名稱為 '{fsmName}' 的 FSM!");
            }
        }

        if (_cachedFsm != null && !string.IsNullOrEmpty(eventName))
        {
            _cachedFsm.SendEvent(eventName);
        }
    }

    /// <summary>
    /// 清除快取，下次 Send() 時會重新搜尋 FSM。
    /// 適合在 fsmOwner 或 fsmName 被動態更改後呼叫。
    /// </summary>
    public void ClearCache()
    {
        _cachedFsm = null;
        _searched = false;
    }
}
