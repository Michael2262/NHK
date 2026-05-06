using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    // 改用字串定義 Category，避免 Enum 名稱不一致的問題
    [ActionCategory("Variable")]
    [Tooltip("將一個 Sprite 變數的值指定為另一個 Sprite。")]
    public class SetSpriteVariable : FsmStateAction
    {
        [RequiredField]
        [UIHint(UIHint.Variable)]
        [ObjectType(typeof(Sprite))] // 限定只能選取 Sprite 類型的變數
        [Tooltip("你要修改的目標 Sprite 變數 (例如：spriteC)")]
        public FsmObject spriteVariable;

        [RequiredField]
        [ObjectType(typeof(Sprite))] // 限定只能選取 Sprite 圖片
        [Tooltip("你想指定的圖片 (可以是另一個變數或直接拖入圖片)")]
        public FsmObject newValue;

        [Tooltip("是否每一格更新。通常不需要勾選。")]
        public bool everyFrame;

        public override void Reset()
        {
            spriteVariable = null;
            newValue = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoSetSpriteValue();

            if (!everyFrame)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            DoSetSpriteValue();
        }

        void DoSetSpriteValue()
        {
            if (spriteVariable == null) return;

            // 將 newValue 的內容賦予給 spriteVariable
            spriteVariable.Value = newValue.Value;
        }
    }
}