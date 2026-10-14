using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 保留 UnityEvent 原生綁定格式，為每筆持久事件附加可選的 Flag 條件。
/// 呼叫端須使用 InvokeWithConditions；旗標讀取由服務轉接層注入。
/// 同一清單的條件在呼叫開始時統一判斷，事件仍依原順序執行。
/// </summary>
[Serializable]
public sealed class FlagConditionalEvent : UnityEvent
{
    [Serializable]
    public sealed class CallCondition
    {
        public bool Enabled;
        public ProgressFlagDefinition Flag;
        public bool Invert;

        public bool IsSatisfied(Func<string, bool?> readFlag)
        {
            if (!Enabled) return true;
            if (Flag == null || string.IsNullOrWhiteSpace(Flag.FlagID)) return false;
            bool? present = readFlag?.Invoke(Flag.FlagID);
            return present.HasValue && present.Value != Invert;
        }
    }

    // 與 m_PersistentCalls.m_Calls 按索引配對；共用繪製器同步新增、刪除與排序。
    // 舊資料沒有此清單時，所有事件維持無條件。
    [SerializeField] private List<CallCondition> callConditions = new List<CallCondition>();
    [NonSerialized] private bool invokingWithConditions;

    public void InvokeWithConditions(Func<string, bool?> readFlag)
    {
        if (invokingWithConditions)
        {
            Debug.LogWarning("[FlagConditionalEvent] 同一份條件事件正在執行，略過重入呼叫，請檢查事件是否循環觸發。");
            return;
        }

        int count = Math.Min(GetPersistentEventCount(), callConditions?.Count ?? 0);
        bool hasConditions = false;
        for (int i = 0; i < count; i++)
            if (callConditions[i] != null && callConditions[i].Enabled)
            { hasConditions = true; break; }
        if (!hasConditions)
        {
            base.Invoke();
            return;
        }
        var skipped = new List<KeyValuePair<int, UnityEventCallState>>();
        invokingWithConditions = true;
        try
        {
            for (int i = 0; i < count; i++)
            {
                CallCondition condition = callConditions[i];
                UnityEventCallState state = GetPersistentListenerState(i);
                if (condition == null || !condition.Enabled || state == UnityEventCallState.Off) continue;
                if (condition.IsSatisfied(readFlag)) continue;
                skipped.Add(new KeyValuePair<int, UnityEventCallState>(i, state));
                SetPersistentListenerState(i, UnityEventCallState.Off);
            }

            // 交由 Unity 處理參數、RuntimeOnly 與執行順序；AddListener 的監聽仍只執行一次。
            base.Invoke();
        }
        finally
        {
            // 即使監聽函式拋出例外，也不得把暫時略過的事件永久關閉。
            foreach (var entry in skipped) SetPersistentListenerState(entry.Key, entry.Value);
            invokingWithConditions = false;
        }
    }
}
