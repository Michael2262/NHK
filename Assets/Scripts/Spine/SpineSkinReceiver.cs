using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>將保存的外觀 ID 轉成此 Spine 的本地 skin 組合；所有套用皆整組取代。</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class SpineSkinReceiver : MonoBehaviour
{
    [Serializable]
    public class SkinGroup
    {
        public string id;
        public List<string> skins = new List<string>();
    }

    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [Tooltip("可與其他場景或其他物件共用；相同 ID 讀取同一份外觀狀態，區分大小寫。")]
    [SerializeField] private string receiverID;
    [Tooltip("留空使用下方手動清單；填入本地組合 ID 時，該組合也是編輯預覽與直接試玩的預設。")]
    [SerializeField] private string defaultPresetID;
    [SerializeField] private List<string> defaultSkins = new List<string>();
    [SerializeField] private List<SkinGroup> groups = new List<SkinGroup>();

    public string RequestedPresetID { get; private set; }
    public string DisplaySource { get; private set; }
    public string ActiveSkins { get; private set; }
    public SkeletonDataAsset DataAsset => skeletonAnimation != null ? skeletonAnimation.skeletonDataAsset : null;

    private GameStatusService service;
    private SpineSkinStateModel model;
    private SkeletonAnimation subscribedAnimation;
    private Skeleton appliedSkeleton;
    private string appliedReceiverID;
    private bool dirty = true;
    private bool applying;
    private readonly HashSet<string> warnings = new HashSet<string>();
#if UNITY_EDITOR
    private ISkeletonRenderer previewRenderer;
    private bool previousSkipSkinSync;
#endif

    private bool IsPlaying => Application.IsPlaying(gameObject);

    private void Reset() => skeletonAnimation = GetComponent<SkeletonAnimation>();

    private void OnEnable()
    {
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
        BindAnimation();
        BindService();
        RefreshFromState();
    }

    private void OnDisable()
    {
        UnbindService();
        ReleaseEditorPreview();
        if (subscribedAnimation != null) subscribedAnimation.OnAnimationRebuild -= HandleAnimationRebuild;
        subscribedAnimation = null;
        appliedSkeleton = null;
    }

    // OnValidate 可能來自載入執行緒；只標記，Unity / Spine 呼叫留給主執行緒。
    private void OnValidate() => dirty = true;

    private void Update()
    {
        BindAnimation();
        if (BindService() || appliedReceiverID != receiverID) RefreshFromState();
        if (skeletonAnimation != null && skeletonAnimation.Skeleton != appliedSkeleton) dirty = true;
        if (dirty) ApplySelection();
    }

    private void BindAnimation()
    {
        if (subscribedAnimation == skeletonAnimation) return;
        ReleaseEditorPreview();
        if (subscribedAnimation != null) subscribedAnimation.OnAnimationRebuild -= HandleAnimationRebuild;
        subscribedAnimation = skeletonAnimation;
        if (subscribedAnimation != null) subscribedAnimation.OnAnimationRebuild += HandleAnimationRebuild;
        dirty = true;
    }

    private void HandleAnimationRebuild(ISkeletonAnimation animation)
    {
        dirty = true;
        // Spine 重建時保留本次顯示選擇，不提前消費僅供下次進場的狀態。
        if (!applying) ApplySelection();
    }

    private bool BindService()
    {
        var next = IsPlaying ? GameStatusService.Instance : null;
        if (ReferenceEquals(service, next) && ReferenceEquals(model, next != null ? next.SpineSkins : null)) return false;
        UnbindService();
        service = next;
        model = service != null ? service.SpineSkins : null;
        if (service != null) service.OnGameStatusLoaded += RefreshFromState;
        if (model != null) model.OnApplyRequested += HandleApplyRequested;
        return true;
    }

    private void UnbindService()
    {
        if (!ReferenceEquals(service, null)) service.OnGameStatusLoaded -= RefreshFromState;
        if (model != null) model.OnApplyRequested -= HandleApplyRequested;
        service = null;
        model = null;
    }

    private void HandleApplyRequested(string id)
    {
        if (string.Equals(id, receiverID, StringComparison.Ordinal)) RefreshFromState();
    }

    /// <summary>讀取最新保存狀態；也可供 UnityEvent 手動要求立即刷新。</summary>
    public void RefreshFromState()
    {
        BindService();
        RequestedPresetID = null;
        if (IsPlaying && model != null && model.TryGetPreset(receiverID, out string id)) RequestedPresetID = id;
        appliedReceiverID = receiverID;
        dirty = true;
        ApplySelection();
    }

    /// <summary>Inspector 預覽只更新顯示，不寫入遊戲 Model 或存檔。</summary>
    public void RefreshPreview()
    {
        if (!IsPlaying) RequestedPresetID = null;
        warnings.Clear();
        dirty = true;
        ApplySelection();
    }

    private SkinGroup FindGroup(string id)
    {
        if (string.IsNullOrEmpty(id) || groups == null) return null;
        SkinGroup found = null;
        foreach (var group in groups)
        {
            if (group == null || !string.Equals(group.id, id, StringComparison.Ordinal)) continue;
            if (found != null) WarnOnce($"組合 ID「{id}」重複，使用第一組。");
            else found = group;
        }
        return found;
    }

    private void ApplySelection()
    {
        if (applying || !isActiveAndEnabled || skeletonAnimation == null) return;
        applying = true;
        try
        {
            skeletonAnimation.Initialize(false);
            var skeleton = skeletonAnimation.Skeleton;
            if (skeleton == null || !skeletonAnimation.IsValid) return;
#if UNITY_EDITOR
            // Spine Inspector 預設會同步 InitialSkin；預覽期間停用同步，避免覆蓋合成外觀。
            if (!IsPlaying && !ReferenceEquals(previewRenderer, skeletonAnimation.Renderer))
            {
                ReleaseEditorPreview();
                previewRenderer = skeletonAnimation.Renderer;
                previousSkipSkinSync = previewRenderer.EditorSkipSkinSync;
                previewRenderer.EditorSkipSkinSync = true;
            }
            else if (IsPlaying) ReleaseEditorPreview();
#endif

            var selected = FindGroup(RequestedPresetID);
            bool hasRequest = !string.IsNullOrEmpty(RequestedPresetID);
            if (hasRequest && selected == null)
                WarnOnce($"接收器「{receiverID}」沒有組合「{RequestedPresetID}」，使用自身預設；保存紀錄維持不變。");

            if (selected != null) DisplaySource = "保存狀態：" + selected.id;
            else
            {
                selected = FindGroup(defaultPresetID);
                if (!string.IsNullOrEmpty(defaultPresetID) && selected == null)
                    WarnOnce($"預設組合「{defaultPresetID}」不存在，使用手動清單。");
                DisplaySource = (hasRequest ? "找不到組合，回到預設：" : "自身預設：") +
                    (selected != null ? selected.id : "手動清單");
            }

            var names = selected != null ? selected.skins : defaultSkins;
            var composite = new Skin("RECEIVER_COMPOSITE");
            var activeNames = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (names != null)
            {
                foreach (string name in names)
                {
                    if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) continue;
                    var skin = skeleton.Data.FindSkin(name);
                    if (skin == null) { WarnOnce($"Spine 資料內沒有 skin「{name}」，已略過。"); continue; }
                    composite.AddSkin(skin);
                    activeNames.Add(name);
                }
            }

            // 不追加舊 skin、不自動加入 InitialSkin，也不設固定基底。
            skeleton.SetSkin(composite);
            skeleton.SetupPoseSlots();
            // 以零時間更新重套目前動畫，也讓 UpdateLocal 的視覺工具有機會刷新。
            skeletonAnimation.Update(0f);
            if (!IsPlaying && skeletonAnimation.Renderer != null) skeletonAnimation.Renderer.LateUpdate();
            ActiveSkins = string.Join(", ", activeNames);
            appliedSkeleton = skeleton;
            dirty = false;
        }
        finally { applying = false; }
    }

    private void WarnOnce(string message)
    {
        if (warnings.Add(message)) Debug.LogWarning("[SpineSkinReceiver] " + message, this);
    }

    private void ReleaseEditorPreview()
    {
#if UNITY_EDITOR
        if (previewRenderer != null && previewRenderer.Component != null)
            previewRenderer.EditorSkipSkinSync = previousSkipSkinSync;
        previewRenderer = null;
#endif
    }
}
