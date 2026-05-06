// 檔名：SequencerCommandSetSpriteByName.cs
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;
// 移除 Dictionary，因為 Resources.Load 本身就有快取機制，
// 或者我們希望由 Unity 自行管理記憶體，不要強行 Hold 住 reference。

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：SetSpriteByName(UI物件名, 圖片路徑)
    /// 範例 1：SetSpriteByName(TachieImage, Sister/Angry)
    /// 範例 2：SetSpriteByName(TachieImage, none)  <-- 隱藏圖片
    /// 
    /// 注意：圖片必須放在 Assets/Resources/ 下 (建議分類，如 Assets/Resources/Sister/Angry)
    /// </summary>
    public class SequencerCommandSetSpriteByName : SequencerCommand
    {
        public void Awake()
        {
            string goName = GetParameter(0);
            string spritePath = GetParameter(1); // 傳入完整路徑，如 "Sister/Angry"

            // 1. 找目標物件 -------------------------------------------------
            // 優化：雖然 GameObject.Find 慢，但在 Sequencer 偶爾用一次還行。
            // 如果很在意效能，可以改用 PixelCrushers 的工具來找
            GameObject go = GameObject.Find(goName);
            if (go == null)
            {
                // 嘗試找 Dialogue Manager 下的 Panel (如果是用標準架構)
                var dialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
                if (dialogueUI != null)
                {
                    // 這裡可以寫邏輯去 UI 裡找，但暫時維持 GameObject.Find 最單純
                }

                if (go == null)
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning($"[SetSpriteByName] 找不到物件：{goName}");
                    Stop();
                    return;
                }
            }

            // 2. 處理 "none" (移除圖片) -------------------------------------
            if (string.Equals(spritePath, "none", System.StringComparison.OrdinalIgnoreCase))
            {
                SetImageSprite(go, null, false);
                Stop();
                return;
            }

            // 3. 動態載入 Sprite (Lazy Load) ---------------------------------
            // 優化：只載入這一張。注意路徑是相對於 Resources 的
            Sprite sprite = Resources.Load<Sprite>(spritePath);

            // 如果你的舊習慣是把圖都堆在 "Sprites" 資料夾下，可以保留這個 fallback：
            if (sprite == null && !spritePath.Contains("/"))
            {
                sprite = Resources.Load<Sprite>("Sprites/" + spritePath);
            }

            if (sprite == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[SetSpriteByName] 在 Resources 找不到圖片：{spritePath}");
                Stop();
                return;
            }

            // 4. 套用 -------------------------------------------------------
            SetImageSprite(go, sprite, true);

            Stop();
        }

        private void SetImageSprite(GameObject go, Sprite sprite, bool activeState)
        {
            bool applied = false;

            // 優先處理 UI Image (AVG 最常用)
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                // 如果是移除圖片(null)，通常也要把 Image 設為透明或隱藏，避免顯示白方塊
                img.color = (sprite == null) ? Color.clear : Color.white;
                // 或者直接開關物件：
                // img.enabled = (sprite != null);
                applied = true;
            }
            else
            {
                // 處理 SpriteRenderer (2D 場景用)
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = sprite;
                    applied = true;
                }
            }

            if (!applied && DialogueDebug.logWarnings)
            {
                Debug.LogWarning($"[SetSpriteByName] {go.name} 沒有 Image 或 SpriteRenderer 元件");
            }
        }
    }
}