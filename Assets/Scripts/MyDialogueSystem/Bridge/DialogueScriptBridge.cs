using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// 將 Inspector、Button 與 UnityEvent 的文字輸入轉交給 Dialogue System。
/// Sequence 使用序列指令語法；對話節點的 Script 與 Lua 使用相同的 Lua 執行器。
/// </summary>
[AddComponentMenu("NHK/Bridge/Dialogue Script Bridge")]
public class DialogueScriptBridge : MonoBehaviour
{
    public enum ExecutionMode
    {
        Sequence = 0,
        Lua = 1,
        Script = 2
    }

    [Header("執行內容")]
    [Tooltip("Flash(); 請選 Sequence。Lua 與 Script 都執行 Dialogue System Lua，並非 C#。")]
    [SerializeField] private ExecutionMode mode = ExecutionMode.Sequence;

    [Tooltip("填入原本的指令即可，例如 Flash();。呼叫 Run() 時才會執行。")]
    [TextArea(3, 10)]
    [SerializeField] private string code = string.Empty;

    [Header("Sequence 角色（選填）")]
    [Tooltip("Sequence 指令中的 speaker。留空時沿用 Dialogue System 的預設處理。")]
    [SerializeField] private Transform speaker;

    [Tooltip("Sequence 指令中的 listener。留空時沿用 Dialogue System 的預設處理。")]
    [SerializeField] private Transform listener;

    /// <summary>執行 Inspector 設定的內容，可接 Button.onClick 或 UnityEvent。</summary>
    public void Run()
    {
        RunCode(code);
    }

    /// <summary>以 Inspector 選擇的模式執行傳入字串。</summary>
    public void RunCode(string text)
    {
        switch (mode)
        {
            case ExecutionMode.Sequence:
                RunSequence(text);
                break;
            case ExecutionMode.Lua:
            case ExecutionMode.Script:
                RunLua(text);
                break;
            default:
                Debug.LogError("[DialogueScriptBridge] 未知的執行模式，請重新選擇。", this);
                break;
        }
    }

    /// <summary>直接執行 Sequence，例如 Flash();，播放完成後由套件清理 Sequencer。</summary>
    public void RunSequence(string sequence)
    {
        if (!CanRun(sequence)) return;
        DialogueManager.PlaySequence(sequence, speaker, listener, false, true);
    }

    /// <summary>直接執行 Lua；自訂函式必須先由既有 LuaBridge 註冊。</summary>
    public void RunLua(string script)
    {
        if (!CanRun(script)) return;

        try
        {
            Lua.Run(script, DialogueDebug.logInfo, true);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DialogueScriptBridge] Lua／Script 執行失敗：{exception.Message}", this);
        }
    }

    /// <summary>對話節點的 Script 欄位同樣使用 Lua。</summary>
    public void RunScript(string script)
    {
        RunLua(script);
    }

    private bool CanRun(string text)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DialogueScriptBridge] 請在 Play Mode 中執行。", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(text)) return false;

        if (!DialogueManager.hasInstance)
        {
            Debug.LogWarning("[DialogueScriptBridge] Dialogue Manager 尚未就緒，無法執行指令。", this);
            return false;
        }

        return true;
    }
}
