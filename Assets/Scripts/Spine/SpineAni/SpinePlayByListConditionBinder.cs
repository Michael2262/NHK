using UnityEngine;

/// <summary>
/// SpinePlayByList的通用橋接腳本：
/// 自動尋找任何實作 IConditionChecker 介面的組件，
/// 並將其與 SpinePlayByList 連接。
/// </summary>
[RequireComponent(typeof(SpinePlayByList))]
public class SpinePlayByListConditionBinder : MonoBehaviour
{
    void Awake()
    {
        var spinePlayer = GetComponent<SpinePlayByList>();

        // 【關鍵改動】
        // 我們不再找特定的類別，而是找任何實作 "IConditionChecker" 介面的組件
        var conditionChecker = GetComponent<IConditionChecker>();

        if (conditionChecker != null)
        {
            // 因為我們知道它一定有 CheckCondition() 方法，所以可以安全地連接
            spinePlayer.ConditionalTransitionCheck = conditionChecker.CheckCondition;
            Debug.Log($"已成功將 {conditionChecker.GetType().Name} 連接到 SpinePlayByList。", this);
        }
        else
        {
            Debug.LogWarning("在此 GameObject 上找不到任何實作 IConditionChecker 的組件，SpinePlayByList 的條件轉場將不會被觸發。", this);
        }
    }
}