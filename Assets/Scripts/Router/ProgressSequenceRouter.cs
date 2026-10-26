using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

[AddComponentMenu("Game/UI/Progress Sequence Router")]
public class ProgressSequenceRouter : MonoBehaviour
{
    // 定義單個分支的結構
    [Serializable]
    public class FlagConditionBranch
    {
        public ProgressFlagDefinition flagDefinition;
        // 保留原本的序列化欄位與語意，避免既有場景的條件被反轉。
        // 專用 Inspector 以此欄位的反值顯示「反向」勾選。
        [HideInInspector]
        public bool triggerIfActive = true;
        public UnityEvent onTriggered = new UnityEvent();
    }

    [Header("分支清單 (依序檢查)")]
    public List<FlagConditionBranch> branches = new List<FlagConditionBranch>();

    [Header("預設事件 (皆不符合時觸發)")]
    public UnityEvent onDefault = new UnityEvent();

    private bool invokingEvents;

    /// <summary>
    /// 供 EventTrigger 的 Pointer Click、Button 或其他 UnityEvent 呼叫。
    /// </summary>
    public void Trigger()
    {
        if (invokingEvents) return;
        invokingEvents = true;
        try
        {
            if (branches != null)
            {
                foreach (var branch in branches)
                {
                    if (!IsBranchSatisfied(branch)) continue;
                    // 命中後立即停止，事件改變 Flag 也不繼續檢查。
                    branch.onTriggered?.Invoke();
                    return;
                }
            }

            onDefault?.Invoke();
        }
        finally
        {
            invokingEvents = false;
        }
    }

    private bool IsBranchSatisfied(FlagConditionBranch branch)
    {
        if (branch == null || branch.flagDefinition == null) return false;

        // 核心連結：透過 GameStatusService 取得 ProgressFlags Model
        // 使用 Contains 方法檢查該 FlagID 是否存在
        var flags = GameStatusService.Instance?.ProgressFlags;
        // 服務尚未就緒時不能把未知狀態當成「沒有 Flag」。
        if (flags == null) return false;
        bool isActive = flags.Contains(branch.flagDefinition.FlagID);

        // 判斷邏輯：旗標狀態是否符合我們設定的 triggerIfActive
        return branch.triggerIfActive == isActive;
    }
}
