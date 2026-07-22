// Copyright (c) NHK Project. All rights reserved.
// 繼承 StandardDialogueUI，實現「逐條控制」選項的顯示與可用性。
// 每個選項節點可以有兩組獨立條件：
//
//   | 欄位                        | 角色                                   |
//   |-----------------------------|----------------------------------------|
//   | 原生 Conditions             | 出現條件：不符 → 隱藏（預設行為）      |
//   | Enable Conditions（自訂）   | 可用條件：不符 → 灰顯不可點（[em4]）   |
//
//   | 出現條件 | 可用條件           | 結果               |
//   |----------|--------------------|--------------------|
//   | 符合     | 符合（或欄位空白） | 顯示、可點         |
//   | 符合     | 不符               | 顯示、灰顯不可點   |
//   | 不符     | （不管）           | 隱藏               |
//
//   Enable Conditions 填 Lua 運算式（寫法與 Conditions 欄位相同），
//   例如 Variable["Trust"] >= 30；填 false 則永遠灰顯（舊 Ghost 行為）。
//
//   另保留舊欄位 "Show Invalid"（Boolean）：true = Conditions 不符時灰顯而非隱藏，
//   供既有節點沿用；新節點建議一律改用 Enable Conditions。
//
// 使用方式：
//   1. Dialogue Manager → Input Settings → 勾選 Include Invalid Entries
//   2. Em Tag for Invalid Entries 設為 [em4]
//   3. Dialogue Database → Templates → Dialogue Entries 新增 Text 欄位
//      "Enable Conditions"（勾 Main）；舊 Boolean 欄位 "Show Invalid" 照舊
//   4. 將 Dialogue Manager 上的 StandardDialogueUI 替換為此腳本
//   5. 選項節點上：Conditions 填出現條件（可空白 = 永遠出現），
//      Enable Conditions 填可用條件（可空白 = 永遠可用）

using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Nhk Dialogue UI")]
    public class NhkDialogueUI : StandardDialogueUI
    {
        [Header("Selective Invalid Response Settings")]
        [Tooltip("Dialogue Entry 自訂欄位名稱，Boolean 類型。True = Conditions 不符時灰顯；False = 不符時隱藏。（舊欄位，新節點建議改用 Enable Conditions）")]
        [SerializeField] private string showInvalidFieldName = "Show Invalid";

        [Tooltip("Dialogue Entry 自訂欄位名稱，Text 類型，填 Lua 運算式。空白 = 永遠可用；運算式為 false 時該選項灰顯不可點。")]
        [SerializeField] private string enableConditionsFieldName = "Enable Conditions";

        /// <summary>
        /// 覆寫顯示選項的核心方法：在交給 base 之前，
        /// 過濾掉不需要灰顯的 invalid response，並依 Enable Conditions 把選項轉為失能灰顯。
        /// </summary>
        protected override void ShowResponsesImmediate(Subtitle subtitle, Response[] responses, float timeout)
        {
            responses = FilterResponses(responses);
            base.ShowResponsesImmediate(subtitle, responses, timeout);
        }

        /// <summary>
        /// 過濾邏輯（逐條判斷）：
        ///   - response.enabled == true（Conditions 通過 → 會出現）：
        ///       - Enable Conditions 空白或評估為 true → 原樣保留（可點）
        ///       - Enable Conditions 評估為 false → 轉失能灰顯後保留（可見不可點）
        ///   - response.enabled == false（Conditions 不通過）：
        ///       - Show Invalid == true  → 保留（原生管線已包 [em4] 灰顯）
        ///       - Show Invalid == false → 移除（完全不顯示）
        /// </summary>
        private Response[] FilterResponses(Response[] responses)
        {
            if (responses == null || responses.Length == 0) return responses;

            var filtered = new List<Response>(responses.Length);
            for (int i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                var fields = response.destinationEntry.fields;

                if (response.enabled)
                {
                    // 出現條件通過 → 再檢查可用條件
                    string enableConditions = Field.LookupValue(fields, enableConditionsFieldName);
                    if (!string.IsNullOrWhiteSpace(enableConditions) && !Lua.IsTrue(enableConditions))
                    {
                        // 可用條件不符 → 灰顯不可點
                        MakeGhost(response);
                        if (DialogueDebug.logInfo)
                        {
                            Debug.Log($"Dialogue System: Disabling response '{response.destinationEntry.currentMenuText}' (Enable Conditions = false: {enableConditions})");
                        }
                    }
                    filtered.Add(response);
                }
                else if (Field.LookupBool(fields, showInvalidFieldName))
                {
                    // 出現條件不通過但 Show Invalid = true → 保留（[em4] 灰顯 + 不可點擊）
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
