using HutongGames.PlayMaker;

[ActionCategory("Minigame")]
[Tooltip("跳轉到下一個小遊戲關卡，而不返回主場景")]
public class ContinueToNextMinigameAction : FsmStateAction
{
    [RequiredField]
    public FsmString nextMinigameSceneName;

    public override void OnEnter()
    {
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.ContinueToNextMinigame(nextMinigameSceneName.Value);
        }
        Finish();
    }
}