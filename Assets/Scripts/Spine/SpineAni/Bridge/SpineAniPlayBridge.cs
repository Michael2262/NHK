using UnityEngine;
using MySpineSystem;

/// <summary>
/// 依 Inspector 播放模式播放指定軌道；UnityEvent 選擇軌道方法後填入動畫名稱。
/// </summary>
[AddComponentMenu("Spine/Bridge/Spine Ani Play Bridge")]
[DisallowMultipleComponent]
public class SpineAniPlayBridge : SpineAniTrackBridgeBase
{
    [Header("播放設定")]
    [SerializeField] private SpineAnimationController.ClearMode clearMode =
        SpineAnimationController.ClearMode.ClearOnComplete;
    [Tooltip("ClearAfterDelay 模式使用；負數代表使用 Controller 的預設延遲。")]
    [SerializeField] private float clearDelaySeconds = -1f;

    public void Track00_Skin(string animationName) =>
        PlayTrack(AnimationTrack.Skin, animationName, clearMode, clearDelaySeconds);

    public void Track01_Body(string animationName) =>
        PlayTrack(AnimationTrack.Body, animationName, clearMode, clearDelaySeconds);

    public void Track02_BodyAttach(string animationName) =>
        PlayTrack(AnimationTrack.BodyAttach, animationName, clearMode, clearDelaySeconds);

    public void Track03_LeftHand(string animationName) =>
        PlayTrack(AnimationTrack.LeftHand, animationName, clearMode, clearDelaySeconds);

    public void Track04_RightHand(string animationName) =>
        PlayTrack(AnimationTrack.RightHand, animationName, clearMode, clearDelaySeconds);

    public void Track05_OverBody1(string animationName) =>
        PlayTrack(AnimationTrack.OverBody1, animationName, clearMode, clearDelaySeconds);

    public void Track06_LeftFoot(string animationName) =>
        PlayTrack(AnimationTrack.LeftFoot, animationName, clearMode, clearDelaySeconds);

    public void Track07_RightFoot(string animationName) =>
        PlayTrack(AnimationTrack.RightFoot, animationName, clearMode, clearDelaySeconds);

    public void Track08_OverBody2(string animationName) =>
        PlayTrack(AnimationTrack.OverBody2, animationName, clearMode, clearDelaySeconds);

    public void Track09_Face(string animationName) =>
        PlayTrack(AnimationTrack.Face, animationName, clearMode, clearDelaySeconds);

    public void Track10_Eye(string animationName) =>
        PlayTrack(AnimationTrack.Eye, animationName, clearMode, clearDelaySeconds);

    public void Track11_Mouth(string animationName) =>
        PlayTrack(AnimationTrack.Mouth, animationName, clearMode, clearDelaySeconds);

    public void Track12_Brow(string animationName) =>
        PlayTrack(AnimationTrack.Brow, animationName, clearMode, clearDelaySeconds);

    public void Track13_FaceAll(string animationName) =>
        PlayTrack(AnimationTrack.FaceAll, animationName, clearMode, clearDelaySeconds);

    public void Track14_FaceAyatem(string animationName) =>
        PlayTrack(AnimationTrack.FaceAyatem, animationName, clearMode, clearDelaySeconds);

    public void Track15_SF(string animationName) =>
        PlayTrack(AnimationTrack.SF, animationName, clearMode, clearDelaySeconds);

    public void Track16_BoyBody(string animationName) =>
        PlayTrack(AnimationTrack.BoyBody, animationName, clearMode, clearDelaySeconds);

    public void Track17_BoyLeftHand(string animationName) =>
        PlayTrack(AnimationTrack.BoyLeftHand, animationName, clearMode, clearDelaySeconds);

    public void Track18_BoyRightHand(string animationName) =>
        PlayTrack(AnimationTrack.BoyRightHand, animationName, clearMode, clearDelaySeconds);
}
