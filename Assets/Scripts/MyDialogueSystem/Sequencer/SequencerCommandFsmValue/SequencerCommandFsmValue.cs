using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 指令格式: FsmValue(模式, 類型, 名稱, 變數名, [數值])
    /// 模式: Set, Adjust, Revert
    /// 類型: ID, Group
    /// 範例: 
    /// FsmValue(Set, ID, Sis, LovePoint, 50)
    /// FsmValue(Adjust, Group, NPC, Speed, -2.5)
    /// FsmValue(Revert, ID, Sis, LovePoint)
    /// </summary>
    public class SequencerCommandFsmValue : SequencerCommand
    {
        public void Awake()
        {
            // 取得參數
            string mode = GetParameter(0);          // Set, Adjust, Revert
            string targetType = GetParameter(1);    // ID, Group
            string targetName = GetParameter(2);    // 指定的 ID 或 Group 名稱
            string varName = GetParameter(3);       // FSM 內的變數名稱
            string valueStr = GetParameter(4);      // 要設定或調整的數值 (Revert 可不填)

            if (FsmValueManager.Instance == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: FsmValueManager Instance 尚未初始化！");
                Stop();
                return;
            }

            // 執行邏輯
            ExecuteCommand(mode, targetType, targetName, varName, valueStr);

            Stop(); // 執行完畢立即結束指令
        }

        private void ExecuteCommand(string mode, string type, string target, string var, string val)
        {
            bool isGroup = type.Equals("Group", System.StringComparison.OrdinalIgnoreCase);

            if (mode.Equals("Set", System.StringComparison.OrdinalIgnoreCase))
            {
                object parsedVal = ParseValue(val);
                if (isGroup) FsmValueManager.Instance.SetGroupValue(target, var, parsedVal);
                else FsmValueManager.Instance.SetValue(target, var, parsedVal);
            }
            else if (mode.Equals("Adjust", System.StringComparison.OrdinalIgnoreCase))
            {
                object parsedVal = ParseValue(val);
                if (isGroup) FsmValueManager.Instance.AdjustGroupValue(target, var, parsedVal);
                else FsmValueManager.Instance.AdjustValue(target, var, parsedVal);
            }
            else if (mode.Equals("Revert", System.StringComparison.OrdinalIgnoreCase))
            {
                if (isGroup) FsmValueManager.Instance.RevertGroupValue(target, var);
                else FsmValueManager.Instance.RevertValue(target, var);
            }
        }

        // 將字串參數解析為對應的 object 類型
        private object ParseValue(string s)
        {
            if (int.TryParse(s, out int i)) return i;
            if (float.TryParse(s, out float f)) return f;
            if (bool.TryParse(s, out bool b)) return b;
            return s; // 如果都不是，就當作 string 處理
        }
    }
}