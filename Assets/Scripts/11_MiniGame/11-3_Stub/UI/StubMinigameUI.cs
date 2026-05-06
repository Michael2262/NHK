using UnityEngine;

/// <summary>
/// 清水模的簡易 UI 提示
/// 掛載到場景中的任意 GameObject 即可
/// </summary>
public class StubMinigameUI : MonoBehaviour
{
    [Header("UI 設定")]
    public bool showInstructions = true;
    public Color backgroundColor = new Color(0, 0, 0, 0.7f);
    public Color textColor = Color.white;

    private StubMinigameController _controller;
    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private bool _stylesInitialized = false;

    void Start()
    {
        _controller = FindAnyObjectByType<StubMinigameController>();
    }

    void InitStyles()
    {
        if (_stylesInitialized) return;

        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = MakeTexture(2, 2, backgroundColor);

        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.normal.textColor = textColor;
        _labelStyle.fontSize = 18;
        _labelStyle.alignment = TextAnchor.MiddleCenter;

        _headerStyle = new GUIStyle(_labelStyle);
        _headerStyle.fontSize = 24;
        _headerStyle.fontStyle = FontStyle.Bold;

        _stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!showInstructions || _controller == null) return;

        InitStyles();

        float boxWidth = 400;
        float boxHeight = 200;
        float x = (Screen.width - boxWidth) / 2;
        float y = 50;

        GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);

        GUILayout.BeginArea(new Rect(x + 20, y + 20, boxWidth - 40, boxHeight - 40));

        GUILayout.Label("🎮 清水模小遊戲", _headerStyle);
        GUILayout.Space(10);

        string rankName = GetCurrentRankName();
        GUILayout.Label($"當前等級設定: {rankName}", _labelStyle);
        GUILayout.Label($"結算模式: {(_controller.settleAllAtOnce ? "全部同時" : "逐一結算")}", _labelStyle);

        GUILayout.Space(20);
        GUILayout.Label("👆 點擊畫面任意處開始結算", _labelStyle);

        GUILayout.EndArea();
    }

    private string GetCurrentRankName()
    {
        if (_controller == null) return "---";

        switch (_controller.selectedRankIndex)
        {
            case 0: return "S - 完美";
            case 1: return "A - 良好";
            case 2: return "B - 普通";
            case 3: return "C - 較差";
            case 4: return "隨機";
            default: return "未知";
        }
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
