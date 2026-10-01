using UnityEngine;
using MySpineSystem;

/// <summary>
/// 固定循環播放指定軌道；UnityEvent 選擇軌道方法後填入動畫名稱。
/// </summary>
[AddComponentMenu("Spine/Bridge/Spine Ani Loop Bridge")]
[DisallowMultipleComponent]
public class SpineAniLoopBridge : SpineAniTrackBridgeBase
{
    public void Track00_Skin(string animationName) =>
        PlayTrack(AnimationTrack.Skin, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track01_Body(string animationName) =>
        PlayTrack(AnimationTrack.Body, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track02_BodyAttach(string animationName) =>
        PlayTrack(AnimationTrack.BodyAttach, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track03_LeftHand(string animationName) =>
        PlayTrack(AnimationTrack.LeftHand, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track04_RightHand(string animationName) =>
        PlayTrack(AnimationTrack.RightHand, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track05_OverBody1(string animationName) =>
        PlayTrack(AnimationTrack.OverBody1, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track06_LeftFoot(string animationName) =>
        PlayTrack(AnimationTrack.LeftFoot, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track07_RightFoot(string animationName) =>
        PlayTrack(AnimationTrack.RightFoot, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track08_OverBody2(string animationName) =>
        PlayTrack(AnimationTrack.OverBody2, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track09_Face(string animationName) =>
        PlayTrack(AnimationTrack.Face, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track10_Eye(string animationName) =>
        PlayTrack(AnimationTrack.Eye, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track11_Mouth(string animationName) =>
        PlayTrack(AnimationTrack.Mouth, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track12_Brow(string animationName) =>
        PlayTrack(AnimationTrack.Brow, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track13_FaceAll(string animationName) =>
        PlayTrack(AnimationTrack.FaceAll, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track14_FaceAyatem(string animationName) =>
        PlayTrack(AnimationTrack.FaceAyatem, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track15_SF(string animationName) =>
        PlayTrack(AnimationTrack.SF, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track16_BoyBody(string animationName) =>
        PlayTrack(AnimationTrack.BoyBody, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track17_BoyLeftHand(string animationName) =>
        PlayTrack(AnimationTrack.BoyLeftHand, animationName, SpineAnimationController.ClearMode.Loop, -1f);

    public void Track18_BoyRightHand(string animationName) =>
        PlayTrack(AnimationTrack.BoyRightHand, animationName, SpineAnimationController.ClearMode.Loop, -1f);
}
