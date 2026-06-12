using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  PlayMusic — 交叉淡化播放 BGM                                       ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  功能對應 SequencerCommandPlayMusic：                               ║
    // ║  優先透過 SceneMusicController.PlayOverride 播放（鎖定控制權，     ║
    // ║  避免 Flag / Phase 變動把音樂搶回去）；場景沒有控制器時直接對接     ║
    // ║  MusicManager.PlayMusic。                                          ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Audio")]
    [Tooltip("以交叉淡化效果播放指定 Key 的 BGM。場景有 SceneMusicController 時走 PlayOverride 鎖定控制權。")]
    public class PlayMusic : FsmStateAction
    {
        [RequiredField]
        [Tooltip("MusicManager 中設定的音樂 Key")]
        public FsmString musicKey;

        [Tooltip("交叉淡化時間（秒），預設 1 秒")]
        public FsmFloat fadeDuration;

        public override void Reset()
        {
            musicKey = null;
            fadeDuration = 1.0f;
        }

        public override void OnEnter()
        {
            var sceneController = SceneMusicController.Instance;

            if (sceneController != null)
            {
                // 場景有控制器：用 PlayOverride 確保 Flag 變動不會搶走音樂控制權
                sceneController.PlayOverride(musicKey.Value, fadeDuration.Value);
            }
            else if (MusicManager.Instance != null)
            {
                // 簡單場景：直接對接 Manager
                MusicManager.Instance.PlayMusic(musicKey.Value, fadeDuration.Value);
            }
            else
            {
                Debug.LogWarning("[PlayMusic] 找不到任何音樂控制器或 MusicManager 實例。");
            }

            Finish();
        }
    }
}
