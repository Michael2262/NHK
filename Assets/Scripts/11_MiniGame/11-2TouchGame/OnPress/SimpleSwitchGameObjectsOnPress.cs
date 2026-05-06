using UnityEngine;

/// <summary>
/// 極簡開關物件：收到 GesturePressLogicProxy 的觸發後，
/// 將指定的物件分別開啟或關閉。
/// 不與 FSM 溝通，不處理 Reset / WatchOut。
/// </summary>
public class SimpleSwitchGameObjectsOnPress : ConditionalPressReactionBase
{
    [Header("觸發時要【開啟】的物件")]
    public GameObject[] objectsToEnable;

    [Header("觸發時要【關閉】的物件")]
    public GameObject[] objectsToDisable;

    protected override void Awake()
    {
        base.Awake(); // 讓基類抓取 FsmContext
    }

    public override void OnTouched()
    {
        if (objectsToEnable != null)
        {
            foreach (var obj in objectsToEnable)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (objectsToDisable != null)
        {
            foreach (var obj in objectsToDisable)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }

    public override void WatchOut() { }
    public override void ResetToOriginal() { }
}
