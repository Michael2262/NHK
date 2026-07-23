// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardUIResponseButton，讓每顆選項按鈕可依對話節點的欄位顯示：
//   (1) 一張圖片：自訂 Text 欄位（預設 "Option Image"）填圖片 ID，
//       從 OptionImageDatabase 查對應 Sprite；空白 / 查不到 → 隱藏。
//   (2) 一段額外敘述：自訂 Text 欄位（預設 "Narration"，與字幕面板敘述框共用）直接填字
//       （如同對話文字，可用 "Narration ja" 等語言變體多語系）；空白 → 隱藏。
//   (3) 按鈕樣式：自訂 Text 欄位（預設 "Button Style"）填樣式 ID，同一個 ID 會套用兩種機制：
//       - 皮膚子物件：啟用 ID 對應的皮膚子物件、關閉其餘（版型差異大時用）
//       - 背景圖：從對照表查 ID 對應 Sprite 換到指定 Image（輕量，只換底圖時用）
//       ID 空白或查不到 → 還原成預設皮膚 / 原始背景圖。
// 以上都在按鈕被指派 response 時即時套用；Design-Time 按鈕會重複使用，
// 因此在 Reset()（每次選單前由 StandardUIMenuPanel 呼叫）還原，避免樣式殘留。
//
// 使用方式：
//   1. 在 Response Button prefab 底下新增：一個空的 Image 子物件、一個空的文字子物件（TMP 或 Text）
//   2. 將 prefab 上的 StandardUIResponseButton 元件換成本元件
//      （換完記得重新拖 Button / Label 參照，以及 Menu Panel 上的按鈕參照）
//   3. Inspector 拖入 Icon Image、OptionImageDatabase、Narration Text
//   4. 需要圖片的選項節點：新增 Text 欄位 "Option Image"，填圖片 ID
//      需要敘述的選項節點：新增 Text 欄位 "Narration"，直接填敘述文字（與字幕面板敘述框共用）
//      需要換樣式的選項節點：新增 Text 欄位 "Button Style"，填樣式 ID

using System.Collections.Generic;
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

        [Header("Button Style Settings")]
        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，填樣式 ID。空白 = 使用預設樣式。\n" +
                 "同一個 ID 會同時套用「皮膚子物件」與「背景圖」兩種機制（各自查得到才套）。")]
        [SerializeField] private string styleFieldName = "Button Style";

        [Tooltip("皮膚子物件清單：ID 對應要啟用的子物件。套用時只啟用對應的那一個，清單內其餘全部關閉。")]
        [SerializeField] private List<ButtonSkin> styleSkins = new List<ButtonSkin>();

        [Tooltip("沒填 ID（或 ID 在清單中找不到）時要啟用的預設皮膚子物件。可留空。")]
        [SerializeField] private GameObject defaultSkin;

        [Tooltip("（輕量方案）要套用背景圖的 Image，通常是按鈕本身的底圖。留空 = 不做背景圖切換。")]
        [SerializeField] private UnityEngine.UI.Image styleBackgroundImage;

        [Tooltip("（輕量方案）樣式 ID → 背景 Sprite 的對照表。\n" +
                 "建議另建一個獨立 asset，不要跟 Option Image 的對照表混用。")]
        [SerializeField] private OptionImageDatabase styleSpriteDatabase;

        /// <summary>皮膚對照：樣式 ID → 要啟用的子物件。</summary>
        [System.Serializable]
        public class ButtonSkin
        {
            [Tooltip("樣式 ID，對話節點的 Button Style 欄位填這個字串")]
            public string id;

            [Tooltip("該 ID 對應要啟用的皮膚子物件")]
            public GameObject skinObject;
        }

        // 背景圖原始值（延遲擷取，見 CaptureOriginalBackground）
        private bool m_capturedOriginalBackground;
        private Sprite m_originalBackgroundSprite;

        public override Response response
        {
            get { return base.response; }
            set
            {
                base.response = value;
                UpdateIcon(value);
                UpdateNarration(value);
                UpdateStyle(value);
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
                ResetStyle();
            }
        }

        public override void Reset()
        {
            base.Reset();
            HideIcon();
            HideNarration();
            // Design-Time 按鈕會重複使用，每次選單前 StandardUIMenuPanel.ClearResponseButtons()
            // 都會呼叫 Reset()，在這裡還原預設樣式，避免上一輪的樣式殘留到別的選項。
            ResetStyle();
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

        /// <summary>
        /// 依 response 目的節點的 Button Style 欄位套用樣式（皮膚子物件 + 背景圖）。
        /// 兩種機制共用同一個 ID，各自查得到才套；查不到就用預設。
        /// </summary>
        private void UpdateStyle(Response response)
        {
            string id = null;
            if (response != null && response.destinationEntry != null)
            {
                id = Field.LookupValue(response.destinationEntry.fields, styleFieldName);
            }
            ApplySkin(id);
            ApplyBackground(id);
        }

        /// <summary>還原成預設樣式（預設皮膚 + 原始背景圖）。</summary>
        private void ResetStyle()
        {
            ApplySkin(null);
            ApplyBackground(null);
        }

        /// <summary>
        /// 皮膚切換：關閉清單內所有皮膚，只啟用 ID 對應的那一個；
        /// ID 空白或找不到時啟用 defaultSkin。
        /// </summary>
        private void ApplySkin(string id)
        {
            bool hasSkins = (styleSkins != null && styleSkins.Count > 0);
            if (!hasSkins && defaultSkin == null) return; // 沒設定皮膚機制就不處理

            GameObject matched = null;
            if (hasSkins)
            {
                for (int i = 0; i < styleSkins.Count; i++)
                {
                    var skin = styleSkins[i];
                    if (skin == null || skin.skinObject == null) continue;
                    if (!string.IsNullOrWhiteSpace(id) && string.Equals(skin.id, id))
                    {
                        matched = skin.skinObject;
                    }
                    skin.skinObject.SetActive(false);
                }
            }
            if (defaultSkin != null) defaultSkin.SetActive(false);

            var target = (matched != null) ? matched : defaultSkin;
            if (target != null) target.SetActive(true);
        }

        /// <summary>
        /// 背景圖切換（輕量方案）：ID 查得到就換圖，否則還原成原始背景圖。
        /// </summary>
        private void ApplyBackground(string id)
        {
            if (styleBackgroundImage == null) return;
            CaptureOriginalBackground();

            Sprite sprite = null;
            if (!string.IsNullOrWhiteSpace(id) && styleSpriteDatabase != null)
            {
                sprite = styleSpriteDatabase.GetSprite(id);
            }
            styleBackgroundImage.sprite = (sprite != null) ? sprite : m_originalBackgroundSprite;
        }

        /// <summary>
        /// 延遲擷取原始背景圖：第一次要套樣式時才記錄。
        /// 不放在 Awake 是因為 Design-Time 按鈕若一開始是停用狀態，
        /// Awake 會晚於 response 指派，屆時抓到的會是「已套過樣式」的圖而非原始圖。
        /// </summary>
        private void CaptureOriginalBackground()
        {
            if (m_capturedOriginalBackground) return;
            m_capturedOriginalBackground = true;
            m_originalBackgroundSprite = styleBackgroundImage.sprite;
        }
    }
}
