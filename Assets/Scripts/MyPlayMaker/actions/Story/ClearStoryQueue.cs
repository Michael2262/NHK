using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Story")]
[HutongGames.PlayMaker.Tooltip("清除 StoryManager 中所有未播放的隊列對話。不會影響正在播放的對話。")]
public class ClearStoryQueue : FsmStateAction
{
    public override void OnEnter()
    {
        if (StoryManager.Instance == null)
        {
            LogWarning("StoryManager 尚未初始化！");
            Finish();
            return;
        }

        StoryManager.Instance.ClearConversationQueue();

        Finish();
    }
}
