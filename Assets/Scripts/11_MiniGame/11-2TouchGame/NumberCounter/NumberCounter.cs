using UnityEngine;
using System;

public class NumberCounter : MonoBehaviour
{
    [SerializeField] private int countMax = 10;
    [SerializeField] private int currentCount = 0;

    public event Action OnCountChanged; // 新增事件

    public int CurrentCount => currentCount;
    public bool IsReachedMax => currentCount >= countMax;

    public void AddCount()
    {
        if (currentCount < countMax)
        {
            currentCount++;
            OnCountChanged?.Invoke(); // 通知變化
            Debug.Log($"{gameObject.name} Count: {currentCount}");
        }
    }
}