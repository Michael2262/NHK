using UnityEngine;
using UnityEngine.Events;

public class HeroineSelectorDebug : MonoBehaviour //
{
    [Header("Debug 設定")]
    [Tooltip("請輸入這個按鈕/觸發器代表的女主角 ID")]
    public string heroineIDToSet = "HEROINE_ID_HERE"; //

    [Header("觸發事件")]
    public UnityEvent<string> OnSelectHeroine; //

    public void TriggerInteractionForThisHeroine() //
    {
        if (string.IsNullOrEmpty(heroineIDToSet) || heroineIDToSet == "HEROINE_ID_HERE") //
        { Debug.LogWarning("請先在 Inspector 中指定有效的 ID。", this); return; } //

        Debug.Log($"[Debug] 設定目標 Heroine ID 為 '{heroineIDToSet}'"); // 修改 Log
        OnSelectHeroine?.Invoke(heroineIDToSet); //
    }
}