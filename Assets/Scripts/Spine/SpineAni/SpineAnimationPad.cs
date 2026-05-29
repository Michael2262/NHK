using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Spine.Unity;
using Spine; // for TrackEntry
using System.Collections.Generic;

/// <summary>
/// 在 Inspector 中視覺化地操作 <see cref="SpineAnimationController"/>，
/// 讓你可以選一個 Track Index 與 Animation，並一鍵播放/清除。
/// 已支援三種清軌策略（與控制器一致）：
/// 1) ClearOnComplete：播完立即清軌。
/// 2) KeepTrack：播完不清軌。
/// 3) ClearAfterDelay：播完延遲清軌（秒數可調，預設 2s）。
/// </summary>
[DisallowMultipleComponent]
public class SpineAnimationPad : MonoBehaviour
{
    [Header("Target Controller (必填)")]
    [SerializeField] private SpineAnimationController controller;

    [Header("Default Track (僅供 Inspector 快捷用)")]
    [Min(0)] public int trackIndex = 0;

    [Header("Animation Selection")]
    [SerializeField] private string selectedAnimation = string.Empty;

    [Header("Clear Mode")]
    public SpineAnimationController.ClearMode clearMode = SpineAnimationController.ClearMode.ClearOnComplete;
    [Tooltip("當 Clear Mode = ClearAfterDelay 時才使用。小於 0 代表交由 Controller 的 defaultClearDelaySeconds。")]
    [Min(0f)] public float clearAfterDelaySeconds = 2f;

    /// <summary>允許外部（例如自訂按鈕）在執行期呼叫。</summary>
    public void PlaySelected()
    {
        if (controller == null)
        {
            Debug.LogWarning($"[{nameof(SpineAnimationPad)}] 尚未指定 Controller。", this);
            return;
        }
        controller.Initialize();
        if (string.IsNullOrEmpty(selectedAnimation))
        {
            Debug.LogWarning($"[{nameof(SpineAnimationPad)}] 未選擇動畫名稱。", this);
            return;
        }

        float delay = (clearMode == SpineAnimationController.ClearMode.ClearAfterDelay) ? clearAfterDelaySeconds : -1f;
        controller.PlayAnimation((MySpineSystem.AnimationTrack)trackIndex, selectedAnimation, clearMode, delay);
    }

    public void StopOnTrack()
    {
        if (controller == null) return;
        controller.StopAnimation((MySpineSystem.AnimationTrack)trackIndex);
    }

#if UNITY_EDITOR
    // 讓 Editor 能讀取/寫入目前選擇（不對外開放）。
    internal SpineAnimationController Editor_Controller => controller;
    internal ref string Editor_SelectedAnimation => ref selectedAnimation;
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpineAnimationPad))]
public class SpineAnimationPadEditor : Editor
{
    private SerializedProperty _controllerProp;
    private SerializedProperty _trackIndexProp;
    private SerializedProperty _selectedAnimProp;
    private SerializedProperty _clearModeProp;
    private SerializedProperty _delayProp;

    private List<string> _animationNames = new List<string>();
    private int _animPopupIndex = -1;

    private void OnEnable()
    {
        _controllerProp = serializedObject.FindProperty("controller");
        _trackIndexProp = serializedObject.FindProperty("trackIndex");
        _selectedAnimProp = serializedObject.FindProperty("selectedAnimation");
        _clearModeProp = serializedObject.FindProperty("clearMode");
        _delayProp = serializedObject.FindProperty("clearAfterDelaySeconds");
        RefreshAnimationList();
        SyncPopupIndexWithSelectedAnimation();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_controllerProp);

        using (new EditorGUI.DisabledScope(_controllerProp.objectReferenceValue == null))
        {
            // Track index 欄位
            EditorGUILayout.PropertyField(_trackIndexProp, new GUIContent("Track Index"));

            // 動畫下拉清單
            DrawAnimationPopup();

            // 清軌策略
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clear Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_clearModeProp);
            var mode = (SpineAnimationController.ClearMode)_clearModeProp.enumValueIndex;
            using (new EditorGUI.DisabledScope(mode != SpineAnimationController.ClearMode.ClearAfterDelay))
            {
                EditorGUILayout.PropertyField(_delayProp, new GUIContent("Delay Seconds"));
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!UnityEngine.Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶ 播放所選動畫 (使用上方模式)"))
                {
                    var pad = (SpineAnimationPad)target;
                    pad.PlaySelected();
                }
                if (GUILayout.Button("⏹ 清除此 Track"))
                {
                    var pad = (SpineAnimationPad)target;
                    pad.StopOnTrack();
                }
                EditorGUILayout.EndHorizontal();

                // 快速三鍵：直接以三種模式播放，不改動上方模式設定
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("快速模式播放", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("ClearOnComplete"))
                    PlaySelectedWithMode(SpineAnimationController.ClearMode.ClearOnComplete);
                if (GUILayout.Button("KeepTrack"))
                    PlaySelectedWithMode(SpineAnimationController.ClearMode.KeepTrack);
                if (GUILayout.Button("ClearAfterDelay"))
                    PlaySelectedWithMode(SpineAnimationController.ClearMode.ClearAfterDelay);
                if (GUILayout.Button("🔁 Loop"))
                    PlaySelectedWithMode(SpineAnimationController.ClearMode.Loop);
                EditorGUILayout.EndHorizontal();
            }

            // 快速清單（全部動畫列出，點一下即在目前 Track 以所選模式播放）
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("全部動畫（點擊於當前 Track 播放，使用上方模式）", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!UnityEngine.Application.isPlaying))
            {
                for (int i = 0; i < _animationNames.Count; i++)
                {
                    if (GUILayout.Button(_animationNames[i]))
                    {
                        PlayByName(_animationNames[i]);
                    }
                }
            }
        }

        if (GUILayout.Button("↻ 重新載入動畫清單"))
        {
            RefreshAnimationList();
            SyncPopupIndexWithSelectedAnimation();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAnimationPopup()
    {
        if (_animationNames.Count == 0)
        {
            EditorGUILayout.HelpBox("找不到動畫。請確認 Controller 上的 SkeletonAnimation / SkeletonGraphic 是否綁定了有效的 SkeletonDataAsset。", MessageType.Info);
            return;
        }

        int newIndex = EditorGUILayout.Popup(new GUIContent("Animation"), Mathf.Max(0, _animPopupIndex), _animationNames.ToArray());
        if (newIndex != _animPopupIndex)
        {
            _animPopupIndex = newIndex;
            _selectedAnimProp.stringValue = _animationNames[_animPopupIndex];
        }
    }

    private void PlaySelectedWithMode(SpineAnimationController.ClearMode mode)
    {
        var pad = (SpineAnimationPad)target;
        if (pad == null || pad.Editor_Controller == null)
        {
            Debug.LogWarning("SpineAnimationController 未指定。");
            return;
        }
        float delay = (mode == SpineAnimationController.ClearMode.ClearAfterDelay) ? _delayProp.floatValue : -1f;
        pad.Editor_Controller.PlayAnimation((MySpineSystem.AnimationTrack)_trackIndexProp.intValue, pad.Editor_SelectedAnimation, mode, delay);
        Repaint();
    }

    private void PlayByName(string animName)
    {
        var pad = (SpineAnimationPad)target;
        if (pad == null) return;
        if (pad.Editor_Controller == null)
        {
            Debug.LogWarning("SpineAnimationController 未指定。");
            return;
        }
        pad.Editor_SelectedAnimation = animName;

        var mode = (SpineAnimationController.ClearMode)_clearModeProp.enumValueIndex;
        float delay = (mode == SpineAnimationController.ClearMode.ClearAfterDelay) ? _delayProp.floatValue : -1f;
        pad.Editor_Controller.PlayAnimation((MySpineSystem.AnimationTrack)_trackIndexProp.intValue, animName, mode, delay);
        Repaint();
    }

    private void RefreshAnimationList()
    {
        _animationNames.Clear();

        var controller = _controllerProp.objectReferenceValue as SpineAnimationController;
        if (controller == null)
            return;

        // 從 Controller 的目標 Skeleton 取得可用動畫清單
        SkeletonDataAsset sda = null;
        var go = controller.gameObject;
        var sa = go.GetComponent<SkeletonAnimation>();
        if (sa != null && sa.skeletonDataAsset != null) sda = sa.skeletonDataAsset;
        else
        {
            var sg = go.GetComponent<SkeletonGraphic>();
            if (sg != null && sg.SkeletonDataAsset != null) sda = sg.SkeletonDataAsset;
        }

        if (sda == null || sda.GetSkeletonData(true) == null)
            return;

        var data = sda.GetSkeletonData(true);
        for (int i = 0; i < data.Animations.Count; i++)
        {
            _animationNames.Add(data.Animations.Items[i].Name);
        }

        _animationNames.Sort(System.StringComparer.Ordinal);
    }

    private void SyncPopupIndexWithSelectedAnimation()
    {
        string current = _selectedAnimProp.stringValue;
        _animPopupIndex = Mathf.Max(0, _animationNames.FindIndex(n => n == current));
        if (_animationNames.Count > 0 && _animPopupIndex < 0)
        {
            _animPopupIndex = 0;
            _selectedAnimProp.stringValue = _animationNames[0];
        }
    }
}
#endif