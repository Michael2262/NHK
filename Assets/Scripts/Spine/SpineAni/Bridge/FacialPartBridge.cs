using UnityEngine;

/// <summary>
/// 表情部位控制器的 UnityEvent 入口，以 ID 尋找目前啟用的目標。
/// 表情模式、計數與播放規則交由 FacialPartController 處理，不快取場景元件。
/// </summary>
[AddComponentMenu("Spine/Bridge/Facial Part Bridge")]
public class FacialPartBridge : MonoBehaviour
{
    [Header("目標設定")]
    [SerializeField, Tooltip("填入 FacialPartController 的 Controller Id，區分大小寫；不是 SpineAnimationController 的 ID。")]
    private string controllerId;

    [Header("表情模式設定")]
    [SerializeField, Tooltip("ApplyConfiguredFacialMode 呼叫時套用的表情模式。只變更表情模式，不立即播放或重置倒數。")]
    [UnityEngine.Serialization.FormerlySerializedAs("emotion")]
    private HeroineEmotionType facialMode = HeroineEmotionType.Idle;

    /// <summary>切換此 Bridge 後續操作的目標 ID，不修改 Controller 本身的 ID。</summary>
    public void SetTargetId(string id)
    {
        controllerId = (id ?? string.Empty).Trim();
    }

    /// <summary>讓 Inspector 指定目標的所有部位計數各減一；需在目標 Start 完成後呼叫。</summary>
    public void NotifyRemote()
    {
        FacialPartController.NotifyRemote(controllerId);
    }

    /// <summary>只對當次傳入的 ID 推進計數，不變更 Bridge 的目標設定。</summary>
    public void NotifyRemoteById(string id)
    {
        FacialPartController.NotifyRemote(id);
    }

    /// <summary>廣播給所有已註冊且就緒的目標；不使用此 Bridge 的目標 ID。</summary>
    public void NotifyRemoteAll()
    {
        FacialPartController.NotifyRemoteAll();
    }

    /// <summary>套用 Inspector 選定的表情模式至目前目標。</summary>
    public void ApplyConfiguredFacialMode()
    {
        SetFacialMode(facialMode);
    }

    /// <summary>供程式或帶有表情模式參數的動態 UnityEvent 呼叫。</summary>
    public void SetFacialMode(HeroineEmotionType value)
    {
        FacialPartController.SetFacialModeById(controllerId, value);
    }

    // 無參數入口，可直接從一般 UnityEvent 的選單選取。
    public void SetFacialModeIdle() => SetFacialMode(HeroineEmotionType.Idle);
    public void SetFacialModeSad() => SetFacialMode(HeroineEmotionType.Sad);
    public void SetFacialModeAngry() => SetFacialMode(HeroineEmotionType.Angry);
    public void SetFacialModeShy() => SetFacialMode(HeroineEmotionType.Shy);
    public void SetFacialModeSexy() => SetFacialMode(HeroineEmotionType.Sexy);
    public void SetFacialModeOrgasm() => SetFacialMode(HeroineEmotionType.Orgasm);
}
