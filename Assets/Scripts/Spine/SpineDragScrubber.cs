using UnityEngine;
using Spine.Unity;
using Spine;

public class SpineDragScrubber : MonoBehaviour
{
    public enum AxisDirection { Up, Down, Left, Right }

    [Header("Spine 參數")]
    public SkeletonAnimation skeleton;
    [SpineAnimation(dataField: "skeleton")]
    public string animationName;
    public int trackIndex = 0;

    [Header("拖曳設定")]
    public AxisDirection axis = AxisDirection.Up;
    [Tooltip("拖曳到這個距離時，剛好播完整支動畫")]
    public float maxDistance = 300f;
    [Tooltip("忽略微小抖動的範圍")]
    public float deadZone = 4f;
    [Tooltip("是否限制只能朝正向前進（關閉後可逆向回捲）")]
    public bool forwardOnly = false;

    [Header("互動區域(必須在此 Collider 內按下才會開始)")]
    [Tooltip("3D 物理用的 Collider；若留空則嘗試抓取同物件上的 Collider")]
    public Collider targetCollider;
    [Tooltip("2D 物理用的 Collider2D；若留空則嘗試抓取同物件上的 Collider2D")]
    public Collider2D targetCollider2D;

    [Header("輸入設定")]
    [Tooltip("是否啟用滑鼠左鍵自動開始/結束。啟用時也一樣必須在 Collider 內按下才會開始。")]
    public bool useMouseInput = true;

    // 內部狀態
    Vector2 dragStartPos;
    bool dragging = false;
    Spine.TrackEntry entry;
    float clipDuration = 0f;
    float baseProgress01 = 0f; // 按下當下的基準進度（0~1）

    void Reset()
    {
        skeleton = GetComponentInChildren<SkeletonAnimation>();
        // 預設抓同物件上的 collider
        if (targetCollider == null) targetCollider = GetComponent<Collider>();
        if (targetCollider2D == null) targetCollider2D = GetComponent<Collider2D>();
        // 新需求預設：必須在區域內按下
        useMouseInput = true;
    }

    void Awake()
    {
        if (skeleton == null) skeleton = GetComponentInChildren<SkeletonAnimation>();
        if (targetCollider == null) targetCollider = GetComponent<Collider>();
        if (targetCollider2D == null) targetCollider2D = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (useMouseInput)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 只有在 collider 內按下才允許開始
                if (IsMouseOverTargetCollider()) BeginDrag();
            }
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        if (!dragging || entry == null) return;

        Vector2 current = Input.mousePosition;
        Vector2 axisDir = GetAxisVector();     // 取得單位方向
        float signedDelta = Vector2.Dot(current - dragStartPos, axisDir);

        // Dead zone 處理
        if (Mathf.Abs(signedDelta) < deadZone) signedDelta = 0f;

        // 轉成進度（-max..+max 對應 -1..+1，再加到 baseProgress 上）
        float delta01 = Mathf.Clamp(signedDelta / Mathf.Max(0.0001f, maxDistance), -1f, 1f);

        float targetProgress = baseProgress01 + delta01;
        if (forwardOnly) targetProgress = Mathf.Clamp01(Mathf.Max(targetProgress, baseProgress01)); // 只前進
        else targetProgress = Mathf.Clamp01(targetProgress);                                        // 可前後

        // 設定 TrackTime（TimeScale=0 由我們手動推進）
        entry.TrackTime = targetProgress * clipDuration;
        // 避免自動混合造成卡頓
        entry.MixTime = 0f;
    }

    /// <summary>
    /// 外部若手動呼叫 BeginDrag，也會檢查目前滑鼠是否在 collider 內；不在就不開始。
    /// </summary>
    public void BeginDrag()
    {
        if (!IsMouseOverTargetCollider()) return; // 關鍵：必須在互動區域內才開始
        if (skeleton == null || string.IsNullOrEmpty(animationName)) return;

        dragStartPos = Input.mousePosition;
        dragging = true;

        // 如果當前不是這支動畫，或沒有 entry，就重新設。
        bool needSet = (entry == null) || (entry.Animation == null) || (entry.Animation.Name != animationName);
        if (needSet)
        {
            entry = skeleton.AnimationState.SetAnimation(trackIndex, animationName, false);
            entry.TimeScale = 0f; // 關掉自動播放
            entry.MixDuration = 0f;
        }
        else
        {
            // 已經有同一支動畫，繼續停在現在的時間軸，維持 TimeScale=0 以便手動 scrub
            entry.TimeScale = 0f;
        }

        clipDuration = Mathf.Max(0.0001f, entry.Animation?.Duration ?? 0f);
        baseProgress01 = Mathf.Clamp01(entry.TrackTime / clipDuration);
    }

    public void EndDrag()
    {
        dragging = false;
        // 不自動播放，維持停在目前最後格；若想放開後自動播放到結尾，可改：
        // if (entry != null) entry.TimeScale = 1f;
    }

    bool IsMouseOverTargetCollider()
    {
        var cam = Camera.main;
        if (cam == null) return false;

        // 3D 檢查
        if (targetCollider != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (targetCollider.Raycast(ray, out var _, 1000f)) return true;
        }

        // 2D 檢查
        if (targetCollider2D != null)
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            if (targetCollider2D.OverlapPoint(wp)) return true;
        }

        // 若兩者都沒指定，嘗試抓同物件上的 collider
        var c3 = targetCollider ?? GetComponent<Collider>();
        if (c3 != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (c3.Raycast(ray, out var _, 1000f)) return true;
        }
        var c2 = targetCollider2D ?? GetComponent<Collider2D>();
        if (c2 != null)
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            if (c2.OverlapPoint(wp)) return true;
        }

        return false;
    }

    Vector2 GetAxisVector()
    {
        switch (axis)
        {
            case AxisDirection.Up: return Vector2.up;
            case AxisDirection.Down: return Vector2.down;
            case AxisDirection.Left: return Vector2.left;
            case AxisDirection.Right: return Vector2.right;
        }
        return Vector2.up;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        if (Camera.main == null) return;

        Gizmos.color = Color.cyan;
        Vector3 p0 = (Vector3)dragStartPos;
        Vector3 dir = (Vector3)GetAxisVector();
        Vector3 a = Camera.main.ScreenToWorldPoint(new Vector3(p0.x, p0.y, 10f));
        Vector3 b = Camera.main.ScreenToWorldPoint(new Vector3(p0.x + dir.x * maxDistance, p0.y + dir.y * maxDistance, 10f));
        Gizmos.DrawLine(a, b);
    }
#endif
}
