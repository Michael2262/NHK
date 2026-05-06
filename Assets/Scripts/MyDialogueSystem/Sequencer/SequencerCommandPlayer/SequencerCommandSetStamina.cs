// ============================================================
// SequencerCommandSetStamina.cs
// NHK compatibility version
// 舊指令名稱保留：SetStamina(value)
// NHK 語意：SetStress(value)
// ============================================================

using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSetStamina : SequencerCommand
    {
        public void Awake()
        {
            int value = Mathf.RoundToInt(GetParameterAsFloat(0));

            var service = GameStatusService.Instance;
            var protagonist = service != null ? service.Protagonist : null;

            if (protagonist != null)
            {
                protagonist.SetStress(value);
            }
            else
            {
                Debug.LogWarning("Dialogue System: SetStamina/NHK 指令找不到 GameStatusService 或 Protagonist 實例。");
            }

            Stop();
        }
    }
}
