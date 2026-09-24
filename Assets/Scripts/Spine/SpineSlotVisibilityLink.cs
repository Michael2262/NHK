using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>在動畫套用後，依啟用群組讓 Target 跟隨 Source；只處理視覺呈現。</summary>
[DisallowMultipleComponent]
public class SpineSlotVisibilityLink : MonoBehaviour
{
    [Serializable]
    public class SlotPair
    {
        [InspectorName("Source：被跟隨的插槽")]
        [Tooltip("填入 Spine 插槽名稱；Target 會跟隨這個插槽一起顯示、一起隱藏")]
        public string sourceSlotName;

        [InspectorName("Target：跟隨顯示的插槽")]
        [Tooltip("填入 Spine 插槽名稱；這個插槽的顯示狀態由 Source 決定")]
        public string targetSlotName;

        [InspectorName("Target：顯示的附件（可留空）")]
        [Tooltip("Target 顯示時使用的附件名稱；留空時使用 Target 插槽名稱尋找同名附件")]
        public string targetAttachmentName;
    }

    [Serializable]
    public class LinkGroup
    {
        [InspectorName("群組 ID")]
        [Tooltip("必填且不可重複；程式或 UnityEvent 透過此 ID 開啟／關閉群組，區分大小寫")]
        public string id;

        [InspectorName("啟用此群組")]
        public bool isEnabled = true;

        [InspectorName("可視度配對清單")]
        public List<SlotPair> links = new List<SlotPair>();
    }

    [Header("Spine 目標")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("可視度關聯")]
    [InspectorName("啟用顯示跟隨（總開關）")]
    [Tooltip("開啟後套用各啟用群組；關閉後所有群組暫停跟隨，回到動畫本身的控制")]
    public bool enableVisibilityLinks = true;

    [Header("Target 跟隨 Source：一起顯示、一起隱藏")]
    [InspectorName("Visibility Links（群組清單）")]
    [Tooltip("每組有獨立 ID、開關及配對清單。啟用群組的插槽若重複使用，以清單中先出現的有效配對為準，不支援串接或雙向控制")]
    public List<LinkGroup> visibilityLinkGroups = new List<LinkGroup>();

    // 保留舊版序列化資料，再移入群組；不需手動修改場景或 Prefab。
    [SerializeField, HideInInspector, FormerlySerializedAs("visibilityLinks")]
    private List<SlotPair> legacyVisibilityLinks = new List<SlotPair>();

    private struct OriginalState
    {
        public Slot slot;
        public Attachment attachment;
        public Attachment appliedAttachment;
        public float alpha;
        public float appliedAlpha;
    }

    private readonly List<OriginalState> originalStates = new List<OriginalState>();
    private readonly HashSet<string> usedSlots = new HashSet<string>();
    private readonly HashSet<string> usedGroupIds = new HashSet<string>();
    private readonly HashSet<string> warnings = new HashSet<string>();

    private void Reset()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    private void OnEnable()
    {
        MigrateLegacyLinks();
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation == null)
        {
            Debug.LogWarning("[SpineSlotVisibilityLink] 請指定 SkeletonAnimation。", this);
            enabled = false;
            return;
        }

        skeletonAnimation.Initialize(false);
        skeletonAnimation.BeforeApply += BeforeApply;
        skeletonAnimation.UpdateLocal += ApplyVisibility;
    }

    private void OnDisable()
    {
        if (skeletonAnimation != null)
        {
            skeletonAnimation.BeforeApply -= BeforeApply;
            skeletonAnimation.UpdateLocal -= ApplyVisibility;
        }
        RestoreOriginalStates();
        warnings.Clear();
    }

    private void BeforeApply(ISkeletonAnimation animated)
    {
        // 先撤銷上一幀的顯示覆寫，再讓動畫計算本幀狀態。
        RestoreOriginalStates();
    }

    private void ApplyVisibility(ISkeletonRenderer renderer)
    {
        RestoreOriginalStates();
        if (!enableVisibilityLinks || visibilityLinkGroups == null) return;

        Skeleton skeleton = skeletonAnimation.Skeleton;
        if (skeleton == null) return;

        usedSlots.Clear();
        usedGroupIds.Clear();
        foreach (LinkGroup group in visibilityLinkGroups)
        {
            if (group == null) continue;
            if (string.IsNullOrWhiteSpace(group.id))
            {
                WarnOnce("有群組未填 ID，已略過；請為每組填入唯一 ID。");
                continue;
            }
            if (!usedGroupIds.Add(group.id))
            {
                WarnOnce($"群組 ID「{group.id}」重複，只使用清單中第一組。");
                continue;
            }
            if (group.isEnabled && group.links != null)
                ApplyGroup(skeleton, group);
        }
    }

    private void ApplyGroup(Skeleton skeleton, LinkGroup group)
    {
        foreach (SlotPair pair in group.links)
        {
            if (pair == null || string.IsNullOrEmpty(pair.sourceSlotName)
                || string.IsNullOrEmpty(pair.targetSlotName)) continue;

            if (pair.sourceSlotName == pair.targetSlotName
                || usedSlots.Contains(pair.sourceSlotName) || usedSlots.Contains(pair.targetSlotName))
            {
                WarnOnce($"群組「{group.id}」的重複或自我關聯已略過：{pair.sourceSlotName} → {pair.targetSlotName}");
                continue;
            }

            Slot source = skeleton.FindSlot(pair.sourceSlotName);
            Slot target = skeleton.FindSlot(pair.targetSlotName);
            if (source == null || target == null)
            {
                WarnOnce($"找不到插槽：{pair.sourceSlotName} → {pair.targetSlotName}");
                continue;
            }

            usedSlots.Add(pair.sourceSlotName);
            usedSlots.Add(pair.targetSlotName);

            bool visible = source.Pose.Attachment != null && source.Pose.GetColor().a > 0f;
            Attachment attachment = target.Pose.Attachment;
            if (visible)
            {
                // 明確指定 Target 要顯示的附件；未填名稱時使用 Target 插槽名。
                string attachmentName = string.IsNullOrWhiteSpace(pair.targetAttachmentName)
                    ? pair.targetSlotName : pair.targetAttachmentName;
                attachment = skeleton.GetAttachment(target.Data.Index, attachmentName);
                if (attachment == null)
                    WarnOnce($"Target 插槽「{pair.targetSlotName}」找不到附件「{attachmentName}」，無法顯示。請檢查附件名稱與目前 Skin。");
            }

            Color color = target.Pose.GetColor();
            originalStates.Add(new OriginalState
            {
                slot = target,
                attachment = target.Pose.Attachment,
                appliedAttachment = attachment,
                alpha = color.a,
                appliedAlpha = visible ? 1f : 0f
            });

            // 隱藏時保留附件，只改 alpha，避免清除網格變形資料。
            target.Pose.Attachment = attachment;
            color.a = visible ? 1f : 0f;
            target.Pose.SetColor(color);
        }
    }

    /// <summary>供 UnityEvent 或程式以 ID 開啟群組；於下一次 Spine 動畫更新生效。</summary>
    public void EnableLinkGroup(string id) => SetLinkGroupEnabled(id, true);

    /// <summary>停止此群組的跟隨，恢復動畫控制；不是強制隱藏 Target。</summary>
    public void DisableLinkGroup(string id) => SetLinkGroupEnabled(id, false);

    public void SetLinkGroupEnabled(string id, bool isEnabled)
    {
        MigrateLegacyLinks();
        if (!string.IsNullOrWhiteSpace(id) && visibilityLinkGroups != null)
        {
            foreach (LinkGroup group in visibilityLinkGroups)
            {
                if (group == null || group.id != id) continue;
                group.isEnabled = isEnabled;
                return;
            }
        }
        WarnOnce($"找不到可視度群組 ID「{id}」，請檢查群組清單。");
    }

    private void OnValidate()
    {
        MigrateLegacyLinks();
    }

    private void MigrateLegacyLinks()
    {
        if (legacyVisibilityLinks == null || legacyVisibilityLinks.Count == 0) return;
        if (visibilityLinkGroups == null) visibilityLinkGroups = new List<LinkGroup>();

        string id = "Default";
        int suffix = 1;
        while (visibilityLinkGroups.Exists(group => group != null && group.id == id))
            id = "Default_" + suffix++;

        visibilityLinkGroups.Add(new LinkGroup
        {
            id = id,
            isEnabled = true,
            links = new List<SlotPair>(legacyVisibilityLinks)
        });
        legacyVisibilityLinks.Clear();
    }

    private void RestoreOriginalStates()
    {
        foreach (OriginalState state in originalStates)
        {
            // 若其他控制器已寫入不同狀態，保留它的新值。
            if (state.slot.Pose.Attachment == state.appliedAttachment)
                state.slot.Pose.Attachment = state.attachment;
            Color color = state.slot.Pose.GetColor();
            if (color.a == state.appliedAlpha)
            {
                color.a = state.alpha;
                state.slot.Pose.SetColor(color);
            }
        }
        originalStates.Clear();
    }

    private void WarnOnce(string message)
    {
        if (warnings.Add(message))
            Debug.LogWarning($"[SpineSlotVisibilityLink] {message}", this);
    }
}
