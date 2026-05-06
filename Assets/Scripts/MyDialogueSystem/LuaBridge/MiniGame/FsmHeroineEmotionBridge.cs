using UnityEngine;
using HutongGames.PlayMaker;
using PixelCrushers.DialogueSystem;

public class FsmHeroineEmotionBridge : MonoBehaviour
{
    [Header("PlayMaker 設定")]
    [UnityEngine.Tooltip("掛載 FSM 的物件 (例如: G1_MainFSM_TouchGame)")]
    public GameObject targetObject;

    [UnityEngine.Tooltip("FSM 的名稱")]
    public string fsmName = "HeroineEmotionFSM";

    [UnityEngine.Tooltip("FSM 內 Enum 變數的名稱")]
    public string variableName = "MyCurrentEmotion";

    private PlayMakerFSM fsm;

    void OnEnable()
    {
        // 1. 初始化 FSM 引用
        if (targetObject != null)
        {
            fsm = ActionHelpers.GetGameObjectFsm(targetObject, fsmName);
        }

        // 2. 向 Dialogue System 註冊 Lua 函數
        // 註冊後，你就能在對話條件中使用 GetFsmEmotion()
        Lua.RegisterFunction("GetFsmEmotion", this, SymbolExtensions.GetMethodInfo(() => GetFsmEmotion()));
    }

    void OnDisable()
    {
        // 當此組件被停用時，取消註冊以釋放資源
        Lua.UnregisterFunction("GetFsmEmotion");
    }

    // 供 Lua 調用的 C# 實作方法
    public string GetFsmEmotion()
    {
        if (fsm == null)
        {
            if (targetObject != null) fsm = ActionHelpers.GetGameObjectFsm(targetObject, fsmName);
            if (fsm == null) return "Idle"; // 找不到 FSM 時的預設回傳值
        }

        // 取得 PlayMaker 的 Enum 變數
        FsmEnum emotionVar = fsm.FsmVariables.GetFsmEnum(variableName);

        // 將 Enum 的值轉為字串回傳給 Lua
        return (emotionVar != null) ? emotionVar.Value.ToString() : "Idle";
    }
}