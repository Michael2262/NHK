using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//[RequireComponent(typeof(Button))]
public class MinigameTrigger : MonoBehaviour
{
    [Header("小遊戲設定")]
    [Tooltip("要載入的小遊戲「場景名稱」")]
    [SerializeField] private string minigameSceneName;

    [Header("目標對象")]
    [Tooltip("要互動的女主角 ID 列表")]
    [SerializeField] private List<string> targetHeroineIDs = new List<string>();

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(StartMinigame);
    }

    public void StartMinigame()
    {
        if (string.IsNullOrEmpty(minigameSceneName) || targetHeroineIDs.Count == 0)
        {
            Debug.LogError("[MinigameTrigger] 場景名稱或女主角 ID 未設定！", this);
            return;
        }

        if (MinigameManager.Instance != null)
        {
            Debug.Log($"[MinigameTrigger] 請求啟動小遊戲 '{minigameSceneName}'，目標數量: {targetHeroineIDs.Count}");
            MinigameManager.Instance.StartMinigame(minigameSceneName, targetHeroineIDs);
        }
        else
        {
            Debug.LogError("[MinigameTrigger] MinigameManager 實例不存在！");
        }
    }

    /// <summary>
    /// (可選) 動態設定此觸發器 (單一女主角版本)
    /// </summary>
    public void SetInteraction(string scene, string heroineID)
    {
        minigameSceneName = scene;

        // ★ 修正點：
        // 清空舊列表，並將單一 ID 作為新列表的第一個元素
        targetHeroineIDs.Clear();
        targetHeroineIDs.Add(heroineID);
    }

    /// <summary>
    /// (可選) 動態設定此觸發器 (多女主角版本)
    /// </summary>
    public void SetInteraction(string scene, List<string> heroineIDs)
    {
        minigameSceneName = scene;
        // ★ 修正點：直接指派新的列表
        targetHeroineIDs = heroineIDs;
    }

    private void OnDestroy()
    {
        if (_button)
        {
            _button.onClick.RemoveListener(StartMinigame);
        }
    }
}