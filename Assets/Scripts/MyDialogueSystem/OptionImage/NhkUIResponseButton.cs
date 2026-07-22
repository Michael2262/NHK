// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardUIResponseButton，讓每顆選項按鈕可依對話節點的欄位顯示：
//   (1) 一張圖片：自訂 Text 欄位（預設 "Option Image"）填圖片 ID，
//       從 OptionImageDatabase 查對應 Sprite；空白 / 查不到 → 隱藏。
//   (2) 一段額外敘述：自訂 Text 欄位（預設 "Narration"，與字幕面板敘述框共用）直接填字
//       （如同對話文字，可用 "Narration ja" 等語言變體多語系）；空白 → 隱藏。
// 兩者都在按鈕被指派 response 時即時套用，選單關閉 / 尚未指派時保持隱藏。
//
// 使用方式：
//   1. 在 Response Button prefab 底下新增：一個空的 Image 子物件、一個空的文字子物件（TMP 或 Text）
//   2. 將 prefab 上的 StandardUIResponseButton 元件換成本元件
//      （換完記得重新拖 Button / Label 參照，以及 Menu Panel 上的按鈕參照）
//   3. Inspector 拖入 Icon Image、OptionImageDatabase、Narration Text
//   4. 需要圖片的選項節點：新增 Text 欄位 "Option Image"，填圖片 ID
//      需要敘述的選項節點：新增 Text 欄位 "Narration"，直接填敘述文字（與字幕面板敘述框共用）

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/Buttons/Nhk UI Response Button")]
    public class NhkUIResponseButton : StandardUIResponseButton
    {
        [Header("Option Image Settings")]
        [Tooltip("顯示選項圖片的 Image（按鈕底下的子物件，平時隱藏）")]
        [SerializeField] private UnityEngine.UI.Image iconImage;

        [Tooltip("圖片 ID → Sprite 的對照表")]
        [SerializeField] private OptionImageDatabase imageDatabase;

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，填圖片 ID。空白 = 不顯示圖片。")]
        [SerializeField] private string imageFieldName = "Option Image";

        [Header("Option Narration Settings")]
        [Tooltip("顯示選項額外敘述的文字元件（按鈕底下的子物件，平時隱藏）")]
        [SerializeField] private UITextField narrationText;

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，直接填敘述文字。空白 = 不顯示敘述。與字幕面板敘述框共用同一個 \"Narration\" 欄位。")]
        [SerializeField] private string narrationFieldName = "Narration";

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型。該欄位值填 ProgressFlag 名稱：\n" +
                 "留空 = 敘述照常顯示；有填 = 只有該 Flag 為 true 時才顯示敘述。")]
        [SerializeField] private string narrationFlagFieldName = "NarrationFlag";

        public override Response response
        {
            get { return base.response; }
            set
            {
                base.response = value;
                UpdateIcon(value);
                UpdateNarration(value);
            }
        }

        public override void Awake()
        {
            base.Awake();
            // 模板生成的按鈕會「先被指派 response、才 SetActive(true)」，
            // Awake 在啟用時才執行，比 UpdateIcon/UpdateNarration 晚——此時不能把剛設好的內容蓋掉。
            // 只在尚未指派 response 時才隱藏（確保閒置按鈕的初始狀態為空）。
            if (response == null)
            {
                HideIcon();
                HideNarration();
            }
        }

        public override void Reset()
        {
            base.Reset();
            HideIcon();
            HideNarration();
        }

        /// <summary>
        /// 依 response 目的節點的欄位查圖：有 ID 且查得到 → 顯示；否則隱藏。
        /// </summary>
        private void UpdateIcon(Response response)
        {
            if (iconImage == null) return;

            Sprite sprite = null;
            if (response != null && response.destinationEntry != null)
            {
                string id = Field.LookupValue(response.destinationEntry.fields, imageFieldName);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    if (imageDatabase == null)
                    {
                        if (DialogueDebug.logWarnings)
                        {
                            Debug.LogWarning($"Dialogue System: NhkUIResponseButton 未指定 OptionImageDatabase，無法顯示圖片 ID '{id}'。", this);
                        }
                    }
                    else
                    {
                        sprite = imageDatabase.GetSprite(id);
                        if (sprite == null && DialogueDebug.logWarnings)
                        {
                            Debug.LogWarning($"Dialogue System: OptionImageDatabase 查不到圖片 ID '{id}'（選項：'{response.destinationEntry.currentMenuText}'）。", this);
                        }
                    }
                }
            }

            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true; // 防止 Image 元件本身在 prefab 中被取消勾選
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                HideIcon();
            }
        }

        /// <summary>
        /// 依 response 目的節點的欄位取敘述：有字 → 顯示；否則隱藏。
        /// 用 LookupLocalizedValue，多語系機制與對話文字相同（維護 "Option Narration ja" 等變體）。
        /// </summary>
        private void UpdateNarration(Response response)
        {
            if (UITextField.IsNull(narrationText)) return;

            string narration = null;
            if (response != null && response.destinationEntry != null)
            {
                narration = Field.LookupLocalizedValue(response.destinationEntry.fields, narrationFieldName);
            }

            if (!string.IsNullOrWhiteSpace(narration) && IsNarrationAllowedByFlag(response.destinationEntry))
            {
                // 經 FormattedText 管線處理，支援 [em#]、[var=]、[lua(...)] 等標記（與對話文字一致）
                narrationText.text = UITools.GetUIFormattedText(FormattedText.Parse(narration));
                narrationText.SetActive(true);
            }
            else
            {
                HideNarration();
            }
        }

        /// <summary>
        /// 依 NarrationFlag 欄位決定是否允許顯示敘述：
        ///   - 欄位空白 → 允許（照常顯示）
        ///   - 欄位有填 Flag 名稱 → 只有該 Flag 在 ProgressFlags 中為 true 時才允許
        /// </summary>
        private bool IsNarrationAllowedByFlag(DialogueEntry entry)
        {
            if (entry == null) return false;
            string flag = Field.LookupValue(entry.fields, narrationFlagFieldName);
            if (string.IsNullOrWhiteSpace(flag)) return true;
            var flags = (GameStatusService.Instance != null) ? GameStatusService.Instance.ProgressFlags : null;
            return flags != null && flags.Contains(flag);
        }

        private void HideIcon()
        {
            if (iconImage == null) return;
            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
        }

        private void HideNarration()
        {
            if (UITextField.IsNull(narrationText)) return;
            narrationText.text = string.Empty;
            narrationText.SetActive(false);
        }
    }
}
