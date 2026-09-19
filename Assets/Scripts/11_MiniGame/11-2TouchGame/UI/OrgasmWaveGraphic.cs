using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 從左向右填滿的液體網格。所有外觀參數由 OrgasmSliderVisualizer2 提供。
/// 使用 UGUI 預設材質，可配合 Mask、RectMask2D 與 CanvasGroup。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Orgasm Wave Graphic")]
public class OrgasmWaveGraphic : MaskableGraphic
{
    private float fill;
    private float amplitude;
    private float phase;
    private float cycles = 1.2f;
    private int segments = 32;

    public void SetVisual(float fillAmount, float waveAmplitude, float wavePhase,
        float waveCycles, int waveSegments, Color tint)
    {
        fillAmount = Mathf.Clamp01(fillAmount);
        waveAmplitude = Mathf.Max(0f, waveAmplitude);
        waveSegments = Mathf.Clamp(waveSegments, 8, 128);
        // 平靜時不因相位前進而反覆重建網格。
        bool changed = fill != fillAmount || amplitude != waveAmplitude || cycles != waveCycles
            || segments != waveSegments || (waveAmplitude > 0f && phase != wavePhase);
        fill = fillAmount;
        amplitude = waveAmplitude;
        phase = wavePhase;
        cycles = waveCycles;
        segments = waveSegments;
        color = tint;
        if (changed) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (fill <= 0f || rect.width <= 0f || rect.height <= 0f) return;

        float boundary = rect.xMin + rect.width * fill;
        // 靠近兩端時自然縮小，確保 0 完全清空、100 完整填滿。
        float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.Min(fill, 1f - fill) / 0.1f);
        float allowedAmplitude = Mathf.Min(amplitude * edgeFade,
            rect.width * Mathf.Min(fill, 1f - fill));
        Color32 vertexColor = color;

        for (int i = 0; i <= segments; i++)
        {
            float v = (float)i / segments;
            float angle = v * cycles * Mathf.PI * 2f;
            float wave = (Mathf.Sin(angle + phase) + 0.35f * Mathf.Sin(angle * 2.3f - phase + 0.8f)) / 1.35f;
            float right = Mathf.Clamp(boundary + allowedAmplitude * wave, rect.xMin, rect.xMax);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, v);
            vh.AddVert(new Vector3(rect.xMin, y, 0f), vertexColor, new Vector2(0f, v));
            vh.AddVert(new Vector3(right, y, 0f), vertexColor, new Vector2((right - rect.xMin) / rect.width, v));
            if (i == 0) continue;
            int previous = (i - 1) * 2;
            vh.AddTriangle(previous, previous + 2, previous + 1);
            vh.AddTriangle(previous + 1, previous + 2, previous + 3);
        }
    }
}
