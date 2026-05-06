using UnityEngine;

public class FsmStateLogger : MonoBehaviour
{
    void OnDisable()
    {
        Debug.LogWarning($"<color=red>[FSM OnDisable] {gameObject.name} 被停用! frame={Time.frameCount}\nStack:\n{System.Environment.StackTrace}</color>");
    }

    void OnEnable()
    {
        Debug.Log($"<color=lime>[FSM OnEnable] {gameObject.name} 被啟用! frame={Time.frameCount}</color>");
    }
}