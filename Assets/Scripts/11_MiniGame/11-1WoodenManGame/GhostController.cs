using UnityEngine;
using System.Collections;
using System; // 為了 Action

// ★ 引用 PlayMaker
using HutongGames.PlayMaker;

namespace WoodenMan
{
    // 這個腳本仍然存在，以便 WoodenManGameManager 能找到它
    public class GhostController : MonoBehaviour
    {
        // 拖入或自動尋找同一個物件上的 FSM
        public PlayMakerFSM ghostFSM;

        // ★★★ 核心： C# 事件，由 FSM 來觸發
        public event Action<GhostController> OnAwarenessMax;

        void Awake()
        {
            // 自動抓取 FSM
            if (ghostFSM == null)
                ghostFSM = GetComponent<PlayMakerFSM>();
        }

        // --- 1. 從 C# (Manager) 傳遞指令到 FSM ---

        public void StartGhostBehavior()
        {
            ghostFSM.SendEvent("START_BEHAVIOR");
        }

        public void StopGhostBehavior()
        {
            ghostFSM.SendEvent("STOP_BEHAVIOR");
        }

        public void TriggerOrgasmCheck()
        {
            ghostFSM.SendEvent("TRIGGER_CHECK");
        }

        public void AddDangerPoint(int amount)
        {
            

            if (ghostFSM == null)
            {
                Debug.LogError($"[{name}] 上的 ghostFSM 變數是空的！請在 Inspector 拖曳。");
                return;
            }

            // 1. 找到 FSM 裡的變數
            FsmInt dangerAmountVar = ghostFSM.FsmVariables.GetFsmInt("Input_DangerAmount");

            // 2. ★★★ 增加防呆檢查 ★★★
            if (dangerAmountVar != null)
            {
                // 3. 賦值
                dangerAmountVar.Value = amount;

                // 4. 發送事件 (只有在成功賦值後才發送)
                ghostFSM.SendEvent("ADD_DANGER");

            }
        }

        public void FreezeAtGameOver()
        {
            ghostFSM.SendEvent("GAME_OVER_FREEZE");
        }

        public void ResetGhostState()
        {
            ghostFSM.SendEvent("RESET_STATE");
        }

        // --- 2. 從 FSM 讀取狀態回傳給 C# ---

        /// <summary>
        /// ★ Manager 需要知道鬼是否在看。
        /// 這個值現在由 FSM 中的 "IsLooking" 變數控制。
        /// </summary>
        public bool IsLooking
        {
            get
            {
                if (ghostFSM == null) return false;
                // 讀取 FSM 變數
                return ghostFSM.FsmVariables.GetFsmBool("IsLooking").Value;
            }
        }

        // --- 3. 從 FSM 觸發 C# 事件 (回報 Manager) ---

        /// <summary>
        /// ★★★ 這是一個新的公開方法，*專門給 FSM 呼叫*。
        /// 當 FSM 判斷驚覺值滿了，它會呼叫這個方法。
        /// </summary>
        public void NotifyAwarenessMaxFromFSM()
        {
            Debug.Log($"[{name}] FSM 回報：驚覺值已滿！");
            // 觸發 C# 事件，Manager 會監聽到
            OnAwarenessMax?.Invoke(this);
        }
    }
}