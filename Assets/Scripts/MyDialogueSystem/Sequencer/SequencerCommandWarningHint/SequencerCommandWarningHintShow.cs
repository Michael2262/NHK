using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: WarningHintShow([left|right|center], [normal|medium|fast], [normal|medium|fast])
    /// 所有參數皆可省略，預設為 center / normal / normal
    /// </summary>
    public class SequencerCommandWarningHintShow : SequencerCommand
    {
        public void Awake()
        {
            if (WarningHintController.Instance != null)
            {
                var position = ParsePosition(GetParameter(0));
                var duration = ParseIntensity(GetParameter(1), WarningHintController.Intensity.Normal);
                var blink = ParseIntensity(GetParameter(2), WarningHintController.Intensity.Normal);

                WarningHintController.Instance.ShowWarning(position, duration, blink);
            }

            Stop();
        }

        private WarningHintController.Position ParsePosition(string value)
        {
            switch (value?.Trim().ToLower())
            {
                case "left": return WarningHintController.Position.Left;
                case "right": return WarningHintController.Position.Right;
                default: return WarningHintController.Position.Center;
            }
        }

        private WarningHintController.Intensity ParseIntensity(string value, WarningHintController.Intensity defaultValue)
        {
            switch (value?.Trim().ToLower())
            {
                case "normal": return WarningHintController.Intensity.Normal;
                case "medium": return WarningHintController.Intensity.Medium;
                case "fast": return WarningHintController.Intensity.Fast;
                default: return defaultValue;
            }
        }
    }
}