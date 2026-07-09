// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardUIResponseButton，讓每顆選項按鈕可依對話節點的欄位顯示一張圖片：
//   - 選項節點的自訂 Text 欄位（預設 "Option Image"）填圖片 ID
//   - 按鈕被指派 response 時，從 OptionImageDatabase 查 ID 對應的 Sprite 顯示
//   - 欄位空白 / 查不到 / 沒指派 → 圖片物件隱藏（平時為空）
//
// 使用方式：
//   1. 在 Response Button prefab 底下新增一個空的 Image 子物件（位置大小自排）
//   2. 將 prefab 上的 StandardUIResponseButton 元件換成本元件
//      （換完記得重新拖 Button / Label 參照，以及 Menu Panel 上的按鈕參照）
//   3. Inspector 拖入 Icon Image 與 OptionImageDatabase
//   4. 需要圖片的選項節點：新增 Text 欄位 "Option Image"，填圖片 ID

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/Buttons/Option Image Response Button")]
    public class OptionImageResponseButton : StandardUIResponseButton
    {
        [Header("Option Image Settings")]
        [Tooltip("顯示選項圖片的 Image（按鈕底下的子物件，平時隱藏）")]
        [SerializeField] private UnityEngine.UI.Image iconImage;

        [Tooltip("圖片 ID → Sprite 的對照表")]
        [SerializeField] private OptionImageDatabase imageDatabase;

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，填圖片 ID。空白 = 不顯示圖片。")]
        [SerializeField] private string imageFieldName = "Option Image";

        public override Response response
        {
            get { return base.response; }
            set
            {
                base.response = value;
                UpdateIcon(value);
            }
        }

        public override void Awake()
        {
            base.Awake();
            // 模板生成的按鈕會「先被指派 response、才 SetActive(true)」，
            // Awake 在啟用時才執行，比 UpdateIcon 晚——此時不能把剛設好的圖蓋掉。
            // 只在尚未指派 response 時才隱藏（確保閒置按鈕的初始狀態為空）。
            if (response == null) HideIcon();
        }

        public override void Reset()
        {
            base.Reset();
            HideIcon();
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
                            Debug.LogWarning($"Dialogue System: OptionImageResponseButton 未指定 OptionImageDatabase，無法顯示圖片 ID '{id}'。", this);
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

        private void HideIcon()
        {
            if (iconImage == null) return;
            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
        }
    }
}
