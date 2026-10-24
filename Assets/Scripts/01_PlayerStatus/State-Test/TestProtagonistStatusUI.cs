using TMPro;
using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>只顯示正式主角最終數值；名稱標籤由場景自行配置，欄位可留空。</summary>
    [AddComponentMenu("Game/State Test/Test Protagonist Status UI")]
    public class TestProtagonistStatusUI : MonoBehaviour
    {
        [Tooltip("體力：當前值/最大值；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text staminaText;
        [Tooltip("射精次數：當前值/最大值；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text shootTimesText;
        [Tooltip("邪念；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text lustText;
        [Tooltip("貼紙數量；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text stickerCountText;
        [Tooltip("達標未領貼紙數量；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text pendingStickerCountText;
        [Tooltip("金錢；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text moneyText;

        private GameStatusService service;
        private global::ProtagonistStatusModel model;

        private void OnEnable() => RefreshUI();

        private void Update()
        {
            // 只檢查資料來源；正常狀態由事件刷新文字，不每幀重寫。
            // 也支援 UI 比 Service 先啟用或 Service 在執行期間重建。
            var current = GameStatusService.Instance;
            var currentModel = current != null ? current.Protagonist : null;
            if (!ReferenceEquals(service, current) || !ReferenceEquals(model, currentModel))
                RefreshUI();
        }

        private void OnDisable() => Unbind();

        private void Unbind()
        {
            if (model != null) model.OnChanged -= RefreshUI;
            if (!ReferenceEquals(service, null)) service.OnGameStatusLoaded -= RefreshUI;
            model = null;
            service = null;
        }

        private void Bind()
        {
            var current = GameStatusService.Instance;
            var currentModel = current != null ? current.Protagonist : null;
            if (ReferenceEquals(service, current) && ReferenceEquals(model, currentModel)) return;

            Unbind();
            service = current;
            model = currentModel;
            if (service != null) service.OnGameStatusLoaded += RefreshUI;
            if (model != null) model.OnChanged += RefreshUI;
        }

        /// <summary>可手動呼叫；啟用時、數值變更及新遊戲／讀檔通知後自動執行。</summary>
        public void RefreshUI()
        {
            if (!isActiveAndEnabled) return;
            Bind();
            SetText(staminaText, model != null ? $"{model.Stamina}/{model.StaminaMax}" : "—");
            SetText(shootTimesText, model != null ? $"{model.ShootTimes}/{model.ShootTimesMax}" : "—");
            SetText(lustText, model != null ? model.Lust.ToString() : "—");
            SetText(stickerCountText, model != null ? model.StickerCount.ToString() : "—");
            SetText(pendingStickerCountText, model != null ? model.PendingStickerCount.ToString() : "—");
            SetText(moneyText, model != null ? model.Money.ToString() : "—");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}

