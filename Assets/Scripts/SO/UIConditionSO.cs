// UIConditionSO.cs
using UnityEngine;
using System;

namespace UIVisibility
{
    /// <summary>所有顯示／隱藏條件的基底</summary>
    public abstract class UIConditionSO : ScriptableObject
    {
        protected event Action ConditionChanged;

        // Controller 在 Awake 時呼叫
        public virtual void Init(Action listener)
        {
            // 直接訂閱事件。C# 的 event 會處理好內部的訂閱者列表。
            ConditionChanged += listener;
        }

        // Controller 在 OnDestroy 時呼叫
        public virtual void Dispose(Action listener)
        {
            // 直接取消訂閱。即使 listener 不在訂閱列表中，這行程式碼也不會報錯。
            ConditionChanged -= listener;
        }

        /// <summary>衍生類別呼叫，通知所有 Controller 重新 Evaluate</summary>
        protected void Raise() => ConditionChanged?.Invoke();

        /// <summary>
        /// 這個方法在 Unity Editor 中停止播放模式或重新編譯腳本時會被呼叫。
        /// 這是確保 ScriptableObject 狀態乾淨的關鍵。
        /// </summary>
        private void OnDisable()
        {
            // 強制清空所有事件監聽者，避免記憶體洩漏和殭屍引用。
            ConditionChanged = null;
        }

        /*──────── 要由子類別實作 ────────*/
        public abstract bool IsMet();
    }
}