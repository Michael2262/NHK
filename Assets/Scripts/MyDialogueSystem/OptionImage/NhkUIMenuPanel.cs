using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// 自動產生選項時，依節點的 Button Style 選擇完整模板。
    /// 原生流程繼續負責文字、點擊、可用狀態、編號、導覽與關閉時的回收。
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/Dialogue/Nhk UI Menu Panel")]
    public class NhkUIMenuPanel : StandardUIMenuPanel
    {
        [Serializable]
        public class ButtonPrefabStyle
        {
            [Tooltip("對話選項 Button Style 欄位填入的 ID，區分大小寫。")]
            public string id;

            [Tooltip("完整按鈕模板的根物件須同時有 Button 與 NhkUIResponseButton（或 StandardUIResponseButton）。")]
            public StandardUIResponseButton prefab;
        }

        [SerializeField, Tooltip("選項節點的樣式欄位名稱。未填 ID 或查不到時使用原本的 Button Template。")]
        private string buttonStyleFieldName = "Button Style";

        [SerializeField, Tooltip("樣式 ID 對應完整 Button Prefab。僅適用 Auto-Created Buttons；固定位置的 Buttons 維持原生處理。")]
        private List<ButtonPrefabStyle> buttonPrefabStyles = new List<ButtonPrefabStyle>();

        private Queue<StandardUIResponseButton> pendingTemplates;
        private readonly Dictionary<GameObject, StandardUIResponseButton> instanceTemplates =
            new Dictionary<GameObject, StandardUIResponseButton>();
        private readonly HashSet<string> warnings = new HashSet<string>();

        public override void Awake()
        {
            base.Awake();
            // 也允許使用場景內的模板；只有場景物件需要先隱藏，不修改 Prefab 資產。
            if (buttonPrefabStyles == null) return;
            foreach (var style in buttonPrefabStyles)
            {
                if (style != null && IsValidTemplate(style.prefab) && style.prefab.gameObject.scene.IsValid())
                    style.prefab.gameObject.SetActive(false);
            }
        }

        protected override void SetResponseButtons(Response[] responses, Transform target)
        {
            // 原生實作以 buttons != null 作為產生模板按鈕的前提。
            if (buttons == null) buttons = new StandardUIResponseButton[0];
            var previousTemplates = pendingTemplates;
            pendingTemplates = new Queue<StandardUIResponseButton>();
            if (responses != null && buttonTemplate != null && buttonTemplateHolder != null)
            {
                foreach (var response in responses)
                {
                    // 原生先處理指定位置的選項，之後才依序生成未指定位置的選項。
                    if (response != null && response.formattedText != null &&
                        response.formattedText.position == FormattedText.NoAssignedPosition)
                        pendingTemplates.Enqueue(ResolveTemplate(response));
                }
            }
            try
            {
                base.SetResponseButtons(responses, target);
            }
            finally
            {
                pendingTemplates = previousTemplates;
            }
        }

        private StandardUIResponseButton ResolveTemplate(Response response)
        {
            string id = response.destinationEntry != null && !string.IsNullOrEmpty(buttonStyleFieldName)
                ? Field.LookupValue(response.destinationEntry.fields, buttonStyleFieldName) : null;
            if (string.IsNullOrWhiteSpace(id)) return buttonTemplate;
            if (buttonPrefabStyles != null)
            {
                foreach (var style in buttonPrefabStyles)
                {
                    if (style == null || !string.Equals(style.id, id, StringComparison.Ordinal)) continue;
                    if (IsValidTemplate(style.prefab)) return style.prefab;
                    WarnOnce(id, "樣式 '" + id + "' 的模板無效：根物件需要 Button，且 Response Button 的 Button 引用須指向根物件。改用預設模板。");
                    return buttonTemplate;
                }
            }
            WarnOnce(id, "找不到 Button Style '" + id + "' 的完整模板，改用預設 Button Template。");
            return buttonTemplate;
        }

        private static bool IsValidTemplate(StandardUIResponseButton template)
        {
            if (template == null) return false;
            var rootButton = template.GetComponent<Button>();
            return rootButton != null && (template.button == null || template.button == rootButton);
        }

        private void WarnOnce(string id, string message)
        {
            if (warnings.Add(id)) Debug.LogWarning("[NhkUIMenuPanel] " + message, this);
        }

        protected override GameObject InstantiateButton()
        {
            var template = pendingTemplates != null && pendingTemplates.Count > 0
                ? pendingTemplates.Dequeue() : buttonTemplate;
            if (!IsValidTemplate(template))
            {
                WarnOnce("__default", "預設 Button Template 無效，請指定根物件有 Button 的完整按鈕模板。");
                return null;
            }

            // 原生回收池混放各種樣式，取出時按來源模板比對，避免取消樣式被一般選項誤用。
            for (int i = instantiatedButtonPool.Count - 1; i >= 0; i--)
            {
                var candidate = instantiatedButtonPool[i];
                if (candidate == null)
                {
                    instantiatedButtonPool.RemoveAt(i);
                    if (!ReferenceEquals(candidate, null)) instanceTemplates.Remove(candidate);
                    continue;
                }
                if (!instanceTemplates.TryGetValue(candidate, out var source) || source != template) continue;
                instantiatedButtonPool.RemoveAt(i);
                candidate.GetComponent<StandardUIResponseButton>().Reset();
                return candidate;
            }

            var instance = Instantiate(template.gameObject, buttonTemplateHolder.transform, false);
            instanceTemplates.Add(instance, template);
            return instance;
        }
    }
}
