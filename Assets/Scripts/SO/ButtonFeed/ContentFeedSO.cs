using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/Content Feed", fileName = "ContentFeed")]
public class ContentFeedSO : ScriptableObject
{
    [SerializeField] private int contentVersion;    // 當前內容版本
    public int ContentVersion => contentVersion;

    /// <summary>有新內容時廣播（參數=新版本號）。</summary>
    public event Action<int> OnContentUpdated;

    /// <summary>宣告「有新內容」。可在任意遊戲邏輯處呼叫。</summary>
    public void PublishUpdate(int increment = 1)
    {
        if (increment <= 0) increment = 1;
        contentVersion += increment;
        OnContentUpdated?.Invoke(contentVersion);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // 讓版本變更在 Editor 可保存
#endif
    }

    /// <summary>（可選）強制設定版本號，用於遷移或Debug。</summary>
    public void SetVersion(int newVersion)
    {
        contentVersion = Mathf.Max(0, newVersion);
        OnContentUpdated?.Invoke(contentVersion);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // 讓你在 Inspector 右鍵快速測試
    [ContextMenu("Publish Update (+1)")]
    private void CtxPublishUpdate() => PublishUpdate(1);
}
