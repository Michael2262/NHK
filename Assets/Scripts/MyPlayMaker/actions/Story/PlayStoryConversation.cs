using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Story")]
[HutongGames.PlayMaker.Tooltip("觸發 StoryManager 播放對話。對話將自動使用全域設定的 customDelay。")]
public class PlayStoryConversation : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("對話的標題 (Conversation Title)")]
    public FsmString title;

    [HutongGames.PlayMaker.Tooltip("播放模式：Skip (跳過)、Interrupt (中斷)、Queue (排隊)、Priority (插隊)")]
    public DialogueMode mode = DialogueMode.Skip;

    public override void Reset()
    {
        title = null;
        mode = DialogueMode.Skip;
    }

    public override void OnEnter()
    {
        if (StoryManager.Instance == null)
        {
            LogWarning("StoryManager 尚未初始化！");
            Finish();
            return;
        }

        StoryManager.Instance.PlayConversation(title.Value, mode);

        Finish();
    }
}