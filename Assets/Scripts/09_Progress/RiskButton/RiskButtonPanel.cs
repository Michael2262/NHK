using HutongGames.PlayMaker;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 風險門檻按鈕面板（獨立系統）
///
/// 每個按鈕有基礎成功機率 + PlayMaker FSM 傳入的修正值 Y。
/// 點擊時擲骰判定成功/失敗，分別觸發不同 UnityEvent。
///
/// 分頁邏輯：
///   一頁最多 X 個按鈕（Inspector 設定）
///   超出時最後一個位置顯示「其他操作」，點擊循環翻頁
///   不足 X 個時不顯示「其他操作」
///
/// 顯示條件：
///   依多個 ProgressFlagDefinition + All/Any 邏輯決定是否顯示
///   ShowGray = true 時，Flag 不成立仍顯示但壓灰不可點
/// </summary>
public class RiskButtonPanel : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  列舉定義
    // ══════════════════════════════════════════

    public enum FlagLogic
    {
        All, // 所有 Flag 都要為 true
        Any  // 任一 Flag 為 true 即可
    }
    // ══════════════════════════════════════════
    //  資料結構
    // ══════════════════════════════════════════

    [Serializable]
    public class RiskButtonEntry
    {
        [Header("按鈕文字")]
        [Tooltip("Dialogue System Text Table 的 Key")]
        public string localizationKey;

        [Header("成功機率")]
        [Tooltip("基礎成功機率（0~100）")]
        [Range(0, 100)]
        public int baseSuccessRate = 50;

        [Header("成功後行為")]
        [Tooltip("成功後是否可再次觸發（false = 重開面板時不顯示此按鈕）")]
        public bool canRepeat = true;

        [Tooltip("成功後重開面板的等待秒數（-1 = 使用全域設定）")]
        public float overrideDelay = -1f;

        [Header("顯示條件")]
        [Tooltip("需要檢查的 Flag 列表，留空 = 永遠顯示")]
        public List<ProgressFlagDefinition> showFlags = new List<ProgressFlagDefinition>();

        [Tooltip("All = 全部為 true 才顯示；Any = 任一為 true 就顯示")]
        public FlagLogic flagLogic = FlagLogic.All;

        [Tooltip("Flag 不成立時：true = 壓灰不可點，false = 完全隱藏")]
        public bool showGray = false;

        [Header("結果事件")]
        public UnityEvent onSuccess;
        public UnityEvent onFailure;
    }

    // ══════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════

    [Header("按鈕資料")]
    [SerializeField] private List<RiskButtonEntry> entries = new List<RiskButtonEntry>();

    [Header("分頁設定")]
    [Tooltip("每頁最多顯示幾個按鈕（不含「其他操作」）")]
    [SerializeField, Min(1)] private int maxPerPage = 5;

    [Header("PlayMaker 機率修正")]
    [Tooltip("PlayMaker FSM 物件（留空時修正值 Y = 0）")]
    [SerializeField] private PlayMakerFSM targetFSM;

    [Tooltip("FSM 上的變數名（支援 Int 或 Float）")]
    [SerializeField] private string fsmVariableName;

    [Header("UI 引用")]
    [Tooltip("面板根物件")]
    [SerializeField] private GameObject panel;

    [Tooltip("按鈕生成區域（建議搭配 VerticalLayoutGroup）")]
    [SerializeField] private Transform contentParent;

    [Tooltip("關閉按鈕")]
    [SerializeField] private Button closeButton;

    [Header("Prefab")]
    [Tooltip("按鈕 Prefab（需有 Button + CanvasGroup + TextMeshProUGUI）")]
    [SerializeField] private GameObject buttonPrefab;

    [Header("「其他操作」文字")]
    [Tooltip("Text Table Key（留空則直接顯示「其他操作」）")]
    [SerializeField] private string moreActionLocKey = "";

    [Header("壓灰設定")]
    [SerializeField, Range(0f, 1f)] private float grayAlpha = 0.4f;

    [Header("成功後重開設定")]
    [Tooltip("成功後重新開啟面板的預設等待秒數")]
    [SerializeField] private float defaultReopenDelay = 2f;

    // ══════════════════════════════════════════
    //  內部狀態
    // ══════════════════════════════════════════

    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();
    private List<int> _visibleIndices = new List<int>(); // 通過條件的 entry 索引
    private int _currentPage = 0;

    // 追蹤已使用且 canRepeat=false 的 entry 索引
    private readonly HashSet<int> _usedNonRepeatIndices = new HashSet<int>();

    // 延遲重開的 Coroutine
    private Coroutine _reopenCoroutine;

    // ══════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
    }

    // ══════════════════════════════════════════
    //  公開 API
    // ══════════════════════════════════════════

    /// <summary>
    /// 開啟面板
    /// </summary>
    public void Show()
    {
        _currentPage = 0;
        _usedNonRepeatIndices.Clear();

        ShowInternal();
    }

    /// <summary>
    /// 內部開啟（成功後延遲重開用，不重置已使用記錄）
    /// </summary>
    private void ShowInternal()
    {
        _currentPage = 0;

        BuildVisibleIndices();

        // 如果重開後沒有任何可見按鈕了，就不開了
        if (_visibleIndices.Count == 0) return;

        BuildPage();

        if (panel != null) panel.SetActive(true);
    }

    /// <summary>
    /// 關閉面板
    /// </summary>
    public void Close()
    {
        if (_reopenCoroutine != null)
        {
            StopCoroutine(_reopenCoroutine);
            _reopenCoroutine = null;
        }

        ClearSpawnedButtons();

        if (panel != null) panel.SetActive(false);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    // ══════════════════════════════════════════
    //  可見性篩選
    // ══════════════════════════════════════════

    /// <summary>
    /// 建立可見按鈕的索引列表（含壓灰的）
    /// </summary>
    private void BuildVisibleIndices()
    {
        _visibleIndices.Clear();

        if (GameStatusService.Instance == null) return;
        var progressFlags = GameStatusService.Instance.ProgressFlags;

        for (int i = 0; i < entries.Count; i++)
        {
            if (_usedNonRepeatIndices.Contains(i)) continue;

            var entry = entries[i];
            bool flagMet = EvaluateFlags(entry, progressFlags);

            if (flagMet)
            {
                _visibleIndices.Add(i);
            }
            else if (entry.showGray)
            {
                _visibleIndices.Add(i);
            }
        }
    }

    /// <summary>
    /// 評估 Flag 條件。沒配任何 Flag → true（永遠顯示）
    /// </summary>
    private bool EvaluateFlags(RiskButtonEntry entry, ProgressFlagModel progressFlags)
    {
        if (entry.showFlags == null || entry.showFlags.Count == 0)
            return true;

        var validFlags = entry.showFlags.Where(f => f != null).ToList();
        if (validFlags.Count == 0) return true;

        if (entry.flagLogic == FlagLogic.All)
            return validFlags.All(f => progressFlags.Contains(f.name));
        else
            return validFlags.Any(f => progressFlags.Contains(f.name));
    }

    // ══════════════════════════════════════════
    //  分頁 & 生成
    // ══════════════════════════════════════════

    private void BuildPage()
    {
        ClearSpawnedButtons();

        if (contentParent == null || buttonPrefab == null) return;

        int totalVisible = _visibleIndices.Count;
        bool needsPaging = totalVisible > maxPerPage;

        // 每頁最多 X 個正常按鈕，超出時額外出現「其他操作」
        int slotsForEntries = needsPaging ? maxPerPage : totalVisible;

        int startIndex = 0;
        if (needsPaging)
        {
            int entriesPerPage = maxPerPage;
            int totalPages = Mathf.CeilToInt((float)totalVisible / entriesPerPage);

            // 確保 page 在合理範圍內循環
            _currentPage = _currentPage % totalPages;
            if (_currentPage < 0) _currentPage += totalPages;

            startIndex = _currentPage * entriesPerPage;
            slotsForEntries = Mathf.Min(entriesPerPage, totalVisible - startIndex);
        }

        // 取得 PlayMaker 修正值
        float bonusY = GetFSMBonus();

        // 生成本頁的按鈕
        var progressFlags = GameStatusService.Instance?.ProgressFlags;

        for (int i = 0; i < slotsForEntries; i++)
        {
            int visIdx = startIndex + i;
            if (visIdx >= _visibleIndices.Count) break;

            int entryIdx = _visibleIndices[visIdx];
            var entry = entries[entryIdx];

            // 判斷是否壓灰（Flag 不成立但 showGray=true 時）
            bool isGrayed = false;
            if (progressFlags != null)
            {
                isGrayed = !EvaluateFlags(entry, progressFlags);
            }

            // 計算最終機率
            float finalRate = Mathf.Clamp(entry.baseSuccessRate + bonusY, 0f, 100f);

            SpawnEntryButton(entry, finalRate, isGrayed, entryIdx);
        }

        // 生成「其他操作」按鈕（如果需要分頁）
        if (needsPaging)
        {
            SpawnMoreButton();
        }
    }

    // ══════════════════════════════════════════
    //  按鈕生成
    // ══════════════════════════════════════════

    private void SpawnEntryButton(RiskButtonEntry entry, float finalRate, bool isGrayed, int entryIndex)
    {
        var btnObj = Instantiate(buttonPrefab, contentParent);
        btnObj.SetActive(true);
        _spawnedButtons.Add(btnObj);

        // ── 文字：「名稱 機率%」 ──
        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            string localizedName = DialogueManager.GetLocalizedText(entry.localizationKey);
            if (string.IsNullOrEmpty(localizedName))
            {
                localizedName = entry.localizationKey;
            }

            int displayRate = Mathf.RoundToInt(finalRate);
            tmp.text = $"{localizedName} {displayRate}%";
        }

        // ── CanvasGroup ──
        var cg = btnObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnObj.AddComponent<CanvasGroup>();

        // ── 按鈕行為 ──
        var btn = btnObj.GetComponent<Button>();

        if (isGrayed)
        {
            cg.alpha = grayAlpha;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        else
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            if (btn != null)
            {
                var capturedEntry = entry;
                float capturedRate = finalRate;
                int capturedIndex = entryIndex;

                btn.onClick.AddListener(() =>
                {
                    float roll = UnityEngine.Random.Range(0f, 100f);
                    bool success = roll < capturedRate;

                    if (success)
                    {
                        capturedEntry.onSuccess?.Invoke();

                        // 記錄不可重複的按鈕
                        if (!capturedEntry.canRepeat)
                            _usedNonRepeatIndices.Add(capturedIndex);

                        // 關閉面板 → 延遲重開
                        float delay = capturedEntry.overrideDelay >= 0f
                            ? capturedEntry.overrideDelay
                            : defaultReopenDelay;

                        CloseForReopen();
                        _reopenCoroutine = StartCoroutine(ReopenAfterDelay(delay));
                    }
                    else
                    {
                        capturedEntry.onFailure?.Invoke();
                        Close(); // 失敗 → 最終關閉
                    }
                });
            }
        }
    }

    private void SpawnMoreButton()
    {
        var btnObj = Instantiate(buttonPrefab, contentParent);
        btnObj.SetActive(true);
        _spawnedButtons.Add(btnObj);

        // ── 文字 ──
        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            string moreText;
            if (!string.IsNullOrEmpty(moreActionLocKey))
            {
                moreText = DialogueManager.GetLocalizedText(moreActionLocKey);
                if (string.IsNullOrEmpty(moreText)) moreText = moreActionLocKey;
            }
            else
            {
                moreText = "其他操作";
            }
            tmp.text = moreText;
        }

        // ── 點擊 → 翻頁 ──
        var btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                _currentPage++;
                BuildPage(); // 重建本頁（會自動循環）
            });
        }

        // 確保正常顯示
        var cg = btnObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    // ══════════════════════════════════════════
    //  成功後延遲重開
    // ══════════════════════════════════════════

    /// <summary>
    /// 關閉面板但不觸發 onCloseCallback（因為之後還會重開）
    /// </summary>
    private void CloseForReopen()
    {
        ClearSpawnedButtons();
        if (panel != null) panel.SetActive(false);
    }

    private IEnumerator ReopenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _reopenCoroutine = null;
        ShowInternal();
    }

    // ══════════════════════════════════════════
    //  PlayMaker 變數讀取
    // ══════════════════════════════════════════

    private float GetFSMBonus()
    {
        if (targetFSM == null || string.IsNullOrEmpty(fsmVariableName))
        {
            if (targetFSM == null && !string.IsNullOrEmpty(fsmVariableName))
                Debug.Log("[RiskButtonPanel] 未指定 PlayMaker FSM，修正值 Y = 0");
            else if (targetFSM != null && string.IsNullOrEmpty(fsmVariableName))
                Debug.Log("[RiskButtonPanel] 未指定 FSM 變數名，修正值 Y = 0");

            return 0f;
        }

        // 嘗試讀取 Int
        var intVar = targetFSM.FsmVariables.GetFsmInt(fsmVariableName);
        if (intVar != null && intVar.Value != 0)
        {
            return intVar.Value;
        }

        // 嘗試讀取 Float
        var floatVar = targetFSM.FsmVariables.GetFsmFloat(fsmVariableName);
        if (floatVar != null)
        {
            return floatVar.Value;
        }

        Debug.LogWarning($"[RiskButtonPanel] FSM 變數 '{fsmVariableName}' 找不到或為 0，修正值 Y = 0");
        return 0f;
    }

    // ══════════════════════════════════════════
    //  工具方法
    // ══════════════════════════════════════════

    private void ClearSpawnedButtons()
    {
        foreach (var obj in _spawnedButtons)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedButtons.Clear();
    }
}