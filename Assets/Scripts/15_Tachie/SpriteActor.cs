using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SpriteActor：通用的 Sprite 替換元件。
/// 每個實例持有自己的 ID 與圖片清單，透過靜態字典供外部查找。
///
/// 用法：
///   // 透過 ID 查找並替換圖片
///   SpriteActor.Find("Enemy01")?.ChangeSprite("Damaged");
///
///   // 或直接拿到參考後操作
///   actor.ChangeSprite("Happy");
/// </summary>
public class SpriteActor : MonoBehaviour
{
    // ── 靜態查找系統 ──
    private static readonly Dictionary<string, SpriteActor> registry =
        new Dictionary<string, SpriteActor>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 透過 ID 查找場景中的 SpriteActor，找不到回傳 null。
    /// </summary>
    public static SpriteActor Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        registry.TryGetValue(id, out SpriteActor actor);
        return actor;
    }

    /// <summary>
    /// 透過 ID 查找並替換圖片的便捷靜態方法。
    /// </summary>
    public static void Set(string actorID, string spriteName)
    {
        var actor = Find(actorID);
        if (actor != null)
        {
            actor.ChangeSprite(spriteName);
        }
        else
        {
            Debug.LogWarning($"[SpriteActor] 找不到 ID: {actorID}");
        }
    }

    // ── 實例設定 ──

    [Tooltip("此元件的唯一識別 ID，供外部查找用")]
    public string actorID;

    [Tooltip("目標 SpriteRenderer，未指定則自動抓自身的")]
    public SpriteRenderer targetRenderer;

    [Tooltip("名稱與 Sprite 的對應清單")]
    public List<SpriteEntry> spriteList = new List<SpriteEntry>();

    // 內部快查字典
    private Dictionary<string, Sprite> spriteDict;

    [System.Serializable]
    public struct SpriteEntry
    {
        public string name;
        public Sprite sprite;
    }

    // ── 生命週期 ──

    private void Awake()
    {
        // 自動抓取 SpriteRenderer
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        // 建立快查字典
        spriteDict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in spriteList)
        {
            if (string.IsNullOrEmpty(entry.name)) continue;
            if (!spriteDict.ContainsKey(entry.name))
                spriteDict.Add(entry.name, entry.sprite);
            else
                Debug.LogWarning($"[SpriteActor] {actorID} 有重複的圖片名稱: {entry.name}");
        }

        // 註冊到靜態字典
        Register();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    // ── 註冊/反註冊 ──

    private void Register()
    {
        if (string.IsNullOrEmpty(actorID))
        {
            Debug.LogWarning($"[SpriteActor] {gameObject.name} 的 actorID 為空，無法註冊。");
            return;
        }

        if (registry.ContainsKey(actorID))
        {
            Debug.LogWarning($"[SpriteActor] 重複的 ID: {actorID}（物件: {gameObject.name}），覆蓋舊的註冊。");
        }

        registry[actorID] = this;
    }

    private void Unregister()
    {
        if (!string.IsNullOrEmpty(actorID) && registry.TryGetValue(actorID, out SpriteActor registered))
        {
            // 只移除自己，避免誤刪同 ID 的新物件
            if (registered == this)
                registry.Remove(actorID);
        }
    }

    // ── 核心 API ──

    /// <summary>
    /// 替換為指定名稱的 Sprite。傳入空字串或 "None" 會清除圖片。
    /// </summary>
    public void ChangeSprite(string spriteName)
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"[SpriteActor] {actorID} 沒有 SpriteRenderer。");
            return;
        }

        // 清除圖片
        if (string.IsNullOrEmpty(spriteName) ||
            string.Equals(spriteName, "None", System.StringComparison.OrdinalIgnoreCase))
        {
            targetRenderer.sprite = null;
            return;
        }

        // 查找並替換
        if (spriteDict.TryGetValue(spriteName, out Sprite spr))
        {
            targetRenderer.sprite = spr;
        }
        else
        {
            Debug.LogWarning($"[SpriteActor] {actorID} 找不到圖片名稱: {spriteName}");
        }
    }

    /// <summary>
    /// 取得目前顯示的 Sprite 名稱（反查），找不到回傳 null。
    /// </summary>
    public string GetCurrentSpriteName()
    {
        if (targetRenderer == null || targetRenderer.sprite == null) return null;

        foreach (var entry in spriteList)
        {
            if (entry.sprite == targetRenderer.sprite)
                return entry.name;
        }
        return null;
    }
}
