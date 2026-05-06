// DialogueVariableBoolConditionSO.cs 

// 使用 Pixel Crushers 的 Dialogue System 來檢查數值型變數



using UnityEngine;

using PixelCrushers;

using PixelCrushers.DialogueSystem;



namespace UIVisibility

{

    [CreateAssetMenu(fileName = "New Dialogue Bool Condition", menuName = "UI Conditions/Dialogue/Dialogue Variable (Bool)")]

    public class DialogueVariableBoolConditionSO : UIConditionSO, IMessageHandler

    {

        [Header("Dialogue System 變數")]

        [Tooltip("要檢查的 Dialogue System 變數名稱（大小寫敏感）")]

        public string variableName;



        [Tooltip("期望該變數為何值時，此條件才算滿足")]

        public bool requiredValue = true;



        private void OnEnable()

        {

            MessageSystem.AddListener(this, "Variable Changed", (string)null);

        }



        private void OnDisable()

        {

            MessageSystem.RemoveListener(this);

        }



        public void OnMessage(MessageArgs messageArgs)

        {

            if (messageArgs.message == "Variable Changed")

            {

                // ★ 最終修正：messageArgs.parameter 本身就是 string，直接使用即可

                string changedVarName = messageArgs.parameter;



                if (variableName == changedVarName)

                {

                    Raise();

                }

            }

        }



        public override bool IsMet()

        {

            if (string.IsNullOrEmpty(variableName))

            {

                return false;

            }

            return DialogueLua.GetVariable(variableName).AsBool == requiredValue;

        }

    }

}