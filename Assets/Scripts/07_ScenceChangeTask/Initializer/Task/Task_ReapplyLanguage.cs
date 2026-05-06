using UnityEngine;
using System.Collections;

/// <summary>
/// 薄包裝任務:在 Coordinator 管線中,重新套用當前語言到所有 LocalizeUI。
/// 
/// 【使用時機】
/// 主要用於「從 Title 進入遊戲世界」的入口場景(例如新遊戲初始場景、讀檔載入後的場景),
/// 確保場景中所有 LocalizeUI 元件顯示為玩家當前選擇的語言。
/// 
/// 【為什麼需要這個 Task】
/// 在某些跨場景的時序下(特別是開新檔/讀檔的瞬間),
/// 新場景的 LocalizeUI.OnEnable 可能無法正確讀取到當前語言設定,
/// 導致部分 UI 文字顯示為舊語言或預設語言。
/// 此 Task 在 Coordinator 管線執行時,會強制刷新所有 LocalizeUI 為正確語言。
/// 
/// 【建議放置位置】
/// 通常放在 Task 列表的「較後段」——等其他系統(HubController、存檔資料套用等)
/// 都把 UI 物件就位後,再統一刷新一次語言文字。
/// </summary>
public class Task_ReapplyLanguage : SceneReadyTaskBase
{
    public override IEnumerator ExecuteTask(string entryID)
    {
        Debug.Log($"<color=magenta>[Task_ReapplyLanguage] ExecuteTask 進入,entryID={entryID}</color>");

        if (LanguageManager.Instance == null)
        {
            Debug.LogWarning("[Task_ReapplyLanguage] LanguageManager 不存在,跳過。");
            yield break;
        }

        LanguageManager.Instance.ReapplyCurrentLanguage();
        Debug.Log("[Task_ReapplyLanguage] 已重新套用當前語言。");

        yield break;
    }
}