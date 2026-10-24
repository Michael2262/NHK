using TMPro;
using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>分別顯示天數、phase_slot，以及目前 Mini 對應的鐘面時間。</summary>
    [AddComponentMenu("Game/State Test/Time UI")]
    public class TimeUI : MonoBehaviour
    {
        [Tooltip("天數，只顯示數字，例如 1。")]
        [SerializeField] private TMP_Text dayText;

        [Tooltip("階段_格數，例如 2_3。階段沿用 Model 索引，從 0 開始；格數從 1 開始。")]
        [SerializeField] private TMP_Text phaseSlotText;

        [Header("Mini 顯示時間")]
        [Tooltip("專門顯示目前 Mini 對應的時間，例如 08:05。")]
        [SerializeField] private TMP_Text timeText;

        [Tooltip("指定整合時間設定視窗使用的 TimeMapping 資產。")]
        [SerializeField] private TimeMappingSO timeMapping;

        private GameStatusService service;
        private TimeSystemModel model;

        private void OnEnable() => RefreshUI();

        private void Update()
        {
            // 只檢查來源是否就緒或更換，文字平時由事件刷新。
            var current = GameStatusService.Instance;
            var currentModel = current != null ? current.Time : null;
            if (!ReferenceEquals(service, current) || !ReferenceEquals(model, currentModel))
                RefreshUI();
        }

        private void OnDisable() => Unbind();

        private void Unbind()
        {
            if (model != null)
            {
                model.OnTimeSlotAdvanced -= HandleSlotAdvanced;
                model.OnMiniAdvanced -= HandleSlotAdvanced;
                model.OnPhaseChanged -= RefreshUI;
                model.OnDayPassed -= RefreshUI;
            }
            if (!ReferenceEquals(service, null))
                service.OnGameStatusLoaded -= RefreshUI;
            model = null;
            service = null;
        }

        private void Bind()
        {
            var current = GameStatusService.Instance;
            var currentModel = current != null ? current.Time : null;
            if (ReferenceEquals(service, current) && ReferenceEquals(model, currentModel)) return;

            Unbind();
            service = current;
            model = currentModel;
            if (service != null) service.OnGameStatusLoaded += RefreshUI;
            if (model != null)
            {
                model.OnTimeSlotAdvanced += HandleSlotAdvanced;
                model.OnMiniAdvanced += HandleSlotAdvanced;
                model.OnPhaseChanged += RefreshUI;
                model.OnDayPassed += RefreshUI;
            }
        }

        private void HandleSlotAdvanced(int slotsAdvanced) => RefreshUI();

        /// <summary>啟用、時間推進、新遊戲及讀檔後刷新，也可由 UnityEvent 手動呼叫。</summary>
        public void RefreshUI()
        {
            if (!isActiveAndEnabled) return;
            Bind();
            if (dayText != null)
                dayText.text = model != null ? model.DayIndex.ToString() : "—";
            if (phaseSlotText != null)
                phaseSlotText.text = model != null
                    ? $"{model.CurrentPhaseIndex}_{model.CurrentSlotInPhase}"
                    : "—";
            if (timeText != null)
                timeText.text = model != null && timeMapping != null
                    ? timeMapping.GetTimeDisplay(model.CurrentPhaseIndex, model.CurrentSlotInPhase, model.CurrentMiniInSlot)
                    : "--:--";
        }
    }
}
