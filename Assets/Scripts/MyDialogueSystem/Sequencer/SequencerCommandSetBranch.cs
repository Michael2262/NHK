using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 記錄劇情分支，讓合流後的對話仍可依先前路線分開。
    /// 語法：SetBranch(整數編號, 選填變數名)
    /// SetBranch(1) 寫入 StoryBranch = 1；SetBranch(2, HeadRoute) 寫入 HeadRoute = 2。
    /// Conditions 使用 Variable["StoryBranch"] == 1 或 Variable["HeadRoute"] == 2。
    /// 省略或留空變數名時共用 StoryBranch；再次寫入會覆蓋，不會隨對話結束自動清除。
    /// 可用 SetBranch(0) 初始化。此命令只轉接 Dialogue System 的 Lua 變數，不另存遊戲狀態。
    /// 與其他 Sequence 相同，若緊接的下一句就讀取結果，須注意連線條件的評估時機；
    /// 可插入無條件的一般節點，或使用 Dialogue Manager 的字幕結束後重新評估設定。
    /// </summary>
    public class SequencerCommandSetBranch : SequencerCommand
    {
        private const string DefaultVariableName = "StoryBranch";

        public void Awake()
        {
            int count = parameters != null ? parameters.Length : 0;
            if (count < 1 || count > 2)
            {
                Debug.LogWarning("[SetBranch] 請填入整數編號與選填變數名，例如 SetBranch(1) 或 SetBranch(1, HeadRoute)。本次未寫入。");
                Stop();
                return;
            }

            int branch;
            if (!int.TryParse(GetParameter(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out branch))
            {
                Debug.LogWarning($"[SetBranch] 分支編號「{GetParameter(0)}」不是有效的整數。本次未寫入。");
                Stop();
                return;
            }

            string variableName = (GetParameter(1, DefaultVariableName) ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(variableName)) variableName = DefaultVariableName;

            DialogueLua.SetVariable(variableName, branch);

            if (DialogueDebug.logInfo)
            {
                Debug.Log($"[SetBranch] 已記錄分支：{variableName} = {branch}");
            }

            Stop();
        }
    }
}
