using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 預先定義多組「具名數值清單」，透過 Key 觸發時從對應清單中隨機取一個值。
/// 
/// 用法：
///   1. 在 Inspector 的 Entries 中新增項目，填入 Key 和多個候選值
///   2. 在 On Random Value 拖入目標方法（例如 ProtagonistBridgeAPI.ReduceStamina）
///   3. 按鈕或其他 UnityEvent 呼叫 Invoke("你的Key")
///
/// 範例設定：
///   Key: "低傷害"    Values: [3, 5, 7, 8]
///   Key: "高傷害"    Values: [15, 20, 25, 30]
///   Key: "獎勵金"    Values: [100, 200, 500]
/// </summary>
[AddComponentMenu("Game/Tools/Random List Invoker")]
public class RandomListInvoker : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("用來呼叫的識別名稱")]
        public string Key;

        [Tooltip("候選數值，觸發時會從中隨機取一個")]
        public List<int> Values = new List<int>();
    }

    [Header("數值清單")]
    [SerializeField] private List<Entry> _entries = new List<Entry>();

    [Header("結果輸出")]
    [SerializeField] private UnityEvent<int> _onRandomValue;

    // 執行期用 Dictionary 加速查找
    private Dictionary<string, Entry> _lookup;

    private void Awake()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, Entry>(_entries.Count);
        foreach (var entry in _entries)
        {
            if (string.IsNullOrEmpty(entry.Key))
            {
                Debug.LogWarning($"[{nameof(RandomListInvoker)}] 發現空白 Key，已跳過。", this);
                continue;
            }
            if (!_lookup.TryAdd(entry.Key, entry))
            {
                Debug.LogWarning($"[{nameof(RandomListInvoker)}] 重複的 Key \"{entry.Key}\"，已跳過。", this);
            }
        }
    }

    /// <summary>
    /// 用 Key 觸發，從對應清單中隨機取值並透過 _onRandomValue 傳出。
    /// 可直接在 UnityEvent 中拖曳使用（選 Dynamic string）。
    /// </summary>
    public void Invoke(string key)
    {
        if (_lookup == null) BuildLookup();

        if (!_lookup.TryGetValue(key, out var entry))
        {
            Debug.LogError($"[{nameof(RandomListInvoker)}] 找不到 Key \"{key}\"。", this);
            return;
        }

        if (entry.Values == null || entry.Values.Count == 0)
        {
            Debug.LogError($"[{nameof(RandomListInvoker)}] Key \"{key}\" 的 Values 為空。", this);
            return;
        }

        int value = entry.Values[UnityEngine.Random.Range(0, entry.Values.Count)];
        _onRandomValue?.Invoke(value);
    }
}
