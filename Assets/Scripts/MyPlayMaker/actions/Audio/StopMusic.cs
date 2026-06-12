using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  StopMusic — 淡出並停止 BGM                                         ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  功能對應 SequencerCommandStopMusic：                               ║
    // ║  場景有 SceneMusicController 時走 StopOverride，可選擇停止後是否    ║
    // ║  恢復場景音樂（RefreshSceneMusic）；沒有控制器時直接停 Manager。    ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Audio")]
    [Tooltip("以淡出效果停止目前的 BGM。可選擇停止後是否恢復場景音樂（SceneMusicController）。")]
    public class StopMusic : FsmStateAction
    {
        [Tooltip("淡出時間（秒），預設 1 秒")]
        public FsmFloat fadeDuration;

        [Tooltip("停止後是否恢復場景音樂（交還控制權給 SceneMusicController）。勾選 = 立即依場景規則重播 BGM；不勾 = 保持靜音並鎖定控制權。")]
        public FsmBool resumeSceneMusic;

        public override void Reset()
        {
            fadeDuration = 1.0f;
            resumeSceneMusic = true;
        }

        public override void OnEnter()
        {
            var sceneController = SceneMusicController.Instance;

            if (sceneController != null)
            {
                // 透過控制器停止，並決定是否恢復 RefreshSceneMusic
                sceneController.StopOverride(resumeSceneMusic.Value, fadeDuration.Value);
            }
            else if (MusicManager.Instance != null)
            {
                // 一般場景直接停止
                MusicManager.Instance.StopMusic(fadeDuration.Value);
            }
            else
            {
                Debug.LogWarning("[StopMusic] 找不到任何音樂控制器或 MusicManager 實例。");
            }

            Finish();
        }
    }
}
