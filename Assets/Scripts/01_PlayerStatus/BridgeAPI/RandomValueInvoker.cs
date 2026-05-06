using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 從設定的上下限中隨機取一個整數值，並透過 UnityEvent 傳出。
/// 
/// 用法：
///   1. 掛在按鈕（或任何物件）上
///   2. 設定 Min / Max（含兩端）
///   3. 在 OnRandomValue 拖入目標方法（例如 ProtagonistBridgeAPI.ReduceStamina）
///   4. 按鈕的 OnClick 呼叫此腳本的 Invoke()
/// </summary>
[AddComponentMenu("Game/Tools/Random Value Invoker")]
public class RandomValueInvoker : MonoBehaviour
{
    [Header("隨機範圍（含兩端）")]
    [SerializeField] private int _min = 1;
    [SerializeField] private int _max = 10;

    [Header("結果輸出")]
    [SerializeField] private UnityEvent<int> _onRandomValue;

    /// <summary>
    /// 產生隨機值並觸發事件。掛在按鈕 OnClick 或其他 UnityEvent 上即可。
    /// </summary>
    public void Invoke()
    {
        int value = Random.Range(_min, _max + 1);
        _onRandomValue?.Invoke(value);
    }

    /// <summary>
    /// 執行時動態修改範圍（例如隨難度提升）。
    /// </summary>
    public void SetRange(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public void SetMin(int min) => _min = min;
    public void SetMax(int max) => _max = max;
}
