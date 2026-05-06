using UnityEngine;

/// <summary>
/// 監聽 SpineExcitementAnimationPlayer.OnExcitementLevelPlayed，
/// 在指定等級時觸發粒子。
/// </summary>
public class ParticleBurstOnLevel : MonoBehaviour
{
    [Tooltip("要在哪個 Excitement Level 觸發？")]
    [Range(0, 5)] public int triggerLevel = 2;

    [Tooltip("要 Play 的粒子（可指定多個）")]
    public ParticleSystem[] targets;

    /// <summary>事件接收端：由 UnityEvent<int> 呼叫</summary>
    public void Trigger(int level)
    {
        if (level != triggerLevel) return;
        foreach (var ps in targets)
            if (ps) ps.Play();
    }
}
