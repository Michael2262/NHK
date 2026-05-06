// 引入必要的命名空間
using Spine;
using Spine.Unity;
using System.Collections.Generic; // 需要使用 Dictionary
using UnityEngine;

// [System.Serializable] 讓這個 class 的實例可以顯示在 Unity Inspector 中
[System.Serializable]
public class AttachmentChange
{
    [Tooltip("要更換圖片的 Slot 名稱 (必須與 Spine 編輯器中的名稱完全一致)")]
    public string slotName;

    [Tooltip("要換上的 Attachment 名稱 (留空表示清除該 Slot 的附件)")]
    public string attachmentName;
}

[System.Serializable]
public class AttachmentSet
{
    [Tooltip("這組設定的自訂 ID，呼叫 API 時會使用這個 ID")]
    public string customID;

    [Tooltip("這組 ID 包含的所有 Slot 與 Attachment 變更")]
    public List<AttachmentChange> changes;
}


// ------------------------------------------------------------------------------------
// -- 主要控制器腳本 --
// ------------------------------------------------------------------------------------
[RequireComponent(typeof(SkeletonAnimation))] // 確保物件上一定有 SkeletonAnimation 組件
public class SpineAttachmentController : MonoBehaviour
{
    [Header("必要組件")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("附件設定組")]
    [Tooltip("在這裡定義所有可用的附件更換組合")]
    [SerializeField] private List<AttachmentSet> attachmentSets;

    // 內部資料結構，用於快速查找
    // Key: customID, Value: List of changes
    private Dictionary<string, List<AttachmentChange>> attachmentSetMap;
    private Skeleton skeleton;

    // Awake 在 Start 之前執行，適合用來初始化內部資料
    void Awake()
    {
        // 自動獲取 SkeletonAnimation 組件
        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        // 獲取骨架物件
        skeleton = skeletonAnimation.Skeleton;

        // 將 List 轉換為 Dictionary 以加速查找，這是優化效能的好方法
        attachmentSetMap = new Dictionary<string, List<AttachmentChange>>();
        foreach (var set in attachmentSets)
        {
            // 檢查 ID 是否重複
            if (!attachmentSetMap.ContainsKey(set.customID))
            {
                attachmentSetMap.Add(set.customID, set.changes);
            }
            else
            {
                Debug.LogWarning($"發現重複的 Custom ID: '{set.customID}'。請確保每個 ID 都是唯一的。");
            }
        }
    }

    /// <summary>
    /// 核心 API：根據提供的 ID，應用對應的一整組附件變更。
    /// </summary>
    /// <param name="customID">在 Inspector 中設定的自訂 ID</param>
    public void ApplyAttachmentSet(string customID)
    {
        if (string.IsNullOrEmpty(customID))
        {
            Debug.LogWarning("傳入的 customID 是空的。");
            return;
        }

        // 從 Dictionary 中快速查找對應的設定
        if (attachmentSetMap.TryGetValue(customID, out List<AttachmentChange> changesToApply))
        {
            // 遍歷該組設定中的每一個變更並應用
            foreach (var change in changesToApply)
            {
                if (string.IsNullOrEmpty(change.slotName)) continue;

                // 核心邏輯：設定附件
                // 如果 attachmentName 是 null 或空字串，SetAttachment 會自動清除該 slot
                skeleton.SetAttachment(change.slotName, change.attachmentName);
            }
        }
        else
        {
            Debug.LogError($"找不到 ID 為 '{customID}' 的附件設定組。請檢查 Inspector 中的設定或傳入的 ID 是否正確。");
        }
    }

    /// <summary>
    /// 清除所有在設定組中定義過的 Slot 的附件。
    /// </summary>
    public void ClearAllManagedSlots()
    {
        if (skeleton == null) return;

        // 建立一個 HashSet 來儲存所有被管理過的 slot 名稱，避免重複清除
        HashSet<string> managedSlots = new HashSet<string>();
        foreach (var set in attachmentSets)
        {
            foreach (var change in set.changes)
            {
                if (!string.IsNullOrEmpty(change.slotName))
                {
                    managedSlots.Add(change.slotName);
                }
            }
        }

        // 遍歷所有被管理過的 slot 並清除它們
        foreach (var slotName in managedSlots)
        {
            skeleton.SetAttachment(slotName, null);
        }
    }
}