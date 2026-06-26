using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AutoSave — 執行自動存檔                                          ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AutoSave()                                                  ║
    // ║                                                                    ║
    // ║  行為：                                                            ║
    // ║  呼叫 GameStatusService.Instance.AutoSave()，                      ║
    // ║  將目前遊戲狀態存入自動存檔槽（槽位 0）。                          ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AutoSave()   → 立即自動存檔                                      ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAutoSave : SequencerCommand
    {
        public void Awake()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AutoSave] 找不到 GameStatusService！");
                Stop();
                return;
            }

            service.AutoSave();
            Debug.Log("[AutoSave] 自動存檔完成。");
            Stop();
        }
    }
}
