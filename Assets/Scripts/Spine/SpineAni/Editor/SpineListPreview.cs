using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>編輯模式專用的獨立骨架預覽，不執行遊戲事件或修改來源動畫狀態。</summary>
internal sealed class SpineListPreview : IDisposable
{
    private GameObject previewObject, sourceObject;
    private bool sourceWasHidden;
    private ISkeletonRenderer renderer;
    private Spine.AnimationState state;
    private IEnumerator<float> sequence;
    private float remaining;
    private double lastTime;
    private readonly System.Random random = new System.Random();
    private List<SpinePlayByList.SpinePlayGroup> groups;
    public bool Active => previewObject != null;
    public bool Paused { get; set; }
    public bool Condition { get; set; }
    public string Group { get; private set; }
    public int Clip { get; private set; } = -1;
    public string Status { get; private set; } = "尚未試播";

    internal static SpineAnimationController Controller(SpinePlayByList target)
    {
        if (target == null) return null;
        using (var data = new SerializedObject(target))
            return data.FindProperty("_controller").objectReferenceValue as SpineAnimationController
                ?? target.GetComponent<SpineAnimationController>();
    }

    internal static ISkeletonRenderer SourceRenderer(SpinePlayByList target)
    {
        var controller = Controller(target);
        if (controller == null) return null;
        using (var data = new SerializedObject(controller))
        {
            var animation = data.FindProperty("skeletonAnimation").objectReferenceValue as SkeletonAnimation
                ?? controller.GetComponent<SkeletonAnimation>();
            if (animation != null) return animation.Renderer;
            var graphic = data.FindProperty("skeletonGraphic").objectReferenceValue as SkeletonGraphic
                ?? controller.GetComponent<SkeletonGraphic>();
            return graphic;
        }
    }

    public void Start(SpinePlayByList target, string groupName)
    {
        Dispose();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            var source = SourceRenderer(target);
            var component = source as Component;
            if (component == null || EditorUtility.IsPersistent(component) || !component.gameObject.scene.IsValid())
                throw new InvalidOperationException("請指定場景或 Prefab Mode 內具有 Spine 顯示元件的目標。");
            if (!component.gameObject.activeInHierarchy)
                throw new InvalidOperationException("請先啟用目標物件及其父物件，再進行試播。");
            if (source.SkeletonDataAsset == null)
                throw new InvalidOperationException("Spine 顯示元件尚未指定 SkeletonDataAsset。");

            // 預覽使用設定快照；可繼續編輯，重新播放時才套用新設定。
            groups = new List<SpinePlayByList.SpinePlayGroup>();
            foreach (var group in target.groups)
            {
                if (group == null) continue;
                var copy = new SpinePlayByList.SpinePlayGroup { groupName = group.groupName, track = group.track,
                    nextGroupName = group.nextGroupName, checkGroupName = group.checkGroupName };
                if (group.clips != null) foreach (var clip in group.clips)
                    if (clip != null) copy.clips.Add(new SpinePlayByList.SpineClip { animationName = clip.animationName,
                        repeatCount = clip.repeatCount, isLoop = clip.isLoop, isRandom = clip.isRandom });
                groups.Add(copy);
            }
            var first = Find(groupName);
            if (first == null) throw new InvalidOperationException("找不到試播組：" + groupName);
            var groupNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in groups)
                if (string.IsNullOrWhiteSpace(group.groupName) || !groupNames.Add(group.groupName))
                    throw new InvalidOperationException("請先修正空白或重複的組名，再進行試播。");
            CreateRenderer(source, component);
            state = new Spine.AnimationState(new AnimationStateData(renderer.Skeleton.Data));
            sourceObject = component.gameObject;
            sourceWasHidden = SceneVisibilityManager.instance.IsHidden(sourceObject);
            // 僅在 Scene View 隱藏來源；不改 enabled、active 或來源的序列化設定。
            if (!sourceWasHidden) SceneVisibilityManager.instance.Hide(sourceObject, false);
            sequence = Run(first).GetEnumerator();
            remaining = 0;
            Paused = false;
            lastTime = EditorApplication.timeSinceStartup;
            Tick();
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void CreateRenderer(ISkeletonRenderer source, Component component)
    {
        bool ui = source is SkeletonGraphic;
        previewObject = new GameObject("Spine 清單試播（暫時）", ui ? typeof(RectTransform) : typeof(Transform));
        previewObject.hideFlags = HideFlags.HideAndDontSave;
        previewObject.layer = component.gameObject.layer;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(previewObject, component.gameObject.scene);
        var t = previewObject.transform;
        t.SetParent(component.transform.parent, false);
        t.localPosition = component.transform.localPosition;
        t.localRotation = component.transform.localRotation;
        t.localScale = component.transform.localScale;
        if (source is SkeletonGraphic originalGraphic)
        {
            var rect = (RectTransform)t;
            var originalRect = originalGraphic.rectTransform;
            rect.anchorMin = originalRect.anchorMin;
            rect.anchorMax = originalRect.anchorMax;
            rect.pivot = originalRect.pivot;
            rect.sizeDelta = originalRect.sizeDelta;
            rect.anchoredPosition3D = originalRect.anchoredPosition3D;
            t.SetSiblingIndex(component.transform.GetSiblingIndex() + 1);
            var graphic = previewObject.AddComponent<SkeletonGraphic>();
            graphic.skeletonDataAsset = source.SkeletonDataAsset;
            graphic.material = originalGraphic.material;
            graphic.color = originalGraphic.color;
            graphic.MeshSettings = originalGraphic.MeshSettings;
            graphic.allowMultipleCanvasRenderers = originalGraphic.allowMultipleCanvasRenderers;
            graphic.additiveMaterial = originalGraphic.additiveMaterial;
            graphic.multiplyMaterial = originalGraphic.multiplyMaterial;
            graphic.screenMaterial = originalGraphic.screenMaterial;
            graphic.forceAdditiveMaterial = originalGraphic.forceAdditiveMaterial;
            graphic.layoutScaleMode = originalGraphic.layoutScaleMode;
            graphic.raycastTarget = false;
            graphic.Initialize(true);
            renderer = graphic;
        }
        else if (source is SkeletonRenderer originalRenderer)
        {
            var skeletonRenderer = previewObject.AddComponent<SkeletonRenderer>();
            skeletonRenderer.skeletonDataAsset = source.SkeletonDataAsset;
            skeletonRenderer.MeshSettings = originalRenderer.MeshSettings;
            skeletonRenderer.Initialize(true);
            foreach (var pair in originalRenderer.CustomMaterialOverride)
                skeletonRenderer.CustomMaterialOverride[pair.Key] = pair.Value;
            var originalMesh = component.GetComponent<MeshRenderer>();
            var mesh = previewObject.GetComponent<MeshRenderer>();
            if (originalMesh != null && mesh != null)
            {
                mesh.sortingLayerID = originalMesh.sortingLayerID;
                mesh.sortingOrder = originalMesh.sortingOrder;
            }
            renderer = skeletonRenderer;
        }
        else throw new InvalidOperationException("目前試播支援 SkeletonRenderer 與 SkeletonGraphic。");
        // Spine 4.3 的升級流程可能自動補上動畫元件；停用暫時物件上的自動更新，避免覆蓋預覽姿勢。
        foreach (var animation in previewObject.GetComponents<SkeletonAnimationBase>()) animation.enabled = false;
        // 保留皮膚、翻轉及目前姿勢；所有動畫都套用在獨立骨架上。
        renderer.Skeleton = new Skeleton(source.Skeleton);
        renderer.Animation = null;
        renderer.UpdateMode = UpdateMode.FullUpdate;
        renderer.UpdateWhenInvisible = UpdateMode.FullUpdate;
        renderer.EditorSkipSkinSync = true;
        foreach (var pair in source.CustomSlotMaterials)
        {
            var slot = renderer.Skeleton.FindSlot(pair.Key.Data.Name);
            if (slot != null) renderer.CustomSlotMaterials[slot] = pair.Value;
        }
    }

    public void Tick()
    {
        if (!Active)
        {
            if (sourceObject != null) Dispose();
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        float delta = Mathf.Clamp((float)(now - lastTime), 0, 0.1f);
        lastTime = now;
        if (sourceObject == null || EditorApplication.isPlayingOrWillChangePlaymode) { Dispose(); return; }
        if (Paused) return;
        try
        {
            // 限制每次更新推進次數，零長度動畫或無效鏈結不會卡住 Editor。
            int steps = 0;
            while (remaining <= 0)
            {
                if (++steps > 128) throw new InvalidOperationException("試播包含過多零長度動畫，已停止。");
                if (!sequence.MoveNext()) { Dispose(); Status = "試播完成"; return; }
                remaining = sequence.Current;
            }
            float advance = Mathf.Min(delta, remaining);
            state.Update(advance);
            state.Apply(renderer.Skeleton);
            renderer.Skeleton.Update(advance);
            renderer.Skeleton.UpdateWorldTransform(Spine.Physics.Update);
            remaining -= advance;
            if (renderer is SkeletonRenderer sr) sr.LateUpdate();
            else if (renderer is SkeletonGraphic sg) sg.LateUpdate();
            SceneView.RepaintAll();
        }
        catch (Exception exception) { Fail(exception); }
    }

    private IEnumerable<float> Run(SpinePlayByList.SpinePlayGroup group)
    {
        // 與 PlayGroup 相同：開始前只做一次立即條件跳轉。
        if (Condition && Find(group.checkGroupName) != null) group = Find(group.checkGroupName);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (group != null)
        {
            if (!visited.Add(group.groupName)) throw new InvalidOperationException("組鏈結形成循環：" + group.groupName);
            Group = group.groupName;
            int loop = group.clips.FindIndex(c => c.isLoop);
            bool allowLoop = string.IsNullOrEmpty(group.nextGroupName) && loop >= 0;
            foreach (float duration in Range(group, 0, allowLoop ? loop : group.clips.Count)) yield return duration;
            var next = Condition ? Find(group.checkGroupName) : null;
            while (next == null && allowLoop)
            {
                bool played = false;
                foreach (float duration in Range(group, loop, group.clips.Count)) { played = true; yield return duration; }
                if (!played) throw new InvalidOperationException("循環後段沒有可播放的動畫。");
                next = Condition ? Find(group.checkGroupName) : null;
            }
            if (next != null) { group = next; continue; }
            if (string.IsNullOrEmpty(group.nextGroupName)) yield break;
            next = Find(group.nextGroupName);
            if (next == null) throw new InvalidOperationException("找不到下一組：" + group.nextGroupName);
            group = next;
        }
    }

    private IEnumerable<float> Range(SpinePlayByList.SpinePlayGroup group, int start, int end)
    {
        var order = new List<int>();
        for (int i = start; i < end; i++) order.Add(i);
        int randomStart = group.clips.FindIndex(c => c.isRandom);
        if (randomStart >= 0)
            for (int i = order.Count - 1, first = Mathf.Max(start, randomStart) - start; i > first; i--)
            { int j = random.Next(first, i + 1); int temp = order[i]; order[i] = order[j]; order[j] = temp; }
        foreach (int index in order)
        {
            var clip = group.clips[index];
            if (string.IsNullOrEmpty(clip.animationName)) continue;
            var animation = renderer.Skeleton.Data.FindAnimation(clip.animationName);
            if (animation == null) throw new InvalidOperationException("找不到動畫：" + clip.animationName);
            Clip = index;
            for (int repeat = 0; repeat < Mathf.Max(1, clip.repeatCount); repeat++)
            {
                state.ClearTrack((int)group.track);
                state.SetAnimation((int)group.track, animation, false);
                Status = group.groupName + " / " + clip.animationName + "  第 " + (repeat + 1) + " 次";
                yield return Mathf.Max(0.001f, animation.Duration);
            }
        }
    }

    private SpinePlayByList.SpinePlayGroup Find(string name) => string.IsNullOrEmpty(name) ? null
        : groups.Find(g => string.Equals(g.groupName, name, StringComparison.Ordinal));
    private void Fail(Exception exception) { Dispose(); Status = "試播停止：" + exception.Message; }
    public void Dispose()
    {
        sequence?.Dispose();
        sequence = null;
        state = null;
        renderer = null;
        if (previewObject != null) UnityEngine.Object.DestroyImmediate(previewObject);
        previewObject = null;
        if (sourceObject != null && !sourceWasHidden) SceneVisibilityManager.instance.Show(sourceObject, false);
        sourceObject = null;
        Group = null;
        Clip = -1;
        Paused = false;
        Status = "已停止試播";
        SceneView.RepaintAll();
    }
}
