using System.Collections.Generic;
using UnityEngine;

public class CountNumberAPI : MonoBehaviour
{
    [Header("狀態設定")]
    [Tooltip("是否啟用此計數觸發器")]
    public bool isEnabled = true;

    [Header("目標計數器列表")]
    [SerializeField] private List<NumberCounter> targetCounters = new List<NumberCounter>();

    /// <summary>
    /// 外部呼叫此 API 來觸發所有關聯的計數器
    /// </summary>
    public void Trigger()
    {
        // 檢查開關狀態
        if (!isEnabled) return;

        if (targetCounters.Count == 0)
        {
            //Debug.LogWarning($"{gameObject.name}: 尚未指定任何 NumberCounter！");
            return;
        }

        foreach (var counter in targetCounters)
        {
            if (counter != null)
            {
                counter.AddCount();
            }
        }
    }
}