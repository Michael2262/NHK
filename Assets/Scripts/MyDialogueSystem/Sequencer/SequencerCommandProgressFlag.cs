using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：ProgressFlag(動作, FlagID)
    ///
    /// 支援動作與生命週期：
    /// 1. Add/Persistent: ProgressFlag(Add, FlagID)    -> 永久標記，寫入存檔
    /// 2. Scene/Temp:      ProgressFlag(Scene, FlagID)  -> 場景標記，切換場景即消失
    /// 3. Slot:           ProgressFlag(Slot, FlagID)   -> 時段標記，時間推進一個 Slot 即消失
    /// 4. Phase:          ProgressFlag(Phase, FlagID)  -> 階段標記，進入下一個時間大階段即消失
    /// 5. Day/Daily:      ProgressFlag(Day, FlagID)    -> 每日標記，隔天凌晨 (DayPassed) 即消失
    /// 6. Remove/Clear:   ProgressFlag(Remove, FlagID) -> 手動移除該 Flag
    /// 數值類
    /// 1. 數值設定：ProgressFlag(SetInt, VarID, 10) -> 直接將變數設為 10
    /// 2. 數值增減：ProgressFlag(AddInt, VarID, 5)  -> 將變數加 5 (減法請用 -5)
    public class SequencerCommandProgressFlag : SequencerCommand
    {
        public void Awake()
        {
            // 從 GameStatusService 取得 Model 實例
            var model = GameStatusService.Instance?.ProgressFlags;

            if (model == null)
            {
                Debug.LogWarning("[ProgressFlag] 找不到 ProgressFlags 實例。請確認 GameStatusService 已正確初始化。");
                Stop();
                return;
            }

            string action = GetParameter(0);
            string flagID = GetParameter(1);

            if (string.IsNullOrEmpty(flagID))
            {
                Debug.LogWarning("[ProgressFlag] 缺少 FlagID。用法：ProgressFlag(動作, FlagID)");
                Stop();
                return;
            }

            // --- 區塊 A：處理數值邏輯 ---

            // 設定數值
            if (IsAction(action, "SetInt", "SetValue", "SetV"))
            {
                int val = GetParameterAsInt(2, 0);
                model.SetValue(flagID, val);
                Stop(); // 執行完畢
                return; // 務必 return，否則會跑進下方的「區塊 B」導致報錯
            }
            // 增減數值
            else if (IsAction(action, "AddInt", "AddValue", "AddV"))
            {
                int amount = GetParameterAsInt(2, 1);
                model.AddValue(flagID, amount);
                Stop();
                return;
            }

            // --- 區塊 B：根據參數執行對應的生命週期方法 ---

            // 1. 永久 (Persistent)
            if (IsAction(action, "Add", "Set", "Permanent", "Persistent"))
            {
                model.AddPersistentFlag(flagID);
            }
            // 2. 場景 (Scene/Temp)
            else if (IsAction(action, "Scene", "Temp", "Temporary"))
            {
                model.AddSceneFlag(flagID);
            }
            // 3. 時段 (UntilNextSlot)
            else if (IsAction(action, "Slot"))
            {
                model.AddSlotFlag(flagID);
            }
            // 4. 階段 (UntilNextPhase)
            else if (IsAction(action, "Phase"))
            {
                model.AddPhaseFlag(flagID);
            }
            // 5. 每日 (UntilNextDay)
            else if (IsAction(action, "Day", "Daily"))
            {
                model.AddDailyFlag(flagID);
            }
            // 6. 移除 (Remove)
            else if (IsAction(action, "Remove", "Clear", "Unset"))
            {
                model.RemoveFlag(flagID);
            }
            else
            {
                // 如果跑到這裡，代表 action 既不是數值指令，也不是生命週期指令
                Debug.LogWarning($"[ProgressFlag] 未知的動作類型: {action}。可用選項: SetInt, AddInt, Add, Scene, Slot, Phase, Day, Remove。");
            }

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            foreach (var target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}