using UnityEngine;
using Spine.Unity;
using Spine;

public class SpineCyclicDragScrubber : MonoBehaviour
{
    // 用於獨立設定每個動畫的參數
    [System.Serializable]
    public class AnimationConfig
    {
        [SpineAnimation(dataField: "skeleton")]
        public string animationName;
        public int trackIndex = 0;
        public AxisDirection axis = AxisDirection.Up;
        [Tooltip("拖曳到這個距離時，剛好播完整支動畫")]
        public float maxDistance = 300f;
        [Tooltip("是否限制只能朝正向前進（關閉後可逆向回捲）")]
        public bool forwardOnly = false;

        // --- 內部狀態，不需在 Inspector 中設定 ---
        [System.NonSerialized]
        public TrackEntry entry;
        [System.NonSerialized]
        public float clipDuration;
    }

    public enum AxisDirection { Up, Down, Left, Right }

    [Header("共用 Spine 元件")]
    public SkeletonAnimation skeleton;

    [Header("動畫循環設定")]
    [Tooltip("設定兩個或多個依序播放的動畫")]
    public AnimationConfig[] animationConfigs = new AnimationConfig[2];

    [Header("共用拖曳設定")]
    [Tooltip("忽略微小抖動的範圍")]
    public float deadZone = 4f;

    [Header("互動區域(必須在此 Collider 內按下才會開始)")]
    public Collider targetCollider;
    public Collider2D targetCollider2D;

    [Header("輸入設定")]
    [Tooltip("是否啟用滑鼠左鍵自動開始/結束。")]
    public bool useMouseInput = true;

    // 內部狀態
    private bool dragging = false;
    private int currentConfigIndex = 0;
    private Vector2 segmentStartPos; // 當前動畫片段的拖曳起始點

    void Reset()
    {
        skeleton = GetComponentInChildren<SkeletonAnimation>();
        if (targetCollider == null) targetCollider = GetComponent<Collider>();
        if (targetCollider2D == null) targetCollider2D = GetComponent<Collider2D>();
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
                if (IsMouseOverTargetCollider()) BeginDrag();
            }
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        if (!dragging || animationConfigs.Length == 0) return;

        // --- 主要拖曳邏輯 ---
        var currentConfig = animationConfigs[currentConfigIndex];
        Vector2 currentMousePos = Input.mousePosition;
        Vector2 axisDir = GetAxisVector(currentConfig.axis);
        float signedDelta = Vector2.Dot(currentMousePos - segmentStartPos, axisDir);

        // 檢查是否達到切換到下一個動畫的閾值
        if (signedDelta >= currentConfig.maxDistance && currentConfig.maxDistance > 0)
        {
            // 確保前一個動畫停在結尾
            if (currentConfig.entry != null) currentConfig.entry.TrackTime = currentConfig.clipDuration;

            // 計算切換點 (Switch Point)，作為下一個動畫拖曳的起始點
            Vector2 switchPoint = segmentStartPos + axisDir * currentConfig.maxDistance;

            // 切換到下一個動畫設定
            currentConfigIndex = (currentConfigIndex + 1) % animationConfigs.Length;
            var nextConfig = animationConfigs[currentConfigIndex];

            // 更新狀態以反映新的動畫片段
            segmentStartPos = switchPoint;
            SetActiveAnimation(currentConfigIndex);

            // 用新的狀態重新計算當前幀的 delta
            axisDir = GetAxisVector(nextConfig.axis);
            signedDelta = Vector2.Dot(currentMousePos - segmentStartPos, axisDir);
            currentConfig = nextConfig;
        }

        // Dead zone 處理
        if (Mathf.Abs(signedDelta) < deadZone) signedDelta = 0f;

        // 計算當前動畫的播放進度
        float targetProgress = signedDelta / Mathf.Max(0.0001f, currentConfig.maxDistance);

        if (currentConfig.forwardOnly)
            targetProgress = Mathf.Clamp01(Mathf.Max(targetProgress, 0));
        else
            targetProgress = Mathf.Clamp01(targetProgress);

        // 手動設定動畫時間
        if (currentConfig.entry != null)
        {
            currentConfig.entry.TrackTime = targetProgress * currentConfig.clipDuration;
            currentConfig.entry.MixTime = 0f;
        }
    }

    public void BeginDrag()
    {
        if (!IsMouseOverTargetCollider()) return;
        if (skeleton == null || animationConfigs.Length == 0) return;

        dragging = true;
        currentConfigIndex = 0; // 每次拖曳都從第一個動畫開始
        segmentStartPos = Input.mousePosition;
        SetActiveAnimation(currentConfigIndex);
    }

    public void EndDrag()
    {
        dragging = false;
    }

    // 設定並啟用指定索引的動畫
    void SetActiveAnimation(int index)
    {
        if (index < 0 || index >= animationConfigs.Length) return;

        var config = animationConfigs[index];
        if (string.IsNullOrEmpty(config.animationName)) return;

        config.entry = skeleton.AnimationState.SetAnimation(config.trackIndex, config.animationName, false);
        config.entry.TimeScale = 0f; // 關掉自動播放，改為手動控制
        config.entry.MixDuration = 0f; // 避免混合造成卡頓
        config.clipDuration = Mathf.Max(0.0001f, config.entry.Animation?.Duration ?? 0f);
    }

    bool IsMouseOverTargetCollider()
    {
        var cam = Camera.main;
        if (cam == null) return false;

        // 3D
        if (targetCollider != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (targetCollider.Raycast(ray, out var _, float.MaxValue)) return true;
        }

        // 2D
        if (targetCollider2D != null)
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            if (targetCollider2D.OverlapPoint(wp)) return true;
        }

        return false;
    }

    Vector2 GetAxisVector(AxisDirection axis)
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
}