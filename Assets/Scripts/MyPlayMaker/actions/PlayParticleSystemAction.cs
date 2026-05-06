using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = UnityEngine.TooltipAttribute;

[ActionCategory("Custom")]
[Tooltip("Play target ParticleSystem.")]
public class PlayParticleSystemAction : FsmStateAction
{
    [RequiredField]
    public FsmGameObject targetObject;

    public FsmBool restart;

    public override void Reset()
    {
        targetObject = null;
        restart = true;
    }

    public override void OnEnter()
    {
        if (targetObject.Value == null)
        {
            Finish();
            return;
        }

        ParticleSystem ps = targetObject.Value.GetComponent<ParticleSystem>();

        if (ps == null)
        {
            Debug.LogWarning("PlayParticleSystemAction: target object has no ParticleSystem.");
            Finish();
            return;
        }

        if (restart.Value)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ps.Play(true);
        Finish();
    }
}