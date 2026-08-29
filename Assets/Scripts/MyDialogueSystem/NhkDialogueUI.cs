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

        // ===================== Queue 對話無縫交接 =====================
        //
        // Dialogue System 結束一段對話時，會先呼叫 Dialogue UI.Close()，
        // 逐一關閉 Subtitle / Menu Panel，之後才發出 conversationEnded。
        // StoryManager 的 Queue 若等到 conversationEnded 才保護面板，已經太遲。
        //
        // 因此 StoryManager 在 A 還在播放時預先準備「只吞掉下一次 Close」。
        // A 結束後 UI 維持原狀；B 第一句若使用同一字幕板，只更新內容；
        // 若使用不同字幕板，交回 StandardUISubtitleControls 正常切板。
        private bool _suppressNextCloseForQueuedHandoff;
        private bool _queuedHandoffCloseWasSuppressed;

        /// <summary>準備吞掉下一次由「對話結束」引發的 Close。</summary>
        public void PrepareQueuedHandoff()
        {
            _suppressNextCloseForQueuedHandoff = true;
        }

        /// <summary>
        /// 取消待命的 Queue 交接。若上一段對話的 Close 已被吞掉，
        /// 代表已沒有下一段可接力，此時補做正常關閉，避免 UI 永久卡住。
        /// </summary>
        public void CancelQueuedHandoff()
        {
            _suppressNextCloseForQueuedHandoff = false;

            if (!_queuedHandoffCloseWasSuppressed) return;

            _queuedHandoffCloseWasSuppressed = false;
            if (isOpen) base.Close();
        }

        public override void Open()
        {
            // 新對話進入 Open 就代表上一次交接已成功。
            // 放在 StartConversation 的 UI.Open 階段清除，可正確處理「啟動後立即結束」的極短對話：
            // 若它後面還有一段 Queue，隨後的 Close 會再正確留下新的抑止記號。
            _queuedHandoffCloseWasSuppressed = false;
            base.Open();
        }

        public override void Close()
        {
            if (_suppressNextCloseForQueuedHandoff)
            {
                _suppressNextCloseForQueuedHandoff = false; // 一次性，只吞這次 Close
                _queuedHandoffCloseWasSuppressed = true;
                return;
            }

            _queuedHandoffCloseWasSuppressed = false;
            base.Close();
        }

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
