using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  PlaySound — 播放音效（SFX）                                        ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  對接 AudioManager.PlaySound：依 Key 查表後 PlayOneShot 播放，      ║
    // ║  支援 AudioManager 中設定的固定 / 隨機音效類型。                    ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Audio")]
    [Tooltip("播放 AudioManager 中設定的音效（依 Key 查表，PlayOneShot 一次性播放）。")]
    public class PlaySound : FsmStateAction
    {
        [RequiredField]
        [Tooltip("AudioManager 中設定的音效 Key")]
        public FsmString soundKey;

        public override void Reset()
        {
            soundKey = null;
        }

        public override void OnEnter()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundKey.Value);
            }
            else
            {
                Debug.LogWarning("[PlaySound] 找不到 AudioManager 實例。");
            }

            Finish();
        }
    }
}
