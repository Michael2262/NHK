using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 沉思表演（Ponder）：最多三階段，依「已知成敗」反推她停在哪一拍。
///
/// 停點體感梯子（成功體感%）：
///   第1階段結束 = 0%（鐵定失敗端）
///   第2階段各停點 = 你填的 %（如 10 / 30 / 50）
///   進到第3階段 = 100%（鐵定成功端）
///
/// 抽停點：成功局用「體感%」當權重、失敗局用「100−體感%」。
/// 於是失敗偏早停（絕不到第3階段）、成功偏晚停（絕不在第1階段結束就停），
/// 中間停點成敗皆可 → 瞇牌懸念。表演只分配結果，不決定結果。
/// </summary>
[CreateAssetMenu(menuName = "NHK/Request Performance/Ponder", fileName = "Ponder")]
public class PonderPerformanceConfig : RequestPerformanceConfig
{
    [Serializable]
    public class PonderStop
    {
        [Tooltip("停在這個點要等的秒數。")]
        [Min(0f)] public float Duration = 1f;

        [Tooltip("停在這裡的『成功體感』%（0~100）。")]
        [Range(0, 100)] public int FeelPercent = 10;
    }

    [Header("Tachie")]
    [Tooltip("立繪切換用的 groupID。")]
    [SerializeField] private string tachieGroupID = "Sister";

    [Header("第 1 階段")]
    [Tooltip("進第 1 階段時，從這清單隨機挑一組臉。")]
    [SerializeField] private List<TachieFace> phase1Faces = new List<TachieFace>();
    [SerializeField, Min(0f)] private float phase1Duration = 2f;

    [Header("第 2 階段")]
    [Tooltip("進第 2 階段時，從這清單隨機挑一組臉。")]
    [SerializeField] private List<TachieFace> phase2Faces = new List<TachieFace>();
    [Tooltip("第 2 階段的各停點（由上到下依序）：秒數 + 成功體感%。")]
    [SerializeField] private List<PonderStop> phase2Stops = new List<PonderStop>();

    [Header("第 3 階段")]
    [Tooltip("進第 3 階段時，從這清單隨機挑一組臉。")]
    [SerializeField] private List<TachieFace> phase3Faces = new List<TachieFace>();
    [SerializeField, Min(0f)] private float phase3Duration = 0.8f;

    public override void Play(MonoBehaviour host, string heroineID, bool pass, string[] args, Action onDone)
    {
        host.StartCoroutine(Run(pass, onDone));
    }

    private IEnumerator Run(bool pass, Action onDone)
    {
        int n = phase2Stops != null ? phase2Stops.Count : 0;
        int stopIndex = PickStopIndex(pass, n);
        // 停點索引：0 = 第1階段結束；1..n = 第2階段各停點；n+1 = 進第3階段。

        // ── 第 1 階段 ──
        ApplyRandomFace(phase1Faces);
        if (phase1Duration > 0f) yield return new WaitForSeconds(phase1Duration);
        if (stopIndex == 0) { onDone?.Invoke(); yield break; }

        // ── 第 2 階段 ──
        ApplyRandomFace(phase2Faces);
        for (int i = 0; i < n; i++)
        {
            float d = phase2Stops[i].Duration;
            if (d > 0f) yield return new WaitForSeconds(d);
            if (stopIndex == i + 1) { onDone?.Invoke(); yield break; }
        }

        // ── 第 3 階段（stopIndex == n + 1）──
        ApplyRandomFace(phase3Faces);
        if (phase3Duration > 0f) yield return new WaitForSeconds(phase3Duration);
        onDone?.Invoke();
    }

    /// <summary>依成敗加權抽停點。成功用體感%、失敗用 100−體感% 當權重。</summary>
    private int PickStopIndex(bool pass, int n)
    {
        int count = n + 2; // 兩端 + n 個中間停點
        float[] weights = new float[count];
        for (int i = 0; i < count; i++)
        {
            int feel = (i == 0) ? 0
                     : (i == count - 1) ? 100
                     : phase2Stops[i - 1].FeelPercent;
            weights[i] = pass ? feel : (100 - feel);
        }
        return WeightedPick(weights);
    }

    private static int WeightedPick(float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        if (total <= 0f) return 0; // 全 0 保底：停在第 1 階段結束

        float r = UnityEngine.Random.value * total;
        for (int i = 0; i < weights.Length; i++)
        {
            r -= weights[i];
            if (r < 0f) return i;
        }
        return weights.Length - 1;
    }

    private void ApplyRandomFace(List<TachieFace> faces)
    {
        if (faces == null || faces.Count == 0) return;
        TachieFace face = faces[UnityEngine.Random.Range(0, faces.Count)];
        RequestTachieUtil.Apply(face, tachieGroupID);
    }
}
