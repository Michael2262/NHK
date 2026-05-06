// Copyright © 2025, Your Name or Company
// All rights reserved.

using UnityEngine;
using Spine.Unity;
using System.Collections.Generic;
using System; // Required for [Serializable]

/// <summary>
/// 控制 Spine Slot 的附件（Attachment）替換。
/// 允許在 Inspector 中定義多個替換組，並透過 title 字串來調用。
/// </summary>
public class SpineSlotChangeController : MonoBehaviour
{
    #region Nested Classes for Inspector
    // 定義單一的 "Slot名稱" 與 "要換上的附件名稱" 的配對
    [Serializable]
    public class SlotAttachmentPair
    {
        [Tooltip("要變更的 Slot 名稱")]
        public string slotName;

        [Tooltip("要換上的附件 (Attachment) 名稱")]
        public string attachmentName;
    }

    // 定義一組變更，包含一個標題和多個 Slot/Attachment 配對
    [Serializable]
    public class SlotChangeGroup
    {
        [Tooltip("用於從程式碼中搜尋並調用此組變更的唯一標題")]
        public string title;

        [Tooltip("此組變更包含的所有 Slot 與 Attachment 替換")]
        public List<SlotAttachmentPair> slotChanges;
    }
    #endregion

    #region Inspector Fields
    [Header("Spine 目標")]
    [Tooltip("要操作的 Spine 動畫組件 (SkeletonAnimation 或 SkeletonGraphic)")]
    public SkeletonAnimation skeletonAnimation;

    [Header("Slot 變更設定")]
    [Tooltip("定義所有可用的 Slot 變更組")]
    public List<SlotChangeGroup> changeGroups;
    #endregion

    #region Private Fields
    // 為了快速查找，將 List 轉換為 Dictionary
    private Dictionary<string, SlotChangeGroup> _changeGroupsDictionary;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 檢查 Spine 組件是否已設定
        if (skeletonAnimation == null)
        {
            // 嘗試自動從同一個 GameObject 上獲取
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                Debug.LogError("SpineSlotChangeController: SkeletonAnimation 組件未指定，且無法在此 GameObject 上找到！", this);
                this.enabled = false; // 禁用此腳本以防止錯誤
                return;
            }
        }

        InitializeDictionary();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// 將 Inspector 中設定的 List 轉換為 Dictionary 以提高搜尋效能。
    /// </summary>
    private void InitializeDictionary()
    {
        _changeGroupsDictionary = new Dictionary<string, SlotChangeGroup>();
        foreach (var group in changeGroups)
        {
            if (!string.IsNullOrEmpty(group.title) && !_changeGroupsDictionary.ContainsKey(group.title))
            {
                _changeGroupsDictionary.Add(group.title, group);
            }
            else
            {
                Debug.LogWarning($"SpineSlotChangeController: 發現重複或無效的 title: '{group.title}'。此設定組將被忽略。", this);
            }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// 根據提供的 title，應用對應的 Slot 附件變更。
    /// </summary>
    /// <param name="title">在 Inspector 中設定的 SlotChangeGroup 的 title。</param>
    public void ApplySlotChangeByTitle(string title)
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
        {
            Debug.LogError("SpineSlotChangeController: SkeletonAnimation 未初始化，無法執行變更。", this);
            return;
        }

        if (_changeGroupsDictionary.TryGetValue(title, out SlotChangeGroup groupToApply))
        {
            foreach (var change in groupToApply.slotChanges)
            {
                if (string.IsNullOrEmpty(change.slotName))
                {
                    Debug.LogWarning($"在 title '{title}' 的設定中，有 Slot 名稱未填寫。", this);
                    continue;
                }

                // 執行 Spine 的核心替換功能
                // 如果 attachmentName 是 null 或空字串，Spine 會隱藏該 Slot 的附件
                skeletonAnimation.Skeleton.SetAttachment(change.slotName, change.attachmentName);
            }
        }
        else
        {
            Debug.LogWarning($"SpineSlotChangeController: 找不到 title 為 '{title}' 的設定組。", this);
        }
    }
    #endregion
}