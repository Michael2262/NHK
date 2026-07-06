using UnityEngine;
using UnityEngine.InputSystem;
using Spine;
using Spine.Unity;

/// <summary>
/// 讓指定的 Spine 骨頭跟隨滑鼠移動。
///
/// 兩種跟隨模式：
///   - AbsolutePosition（絕對位置）：骨頭平滑貼到滑鼠所在位置。
///   - RelativeDelta（相對位移）：骨頭以原位為基準，依「滑鼠的移動量」偏移，
///     偏移量 = 滑鼠移動距離 ÷ mouseDistanceDivisor。適合假 3D 控制骨（如「頭正面」）。
///
/// 呼叫 StartFollow() 開始跟隨、StopFollow() 停止並平滑回彈到原位。
/// 搭配 SensorLogicTrigger 使用：PRESS 綁 StartFollow、RELEASE 綁 StopFollow。
///
/// 寫入時機掛在 SkeletonAnimation.UpdateLocal（動畫套用後、世界座標計算前）。
/// 注意：動畫「沒有 key」的骨頭不會被動畫覆寫，其值會保留我們上幀的寫入，
/// 因此內部用 _basePos 追蹤真正的原位，避免偏移逐幀疊加（解體 bug 的成因）。
///
/// ⚠️ 若目標骨頭在 Spine 裡被 IK constraint 控制（如手臂 IK），
/// 請改指定 IK 的 target 骨頭，直接寫被 IK 控制的骨頭會互相打架。
/// </summary>
public class BoneMouseFollower : MonoBehaviour
{
    public enum FollowMode
    {
        [InspectorName("絕對位置（貼到滑鼠）")]
        AbsolutePosition,

        [InspectorName("相對位移（跟著滑鼠移動量）")]
        RelativeDelta,
    }

    [Header("Spine")]
    [Tooltip("目標 SkeletonAnimation。留空則自動抓取同物件組件。")]
    public SkeletonAnimation skeletonAnimation;

    [SpineBone(dataField: "skeletonAnimation")]
    [Tooltip("要被滑鼠帶動的骨頭名稱。")]
    public string boneName;

    [Header("跟隨模式")]
    [Tooltip("絕對位置：骨頭平滑貼到滑鼠位置。相對位移：骨頭以原位為基準，跟著滑鼠的移動量偏移。")]
    [SerializeField] private FollowMode followMode = FollowMode.RelativeDelta;

    [Tooltip("相對位移模式用：滑鼠移動距離 ÷ 此值 = 骨頭移動距離。1 = 等速跟隨；小於 1 骨頭動得比滑鼠多；大於 1 動得比滑鼠少。")]
    [SerializeField] private float mouseDistanceDivisor = 0.8f;

    [Header("跟隨設定")]
    [Tooltip("跟隨滑鼠的平滑速度。數值越大越貼手，越小越黏滯。")]
    [SerializeField] private float followSpeed = 15f;

    [Tooltip("放開後回彈到原位的速度。")]
    [SerializeField] private float returnSpeed = 8f;

    [Header("範圍限制（可選）")]
    [Tooltip("骨頭的可移動範圍（Unity 世界空間）。骨頭目標位置超出此 Collider2D 時會被夾在邊界最近點。留空 = 不限制。")]
    [SerializeField] private Collider2D boundary;

    [Header("攝影機")]
    [Tooltip("換算滑鼠座標用的攝影機。留空則每次自動抓 Camera.main（支援攝影機由其他場景晚點載入）。")]
    [SerializeField] private Camera targetCamera;

    [Header("除錯")]
    [Tooltip("測試用：啟用時自動開始跟隨，不需要外部呼叫 StartFollow。")]
    [SerializeField] private bool testFollowOnEnable = false;

    [Tooltip("在 Console 輸出跟隨狀態與座標換算結果（每秒一次）。")]
    [SerializeField] private bool logDebug = false;

    // ─────────────────────────────────────────────
    // 內部狀態
    // ─────────────────────────────────────────────
    private Bone _bone;
    private bool _following;         // 是否正在跟隨滑鼠
    private bool _overrideActive;    // 是否還在覆寫骨頭（跟隨中或回彈中）
    private Vector2 _currentLocalPos;
    private bool _cameraWarned;
    private float _nextLogTime;

    // 真正的原位追蹤：動畫沒 key 的骨頭不會被動畫覆寫，
    // 讀到的 bone.X/Y 會是我們上幀寫入的值，必須靠這組欄位還原原位
    private Vector2 _basePos;        // 未被我們污染的骨頭原位
    private Vector2 _lastWritten;    // 我們上幀寫入的值
    private bool _hasLastWritten;

    // 相對位移模式：跟隨起點的滑鼠世界座標
    private bool _startCaptured;
    private Vector3 _startMouseWorld;

    // 回彈到與原位差距小於此值（Spine 單位）時，結束覆寫交還給動畫
    private const float ARRIVE_EPSILON_SQR = 0.01f;

    /// <summary>目前是否正在跟隨滑鼠。</summary>
    public bool IsFollowing => _following;

    private void Awake()
    {
        if (!skeletonAnimation) skeletonAnimation = GetComponent<SkeletonAnimation>();
        // 攝影機不在這裡抓：可能由其他場景晚點載入，改在 TryGetMouseWorld 懶抓
    }

    private void OnEnable()
    {
        if (skeletonAnimation == null)
        {
            Debug.LogError($"[BoneMouseFollower] {name} 找不到 SkeletonAnimation，元件停用。", this);
            enabled = false;
            return;
        }

        skeletonAnimation.UpdateLocal += HandleUpdateLocal;
        skeletonAnimation.OnRebuild += HandleRebuild;

        if (testFollowOnEnable) StartFollow();
    }

    private void OnDisable()
    {
        if (skeletonAnimation != null)
        {
            skeletonAnimation.UpdateLocal -= HandleUpdateLocal;
            skeletonAnimation.OnRebuild -= HandleRebuild;
        }

        // 覆寫中被停用：把骨頭放回原位，避免沒 key 的骨頭永久停在偏移處
        if (_overrideActive && _bone != null)
        {
            _bone.X = _basePos.x;
            _bone.Y = _basePos.y;
        }

        _following = false;
        _overrideActive = false;
        _hasLastWritten = false;
    }

    // ─────────────────────────────────────────────
    // 公開 API（掛給 SensorLogicTrigger 的 UnityEvent）
    // ─────────────────────────────────────────────

    /// <summary>開始跟隨滑鼠。</summary>
    public void StartFollow()
    {
        _following = true;
        _startCaptured = false; // 相對位移模式：下一幀重新記錄起點
    }

    /// <summary>停止跟隨，骨頭平滑回彈到原位。</summary>
    public void StopFollow()
    {
        _following = false;
    }

    /// <summary>開關式入口，方便 UnityEvent 傳 bool。</summary>
    public void SetFollow(bool follow)
    {
        if (follow) StartFollow();
        else StopFollow();
    }

    // ─────────────────────────────────────────────
    // 核心：在 UpdateLocal 時機覆寫骨頭 local 位置
    // ─────────────────────────────────────────────

    private void HandleUpdateLocal(ISkeletonAnimation animated)
    {
        if (!_following && !_overrideActive) return;
        if (!TryResolveBone()) return;

        Vector2 boneNow = new Vector2(_bone.X, _bone.Y);
        Vector2 animatedPos;

        // 判斷此刻的骨頭值是「動畫寫入的原位」還是「我們上幀寫入的殘留值」：
        // 動畫有 key 這根骨頭 → 每幀覆寫，boneNow 就是新原位；
        // 動畫沒 key → boneNow 會與我們上幀寫入的值完全相同，原位取 _basePos。
        if (_overrideActive && _hasLastWritten &&
            boneNow.x == _lastWritten.x && boneNow.y == _lastWritten.y)
        {
            animatedPos = _basePos;
        }
        else
        {
            animatedPos = boneNow;
            _basePos = boneNow;
        }

        if (_following)
        {
            // 剛開始跟隨：從原位出發，避免瞬間跳到目標位置
            if (!_overrideActive)
            {
                _overrideActive = true;
                _currentLocalPos = animatedPos;
            }

            if (TryGetMouseWorld(out Vector3 mouseWorld))
            {
                Vector2 target;

                if (followMode == FollowMode.AbsolutePosition)
                {
                    target = WorldToParentLocal(mouseWorld);
                }
                else // RelativeDelta
                {
                    if (!_startCaptured)
                    {
                        _startCaptured = true;
                        _startMouseWorld = mouseWorld;
                    }

                    float scale = 1f / Mathf.Max(mouseDistanceDivisor, 0.01f);
                    Vector2 offset =
                        (WorldToParentLocal(mouseWorld) - WorldToParentLocal(_startMouseWorld)) * scale;

                    target = animatedPos + offset;
                }

                // 範圍限制：夾住骨頭目標位置（不是滑鼠位置）
                target = ClampToBoundary(target);

                float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
                _currentLocalPos = Vector2.Lerp(_currentLocalPos, target, t);

                if (logDebug && Time.time >= _nextLogTime)
                {
                    _nextLogTime = Time.time + 1f;
                    Debug.Log($"[BoneMouseFollower] {name} 跟隨中({followMode}) 骨頭='{boneName}' 原位={animatedPos} 目標(父層local)={target} 目前={_currentLocalPos}", this);
                }
            }
        }
        else
        {
            // 回彈：朝原位收斂，夠近就交還給動畫
            float t = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);
            _currentLocalPos = Vector2.Lerp(_currentLocalPos, animatedPos, t);

            if ((_currentLocalPos - animatedPos).sqrMagnitude < ARRIVE_EPSILON_SQR)
            {
                // 沒 key 的骨頭動畫不會幫忙歸位，交還前先寫回原位
                _bone.X = animatedPos.x;
                _bone.Y = animatedPos.y;
                _overrideActive = false;
                _hasLastWritten = false;
                return;
            }
        }

        _bone.X = _currentLocalPos.x;
        _bone.Y = _currentLocalPos.y;
        _lastWritten = _currentLocalPos;
        _hasLastWritten = true;
    }

    // ─────────────────────────────────────────────
    // 座標換算
    // ─────────────────────────────────────────────

    /// <summary>
    /// 滑鼠螢幕座標 → Unity 世界座標。
    /// </summary>
    private bool TryGetMouseWorld(out Vector3 world)
    {
        world = default;

        // 懶抓攝影機：可能由其他場景晚點載入，抓到為止
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                if (!_cameraWarned)
                {
                    _cameraWarned = true;
                    Debug.LogError($"[BoneMouseFollower] {name} 找不到 Camera.main（targetCamera 也未指定）。請確認攝影機有 MainCamera tag。", this);
                }
                return false;
            }
            _cameraWarned = false;
        }

        // 新版 Input System：Pointer 涵蓋滑鼠與觸控
        Pointer pointer = Pointer.current;
        if (pointer == null) return false;

        Vector3 screen = pointer.position.ReadValue();
        screen.z = targetCamera.WorldToScreenPoint(skeletonAnimation.transform.position).z;
        world = targetCamera.ScreenToWorldPoint(screen);
        return true;
    }

    /// <summary>
    /// Unity 世界座標 → Skeleton 空間 → 骨頭父層 local。
    /// （Spine 的「world」指 skeleton 空間）
    /// </summary>
    private Vector2 WorldToParentLocal(Vector3 world)
    {
        Vector3 skeletonSpace = skeletonAnimation.transform.InverseTransformPoint(world);

        if (_bone.Parent != null)
        {
            _bone.Parent.WorldToLocal(skeletonSpace.x, skeletonSpace.y, out float lx, out float ly);
            return new Vector2(lx, ly);
        }

        return new Vector2(skeletonSpace.x, skeletonSpace.y);
    }

    /// <summary>
    /// 把「骨頭目標位置」（父層 local）夾在 boundary Collider2D（Unity 世界空間）內。
    /// 使用父骨頭上一幀的世界變換換算，一幀的誤差對範圍限制可忽略。
    /// </summary>
    private Vector2 ClampToBoundary(Vector2 targetLocal)
    {
        if (boundary == null) return targetLocal;

        // 父層 local → skeleton 空間 → Unity 世界
        float sx, sy;
        if (_bone.Parent != null)
            _bone.Parent.LocalToWorld(targetLocal.x, targetLocal.y, out sx, out sy);
        else
        {
            sx = targetLocal.x;
            sy = targetLocal.y;
        }

        Vector3 world = skeletonAnimation.transform.TransformPoint(new Vector3(sx, sy, 0f));

        if (boundary.OverlapPoint(world)) return targetLocal;

        // 超出範圍：取邊界最近點換算回父層 local
        Vector2 clamped = boundary.ClosestPoint(world);
        return WorldToParentLocal(new Vector3(clamped.x, clamped.y, world.z));
    }

    private bool TryResolveBone()
    {
        if (_bone != null) return true;

        if (string.IsNullOrEmpty(boneName) || skeletonAnimation.Skeleton == null) return false;

        _bone = skeletonAnimation.Skeleton.FindBone(boneName);
        if (_bone == null)
        {
            Debug.LogError($"[BoneMouseFollower] {name} 在 Skeleton 中找不到骨頭 '{boneName}'，元件停用。", this);
            enabled = false;
            return false;
        }

        return true;
    }

    /// <summary>Skeleton 重建（換 Skin / 重載）後骨頭引用會失效，清掉下次重抓。</summary>
    private void HandleRebuild(SkeletonRenderer renderer)
    {
        _bone = null;
        _overrideActive = false;
        _hasLastWritten = false;
    }
}
