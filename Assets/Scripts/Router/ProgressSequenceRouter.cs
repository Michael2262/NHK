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
        public string label; // 僅供 Inspector 辨識用，例如 "進入過妹妹房間"
        public ProgressFlagDefinition flagDefinition;
        [Tooltip("預設勾選為『Flag開啟時滿足』；取消勾選則為『Flag關閉時滿足』")]
        public bool triggerIfActive = true;
        public UnityEvent onTriggered;
    }

    [Header("分支清單 (依序檢查)")]
    public List<FlagConditionBranch> branches = new List<FlagConditionBranch>();

    [Header("預設事件 (皆不符合時觸發)")]
    public UnityEvent onDefault;

    /// <summary>
    /// 綁定在 Unity Button 的 OnClick() 事件
    /// </summary>
    public void Trigger()
    {
        foreach (var branch in branches)
        {
            if (IsBranchSatisfied(branch))
            {
                // 找到第一個符合的條件就觸發並中斷
                branch.onTriggered.Invoke();
                return;
            }
        }

        // 如果全部都不符合，執行預設事件
        onDefault.Invoke();
    }

    private bool IsBranchSatisfied(FlagConditionBranch branch)
    {
        if (branch.flagDefinition == null) return false;

        // 核心連結：透過 GameStatusService 取得 ProgressFlags Model
        // 使用 Contains 方法檢查該 FlagID 是否存在
        bool isActive = GameStatusService.Instance.ProgressFlags.Contains(branch.flagDefinition.FlagID);

        // 判斷邏輯：旗標狀態是否符合我們設定的 triggerIfActive
        return branch.triggerIfActive == isActive;
    }
}