using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// CrossfadeSpriteActor：可交叉淡化（crossfade）的 Sprite 替換元件。
///
/// 與 <see cref="SpriteActor"/> 同一家族、同一「ID 註冊表」pattern：
/// 每個實例掛一個 actorID（管道），註冊進靜態字典供外部查找；
/// 差別是換圖時走 DOTween 交叉淡化，而非瞬間替換。
///
/// 主要用途：場景中的「背景圖」切換（SpriteRenderer，非 BGCG／立繪那條 BG）。
/// 但不限背景，任何需要淡化換圖的場景 SpriteRenderer 都可用。
///
/// 用法：
///   // 靜態便捷法（Sequencer 指令走這條）
///   CrossfadeSpriteActor.Set("BG", "night");
///   CrossfadeSpriteActor.Set("BG", "none");   // 淡出清空
///
///   // 或拿到參考後操作
///   CrossfadeSpriteActor.Find("BG")?.Show("day", 1.0f);
///
/// 掛法：
///   在背景 SpriteRenderer 物件上掛此元件 → 填 actorID（例如 "BG"）
///   → 在 spriteList 拖入各張背景圖並命名。
///   執行期會自動複製一個孿生 SpriteRenderer 當淡入層，Editor 不需手動準備第二層。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CrossfadeSpriteActor : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  靜態查找系統（與 SpriteActor 相同 pattern，但獨立字典避免 ID 撞名）
    // ══════════════════════════════════════════
    private static readonly Dictionary<string, CrossfadeSpriteActor> registry =
        new Dictionary<string, CrossfadeSpriteActor>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>透過 ID 查找場景中的 CrossfadeSpriteActor，找不到回傳 null。</summary>
    public static CrossfadeSpriteActor Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        registry.TryGetValue(id, out CrossfadeSpriteActor actor);
        return actor;
    }

    /// <summary>透過 ID 查找並交叉淡化換圖的便捷靜態方法。duration &lt; 0 表示用元件預設值。</summary>
    public static void Set(string actorID, string spriteName, float duration = -1f)
    {
        var actor = Find(actorID);
        if (actor != null) actor.Show(spriteName, duration);
        else Debug.LogWarning($"[CrossfadeSpriteActor] 找不到 ID: {actorID}");
    }

    // ══════════════════════════════════════════
    //  Inspector 設定
    // ══════════════════════════════════════════

    [Tooltip("此元件的唯一識別 ID（管道），供外部查找用，例如 BG")]
    public string actorID;

    [Tooltip("名稱與 Sprite 的對應清單")]
    public List<SpriteEntry> spriteList = new List<SpriteEntry>();

    [Header("交叉淡化")]
    [Tooltip("預設淡化時間（秒）。")]
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private Ease _ease = Ease.InOutQuad;

    [System.Serializable]
    public struct SpriteEntry
    {
        public string name;
        public Sprite sprite;
    }

    // ══════════════════════════════════════════
    //  內部
    // ══════════════════════════════════════════

    private Dictionary<string, Sprite> _spriteDict;

    // 兩個交替使用的圖層：_front = 目前顯示中，_back = 待淡入
    private SpriteRenderer _front;
    private SpriteRenderer _back;

    private void Awake()
    {
        // 本體上的 SpriteRenderer 當作 A 層（front）
        _front = GetComponent<SpriteRenderer>();

        // 建立快查字典
        _spriteDict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in spriteList)
        {
            if (string.IsNullOrEmpty(entry.name)) continue;
            if (!_spriteDict.ContainsKey(entry.name))
                _spriteDict.Add(entry.name, entry.sprite);
            else
                Debug.LogWarning($"[CrossfadeSpriteActor] {actorID} 有重複的圖片名稱: {entry.name}");
        }

        CreateFadeLayer();
        Register();
    }

    private void OnDestroy()
    {
        if (_front != null) _front.DOKill();
        if (_back != null) _back.DOKill();
        Unregister();
    }

    /// <summary>執行期複製一個孿生 SpriteRenderer 當作 B 層（待淡入層）。</summary>
    private void CreateFadeLayer()
    {
        var go = new GameObject((string.IsNullOrEmpty(actorID) ? name : actorID) + "_FadeLayer");
        var t = go.transform;
        t.SetParent(_front.transform, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        _back = go.AddComponent<SpriteRenderer>();
        // 對齊排序，B 層畫在 A 層前面（淡入時疊在上方）
        _back.sortingLayerID = _front.sortingLayerID;
        _back.sortingOrder = _front.sortingOrder + 1;
        _back.sharedMaterial = _front.sharedMaterial;
        _back.drawMode = _front.drawMode;
        _back.maskInteraction = _front.maskInteraction;
        _back.sprite = null;

        SetAlpha(_back, 0f);
    }

    // ══════════════════════════════════════════
    //  核心 API
    // ══════════════════════════════════════════

    /// <summary>
    /// 交叉淡化到指定名稱的 Sprite。傳入空字串或 "none" 會淡出清空。
    /// </summary>
    /// <param name="spriteName">spriteList 內的名稱。</param>
    /// <param name="duration">淡化秒數，&lt; 0 表示用元件預設值。</param>
    public void Show(string spriteName, float duration = -1f)
    {
        if (_front == null || _back == null) return;

        float dur = (duration < 0f) ? _duration : duration;

        // 解析目標 Sprite（none／空 = 清空）
        Sprite target = null;
        bool clear = string.IsNullOrEmpty(spriteName) ||
                     string.Equals(spriteName, "none", System.StringComparison.OrdinalIgnoreCase);
        if (!clear)
        {
            if (!_spriteDict.TryGetValue(spriteName, out target))
            {
                Debug.LogWarning($"[CrossfadeSpriteActor] {actorID} 找不到圖片名稱: {spriteName}");
                return;
            }
        }

        // 目標與現況相同就不重播（同圖、或都是清空）
        if (_front.sprite == target && Mathf.Approximately(_front.color.a, clear ? 0f : 1f))
            return;

        _front.DOKill();
        _back.DOKill();

        // 瞬間路徑（dur <= 0）：不經 DOTween，直接設值歸位
        if (dur <= 0f)
        {
            if (clear)
            {
                _front.sprite = null;
                SetAlpha(_front, 0f);
            }
            else
            {
                _front.sprite = target;
                SetAlpha(_front, 1f);
            }
            _back.sprite = null;
            SetAlpha(_back, 0f);
            return;
        }

        // 清空：只把 front 淡出
        if (clear)
        {
            _front.DOFade(0f, dur).SetEase(_ease)
                  .OnComplete(() => _front.sprite = null);
            return;
        }

        // 交叉淡化：新圖放 back，back 0→1、front 1→0，完成後交換角色
        _back.sprite = target;
        SetAlpha(_back, 0f);

        _front.DOFade(0f, dur).SetEase(_ease);
        _back.DOFade(1f, dur).SetEase(_ease)
             .OnComplete(SwapLayers);
    }

    /// <summary>淡化完成後，把 back 升為 front，舊 front 清空歸零備用。</summary>
    private void SwapLayers()
    {
        _front.sprite = null;
        SetAlpha(_front, 0f);

        var tmp = _front;
        _front = _back;
        _back = tmp;
    }

    /// <summary>取得目前顯示中的 Sprite 名稱（反查），找不到回傳 null。</summary>
    public string GetCurrentSpriteName()
    {
        if (_front == null || _front.sprite == null) return null;
        foreach (var entry in spriteList)
            if (entry.sprite == _front.sprite) return entry.name;
        return null;
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }

    // ══════════════════════════════════════════
    //  註冊 / 反註冊
    // ══════════════════════════════════════════

    private void Register()
    {
        if (string.IsNullOrEmpty(actorID))
        {
            Debug.LogWarning($"[CrossfadeSpriteActor] {gameObject.name} 的 actorID 為空，無法註冊。");
            return;
        }

        if (registry.ContainsKey(actorID))
            Debug.LogWarning($"[CrossfadeSpriteActor] 重複的 ID: {actorID}（物件: {gameObject.name}），覆蓋舊的註冊。");

        registry[actorID] = this;
    }

    private void Unregister()
    {
        if (!string.IsNullOrEmpty(actorID) && registry.TryGetValue(actorID, out CrossfadeSpriteActor registered))
        {
            if (registered == this) registry.Remove(actorID);
        }
    }
}
