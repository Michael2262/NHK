// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardDialogueUI，實現「逐條控制」invalid response 的顯示。
// 每個選項節點有兩個自訂 Boolean 欄位控制行為，組合出三種模式：
//
//   | 模式          | Ghost | Show Invalid | 條件符合      | 條件不符      |
//   |---------------|-------|--------------|---------------|---------------|
//   | Hide（預設）  | false | false        | 顯示、可點    | 隱藏          |
//   | Show Invalid  | false | true         | 顯示、可點    | 顯示、灰顯    |
//   | Ghost         | true  | （略）       | 顯示、灰顯    | 隱藏          |
//
//   Ghost = 「可見性照常（條件符合才出現），但一旦出現就永遠失能灰顯」。
//   Ghost 為 true 時優先於 Show Invalid，灰顯外觀沿用同一個 [em4]。
//
// 使用方式：
//   1. Dialogue Manager → Input Settings → 勾選 Include Invalid Entries
//   2. Em Tag for Invalid Entries 設為 [em4]
//   3. Dialogue Database → Templates → Dialogue Entries 新增 Boolean 欄位
//      "Show Invalid"（預設 false）與 "Ghost"（預設 false），勾 Main
//   4. 將 Dialogue Manager 上的 StandardDialogueUI 替換為此腳本
//   5. 灰顯選項：勾 Show Invalid = true 並填 Conditions
//      幽靈選項：勾 Ghost = true 並填 Conditions（條件符合才出現，但永遠不可點）

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

        [Tooltip("Dialogue Entry 自訂欄位名稱，Boolean 類型。True = 條件符合才出現，但永遠失能灰顯（幽靈選項）。優先於 Show Invalid。")]
        [SerializeField] private string ghostFieldName = "Ghost";

        /// <summary>
        /// 覆寫顯示選項的核心方法：在交給 base 之前，過濾掉不需要灰顯的 invalid response，
        /// 並把 Ghost 選項強制改成失能灰顯。
        /// </summary>
        protected override void ShowResponsesImmediate(Subtitle subtitle, Response[] responses, float timeout)
        {
            responses = FilterResponses(responses);
            base.ShowResponsesImmediate(subtitle, responses, timeout);
        }

        /// <summary>
        /// 過濾邏輯（逐條判斷）：
        ///   - Ghost == true：
        ///       - response.enabled == true  → 條件通過 → 強制失能灰顯後保留（幽靈：可見但不可點）
        ///       - response.enabled == false → 條件不通過 → 移除（不顯示）
        ///   - Ghost == false：
        ///       - response.enabled == true  → 條件通過 → 原樣保留
        ///       - response.enabled == false → 檢查 "Show Invalid"：true 保留灰顯、false 移除
        /// </summary>
        private Response[] FilterResponses(Response[] responses)
        {
            if (responses == null || responses.Length == 0) return responses;

            var filtered = new List<Response>(responses.Length);
            for (int i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                var fields = response.destinationEntry.fields;

                if (Field.LookupBool(fields, ghostFieldName))
                {
                    // 幽靈選項：可見性照常（條件通過才出現），但出現時一律失能灰顯
                    if (response.enabled)
                    {
                        MakeGhost(response);
                        filtered.Add(response);
                    }
                    else if (DialogueDebug.logInfo)
                    {
                        Debug.Log($"Dialogue System: Hiding ghost response '{response.destinationEntry.currentMenuText}' (condition not met)");
                    }
                    continue;
                }

                if (response.enabled)
                {
                    // 條件通過，一律保留
                    filtered.Add(response);
                }
                else if (Field.LookupBool(fields, showInvalidFieldName))
                {
                    // 條件不通過但 Show Invalid = true → 保留（[em4] 灰顯 + 不可點擊）
                    filtered.Add(response);
                }
                else if (DialogueDebug.logInfo)
                {
                    Debug.Log($"Dialogue System: Hiding invalid response '{response.destinationEntry.currentMenuText}' (Show Invalid = false)");
                }
            }

            return filtered.ToArray();
        }

        /// <summary>
        /// 把一個「條件通過、可點」的 response 改成失能灰顯（幽靈狀態）。
        /// 作法與 ConversationModel 處理 invalid response 完全一致：
        /// 用 emTagForInvalidResponses 重新包 [em4]，並關閉 forceAuto / forceMenu，最後 enabled = false。
        /// </summary>
        private void MakeGhost(Response response)
        {
            var emTag = EmTag.None;
            if (DialogueManager.displaySettings != null && DialogueManager.displaySettings.inputSettings != null)
            {
                emTag = DialogueManager.displaySettings.inputSettings.emTagForInvalidResponses;
            }

            string text = response.destinationEntry.responseButtonText;
            if (emTag != EmTag.None)
            {
                text = UITools.StripEmTags(text);
                text = string.Format("[em{0}]{1}[/em{0}]", (int)emTag, text);
            }

            var database = DialogueManager.masterDatabase;
            var emphasisSettings = (database != null) ? database.emphasisSettings : null;
            var formattedText = FormattedText.Parse(text, emphasisSettings);
            formattedText.forceAuto = false;
            formattedText.forceMenu = false;

            response.formattedText = formattedText;
            response.enabled = false;
        }
    }
}
