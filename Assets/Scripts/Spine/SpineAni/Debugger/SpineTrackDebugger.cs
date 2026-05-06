using UnityEngine;
using Spine.Unity;
using Spine;
using System.Text;
using System.Collections.Generic;
using MySpineSystem; // for AnimationTrack enum
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spine 動畫軌道除錯工具。
/// 功能：
/// 1. Scene View 中顯示所有軌道的即時動畫狀態（不需選中物件）
/// 2. 選中物件時額外以高亮顯示（OnDrawGizmosSelected）
/// 3. 可開啟 Console Log，針對特定 Track 輸出動畫變化紀錄
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Spine/Spine Track Debugger")]
public class SpineTrackDebugger : MonoBehaviour
{
    // ==========================================
    //  Scene View Gizmo 設定
    // ==========================================
    [Header("Scene View 顯示設定")]
    [Tooltip("Scene 視窗除錯文字的垂直偏移量")]
    public float verticalOffset = 1.0f;

    [Tooltip("即使沒有選中物件也在 Scene View 顯示（取消勾選則只有選中時顯示）")]
    public bool alwaysShowInSceneView = true;

    // ==========================================
    //  Console Log 設定
    // ==========================================
    [Header("Console Log 設定")]
    [Tooltip("勾選後，動畫開始/結束時會輸出到 Console")]
    public bool enableConsoleLog = false;

    [Tooltip("要輸出 Log 的軌道（勾選要監聽的 Track）。如果一個都沒勾，則輸出全部。")]
    public List<AnimationTrack> logTracks = new List<AnimationTrack>();

    // ==========================================
    //  內部狀態
    // ==========================================
    private Spine.AnimationState _state;
    /// <summary>
    /// 每個 Track 記錄最近兩個播放過的動畫名稱。
    /// Item1 = Last1（最近一個）, Item2 = Last2（再前一個）
    /// </summary>
    private Dictionary<int, (string last1, string last2)> _lastAnimationNames = new();

#if UNITY_EDITOR
    private GUIStyle _normalStyle;
    private GUIStyle _selectedStyle;
#endif

    // ==========================================
    //  Lifecycle
    // ==========================================
    void Start()
    {
        InitializeState();
#if UNITY_EDITOR
        InitializeGUIStyles();
#endif
        SubscribeEvents();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ==========================================
    //  初始化
    // ==========================================
    private void InitializeState()
    {
        if (_state != null) return;

        var skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation != null)
        {
            _state = skeletonAnimation.AnimationState;
            return;
        }

        var skeletonGraphic = GetComponent<SkeletonGraphic>();
        if (skeletonGraphic != null)
        {
            _state = skeletonGraphic.AnimationState;
        }
    }

    private void SubscribeEvents()
    {
        if (_state == null) return;
        _state.Start += OnAnimationStart;
        _state.End += OnAnimationEnd;
    }

    private void UnsubscribeEvents()
    {
        if (_state == null) return;
        _state.Start -= OnAnimationStart;
        _state.End -= OnAnimationEnd;
    }

    // ==========================================
    //  Spine 事件回呼
    // ==========================================
    private void OnAnimationStart(TrackEntry trackEntry)
    {
        if (trackEntry.Animation == null) return;

        string newName = trackEntry.Animation.Name;

        // 把舊的 last1 推到 last2
        if (_lastAnimationNames.TryGetValue(trackEntry.TrackIndex, out var prev))
            _lastAnimationNames[trackEntry.TrackIndex] = (newName, prev.last1);
        else
            _lastAnimationNames[trackEntry.TrackIndex] = (newName, "None");

        // Console Log
        if (enableConsoleLog && ShouldLogTrack(trackEntry.TrackIndex))
        {
            Debug.Log($"[SpineTrackDebugger] <color=cyan>{gameObject.name}</color> " +
                      $"Track[{trackEntry.TrackIndex}] <color=green>START</color> → " +
                      $"<b>{newName}</b>", this);
        }
    }

    private void OnAnimationEnd(TrackEntry trackEntry)
    {
        if (!enableConsoleLog || !ShouldLogTrack(trackEntry.TrackIndex)) return;

        string animName = trackEntry.Animation != null ? trackEntry.Animation.Name : "Unknown";
        Debug.Log($"[SpineTrackDebugger] <color=cyan>{gameObject.name}</color> " +
                  $"Track[{trackEntry.TrackIndex}] <color=red>END</color> → " +
                  $"<b>{animName}</b>", this);
    }

    /// <summary>
    /// 判斷此 trackIndex 是否在要監聽的 Log 清單中。
    /// 如果 logTracks 為空，表示全部都要 log。
    /// </summary>
    private bool ShouldLogTrack(int trackIndex)
    {
        if (logTracks == null || logTracks.Count == 0) return true;
        return logTracks.Contains((AnimationTrack)trackIndex);
    }

    // ==========================================
    //  Scene View Gizmo 繪製
    // ==========================================
#if UNITY_EDITOR
    private void InitializeGUIStyles()
    {
        if (_normalStyle == null)
        {
            _normalStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11
            };
            _normalStyle.normal.textColor = new Color(0.5f, 1f, 0.5f, 0.7f); // 半透明淺綠
        }

        if (_selectedStyle == null)
        {
            _selectedStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };
            _selectedStyle.normal.textColor = Color.green; // 亮綠
        }
    }

    /// <summary>
    /// 不需選中物件就顯示（如果 alwaysShowInSceneView = true）
    /// </summary>
    void OnDrawGizmos()
    {
        if (!alwaysShowInSceneView) return;
        if (!Application.isPlaying) return;

        EnsureInitialized();
        if (_state == null) return;

        // 如果目前已被選中，跳過（讓 OnDrawGizmosSelected 處理，避免重複繪製）
        if (UnityEditor.Selection.activeGameObject == this.gameObject) return;

        DrawTrackInfo(_normalStyle);
    }

    /// <summary>
    /// 選中物件時以高亮樣式顯示
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        EnsureInitialized();
        if (_state == null) return;

        DrawTrackInfo(_selectedStyle);
    }

    private void EnsureInitialized()
    {
        if (_state == null)
        {
            InitializeState();
            InitializeGUIStyles();
            if (_state == null) return;

            _state.Start += OnAnimationStart;
            _state.End += OnAnimationEnd;
        }

        InitializeGUIStyles();
    }

    private void DrawTrackInfo(GUIStyle style)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"--- {gameObject.name} Track Info ---");

        foreach (AnimationTrack trackEnum in System.Enum.GetValues(typeof(AnimationTrack)))
        {
            int trackIndex = (int)trackEnum;
            TrackEntry currentTrackEntry = _state.GetCurrent(trackIndex);

            string lastAnimName = _lastAnimationNames.ContainsKey(trackIndex)
                ? _lastAnimationNames[trackIndex].last1
                : "None";

            string last2AnimName = _lastAnimationNames.ContainsKey(trackIndex)
                ? _lastAnimationNames[trackIndex].last2
                : "None";

            sb.Append($"[{trackIndex}] {trackEnum}: ");

            if (currentTrackEntry != null)
            {
                sb.AppendLine(currentTrackEntry.Animation.Name);
            }
            else
            {
                sb.AppendLine($"None (Last1: {lastAnimName}, Last2: {last2AnimName})");
            }
        }

        Vector3 debugPosition = transform.position + Vector3.up * verticalOffset;
        Handles.Label(debugPosition, sb.ToString(), style);
    }
#endif
}