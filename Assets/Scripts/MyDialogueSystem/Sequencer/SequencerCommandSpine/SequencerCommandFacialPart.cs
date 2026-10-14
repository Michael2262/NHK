using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// FacialPart(StopRemote, ID)：暫停指定目標接收 Remote。
    /// FacialPart(ResumeRemote, ID)：恢復指定目標接收 Remote。
    /// FacialPart(Remote, ID)：指定目標各部位倒數一次。
    /// FacialPart(StopRemoteAll) / FacialPart(ResumeRemoteAll)：停止／恢復當下全部已註冊目標。
    /// FacialPart(RemoteAll)：通知當下全部已註冊目標，跳過暫停者。
    /// 操作名稱不分大小寫，ID 區分大小寫；指令立即完成，不等待動畫。
    /// </summary>
    public class SequencerCommandFacialPart : SequencerCommand
    {
        public void Awake()
        {
            // 無論參數錯誤或目標不存在，都結束本指令，避免對話等待。
            try
            {
                string operation = (GetParameter(0) ?? string.Empty).Trim().ToLowerInvariant();
                switch (operation)
                {
                    case "stopremoteall":
                        FacialPartController.StopRemoteAll();
                        break;
                    case "resumeremoteall":
                        FacialPartController.ResumeRemoteAll();
                        break;
                    case "remoteall":
                        FacialPartController.NotifyRemoteAll();
                        break;
                    case "stopremote":
                    case "resumeremote":
                    case "remote":
                        string id = (GetParameter(1) ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(id))
                        {
                            if (DialogueDebug.logWarnings)
                                Debug.LogWarning("[Sequencer] FacialPart：此操作需要填入 FacialPartController 的 ID。", this);
                            return;
                        }
                        if (operation == "stopremote") FacialPartController.StopRemoteById(id);
                        else if (operation == "resumeremote") FacialPartController.ResumeRemoteById(id);
                        else FacialPartController.NotifyRemote(id);
                        break;
                    default:
                        if (DialogueDebug.logWarnings)
                            Debug.LogWarning($"[Sequencer] FacialPart：未知操作 '{operation}'。可用 StopRemote、ResumeRemote、Remote、StopRemoteAll、ResumeRemoteAll、RemoteAll。", this);
                        break;
                }
            }
            finally
            {
                Stop();
            }
        }
    }
}
