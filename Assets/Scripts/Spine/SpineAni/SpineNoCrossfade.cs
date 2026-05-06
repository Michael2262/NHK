using UnityEngine;
using Spine.Unity;
using Spine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SkeletonAnimation))]
public class SpineNoCrossfade : MonoBehaviour
{
    [Tooltip("若開啟，會把這個 SkeletonDataAsset 的 DefaultMix 設為 0（影響共用同資產的所有實例）。")]
    public bool affectSharedDataDefaultMix = true;

    [Tooltip("強制每個 TrackEntry 的 MixDuration = 0（只影響此實例）。")]
    public bool forceEntryMixZero = true;

    SkeletonAnimation sa;

    void Awake()
    {
        sa = GetComponent<SkeletonAnimation>();
        if (sa == null || sa.state == null) return;

        // 1) 關閉全域 cross-fade（此 SkeletonDataAsset 層級）
        if (affectSharedDataDefaultMix && sa.state.Data != null)
            sa.state.Data.DefaultMix = 0f;

        // 2) 保險：任何人（包含 Dialogue System）設的播放，進來就把 mix 清 0（此實例層級）
        if (forceEntryMixZero)
            sa.state.Start += (TrackEntry e) => e.MixDuration = 0f;
    }
}
