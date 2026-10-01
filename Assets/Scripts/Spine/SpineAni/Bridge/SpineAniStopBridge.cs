using UnityEngine;
using MySpineSystem;

/// <summary>
/// 停止指定軌道；UnityEvent 直接選擇軌道方法，不需參數。
/// </summary>
[AddComponentMenu("Spine/Bridge/Spine Ani Stop Bridge")]
[DisallowMultipleComponent]
public class SpineAniStopBridge : SpineAniTrackBridgeBase
{
    public void Track00_Skin() => StopTrack(AnimationTrack.Skin);

    public void Track01_Body() => StopTrack(AnimationTrack.Body);

    public void Track02_BodyAttach() => StopTrack(AnimationTrack.BodyAttach);

    public void Track03_LeftHand() => StopTrack(AnimationTrack.LeftHand);

    public void Track04_RightHand() => StopTrack(AnimationTrack.RightHand);

    public void Track05_OverBody1() => StopTrack(AnimationTrack.OverBody1);

    public void Track06_LeftFoot() => StopTrack(AnimationTrack.LeftFoot);

    public void Track07_RightFoot() => StopTrack(AnimationTrack.RightFoot);

    public void Track08_OverBody2() => StopTrack(AnimationTrack.OverBody2);

    public void Track09_Face() => StopTrack(AnimationTrack.Face);

    public void Track10_Eye() => StopTrack(AnimationTrack.Eye);

    public void Track11_Mouth() => StopTrack(AnimationTrack.Mouth);

    public void Track12_Brow() => StopTrack(AnimationTrack.Brow);

    public void Track13_FaceAll() => StopTrack(AnimationTrack.FaceAll);

    public void Track14_FaceAyatem() => StopTrack(AnimationTrack.FaceAyatem);

    public void Track15_SF() => StopTrack(AnimationTrack.SF);

    public void Track16_BoyBody() => StopTrack(AnimationTrack.BoyBody);

    public void Track17_BoyLeftHand() => StopTrack(AnimationTrack.BoyLeftHand);

    public void Track18_BoyRightHand() => StopTrack(AnimationTrack.BoyRightHand);
}
