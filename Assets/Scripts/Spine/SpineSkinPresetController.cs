using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;

[DisallowMultipleComponent]
public class SpineSkinPresetController : MonoBehaviour
{
    public enum ApplyMode { Replace, Additive }

    [Header("Target")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [Tooltip("提供 [SpineAnimation] 和 [SpineSkin] 下拉來源；通常設為 skeletonAnimation 用的同一份")]
    [SerializeField] private SkeletonDataAsset skeletonDataAsset;

    [Header("Composite Options")]
    [Tooltip("合成時是否包含 DefaultSkin")]
    [SerializeField] private bool includeDefaultSkin = true;
    [Tooltip("把 SkeletonAnimation.initialSkinName 也一起併進合成")]
    [SerializeField] private bool includeInitialSkin = true;

    [Header("初始啟用的 Skins（進入場景就啟用，照順序合成）")]
    [SerializeField] private List<SkinName> initialActiveSkins = new();

    [Header("定義你的套裝組 Presets")]
    [SerializeField] private List<PresetDef> presets = new();

    [Header("（除錯顯示）目前啟用中的 Skin 名稱（照合成順序）")]
    [SerializeField, TextArea(1, 6)] public string debugActiveList;

    private readonly HashSet<string> _activeSkinSet = new();
    private readonly List<string> _activeSkinOrder = new();
    private Dictionary<string, PresetDef> _map;

    [System.Serializable]
    public class SkinName { [SpineSkin(dataField: "skeletonDataAsset")] public string name; public override string ToString() => name; }

    [System.Serializable]
    public class PresetDef
    {
        [Tooltip("唯一 ID（呼叫 ChangeSkin(id) 用）")]
        public string id = "clothesLift";
        [Tooltip("Inspector 顯示用名稱，可留空")]
        public string displayName;
        [Tooltip("此套裝組合成時是否包含 DefaultSkin（未設定則使用全域設定 includeDefaultSkin）")]
        public bool? overrideIncludeDefault = null;

        [Header("Setup Animation on Change")]
        // ▼▼▼【修改 1/3：新增 useSetSlotsToSetupPose 布林值】▼▼▼
        [Tooltip("啟用後，換膚時將直接呼叫 SetSlotsToSetupPose()，而忽略下方的 Setup Animation 設定。")]
        public bool useSetSlotsToSetupPose = false;
        // ▲▲▲

        [SpineAnimation(dataField: nameof(SpineSkinPresetController.skeletonDataAsset))]
        [Tooltip("可選：指定一個在換膚後播放的「設置動畫」，用來處理 Slot 的顯示狀態。")]
        public string setupAnimationName;
        [Tooltip("設置動畫要播放的軌道 (Track)。預設為 1，以避免影響 Track 0 的主動畫。")]
        public int animationTrackIndex = 1;

        [Header("Skins to Combine")]
        public List<SkinName> addSkins = new();
        public List<SkinName> removeSkins = new();
    }

    private void Reset()
    {
        if (!skeletonAnimation) skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (!skeletonDataAsset && skeletonAnimation) skeletonDataAsset = skeletonAnimation.skeletonDataAsset;
    }

    private void Awake()
    {
        if (!skeletonAnimation) skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (!skeletonAnimation) { Debug.LogError($"[{nameof(SpineSkinPresetController)}] Missing SkeletonAnimation.", this); enabled = false; return; }
        if (!skeletonDataAsset && skeletonAnimation) skeletonDataAsset = skeletonAnimation.skeletonDataAsset;

        BuildPresetMap();

        _activeSkinSet.Clear();
        _activeSkinOrder.Clear();
        foreach (var s in initialActiveSkins) { if (string.IsNullOrEmpty(s?.name)) continue; if (_activeSkinSet.Add(s.name)) _activeSkinOrder.Add(s.name); }
    }

    private void Start()
    {
        if (skeletonAnimation != null && (skeletonAnimation.Skeleton == null || !skeletonAnimation.IsValid))
            skeletonAnimation.Initialize(false);

        // 初始套用時，不使用 SetSlotsToSetupPose，維持原樣
        RebuildAndApplyCompositeSkin(includeDefaultSkin, includeInitialSkin, null, 1, false);
    }

    private void BuildPresetMap()
    {
        _map = new Dictionary<string, PresetDef>();
        foreach (var p in presets) { if (string.IsNullOrEmpty(p?.id)) continue; if (_map.ContainsKey(p.id)) Debug.LogWarning($"[SpineSkinPresetController] Duplicate preset id: {p.id}", this); _map[p.id] = p; }
    }

    #region 公開 API
    public void ChangeSkin(string presetId) => ChangeSkin(presetId, ApplyMode.Additive);
    public void ChangeSkin(string presetId, ApplyMode mode = ApplyMode.Replace)
    {
        if (string.IsNullOrEmpty(presetId)) { Debug.LogWarning("[SpineSkinPresetController] presetId is null/empty.", this); return; }
        if (_map == null || _map.Count != presets.Count) BuildPresetMap();
        if (!_map.TryGetValue(presetId, out var preset)) { Debug.LogWarning($"[SpineSkinPresetController] Preset not found: {presetId}", this); return; }

        if (mode == ApplyMode.Replace) { _activeSkinSet.Clear(); _activeSkinOrder.Clear(); }
        foreach (var s in preset.removeSkins) { if (string.IsNullOrEmpty(s?.name)) continue; if (_activeSkinSet.Remove(s.name)) _activeSkinOrder.RemoveAll(n => n == s.name); }
        foreach (var s in preset.addSkins) { if (string.IsNullOrEmpty(s?.name)) continue; if (_activeSkinSet.Add(s.name)) _activeSkinOrder.Add(s.name); }

        // ▼▼▼【修改 2/3：傳遞 useSetSlotsToSetupPose 參數】▼▼▼
        RebuildAndApplyCompositeSkin(
            withDefault: preset.overrideIncludeDefault ?? includeDefaultSkin,
            withInitial: includeInitialSkin,
            setupAnimationName: preset.setupAnimationName,
            trackIndex: preset.animationTrackIndex,
            useSetSlotsToSetupPose: preset.useSetSlotsToSetupPose // 將 preset 中的設定傳遞下去
        );
        // ▲▲▲
    }

    public List<string> GetAllPresetIds()
    {
        if (_map == null || _map.Count != presets.Count)
        {
            BuildPresetMap();
        }
        return new List<string>(_map.Keys);
    }
    #endregion

    // ▼▼▼【修改 3/3：更新方法簽名並加入核心邏輯】▼▼▼
    private void RebuildAndApplyCompositeSkin(bool withDefault, bool withInitial, string setupAnimationName, int trackIndex, bool useSetSlotsToSetupPose)
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return;

        var skeleton = skeletonAnimation.Skeleton;
        var data = skeleton.Data;
        var composite = new Skin("COMPOSITE_PRESET");

        if (withDefault && data.DefaultSkin != null) composite.AddSkin(data.DefaultSkin);
        if (withInitial) { var initName = skeletonAnimation.Renderer.InitialSkinName; if (!string.IsNullOrEmpty(initName) && data.FindSkin(initName) is { } initSkin) composite.AddSkin(initSkin); }
        foreach (var name in _activeSkinOrder) { if (data.FindSkin(name) is { } skin) composite.AddSkin(skin); }

        skeleton.SetSkin(composite);

        // 根據 useSetSlotsToSetupPose 決定是直接重設姿勢，還是播放設置動畫
        if (useSetSlotsToSetupPose)
        {
            skeleton.SetupPoseSlots();
        }

        skeletonAnimation.AnimationState.Apply(skeleton);

        // 如果 useSetSlotsToSetupPose 為 true，則不播放動畫
        if (!useSetSlotsToSetupPose && !string.IsNullOrEmpty(setupAnimationName) && skeleton.Data.FindAnimation(setupAnimationName) != null)
        {
            var trackEntry = skeletonAnimation.AnimationState.SetAnimation(trackIndex, setupAnimationName, false);
            trackEntry.Complete += HandleSetupAnimationComplete;
        }

        debugActiveList = string.Join(", ", _activeSkinOrder);
    }
    // ▲▲▲

    private void HandleSetupAnimationComplete(TrackEntry trackEntry)
    {
        trackEntry.Complete -= HandleSetupAnimationComplete;

        if (Application.isPlaying && skeletonAnimation != null)
        {
            skeletonAnimation.AnimationState.ClearTrack(trackEntry.TrackIndex);
        }
    }

#if UNITY_EDITOR

#endif
}