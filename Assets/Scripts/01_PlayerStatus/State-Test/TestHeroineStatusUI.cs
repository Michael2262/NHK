using TMPro;
using UnityEngine;

namespace NHK.StateTest
{
    /// <summary>只顯示測試版最終數值；名稱標籤由場景自行配置，欄位可留空。</summary>
    [AddComponentMenu("Game/State Test/Test Heroine Status UI")]
    public class TestHeroineStatusUI : MonoBehaviour
    {
        [Tooltip("信賴；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text trustText;
        [Tooltip("心情；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text moodText;
        [Tooltip("好感積分；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text affinityText;
        [Tooltip("性興味；只顯示值，不附加名稱。")]
        [SerializeField] private TMP_Text libidoText;

        private GameStatusService service;
        private HeroineStatusModel model;

        private void OnEnable() => RefreshUI();

        private void Update()
        {
            // 只檢查資料來源；正常狀態由事件刷新文字，不每幀重寫。
            // 也支援 UI 比 Service 先啟用或 Service 在執行期間重建。
            var current = GameStatusService.Instance;
            var currentModel = current != null ? current.TestHeroine : null;
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
            var currentModel = current != null ? current.TestHeroine : null;
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
            SetText(trustText, model != null ? model.Trust.ToString() : "—");
            SetText(moodText, model != null ? model.Mood.ToString() : "—");
            SetText(affinityText, model != null ? model.Affinity.ToString() : "—");
            SetText(libidoText, model != null ? model.Libido.ToString() : "—");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}

