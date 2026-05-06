using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem; //lua

namespace WoodenMan
{
    public class WoodenManGameManager : MonoBehaviour
    {
        // --- 單例實作 ---
        public static WoodenManGameManager Instance { get; private set; }

        [System.Serializable]
        public class GhostMapping
        {
            [Tooltip("對應 RiskAction 的 inspectionTypeID")]
            public string actionID;
            [Tooltip("鬼怪的 GameObject")]
            public GameObject ghostObject;
            [Tooltip("是否強制啟動")]
            public bool forceStart;
        }

        [Header("對應表設定")]
        [SerializeField] List<GhostMapping> ghostMappings = new();

        private List<GhostController> ghosts = new();
        private bool _hasBeenSetup = false;

        [Header("高潮觸發設定")]
        [Range(0f, 1f)] public float orgasmTriggerProbability = 0.5f;
        [Min(0f)] public float ghostOrgasmCooldown = 2f;

        [Header("遊戲結束設定")]
        public UnityEvent OnGameOver;
        public bool testDontDie = false;

        private float _lastGhostOrgasmTime = -1f;
        private bool _ghostTimerStarted = false;

        public bool HasActiveGhosts => ghosts.Count > 0;

        #region Dialogue System 整合

        void OnEnable()
        {
            // 當物件啟用時，將方法註冊到 Lua 環境中
            // 這樣你在對話節點的 Conditions 就可以直接寫 IsGhostActive()
            Lua.RegisterFunction("IsGhostActive", this, SymbolExtensions.GetMethodInfo(() => IsGhostActive()));
            Lua.RegisterFunction("IsGhostActiveByID", this, SymbolExtensions.GetMethodInfo(() => IsGhostActiveByID(string.Empty)));
        }

        void OnDisable()
        {
            // 當物件停用時，取消註冊以避免錯誤
            Lua.UnregisterFunction("IsGhostActive");
            Lua.UnregisterFunction("IsGhostActiveByID");
        }

        /// <summary>
        /// 提供給 Lua 或外部檢查場上是否有鬼
        /// </summary>
        public bool IsGhostActive()
        {
            return HasActiveGhosts;
        }

        /// <summary>
        /// 檢查指定 ID 的鬼怪是否正在啟動中（物件 active 且在監測清單內）
        /// </summary>
        public bool IsGhostActiveByID(string actionID)
        {
            var mapping = ghostMappings.Find(m => m.actionID == actionID);
            if (mapping == null || mapping.ghostObject == null) return false;
            if (!mapping.ghostObject.activeInHierarchy) return false;

            var ctrl = mapping.ghostObject.GetComponent<GhostController>();
            return ctrl != null && ghosts.Contains(ctrl);
        }

        #endregion

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (ghosts.Count == 0)
                ghosts.AddRange(FindObjectsByType<GhostController>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            ghosts.RemoveAll(g => g == null);

            foreach (var g in ghosts)
                g.OnAwarenessMax += _ => HandleGameOver();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (!_hasBeenSetup)
            {
                SetupGhosts(null);
            }
        }

        // ==================== 動態增減鬼怪 API ====================

        public void ActivateGhostByID(string actionID)
        {
            var mapping = ghostMappings.Find(m => m.actionID == actionID);
            if (mapping != null && mapping.ghostObject != null)
            {
                mapping.ghostObject.SetActive(true);
                GhostController ctrl = mapping.ghostObject.GetComponent<GhostController>();
                if (ctrl != null && !ghosts.Contains(ctrl))
                {
                    ghosts.Add(ctrl);
                    ctrl.OnAwarenessMax += _ => HandleGameOver();
                    if (_ghostTimerStarted) ctrl.StartGhostBehavior();
                    Debug.Log($"<color=green>[Activate] 已手動加入鬼怪: {actionID}</color>");
                }
            }
        }

        public void DeactivateGhostByID(string actionID)
        {
            var mapping = ghostMappings.Find(m => m.actionID == actionID);
            if (mapping != null && mapping.ghostObject != null)
            {
                GhostController ctrl = mapping.ghostObject.GetComponent<GhostController>();
                if (ctrl != null)
                {
                    ctrl.StopGhostBehavior();
                    ctrl.OnAwarenessMax -= _ => HandleGameOver();
                    ghosts.Remove(ctrl);
                }
                mapping.ghostObject.SetActive(false);
                Debug.Log($"<color=orange>[Deactivate] 已手動移除鬼怪: {actionID}</color>");
            }
        }

        // ==================== 核心邏輯 API ====================

        public void SetupGhosts(List<string> activeActionIDs)
        {
            _hasBeenSetup = true;
            ghosts.Clear();
            foreach (var mapping in ghostMappings)
            {
                if (mapping.ghostObject == null) continue;
                bool isMatch = activeActionIDs != null && activeActionIDs.Contains(mapping.actionID);
                bool shouldActivate = mapping.forceStart || isMatch;

                if (shouldActivate)
                {
                    mapping.ghostObject.SetActive(true);
                    GhostController ctrl = mapping.ghostObject.GetComponent<GhostController>();
                    if (ctrl != null)
                    {
                        ghosts.Add(ctrl);
                        ctrl.OnAwarenessMax += _ => HandleGameOver();
                        ctrl.StartGhostBehavior();
                    }
                }
                else mapping.ghostObject.SetActive(false);
            }
        }

        public void StartGhostTimer()
        {
            if (_ghostTimerStarted) return;
            _ghostTimerStarted = true;
            ghosts = ghosts.Where(g => g != null).ToList();
            if (ghosts.Count == 0) ghosts.AddRange(FindObjectsByType<GhostController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var ghost in ghosts) ghost?.StartGhostBehavior();
        }

        public void StopGhostTimer()
        {
            if (!_ghostTimerStarted) return;
            _ghostTimerStarted = false;
            foreach (var ghost in ghosts.Where(g => g != null)) ghost.StopGhostBehavior();
        }

        public void TriggerGhostCheck(float probability = -1f)
        {
            float checkProbability = probability >= 0f ? probability : orgasmTriggerProbability;
            if (IsGhostOrgasmInCooldown()) return;

            _lastGhostOrgasmTime = Time.time;
            ghosts = ghosts.Where(g => g != null).ToList();
            foreach (var ghost in ghosts)
                if (Random.value < checkProbability) ghost.TriggerOrgasmCheck();
        }

        public void AddDangerPoints(int amount)
        {
            ghosts = ghosts.Where(g => g != null).ToList();
            foreach (var g in ghosts) if (g != null && g.IsLooking) g.AddDangerPoint(amount);
        }

        // ==================== 狀態檢查 API ====================

        public float GetRemainingGhostOrgasmCooldown()
        {
            if (_lastGhostOrgasmTime < 0) return 0f;
            return Mathf.Max(0f, ghostOrgasmCooldown - (Time.time - _lastGhostOrgasmTime));
        }

        public bool IsGhostOrgasmInCooldown()
        {
            return GetRemainingGhostOrgasmCooldown() > 0f;
        }

        public bool IsGhostTimerStarted() => _ghostTimerStarted;

        public bool IsAnyGhostLooking()
        {
            ghosts = ghosts.Where(g => g != null).ToList();
            return ghosts.Any(g => g.IsLooking);
        }

        // ==================== 遊戲結束與重設 ====================

        void HandleGameOver()
        {
            if (testDontDie) return;
            _ghostTimerStarted = false;
            foreach (var g in ghosts.Where(g => g != null)) g.FreezeAtGameOver();
            OnGameOver?.Invoke();
        }

        [ContextMenu("手動重設所有鬼的狀態")]
        public void ResetAllGhosts()
        {
            ghosts = ghosts.Where(g => g != null).ToList();
            foreach (var ghost in ghosts) ghost?.ResetGhostState();
            _lastGhostOrgasmTime = -1f;
            _ghostTimerStarted = true;
        }

        // ==================== 手動測試工具 ====================

        [ContextMenu("手動啟動鬼怪計時器")]
        private void ManualStartGhostTimer() => StartGhostTimer();

        [ContextMenu("手動停止鬼怪計時器")]
        private void ManualStopGhostTimer() => StopGhostTimer();

        [ContextMenu("手動觸發鬼檢查")]
        private void ManualTriggerGhostCheck() => TriggerGhostCheck();
    }
}