// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardDialogueUI，實現「逐條控制」invalid response 的顯示：
//   - Show Invalid = true  → 條件不符時灰顯不可點（[em4]）
//   - Show Invalid = false → 條件不符時完全隱藏（預設行為）
//
// 使用方式：
//   1. Dialogue Manager → Input Settings → 勾選 Include Invalid Entries
//   2. Em Tag for Invalid Entries 設為 [em4]
//   3. Dialogue Database → Templates → Dialogue Entries 新增 Boolean 欄位 "Show Invalid"（預設 false），勾 Main
//   4. 將 Dialogue Manager 上的 StandardDialogueUI 替換為此腳本
//   5. 在需要灰顯的對話選項節點上，勾選 Show Invalid = true 並填寫 Conditions

using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard Dialogue UI (Selective Invalid)")]
    public class SelectiveInvalidDialogueUI : StandardDialogueUI
    {
        [Header("Selective Invalid Response Settings")]
        [Tooltip("Dialogue Entry 自訂欄位名稱，Boolean 類型。True = 條件不符時灰顯；False = 條件不符時隱藏。")]
        [SerializeField] private string showInvalidFieldName = "Show Invalid";

        /// <summary>
        /// 覆寫顯示選項的核心方法：在交給 base 之前，過濾掉不需要灰顯的 invalid response。
        /// </summary>
        protected override void ShowResponsesImmediate(Subtitle subtitle, Response[] responses, float timeout)
        {
            responses = FilterResponses(responses);
            base.ShowResponsesImmediate(subtitle, responses, timeout);
        }

        /// <summary>
        /// 過濾邏輯：
        ///   - response.enabled == true  → 條件通過，一律保留
        ///   - response.enabled == false → 檢查 "Show Invalid" 欄位
        ///       - true  → 保留（會以 [em4] 灰顯 + 不可點擊）
        ///       - false → 移除（完全不顯示）
        /// </summary>
        private Response[] FilterResponses(Response[] responses)
        {
            if (responses == null || responses.Length == 0) return responses;

            // 快速檢查：如果全部都是 enabled，不需要過濾
            bool hasAnyInvalid = false;
            for (int i = 0; i < responses.Length; i++)
            {
                if (!responses[i].enabled)
                {
                    hasAnyInvalid = true;
                    break;
                }
            }
            if (!hasAnyInvalid) return responses;

            // 逐條過濾
            var filtered = new List<Response>(responses.Length);
            for (int i = 0; i < responses.Length; i++)
            {
                var response = responses[i];

                if (response.enabled)
                {
                    // 條件通過，一律保留
                    filtered.Add(response);
                }
                else
                {
                    // 條件不通過：只在 Show Invalid = true 時保留（灰顯）
                    bool showInvalid = Field.LookupBool(
                        response.destinationEntry.fields,
                        showInvalidFieldName);

                    if (showInvalid)
                    {
                        filtered.Add(response);
                    }
                    else
                    {
                        if (DialogueDebug.logInfo)
                        {
                            Debug.Log($"Dialogue System: Hiding invalid response '{response.destinationEntry.currentMenuText}' (Show Invalid = false)");
                        }
                    }
                }
            }

            return filtered.ToArray();
        }
    }
}
