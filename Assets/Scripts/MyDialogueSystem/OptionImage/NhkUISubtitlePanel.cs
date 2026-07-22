// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardUISubtitlePanel，讓「額外敘述」成為字幕面板自己的一部分：
//   - 敘述框（文字元件 + 可選容器）是面板底下的子物件，造型跟著該面板走
//   - 不同的 Dialogue UI Panel 各自繼承本元件、各自擺自己的敘述框造型
//   - 面板顯示某句字幕時（SetContent），讀該行節點的自訂 Text 欄位（預設 "Narration"）
//       有字 → 填入敘述框並開啟；空白 → 關閉
//   - 面板關閉（Close）→ 敘述框一併收掉
//   - 沒指定敘述框 UI → 什麼都不做（不顯示）
//
// 填法與對話文字相同：直接在欄位打字，要多語系就加 "Narration ja" 等語言變體，不需要 text table。
//
// 使用方式：
//   1. 在字幕面板 prefab 底下做一個敘述框（容器 GameObject + 文字元件 TMP 或 Text），平時可關閉
//   2. 將該面板上的 StandardUISubtitlePanel 元件換成本元件
//      （換完記得重新拖面板原有的參照：Panel / Subtitle Text / Portrait 等）
//   3. Inspector 拖入 Narration Text（與可選的 Narration Container）
//   4. 需要敘述的對話節點：新增 Text 欄位 "Narration"，直接填敘述文字

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/Dialogue/Nhk UI Subtitle Panel")]
    public class NhkUISubtitlePanel : StandardUISubtitlePanel
    {
        [Header("Extra Narration Settings")]
        [Tooltip("顯示額外敘述的文字元件（面板底下的子物件，TMP 或 Text）")]
        [SerializeField] private UITextField narrationText;

        [Tooltip("（可選）要跟著一起開關的容器 GameObject，例如帶背景的敘述框。留空則只開關文字元件本身。")]
        [SerializeField] private GameObject narrationContainer;

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，直接填敘述文字。空白 = 不顯示。")]
        [SerializeField] private string narrationFieldName = "Narration";

        public override void SetContent(Subtitle subtitle)
        {
            base.SetContent(subtitle);
            UpdateNarration(subtitle);
        }

        public override void Close()
        {
            HideNarration();
            base.Close();
        }

        /// <summary>
        /// 依當前字幕行節點的欄位取敘述：有字 → 顯示；否則隱藏。
        /// 用 LookupLocalizedValue，多語系機制與對話文字相同（維護 "Narration ja" 等變體）。
        /// </summary>
        private void UpdateNarration(Subtitle subtitle)
        {
            if (UITextField.IsNull(narrationText)) return;

            string narration = null;
            if (subtitle != null && subtitle.dialogueEntry != null)
            {
                narration = Field.LookupLocalizedValue(subtitle.dialogueEntry.fields, narrationFieldName);
            }

            if (!string.IsNullOrWhiteSpace(narration))
            {
                if (narrationContainer != null) narrationContainer.SetActive(true);
                narrationText.text = narration;
                narrationText.SetActive(true);
            }
            else
            {
                HideNarration();
            }
        }

        private void HideNarration()
        {
            if (!UITextField.IsNull(narrationText))
            {
                narrationText.text = string.Empty;
                narrationText.SetActive(false);
            }
            if (narrationContainer != null) narrationContainer.SetActive(false);
        }
    }
}
