#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 【Editor Window】女主角日程鍊編輯器 (v5)
/// 
/// v5 更新:
/// - Play Mode 追蹤:顯示當前時段、當日鍊、實際位置 vs 排程位置比對
/// - 活動下拉過濾:只顯示符合當前 heroineID 前綴 (嚴格模式)
/// </summary>
public class HeroineScheduleEditorWindow : EditorWindow
{
    // ==========================================================
    // 開啟入口
    // ==========================================================
    [MenuItem("Tools/Heroine Schedule Editor")]
    public static void Open()
    {
        var window = GetWindow<HeroineScheduleEditorWindow>();
        window.titleContent = new GUIContent("Heroine Schedule");
        window.minSize = new Vector2(900, 560);
        window.Show();
    }

    // ==========================================================
    // 常數:排版
    // ==========================================================
    private const float CELL_WIDTH = 72f;
    private const float CELL_HEIGHT = 44f;
    private const float TIME_COL_WIDTH = 84f;      // 左側資訊欄寬度 (統一)
    private const float PHASE_GAP = 4f;            // Phase 之間的間距
    private const float ROW_PADDING = 4f;          // 列的上下內距
    private const float COL_PADDING_LEFT = 4f;     // 直欄左內距
    private const float COL_PADDING_RIGHT = 4f;    // 直欄右內距

    // ==========================================================
    // 資料狀態
    // ==========================================================

    private List<HeroineScheduleConfig> _allConfigs = new List<HeroineScheduleConfig>();
    private HashSet<string> _visibleHeroineIDs = new HashSet<string>();

    private TimeConfig _timeConfig;
    private List<string> _phaseNames = new List<string>();
    private List<int> _slotsPerPhase = new List<int>();

    private LocationDatabase _locationDB;
    private List<string> _activitySuggestions = new List<string>();

    private BlockEditTarget _editTarget;

    private Vector2 _leftScroll;
    private Vector2 _mainScroll;
    private Vector2 _editPanelScroll;

    private bool _leftPanelCollapsed = false;
    private bool _showFallback = true;
    /// <summary>空的 Fallback dayType 列也要顯示(方便直接建立)</summary>
    private bool _showEmptyFallbackRows = false;

    // ==========================================================
    // Play Mode 追蹤狀態
    // ==========================================================
    /// <summary>Play Mode 時是否啟用追蹤顯示</summary>
    private bool _playModeTrackingEnabled = true;
    /// <summary>是否已經訂閱 Play Mode 的事件</summary>
    private bool _runtimeEventsSubscribed = false;

    // ==========================================================
    // 【NEW】剪貼簿
    // ==========================================================
    /// <summary>複製的「整格」內容(一個 block 的所有 scenarios 的深拷貝)</summary>
    private static List<ScheduleScenario> _blockClipboard;
    /// <summary>剪貼簿來源描述(給 UI 顯示)</summary>
    private static string _blockClipboardSource;
    /// <summary>複製的單一情境</summary>
    private static ScheduleScenario _scenarioClipboard;

    // ==========================================================
    // 編輯目標
    // ==========================================================
    private class BlockEditTarget
    {
        public HeroineScheduleConfig config;
        public DailyChain chain;
        public ChainBlock chainBlock;
        public ScheduleBlock scheduleBlock;
        public int phaseIndex;
        public int slotIndex;
        /// <summary>若為 Fallback,記錄這格所屬的 dayType(用於建立 block 時自動指派)</summary>
        public ScheduleDayType fallbackDayType = ScheduleDayType.EveryDay;
        public bool IsFallback => chain == null;
    }

    // ==========================================================
    // 生命週期
    // ==========================================================
    private void OnEnable()
    {
        RefreshConfigs();
        TryAutoDetectLocationDB();
        TryAutoDetectTimeConfig();

        // Play Mode 追蹤
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        TrySubscribeRuntimeEvents();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        TryUnsubscribeRuntimeEvents();
    }

    private void OnFocus()
    {
        RefreshConfigs();
        RefreshActivitySuggestions();
        if (_timeConfig == null) TryAutoDetectTimeConfig();
        // Play Mode 中切換視窗焦點時,確認事件還訂閱著
        TrySubscribeRuntimeEvents();
    }

    // ==========================================================
    // 掃描
    // ==========================================================
    private void RefreshConfigs()
    {
        _allConfigs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:HeroineScheduleConfig");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<HeroineScheduleConfig>(path);
            if (cfg != null) _allConfigs.Add(cfg);
        }
        _allConfigs = _allConfigs.OrderBy(c => c.heroineID).ToList();

        if (_visibleHeroineIDs.Count == 0)
        {
            foreach (var c in _allConfigs)
                if (!string.IsNullOrEmpty(c.heroineID))
                    _visibleHeroineIDs.Add(c.heroineID);
        }

        RefreshActivitySuggestions();
    }

    private void TryAutoDetectLocationDB()
    {
        if (_locationDB != null) return;
        string[] guids = AssetDatabase.FindAssets("t:LocationDatabase");
        if (guids.Length > 0)
            _locationDB = AssetDatabase.LoadAssetAtPath<LocationDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private void TryAutoDetectTimeConfig()
    {
        if (_timeConfig == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TimeConfig");
            if (guids.Length > 0)
                _timeConfig = AssetDatabase.LoadAssetAtPath<TimeConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        SyncPhaseSlotFromTimeConfig();
    }

    private void SyncPhaseSlotFromTimeConfig()
    {
        _phaseNames.Clear();
        _slotsPerPhase.Clear();

        if (_timeConfig != null && _timeConfig.PhaseNames != null && _timeConfig.SlotsPerPhase != null)
        {
            int phaseCount = Mathf.Min(_timeConfig.PhaseNames.Count, _timeConfig.SlotsPerPhase.Count);
            for (int i = 0; i < phaseCount; i++)
            {
                _phaseNames.Add(_timeConfig.PhaseNames[i]);
                _slotsPerPhase.Add(Mathf.Max(1, _timeConfig.SlotsPerPhase[i]));
            }
        }

        if (_phaseNames.Count == 0)
        {
            _phaseNames.AddRange(new[] { "白天", "黃昏", "晚上", "深夜" });
            _slotsPerPhase.AddRange(new[] { 2, 3, 3, 2 });
        }
    }

    private void RefreshActivitySuggestions()
    {
        var set = new HashSet<string>();
        foreach (var cfg in _allConfigs)
        {
            if (cfg.dailyChains != null)
            {
                foreach (var chain in cfg.dailyChains)
                {
                    if (chain.blocks == null) continue;
                    foreach (var block in chain.blocks)
                    {
                        if (block.possibleScenarios == null) continue;
                        foreach (var sc in block.possibleScenarios)
                            if (!string.IsNullOrEmpty(sc.activityState))
                                set.Add(sc.activityState);
                    }
                }
            }
            if (cfg.schedule != null)
            {
                foreach (var block in cfg.schedule)
                {
                    if (block.possibleScenarios == null) continue;
                    foreach (var sc in block.possibleScenarios)
                        if (!string.IsNullOrEmpty(sc.activityState))
                            set.Add(sc.activityState);
                }
            }
        }
        _activitySuggestions = set.OrderBy(s => s).ToList();
    }

    // ==========================================================
    // Play Mode 追蹤
    // ==========================================================

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            // Play 模式剛啟動時 Service 可能還沒好,延遲訂閱
            EditorApplication.delayCall += TrySubscribeRuntimeEvents;
        }
        else if (change == PlayModeStateChange.ExitingPlayMode)
        {
            TryUnsubscribeRuntimeEvents();
        }
        Repaint();
    }

    private void TrySubscribeRuntimeEvents()
    {
        if (_runtimeEventsSubscribed) return;
        if (!Application.isPlaying) return;
        var service = GameStatusService.Instance;
        if (service == null || service.Scenario == null) return;

        service.Scenario.OnScenarioRecalculated += HandleRuntimeScenarioRecalculated;
        service.Scenario.OnHeroineMoved += HandleRuntimeHeroineMoved;
        _runtimeEventsSubscribed = true;
        Repaint();
    }

    private void TryUnsubscribeRuntimeEvents()
    {
        if (!_runtimeEventsSubscribed) return;
        var service = GameStatusService.Instance;
        if (service != null && service.Scenario != null)
        {
            service.Scenario.OnScenarioRecalculated -= HandleRuntimeScenarioRecalculated;
            service.Scenario.OnHeroineMoved -= HandleRuntimeHeroineMoved;
        }
        _runtimeEventsSubscribed = false;
    }

    private void HandleRuntimeScenarioRecalculated()
    {
        Repaint();
    }

    private void HandleRuntimeHeroineMoved(string hID, string oldLoc, string newLoc, string newAct)
    {
        Repaint();
    }

    /// <summary>是否在 Play Mode 且追蹤開啟</summary>
    private bool IsTrackingActive()
    {
        return _playModeTrackingEnabled && Application.isPlaying && GameStatusService.Instance != null;
    }

    /// <summary>取得當前 Phase/Slot(Play Mode 下)</summary>
    private (int phase, int slot) GetCurrentPhaseSlot()
    {
        if (!IsTrackingActive()) return (-1, -1);
        var t = GameStatusService.Instance.Time;
        if (t == null) return (-1, -1);
        return (t.CurrentPhaseIndex, t.CurrentSlotInPhase);
    }

    /// <summary>取得某女主角實際被放置的地點(Play Mode 下)。null = 不在地圖任何地點</summary>
    private string GetRuntimeActualLocation(string heroineID)
    {
        if (!IsTrackingActive()) return null;
        var scenario = GameStatusService.Instance.Scenario;
        if (scenario == null) return null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var locState = kvp.Value;
            if (locState == null || locState.Heroines == null) continue;
            foreach (var h in locState.Heroines)
            {
                if (string.Equals(h.HeroineID, heroineID, System.StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;
            }
        }
        return null;
    }

    /// <summary>取得某女主角今日被抽中的鍊名(Play Mode 下)</summary>
    private string GetRuntimeTodaysChain(string heroineID)
    {
        if (!IsTrackingActive()) return null;
        return GameStatusService.Instance.Scenario?.GetTodaysChain(heroineID);
    }

    // ==========================================================
    // 排版計算
    // ==========================================================
    private int GetTotalSlots()
    {
        int total = 0;
        foreach (var n in _slotsPerPhase) total += n;
        return total;
    }

    /// <summary>
    /// 計算 (phase, slot) 在「格子區」的 X 位移(相對於格子區的起點)
    /// 用來確保不同列的格子完美對齊
    /// </summary>
    private float GetCellOffsetX(int phase, int slotIndex1Based)
    {
        float x = 0;
        for (int p = 0; p < phase; p++)
        {
            x += _slotsPerPhase[p] * CELL_WIDTH;
            x += PHASE_GAP;
        }
        x += (slotIndex1Based - 1) * CELL_WIDTH;
        return x;
    }

    /// <summary>計算整個格子區的總寬(不含左側資訊欄)</summary>
    private float GetCellsAreaWidth()
    {
        int totalSlots = GetTotalSlots();
        float gapTotal = Mathf.Max(0, _phaseNames.Count - 1) * PHASE_GAP;
        return totalSlots * CELL_WIDTH + gapTotal;
    }

    private float GetColumnWidth()
    {
        return COL_PADDING_LEFT + TIME_COL_WIDTH + GetCellsAreaWidth() + COL_PADDING_RIGHT;
    }

    // ==========================================================
    // 主繪製
    // ==========================================================
    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();

        if (!_leftPanelCollapsed)
            DrawLeftPanel();

        DrawMainArea();

        EditorGUILayout.EndHorizontal();

        if (_editTarget != null)
            DrawBlockEditPanel();
    }

    // ==========================================================
    // 工具列
    // ==========================================================
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(_leftPanelCollapsed ? "▶ 女主角清單" : "◀ 收起清單", EditorStyles.toolbarButton, GUILayout.Width(110)))
            _leftPanelCollapsed = !_leftPanelCollapsed;

        GUILayout.Space(8);

        if (GUILayout.Button("重新掃描", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            RefreshConfigs();
            TryAutoDetectTimeConfig();
        }

        GUILayout.Space(12);

        GUILayout.Label("TimeConfig:", GUILayout.Width(72));
        var newTC = (TimeConfig)EditorGUILayout.ObjectField(_timeConfig, typeof(TimeConfig), false, GUILayout.Width(150));
        if (newTC != _timeConfig)
        {
            _timeConfig = newTC;
            SyncPhaseSlotFromTimeConfig();
        }

        if (_phaseNames.Count > 0)
        {
            var structText = string.Join(" | ",
                _phaseNames.Select((n, i) => $"P{i}:{n}×{_slotsPerPhase[i]}"));
            GUILayout.Label(structText, EditorStyles.miniLabel, GUILayout.Width(300));
        }

        GUILayout.Space(12);
        GUILayout.Label("Location DB:", GUILayout.Width(80));
        var newDB = (LocationDatabase)EditorGUILayout.ObjectField(_locationDB, typeof(LocationDatabase), false, GUILayout.Width(140));
        if (newDB != _locationDB) _locationDB = newDB;

        GUILayout.FlexibleSpace();

        // 剪貼簿狀態顯示
        if (_blockClipboard != null)
        {
            var cbStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(1f, 0.8f, 0.3f) } };
            GUILayout.Label($"📋 已複製格子: {_blockClipboardSource ?? "?"}", cbStyle);
            if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                _blockClipboard = null;
                _blockClipboardSource = null;
            }
            GUILayout.Space(10);
        }

        _showFallback = GUILayout.Toggle(_showFallback, "顯示 Fallback", EditorStyles.toolbarButton, GUILayout.Width(100));

        if (_showFallback)
        {
            _showEmptyFallbackRows = GUILayout.Toggle(_showEmptyFallbackRows, "顯示空列", EditorStyles.toolbarButton, GUILayout.Width(70));
        }

        // Play Mode 追蹤切換
        if (Application.isPlaying)
        {
            GUILayout.Space(8);
            var prevColor = GUI.color;
            GUI.color = _playModeTrackingEnabled ? new Color(0.6f, 1f, 0.6f) : Color.white;
            _playModeTrackingEnabled = GUILayout.Toggle(_playModeTrackingEnabled, "▶ Live", EditorStyles.toolbarButton, GUILayout.Width(60));
            GUI.color = prevColor;

            if (_playModeTrackingEnabled && GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
            {
                var t = GameStatusService.Instance.Time;
                string phaseName = (_phaseNames != null && t.CurrentPhaseIndex < _phaseNames.Count)
                    ? _phaseNames[t.CurrentPhaseIndex] : $"P{t.CurrentPhaseIndex}";
                var liveStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 1f, 0.6f) }
                };
                GUILayout.Label($"Day{t.DayIndex} {phaseName} S{t.CurrentSlotInPhase} ({(t.IsWeekend ? "假日" : "平日")})", liveStyle);
            }
        }

        GUILayout.Label($"女主: {_allConfigs.Count} / 顯示: {_visibleHeroineIDs.Count}", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    // ==========================================================
    // 左側清單
    // ==========================================================
    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(220));

        EditorGUILayout.LabelField("女主角清單", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全選", EditorStyles.miniButton))
        {
            _visibleHeroineIDs.Clear();
            foreach (var c in _allConfigs)
                if (!string.IsNullOrEmpty(c.heroineID)) _visibleHeroineIDs.Add(c.heroineID);
        }
        if (GUILayout.Button("全不選", EditorStyles.miniButton))
            _visibleHeroineIDs.Clear();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

        foreach (var cfg in _allConfigs)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.heroineID)) continue;

            EditorGUILayout.BeginHorizontal();

            bool wasVisible = _visibleHeroineIDs.Contains(cfg.heroineID);
            bool nowVisible = EditorGUILayout.Toggle(wasVisible, GUILayout.Width(18));
            if (nowVisible != wasVisible)
            {
                if (nowVisible) _visibleHeroineIDs.Add(cfg.heroineID);
                else _visibleHeroineIDs.Remove(cfg.heroineID);
            }

            EditorGUILayout.LabelField(cfg.heroineID, GUILayout.Width(100));

            int chainCount = cfg.dailyChains?.Count ?? 0;
            int fallbackCount = cfg.schedule?.Count ?? 0;
            EditorGUILayout.LabelField($"🔗{chainCount}/📋{fallbackCount}", EditorStyles.miniLabel, GUILayout.Width(50));

            if (GUILayout.Button("選", EditorStyles.miniButton, GUILayout.Width(26)))
                EditorGUIUtility.PingObject(cfg);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        var rect = GUILayoutUtility.GetRect(1, float.PositiveInfinity, GUILayout.Width(1));
        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.3f));
    }

    // ==========================================================
    // 主區域
    // ==========================================================
    private void DrawMainArea()
    {
        _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
        EditorGUILayout.BeginHorizontal();

        var visibleConfigs = _allConfigs.Where(c => _visibleHeroineIDs.Contains(c.heroineID)).ToList();

        if (visibleConfigs.Count == 0)
        {
            EditorGUILayout.HelpBox("左側沒有勾選任何女主角。勾選後會在這裡橫向顯示她們的日程。", MessageType.Info);
        }
        else
        {
            foreach (var cfg in visibleConfigs)
            {
                DrawHeroineColumn(cfg);
                GUILayout.Space(8);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    // ==========================================================
    // 單一女主角直欄(重構:使用 Rect 精確對齊)
    // ==========================================================
    private void DrawHeroineColumn(HeroineScheduleConfig cfg)
    {
        float columnWidth = GetColumnWidth();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(columnWidth));

        // 標題列
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"👩 {cfg.heroineID}", EditorStyles.boldLabel, GUILayout.Width(columnWidth - 100));
        if (GUILayout.Button("+ 新鍊", EditorStyles.miniButton, GUILayout.Width(60)))
            AddNewChain(cfg);
        EditorGUILayout.EndHorizontal();

        // Live 資訊區(只在 Play Mode + 追蹤啟用時顯示)
        if (IsTrackingActive())
        {
            DrawLiveInfoPanel(cfg, columnWidth);
        }

        EditorGUILayout.Space(4);

        // 表頭列(Phase 標題)
        DrawPhaseHeaderRow();
        // 表頭列(Slot 標題)
        DrawSlotHeaderRow();

        EditorGUILayout.Space(2);

        // 鍊列
        if (cfg.dailyChains == null || cfg.dailyChains.Count == 0)
        {
            EditorGUILayout.HelpBox("還沒有任何鍊。點右上角「+ 新鍊」開始編輯。", MessageType.None);
        }
        else
        {
            for (int i = 0; i < cfg.dailyChains.Count; i++)
                DrawChainRow(cfg, cfg.dailyChains[i], i);
        }

        // Fallback 三列(按 dayType 區分)
        if (_showFallback)
        {
            EditorGUILayout.Space(6);
            var divider = GUILayoutUtility.GetRect(columnWidth, 2, GUILayout.Width(columnWidth));
            EditorGUI.DrawRect(divider, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            DrawFallbackRowForDayType(cfg, ScheduleDayType.EveryDay);
            DrawFallbackRowForDayType(cfg, ScheduleDayType.WeekdayOnly);
            DrawFallbackRowForDayType(cfg, ScheduleDayType.WeekendOnly);
        }

        EditorGUILayout.EndVertical();
    }

    // ==========================================================
    // Live 資訊區
    // ==========================================================
    private void DrawLiveInfoPanel(HeroineScheduleConfig cfg, float columnWidth)
    {
        var (curP, curS) = GetCurrentPhaseSlot();
        if (curP < 0) return;

        string todaysChain = GetRuntimeTodaysChain(cfg.heroineID);
        string actualLoc = GetRuntimeActualLocation(cfg.heroineID);
        string actualAct = GetRuntimeActualActivity(cfg.heroineID);

        // 解析「排程預期」是什麼
        //  - 如果今日有鍊 → 從鍊找當前格
        //  - 如果今日沒鍊 → 從 fallback(對應當前 dayType)找當前格
        List<ScheduleScenario> expectedScenarios = null;
        string scheduleSourceLabel;

        if (!string.IsNullOrEmpty(todaysChain))
        {
            scheduleSourceLabel = $"鍊「{todaysChain}」";
            var chain = cfg.dailyChains?.Find(c => c.chainName == todaysChain);
            var block = chain?.blocks?.Find(b => b.phaseIndex == curP && b.slotIndex == curS);
            expectedScenarios = block?.possibleScenarios;
        }
        else
        {
            // 今日沒鍊 → 走 fallback
            ScheduleDayType activeDayType = GetCurrentActiveFallbackDayType();
            scheduleSourceLabel = $"Fallback/{GetDayTypeLabels(activeDayType).label}";
            if (cfg.schedule != null)
            {
                // 優先找精準符合 dayType 的,否則找 EveryDay
                var block = cfg.schedule.Find(b =>
                    b.phaseIndex == curP && b.slotIndex == curS && b.dayType == activeDayType);
                if (block == null && activeDayType != ScheduleDayType.EveryDay)
                {
                    block = cfg.schedule.Find(b =>
                        b.phaseIndex == curP && b.slotIndex == curS && b.dayType == ScheduleDayType.EveryDay);
                }
                expectedScenarios = block?.possibleScenarios;
            }
        }

        // 計算比對結果
        LiveMatchStatus status = CalcLiveMatchStatus(expectedScenarios, actualLoc, actualAct);

        // ── 繪製 Live 區塊背景 ──
        float panelHeight = 62f;
        var panelRect = GUILayoutUtility.GetRect(columnWidth, panelHeight, GUILayout.Width(columnWidth), GUILayout.Height(panelHeight));
        EditorGUI.DrawRect(panelRect, new Color(0.2f, 0.35f, 0.25f, 0.35f));
        DrawRectBorder(panelRect, new Color(0.3f, 1f, 0.5f, 0.6f), 1f);

        float x = panelRect.x + 6;
        float y = panelRect.y + 2;
        float w = panelRect.width - 12;

        // 標題行
        var headerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            normal = { textColor = new Color(0.5f, 1f, 0.6f) }
        };
        var curPhaseName = (curP >= 0 && curP < _phaseNames.Count) ? _phaseNames[curP] : $"P{curP}";
        GUI.Label(new Rect(x, y, w, 14),
            $"▶ Live  當前: P{curP}S{curS} ({curPhaseName})   來源: {scheduleSourceLabel}",
            headerStyle);
        y += 15;

        // 排程行
        var lineStyle = new GUIStyle(EditorStyles.miniLabel);
        GUI.Label(new Rect(x, y, w, 14), $"排程: {FormatScenariosForLive(expectedScenarios)}", lineStyle);
        y += 14;

        // 實際行 + 狀態標記
        string actualText = FormatActualForLive(actualLoc, actualAct);
        var statusStyle = new GUIStyle(EditorStyles.miniBoldLabel);
        Color statusColor;
        string statusTag;
        switch (status)
        {
            case LiveMatchStatus.Matched:
                statusColor = new Color(0.3f, 1f, 0.3f); statusTag = "✓ 符合排程"; break;
            case LiveMatchStatus.Mismatched:
                statusColor = new Color(1f, 0.55f, 0.2f); statusTag = "⚠ 與排程不符"; break;
            case LiveMatchStatus.NotOnMap:
                statusColor = new Color(0.7f, 0.7f, 0.7f); statusTag = "👋 不在地圖"; break;
            case LiveMatchStatus.NoSchedule:
                statusColor = new Color(0.8f, 0.8f, 0.5f); statusTag = "— 此時段無排程"; break;
            default:
                statusColor = Color.white; statusTag = ""; break;
        }
        statusStyle.normal.textColor = statusColor;

        GUI.Label(new Rect(x, y, w, 14), $"實際: {actualText}", lineStyle);
        y += 14;

        GUI.Label(new Rect(x, y, w, 14), statusTag, statusStyle);
    }

    private enum LiveMatchStatus { Matched, Mismatched, NotOnMap, NoSchedule }

    private LiveMatchStatus CalcLiveMatchStatus(List<ScheduleScenario> expected, string actualLoc, string actualAct)
    {
        if (string.IsNullOrEmpty(actualLoc))
            return LiveMatchStatus.NotOnMap;
        if (expected == null || expected.Count == 0)
            return LiveMatchStatus.NoSchedule;

        // 只要實際位置符合 expected 其中一個 scenario 的 locationID 就算 matched
        foreach (var sc in expected)
        {
            if (string.Equals(sc.locationID, actualLoc, System.StringComparison.OrdinalIgnoreCase))
                return LiveMatchStatus.Matched;
        }
        return LiveMatchStatus.Mismatched;
    }

    private string FormatScenariosForLive(List<ScheduleScenario> scenarios)
    {
        if (scenarios == null || scenarios.Count == 0)
            return "—";

        if (scenarios.Count == 1)
        {
            var sc = scenarios[0];
            return $"{NullOrDash(sc.locationID)} / {NullOrDash(sc.activityState)}";
        }

        // 多情境:列出所有,以 | 分隔
        var parts = new List<string>();
        foreach (var sc in scenarios)
            parts.Add($"{NullOrDash(sc.locationID)}/{NullOrDash(sc.activityState)}");
        return string.Join("  |  ", parts);
    }

    private string FormatActualForLive(string loc, string act)
    {
        if (string.IsNullOrEmpty(loc)) return "(不在地圖上)";
        return $"{loc} / {NullOrDash(act)}";
    }

    private static string NullOrDash(string s) => string.IsNullOrEmpty(s) ? "—" : s;

    /// <summary>
    /// 取得當前日期對應的 fallback dayType (優先回傳 WeekdayOnly/WeekendOnly,否則 EveryDay)
    /// </summary>
    private ScheduleDayType GetCurrentActiveFallbackDayType()
    {
        if (!IsTrackingActive()) return ScheduleDayType.EveryDay;
        var t = GameStatusService.Instance.Time;
        if (t == null) return ScheduleDayType.EveryDay;
        return t.IsWeekend ? ScheduleDayType.WeekendOnly : ScheduleDayType.WeekdayOnly;
    }

    /// <summary>取得某女主角實際被放置的動作(Play Mode 下)</summary>
    private string GetRuntimeActualActivity(string heroineID)
    {
        if (!IsTrackingActive()) return null;
        var scenario = GameStatusService.Instance.Scenario;
        if (scenario == null) return null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var locState = kvp.Value;
            if (locState == null || locState.Heroines == null) continue;
            foreach (var h in locState.Heroines)
            {
                if (string.Equals(h.HeroineID, heroineID, System.StringComparison.OrdinalIgnoreCase))
                    return h.Activity;
            }
        }
        return null;
    }

    // ==========================================================
    // 表頭:Phase
    // ==========================================================
    private void DrawPhaseHeaderRow()
    {
        // 整行一次預留 Rect,手繪以精確對齊
        float rowWidth = COL_PADDING_LEFT + TIME_COL_WIDTH + GetCellsAreaWidth();
        var rowRect = GUILayoutUtility.GetRect(rowWidth, 18, GUILayout.Width(rowWidth));

        var headerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.4f, 0.7f, 1f) }
        };

        // 左側空白
        // (不用畫,只是佔位)

        // Phase 標題
        float cellsStartX = rowRect.x + COL_PADDING_LEFT + TIME_COL_WIDTH;
        for (int p = 0; p < _phaseNames.Count; p++)
        {
            float phaseX = cellsStartX + GetCellOffsetX(p, 1);
            float phaseWidth = _slotsPerPhase[p] * CELL_WIDTH;
            var r = new Rect(phaseX, rowRect.y, phaseWidth, rowRect.height);
            GUI.Label(r, $"P{p} {_phaseNames[p]}", headerStyle);
        }
    }

    // ==========================================================
    // 表頭:Slot
    // ==========================================================
    private void DrawSlotHeaderRow()
    {
        float rowWidth = COL_PADDING_LEFT + TIME_COL_WIDTH + GetCellsAreaWidth();
        var rowRect = GUILayoutUtility.GetRect(rowWidth, 18, GUILayout.Width(rowWidth));

        var headerStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
        var labelStyle = new GUIStyle(EditorStyles.miniBoldLabel);

        // 左側文字
        var leftRect = new Rect(rowRect.x + COL_PADDING_LEFT, rowRect.y, TIME_COL_WIDTH, rowRect.height);
        GUI.Label(leftRect, "鍊 \\ 時段", labelStyle);

        // 每個 Slot 的標題
        float cellsStartX = rowRect.x + COL_PADDING_LEFT + TIME_COL_WIDTH;
        for (int p = 0; p < _phaseNames.Count; p++)
        {
            int slotCount = _slotsPerPhase[p];
            for (int s = 1; s <= slotCount; s++)
            {
                float cx = cellsStartX + GetCellOffsetX(p, s);
                var r = new Rect(cx, rowRect.y, CELL_WIDTH, rowRect.height);
                GUI.Label(r, $"S{s}", headerStyle);
            }
        }
    }

    // ==========================================================
    // 鍊列(重構:手繪對齊)
    // ==========================================================
    private void DrawChainRow(HeroineScheduleConfig cfg, DailyChain chain, int chainIndex)
    {
        float rowWidth = COL_PADDING_LEFT + TIME_COL_WIDTH + GetCellsAreaWidth();
        float rowHeight = CELL_HEIGHT + ROW_PADDING * 2 + 40f; // 40 給左側 info 的額外高度
        float infoHeight = Mathf.Max(CELL_HEIGHT + 40f, CELL_HEIGHT);

        // 預留整行 Rect
        var rowRect = GUILayoutUtility.GetRect(rowWidth, infoHeight + ROW_PADDING * 2,
            GUILayout.Width(rowWidth), GUILayout.Height(infoHeight + ROW_PADDING * 2));

        // 背景(列底色)
        EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.2f, 0.2f, 0.15f));

        // 左側 info 欄
        var infoRect = new Rect(
            rowRect.x + COL_PADDING_LEFT,
            rowRect.y + ROW_PADDING,
            TIME_COL_WIDTH,
            infoHeight);
        DrawChainInfoCell(infoRect, cfg, chain, chainIndex);

        // 右側格子(置中對齊於 infoRect)
        float cellsStartX = rowRect.x + COL_PADDING_LEFT + TIME_COL_WIDTH;
        float cellsY = rowRect.y + ROW_PADDING + (infoHeight - CELL_HEIGHT) * 0.5f;

        for (int p = 0; p < _phaseNames.Count; p++)
        {
            int slotCount = _slotsPerPhase[p];
            for (int s = 1; s <= slotCount; s++)
            {
                float cx = cellsStartX + GetCellOffsetX(p, s);
                var cellRect = new Rect(cx, cellsY, CELL_WIDTH, CELL_HEIGHT);
                DrawChainCell(cellRect, cfg, chain, p, s);
            }
        }
    }

    private void DrawChainInfoCell(Rect rect, HeroineScheduleConfig cfg, DailyChain chain, int chainIndex)
    {
        float lineH = 18f;
        float y = rect.y;

        // 檢查這條鍊是否為今日選中的鍊
        bool isTodaysChain = false;
        if (IsTrackingActive())
        {
            string todaysChain = GetRuntimeTodaysChain(cfg.heroineID);
            isTodaysChain = !string.IsNullOrEmpty(todaysChain) && todaysChain == chain.chainName;
        }

        // 第1行:名稱(如果是今日鍊,加綠色標記)
        if (isTodaysChain)
        {
            var markRect = new Rect(rect.x, y, 14, lineH);
            var markStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.3f, 1f, 0.3f) }
            };
            GUI.Label(markRect, "●", markStyle);
            string newName = EditorGUI.TextField(new Rect(rect.x + 14, y, rect.width - 14, lineH), chain.chainName);
            if (newName != chain.chainName)
            {
                Undo.RecordObject(cfg, "Rename Chain");
                chain.chainName = newName;
                EditorUtility.SetDirty(cfg);
            }
        }
        else
        {
            string newName = EditorGUI.TextField(new Rect(rect.x, y, rect.width, lineH), chain.chainName);
            if (newName != chain.chainName)
            {
                Undo.RecordObject(cfg, "Rename Chain");
                chain.chainName = newName;
                EditorUtility.SetDirty(cfg);
            }
        }
        y += lineH + 2;

        // 第2行:DayType
        EditorGUI.BeginChangeCheck();
        var newDayType = (ScheduleDayType)EditorGUI.EnumPopup(new Rect(rect.x, y, rect.width, lineH), chain.dayType);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(cfg, "Change DayType");
            chain.dayType = newDayType;
            EditorUtility.SetDirty(cfg);
        }
        y += lineH + 2;

        // 第3行:權重
        float wLabelW = 22f;
        GUI.Label(new Rect(rect.x, y, wLabelW, lineH), "W:", EditorStyles.miniLabel);
        int newWeight = EditorGUI.IntField(new Rect(rect.x + wLabelW, y, rect.width - wLabelW, lineH), chain.probabilityWeight);
        if (newWeight != chain.probabilityWeight)
        {
            Undo.RecordObject(cfg, "Change Weight");
            chain.probabilityWeight = Mathf.Max(0, newWeight);
            EditorUtility.SetDirty(cfg);
        }
        y += lineH + 2;

        // 第4行:按鈕列
        float btnW = (rect.width - 6) / 3;
        if (GUI.Button(new Rect(rect.x, y, btnW, lineH), "⚙", EditorStyles.miniButton))
            ShowChainAdvancedMenu(cfg, chain);
        if (GUI.Button(new Rect(rect.x + btnW + 3, y, btnW, lineH), "▲", EditorStyles.miniButton))
            MoveChain(cfg, chainIndex, -1);
        if (GUI.Button(new Rect(rect.x + (btnW + 3) * 2, y, btnW, lineH), "▼", EditorStyles.miniButton))
            MoveChain(cfg, chainIndex, +1);
        y += lineH + 2;

        // 第5行:Flag 提示
        if (chain.requiredFlag != null || chain.forbiddenFlag != null)
        {
            var flagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(1f, 0.75f, 0.3f) }
            };
            string flagText = "Flag:";
            if (chain.requiredFlag != null) flagText += "✓";
            if (chain.forbiddenFlag != null) flagText += "✗";
            GUI.Label(new Rect(rect.x, y, rect.width, lineH), flagText, flagStyle);
        }
    }

    // ==========================================================
    // Fallback 列(按 dayType 繪製)
    // ==========================================================
    private void DrawFallbackRowForDayType(HeroineScheduleConfig cfg, ScheduleDayType dayType)
    {
        // 計算這個 dayType 在此 cfg 內有幾格(若 0 且使用者沒開啟「顯示空列」,跳過)
        int countForDayType = 0;
        if (cfg.schedule != null)
        {
            foreach (var b in cfg.schedule)
                if (b.dayType == dayType) countForDayType++;
        }
        if (countForDayType == 0 && !_showEmptyFallbackRows)
            return;

        float rowWidth = COL_PADDING_LEFT + TIME_COL_WIDTH + GetCellsAreaWidth();
        float infoHeight = CELL_HEIGHT + 20f;

        var rowRect = GUILayoutUtility.GetRect(rowWidth, infoHeight + ROW_PADDING * 2,
            GUILayout.Width(rowWidth), GUILayout.Height(infoHeight + ROW_PADDING * 2));

        // 背景(依 dayType 用不同色調區別)
        EditorGUI.DrawRect(rowRect, GetFallbackRowBgColor(dayType));

        // 左側 info
        var infoRect = new Rect(
            rowRect.x + COL_PADDING_LEFT,
            rowRect.y + ROW_PADDING,
            TIME_COL_WIDTH,
            infoHeight);

        var (dayTypeLabel, dayTypeTag) = GetDayTypeLabels(dayType);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = GetDayTypeColor(dayType) }
        };
        GUI.Label(new Rect(infoRect.x, infoRect.y, infoRect.width, 18), $"📋 {dayTypeLabel}", titleStyle);
        GUI.Label(new Rect(infoRect.x, infoRect.y + 18, infoRect.width, 14), "Fallback", EditorStyles.miniLabel);
        GUI.Label(new Rect(infoRect.x, infoRect.y + 32, infoRect.width, 14), $"{countForDayType} 格", EditorStyles.miniLabel);

        // 右側格子
        float cellsStartX = rowRect.x + COL_PADDING_LEFT + TIME_COL_WIDTH;
        float cellsY = rowRect.y + ROW_PADDING + (infoHeight - CELL_HEIGHT) * 0.5f;

        for (int p = 0; p < _phaseNames.Count; p++)
        {
            int slotCount = _slotsPerPhase[p];
            for (int s = 1; s <= slotCount; s++)
            {
                float cx = cellsStartX + GetCellOffsetX(p, s);
                var cellRect = new Rect(cx, cellsY, CELL_WIDTH, CELL_HEIGHT);
                DrawFallbackCell(cellRect, cfg, p, s, dayType);
            }
        }
    }

    private Color GetFallbackRowBgColor(ScheduleDayType dayType)
    {
        switch (dayType)
        {
            case ScheduleDayType.WeekdayOnly: return new Color(0.35f, 0.38f, 0.45f, 0.22f);
            case ScheduleDayType.WeekendOnly: return new Color(0.45f, 0.38f, 0.35f, 0.22f);
            default: return new Color(0.35f, 0.35f, 0.35f, 0.22f);
        }
    }

    private Color GetDayTypeColor(ScheduleDayType dayType)
    {
        switch (dayType)
        {
            case ScheduleDayType.WeekdayOnly: return new Color(0.7f, 0.85f, 1f);
            case ScheduleDayType.WeekendOnly: return new Color(1f, 0.85f, 0.6f);
            default: return new Color(0.85f, 0.85f, 0.85f);
        }
    }

    private (string label, string tag) GetDayTypeLabels(ScheduleDayType dayType)
    {
        switch (dayType)
        {
            case ScheduleDayType.WeekdayOnly: return ("平日", "[平]");
            case ScheduleDayType.WeekendOnly: return ("假日", "[假]");
            default: return ("每日", "");
        }
    }

    // ==========================================================
    // 鍊格子
    // ==========================================================
    private void DrawChainCell(Rect rect, HeroineScheduleConfig cfg, DailyChain chain, int phase, int slot)
    {
        ChainBlock block = null;
        if (chain.blocks != null)
            block = chain.blocks.Find(b => b.phaseIndex == phase && b.slotIndex == slot);

        bool isEditingThis = _editTarget != null
            && !_editTarget.IsFallback
            && _editTarget.config == cfg
            && _editTarget.chain == chain
            && _editTarget.phaseIndex == phase
            && _editTarget.slotIndex == slot;

        Color bgColor = GetCellColor(
            block?.possibleScenarios,
            isEditingThis,
            isFallback: false);

        EditorGUI.DrawRect(rect, bgColor);

        DrawCellLabel(rect, block?.possibleScenarios, null);

        DrawRectBorder(rect,
            isEditingThis ? new Color(1f, 0.6f, 0f) : new Color(0, 0, 0, 0.3f),
            isEditingThis ? 2f : 1f);

        // ── Play Mode overlay:當前時段框線 + 反向驗證標記 ──
        bool isTodaysChain = false;
        if (IsTrackingActive())
        {
            string todaysChain = GetRuntimeTodaysChain(cfg.heroineID);
            isTodaysChain = !string.IsNullOrEmpty(todaysChain) && todaysChain == chain.chainName;
        }
        DrawRuntimeOverlay(rect, cfg, phase, slot, block?.possibleScenarios,
            isChainCell: true, isOnTodaysChain: isTodaysChain);

        HandleCellClick(rect, block != null, () =>
        {
            OpenChainEditor(cfg, chain, phase, slot, block);
        }, () =>
        {
            ShowChainCellContextMenu(cfg, chain, block, phase, slot);
        });
    }

    // ==========================================================
    // Fallback 格子
    // ==========================================================
    private void DrawFallbackCell(Rect rect, HeroineScheduleConfig cfg, int phase, int slot, ScheduleDayType dayType)
    {
        // 只找「phase+slot+dayType 都對」的 block
        ScheduleBlock block = null;
        if (cfg.schedule != null)
            block = cfg.schedule.Find(b =>
                b.phaseIndex == phase &&
                b.slotIndex == slot &&
                b.dayType == dayType);

        bool isEditingThis = _editTarget != null
            && _editTarget.IsFallback
            && _editTarget.config == cfg
            && _editTarget.scheduleBlock == block
            && block != null;

        Color bgColor = GetCellColor(
            block?.possibleScenarios,
            isEditingThis,
            isFallback: true);

        EditorGUI.DrawRect(rect, bgColor);

        // 不再在格內顯示 dayType tag (改由所屬列區分)
        DrawCellLabel(rect, block?.possibleScenarios, null);

        DrawRectBorder(rect,
            isEditingThis ? new Color(1f, 0.6f, 0f) : new Color(0, 0, 0, 0.25f),
            isEditingThis ? 2f : 1f);

        // ── Play Mode overlay:當前時段框線 + 反向驗證標記 ──
        // Fallback 格子要判斷「今天有沒有鍊」:沒鍊時才可能走 fallback
        // 另外 dayType 也要對上才可能是當前使用中的
        bool isOnActiveFallback = false;
        if (IsTrackingActive())
        {
            string todaysChain = GetRuntimeTodaysChain(cfg.heroineID);
            bool noChain = string.IsNullOrEmpty(todaysChain);
            bool dayTypeMatches = IsDayTypeMatchCurrent(dayType);
            isOnActiveFallback = noChain && dayTypeMatches;
        }
        DrawRuntimeOverlay(rect, cfg, phase, slot, block?.possibleScenarios,
            isChainCell: false, isOnTodaysChain: isOnActiveFallback);

        HandleCellClick(rect, block != null, () =>
        {
            OpenFallbackEditor(cfg, phase, slot, block, dayType);
        }, () =>
        {
            ShowFallbackCellContextMenu(cfg, block, phase, slot, dayType);
        });
    }

    /// <summary>
    /// 判斷某 dayType 是否對應當前遊戲日
    /// </summary>
    private bool IsDayTypeMatchCurrent(ScheduleDayType dayType)
    {
        if (!IsTrackingActive()) return false;
        var t = GameStatusService.Instance.Time;
        if (t == null) return false;
        bool isWeekend = t.IsWeekend;
        switch (dayType)
        {
            case ScheduleDayType.EveryDay: return true;
            case ScheduleDayType.WeekdayOnly: return !isWeekend;
            case ScheduleDayType.WeekendOnly: return isWeekend;
            default: return false;
        }
    }

    /// <summary>
    /// Play Mode 疊加層:當前時段框線、反向驗證標記
    /// </summary>
    /// <param name="isOnTodaysChain">這個格子所在的行是否為今天正在跑的行(鍊或 fallback)</param>
    private void DrawRuntimeOverlay(Rect rect, HeroineScheduleConfig cfg, int phase, int slot,
        List<ScheduleScenario> scenarios, bool isChainCell, bool isOnTodaysChain)
    {
        if (!IsTrackingActive()) return;

        var (curP, curS) = GetCurrentPhaseSlot();
        bool isCurrentTimeSlot = (phase == curP && slot == curS);

        // 當前時段:在整列的當前格加藍色醒目外框
        if (isCurrentTimeSlot)
        {
            // 亮藍色外框(比編輯中的橘色更細,用疊加)
            DrawRectBorder(rect, new Color(0.3f, 0.7f, 1f, 0.9f), 2f);
        }

        // 反向驗證:只在「今天正在跑的那行 + 當前時段」的格子做
        if (isCurrentTimeSlot && isOnTodaysChain)
        {
            string actualLoc = GetRuntimeActualLocation(cfg.heroineID);

            // 收集這格排程上預期的地點
            var expectedLocs = new HashSet<string>();
            if (scenarios != null)
            {
                foreach (var sc in scenarios)
                    if (!string.IsNullOrEmpty(sc.locationID))
                        expectedLocs.Add(sc.locationID);
            }

            // 右上角畫標記
            var iconRect = new Rect(rect.xMax - 16, rect.y + 1, 14, 14);
            if (string.IsNullOrEmpty(actualLoc))
            {
                // 女主不在地圖任何地點(上學中/離家)
                DrawMarkerIcon(iconRect, "👋", new Color(0.6f, 0.6f, 0.6f));
            }
            else if (expectedLocs.Contains(actualLoc))
            {
                // 符合排程
                DrawMarkerIcon(iconRect, "✓", new Color(0.3f, 1f, 0.3f));
            }
            else
            {
                // 實際位置與排程不符 → 被強制移動
                // 只顯示 ⚠ 標記,完整資訊在上方 Live 區顯示
                DrawMarkerIcon(iconRect, "⚠", new Color(1f, 0.5f, 0.2f));
            }
        }
    }

    private void DrawMarkerIcon(Rect rect, string text, Color color)
    {
        var prevColor = GUI.color;
        GUI.color = color;
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };
        GUI.Label(rect, text, style);
        GUI.color = prevColor;
    }

    // ==========================================================
    // 格子內容輔助
    // ==========================================================
    private Color GetCellColor(List<ScheduleScenario> scenarios, bool isEditing, bool isFallback)
    {
        if (isEditing) return new Color(1f, 0.85f, 0.3f, 0.5f);

        bool isEmpty = scenarios == null || scenarios.Count == 0;
        bool multi = scenarios != null && scenarios.Count > 1;

        if (isFallback)
        {
            if (isEmpty) return new Color(0.3f, 0.3f, 0.3f, 0.2f);
            if (multi) return new Color(0.5f, 0.5f, 0.7f, 0.4f);
            return new Color(0.5f, 0.7f, 0.5f, 0.35f);
        }
        else
        {
            if (isEmpty) return new Color(0.5f, 0.5f, 0.5f, 0.15f);
            if (multi) return new Color(0.6f, 0.85f, 1f, 0.4f);
            return new Color(0.6f, 1f, 0.6f, 0.3f);
        }
    }

    private void DrawCellLabel(Rect rect, List<ScheduleScenario> scenarios, ScheduleDayType? dayType)
    {
        string label;
        if (scenarios == null || scenarios.Count == 0)
        {
            label = "＋";
        }
        else if (scenarios.Count == 1)
        {
            var sc = scenarios[0];
            string dayTag = "";
            if (dayType.HasValue && dayType.Value != ScheduleDayType.EveryDay)
                dayTag = dayType.Value == ScheduleDayType.WeekdayOnly ? " [平]" : " [假]";
            label = $"{Truncate(sc.locationID, 8)}{dayTag}\n{Truncate(sc.activityState, 8)}";
        }
        else
        {
            label = $"◆ {scenarios.Count} 個情境";
        }

        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = 10
        };

        var textRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
        GUI.Label(textRect, label, labelStyle);
    }

    private void HandleCellClick(Rect rect, bool blockExists, System.Action onLeftClick, System.Action onRightClick)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            if (e.button == 0)
            {
                onLeftClick?.Invoke();
                e.Use();
            }
            else if (e.button == 1)
            {
                onRightClick?.Invoke();
                e.Use();
            }
        }
    }

    // ==========================================================
    // 編輯面板
    // ==========================================================
    private void DrawBlockEditPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(280));

        EditorGUILayout.BeginHorizontal();
        string title;
        if (_editTarget.IsFallback)
        {
            var tag = GetDayTypeLabels(_editTarget.fallbackDayType).label;
            title = $"📋 編輯 Fallback ({tag}): [{_editTarget.config.heroineID}] / P{_editTarget.phaseIndex}S{_editTarget.slotIndex}";
        }
        else
            title = $"🔗 編輯鍊: [{_editTarget.config.heroineID}] / 「{_editTarget.chain.chainName}」 / P{_editTarget.phaseIndex}S{_editTarget.slotIndex}";
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        // 複製整格 / 貼上
        GUI.enabled = IsEditTargetBlockExists();
        if (GUILayout.Button("📋 複製此格", EditorStyles.miniButton, GUILayout.Width(80)))
            CopyCurrentBlock();
        GUI.enabled = true;

        GUI.enabled = _blockClipboard != null && IsEditTargetBlockExists();
        if (GUILayout.Button("貼上覆蓋", EditorStyles.miniButton, GUILayout.Width(65)))
            PasteBlockReplace();
        if (GUILayout.Button("貼上附加", EditorStyles.miniButton, GUILayout.Width(65)))
            PasteBlockAppend();
        GUI.enabled = true;

        if (GUILayout.Button("✕ 關閉", GUILayout.Width(60)))
        {
            _editTarget = null;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        bool blockExists = IsEditTargetBlockExists();

        if (!blockExists)
        {
            EditorGUILayout.HelpBox("此時段尚未定義。點下方「建立此時段」開始編輯,或直接貼上剪貼簿內容。", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("建立此時段", GUILayout.Height(30), GUILayout.Width(150)))
            {
                if (_editTarget.IsFallback) CreateFallbackBlockForTarget();
                else CreateChainBlockForTarget();
            }
            if (_blockClipboard != null)
            {
                if (GUILayout.Button("從剪貼簿建立", GUILayout.Height(30), GUILayout.Width(150)))
                {
                    if (_editTarget.IsFallback) CreateFallbackBlockForTarget();
                    else CreateChainBlockForTarget();
                    PasteBlockReplace();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        _editPanelScroll = EditorGUILayout.BeginScrollView(_editPanelScroll);

        // timeName
        string currentTimeName = _editTarget.IsFallback ? _editTarget.scheduleBlock.timeName : _editTarget.chainBlock.timeName;
        EditorGUI.BeginChangeCheck();
        string newTimeName = EditorGUILayout.TextField("時段描述", currentTimeName);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_editTarget.config, "Edit Block TimeName");
            if (_editTarget.IsFallback) _editTarget.scheduleBlock.timeName = newTimeName;
            else _editTarget.chainBlock.timeName = newTimeName;
            EditorUtility.SetDirty(_editTarget.config);
        }

        // Fallback:dayType(改此欄位 = 把格子移到另一列)
        if (_editTarget.IsFallback)
        {
            EditorGUI.BeginChangeCheck();
            var newDayType = (ScheduleDayType)EditorGUILayout.EnumPopup(
                new GUIContent("日期類型", "改這裡會把這個格子移到對應的 dayType 列"),
                _editTarget.scheduleBlock.dayType);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_editTarget.config, "Edit Block DayType");
                _editTarget.scheduleBlock.dayType = newDayType;
                _editTarget.fallbackDayType = newDayType;  // 同步 target 以便面板標題跟著換
                EditorUtility.SetDirty(_editTarget.config);
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("情境清單 (多個 = 在此格再加權抽籤)", EditorStyles.miniBoldLabel);

        List<ScheduleScenario> scenarios = GetCurrentBlockScenarios();
        if (scenarios == null)
        {
            scenarios = new List<ScheduleScenario>();
            if (_editTarget.IsFallback) _editTarget.scheduleBlock.possibleScenarios = scenarios;
            else _editTarget.chainBlock.possibleScenarios = scenarios;
        }

        int removeIndex = -1;
        int duplicateIndex = -1;
        int copyScenarioIndex = -1;

        for (int i = 0; i < scenarios.Count; i++)
        {
            var sc = scenarios[i];
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();

            // 第1行:地點 + 活動
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("地點", GUILayout.Width(40));
            sc.locationID = DrawLocationField(sc.locationID, 180);

            GUILayout.Space(10);
            GUILayout.Label("活動", GUILayout.Width(40));
            sc.activityState = DrawActivityField(sc.activityState, 180, i);
            EditorGUILayout.EndHorizontal();

            // 第2行:權重 + flag
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("權重", GUILayout.Width(40));
            sc.probabilityWeight = EditorGUILayout.IntField(sc.probabilityWeight, GUILayout.Width(50));

            GUILayout.Space(10);
            GUILayout.Label("需要 Flag", GUILayout.Width(60));
            sc.requiredFlag = (ProgressFlagDefinition)EditorGUILayout.ObjectField(
                sc.requiredFlag, typeof(ProgressFlagDefinition), false, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 右側:情境操作按鈕
            EditorGUILayout.BeginVertical(GUILayout.Width(40));
            if (GUILayout.Button(new GUIContent("⎘", "複製此情境"), EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(18)))
                copyScenarioIndex = i;
            if (GUILayout.Button(new GUIContent("+", "複製一份在此下方"), EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(18)))
                duplicateIndex = i;
            if (GUILayout.Button(new GUIContent("✕", "刪除此情境"), EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(18)))
                removeIndex = i;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            scenarios[i] = sc;
        }

        // 處理情境操作
        if (copyScenarioIndex >= 0)
        {
            _scenarioClipboard = DeepCopyScenario(scenarios[copyScenarioIndex]);
            ShowNotification(new GUIContent($"已複製情境: {scenarios[copyScenarioIndex].locationID}/{scenarios[copyScenarioIndex].activityState}"));
        }
        if (duplicateIndex >= 0)
        {
            Undo.RecordObject(_editTarget.config, "Duplicate Scenario");
            scenarios.Insert(duplicateIndex + 1, DeepCopyScenario(scenarios[duplicateIndex]));
            EditorUtility.SetDirty(_editTarget.config);
        }
        if (removeIndex >= 0)
        {
            Undo.RecordObject(_editTarget.config, "Remove Scenario");
            scenarios.RemoveAt(removeIndex);
            EditorUtility.SetDirty(_editTarget.config);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ 新增情境", GUILayout.Width(120)))
        {
            Undo.RecordObject(_editTarget.config, "Add Scenario");
            scenarios.Add(new ScheduleScenario { probabilityWeight = 100 });
            EditorUtility.SetDirty(_editTarget.config);
        }

        GUI.enabled = _scenarioClipboard != null;
        if (GUILayout.Button("📋 貼上情境", GUILayout.Width(120)))
        {
            Undo.RecordObject(_editTarget.config, "Paste Scenario");
            scenarios.Add(DeepCopyScenario(_scenarioClipboard));
            EditorUtility.SetDirty(_editTarget.config);
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("🗑 刪除此時段", GUILayout.Width(120)))
        {
            string msg = _editTarget.IsFallback
                ? $"要從 Fallback 刪除時段 P{_editTarget.phaseIndex}S{_editTarget.slotIndex} 嗎?"
                : $"要從鍊「{_editTarget.chain.chainName}」刪除時段 P{_editTarget.phaseIndex}S{_editTarget.slotIndex} 嗎?";

            if (EditorUtility.DisplayDialog("確認刪除", msg, "刪除", "取消"))
            {
                Undo.RecordObject(_editTarget.config, "Delete Block");
                if (_editTarget.IsFallback)
                    _editTarget.config.schedule.Remove(_editTarget.scheduleBlock);
                else
                    _editTarget.chain.blocks.Remove(_editTarget.chainBlock);
                EditorUtility.SetDirty(_editTarget.config);
                _editTarget = null;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (GUI.changed && _editTarget != null && _editTarget.config != null)
            EditorUtility.SetDirty(_editTarget.config);
    }

    // ==========================================================
    // 剪貼簿輔助
    // ==========================================================

    private bool IsEditTargetBlockExists()
    {
        if (_editTarget == null) return false;
        return _editTarget.IsFallback ? _editTarget.scheduleBlock != null : _editTarget.chainBlock != null;
    }

    private List<ScheduleScenario> GetCurrentBlockScenarios()
    {
        if (_editTarget == null) return null;
        return _editTarget.IsFallback
            ? _editTarget.scheduleBlock?.possibleScenarios
            : _editTarget.chainBlock?.possibleScenarios;
    }

    private void CopyCurrentBlock()
    {
        var src = GetCurrentBlockScenarios();
        if (src == null) return;
        _blockClipboard = src.Select(DeepCopyScenario).ToList();
        _blockClipboardSource = _editTarget.IsFallback
            ? $"[{_editTarget.config.heroineID}] Fallback P{_editTarget.phaseIndex}S{_editTarget.slotIndex}"
            : $"[{_editTarget.config.heroineID}] {_editTarget.chain.chainName} P{_editTarget.phaseIndex}S{_editTarget.slotIndex}";
        ShowNotification(new GUIContent($"已複製: {_blockClipboardSource}"));
    }

    private void PasteBlockReplace()
    {
        if (_blockClipboard == null || !IsEditTargetBlockExists()) return;
        Undo.RecordObject(_editTarget.config, "Paste Block (Replace)");
        var target = GetCurrentBlockScenarios();
        target.Clear();
        foreach (var s in _blockClipboard)
            target.Add(DeepCopyScenario(s));
        EditorUtility.SetDirty(_editTarget.config);
        ShowNotification(new GUIContent($"已貼上覆蓋 ({_blockClipboard.Count} 個情境)"));
    }

    private void PasteBlockAppend()
    {
        if (_blockClipboard == null || !IsEditTargetBlockExists()) return;
        Undo.RecordObject(_editTarget.config, "Paste Block (Append)");
        var target = GetCurrentBlockScenarios();
        foreach (var s in _blockClipboard)
            target.Add(DeepCopyScenario(s));
        EditorUtility.SetDirty(_editTarget.config);
        ShowNotification(new GUIContent($"已附加 {_blockClipboard.Count} 個情境"));
    }

    private static ScheduleScenario DeepCopyScenario(ScheduleScenario s)
    {
        return new ScheduleScenario
        {
            locationID = s.locationID,
            activityState = s.activityState,
            probabilityWeight = s.probabilityWeight,
            requiredFlag = s.requiredFlag
        };
    }

    // 從「鍊/fallback 格子」右鍵選單呼叫的「快速複製」(不用開編輯面板)
    private void QuickCopyBlock(HeroineScheduleConfig cfg, List<ScheduleScenario> scenarios, string sourceDesc)
    {
        if (scenarios == null || scenarios.Count == 0) return;
        _blockClipboard = scenarios.Select(DeepCopyScenario).ToList();
        _blockClipboardSource = sourceDesc;
        ShowNotification(new GUIContent($"已複製: {sourceDesc}"));
    }

    // 從「鍊/fallback 空格子」右鍵選單呼叫的「快速貼上」
    private void QuickPasteToChain(HeroineScheduleConfig cfg, DailyChain chain, int phase, int slot)
    {
        if (_blockClipboard == null) return;
        Undo.RecordObject(cfg, "Quick Paste to Chain");
        if (chain.blocks == null) chain.blocks = new List<ChainBlock>();
        var block = chain.blocks.Find(b => b.phaseIndex == phase && b.slotIndex == slot);
        if (block == null)
        {
            block = new ChainBlock
            {
                timeName = $"P{phase}S{slot}",
                phaseIndex = phase,
                slotIndex = slot,
                possibleScenarios = new List<ScheduleScenario>()
            };
            chain.blocks.Add(block);
        }
        else
        {
            block.possibleScenarios.Clear();
        }
        foreach (var s in _blockClipboard)
            block.possibleScenarios.Add(DeepCopyScenario(s));
        EditorUtility.SetDirty(cfg);
        ShowNotification(new GUIContent($"已貼上 ({_blockClipboard.Count} 個情境)"));
    }

    private void QuickPasteToFallback(HeroineScheduleConfig cfg, int phase, int slot, ScheduleDayType dayType)
    {
        if (_blockClipboard == null) return;
        Undo.RecordObject(cfg, "Quick Paste to Fallback");
        if (cfg.schedule == null) cfg.schedule = new List<ScheduleBlock>();
        var block = cfg.schedule.Find(b =>
            b.phaseIndex == phase &&
            b.slotIndex == slot &&
            b.dayType == dayType);
        if (block == null)
        {
            block = new ScheduleBlock
            {
                timeName = $"P{phase}S{slot}",
                phaseIndex = phase,
                slotIndex = slot,
                dayType = dayType,
                possibleScenarios = new List<ScheduleScenario>()
            };
            cfg.schedule.Add(block);
        }
        else
        {
            block.possibleScenarios.Clear();
        }
        foreach (var s in _blockClipboard)
            block.possibleScenarios.Add(DeepCopyScenario(s));
        EditorUtility.SetDirty(cfg);
        ShowNotification(new GUIContent($"已貼上 ({_blockClipboard.Count} 個情境)"));
    }

    // ==========================================================
    // 欄位:locationID
    // ==========================================================
    private string DrawLocationField(string current, float width)
    {
        if (_locationDB != null && _locationDB.allLocations != null && _locationDB.allLocations.Count > 0)
        {
            // 第 0 項固定為「(未設定)」佔位:對應空值,避免空 locationID 被誤顯示成清單第一個地點。
            // options 與 idValues 兩個 List 以相同 index 對齊(options 給人看,idValues 是實際回傳值)。
            var options = new List<string> { "(未設定)" };
            var idValues = new List<string> { null };
            foreach (var l in _locationDB.allLocations)
            {
                options.Add(l.LocationID);
                idValues.Add(l.LocationID);
            }

            int idx = idValues.IndexOf(current);
            if (idx < 0 && !string.IsNullOrEmpty(current))
            {
                // 目前值不在資料庫(舊值/已刪除的地點)→ 補一個顯示項,保留原值
                options.Add($"{current} (舊值)");
                idValues.Add(current);
                idx = options.Count - 1;
            }
            if (idx < 0) idx = 0;  // current 為空 → 落在「(未設定)」,不再偽裝成第一個地點

            int newIdx = EditorGUILayout.Popup(idx, options.ToArray(), GUILayout.Width(width));
            if (newIdx != idx)
                return idValues[Mathf.Clamp(newIdx, 0, idValues.Count - 1)];  // 可能回傳 null(選了「未設定」)
            return current;
        }
        return EditorGUILayout.TextField(current, GUILayout.Width(width));
    }

    // ==========================================================
    // 欄位:activityState
    // ==========================================================
    private string DrawActivityField(string current, float width, int scenarioIndex)
    {
        EditorGUILayout.BeginHorizontal();
        string newVal = EditorGUILayout.TextField(current, GUILayout.Width(width - 24));
        if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(22)))
        {
            var menu = new GenericMenu();

            // 嚴格模式:只列出以「目前女主角 heroineID_」開頭的活動 (不分大小寫)
            string heroineID = _editTarget?.config?.heroineID;
            List<string> filtered;

            if (string.IsNullOrEmpty(heroineID))
            {
                filtered = _activitySuggestions;
            }
            else
            {
                string prefix = heroineID + "_";
                filtered = _activitySuggestions
                    .Where(a => !string.IsNullOrEmpty(a) &&
                                a.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (filtered.Count == 0)
            {
                if (string.IsNullOrEmpty(heroineID))
                    menu.AddDisabledItem(new GUIContent("(還沒有任何已使用的活動名稱)"));
                else
                    menu.AddDisabledItem(new GUIContent($"(找不到前綴為 {heroineID}_ 的活動)"));
            }
            else
            {
                int capturedIndex = scenarioIndex;
                foreach (var s in filtered)
                {
                    string captured = s;
                    menu.AddItem(new GUIContent(captured), captured == current, () =>
                    {
                        if (_editTarget == null) return;
                        var scenarios = GetCurrentBlockScenarios();
                        if (scenarios == null || capturedIndex < 0 || capturedIndex >= scenarios.Count) return;
                        var x = scenarios[capturedIndex];
                        x.activityState = captured;
                        scenarios[capturedIndex] = x;
                        EditorUtility.SetDirty(_editTarget.config);
                        Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }
        EditorGUILayout.EndHorizontal();
        return newVal;
    }

    // ==========================================================
    // 鍊操作
    // ==========================================================
    private void AddNewChain(HeroineScheduleConfig cfg)
    {
        Undo.RecordObject(cfg, "Add Chain");
        if (cfg.dailyChains == null) cfg.dailyChains = new List<DailyChain>();
        cfg.dailyChains.Add(new DailyChain
        {
            chainName = $"NewChain_{cfg.dailyChains.Count + 1}",
            dayType = ScheduleDayType.EveryDay,
            probabilityWeight = 100,
            blocks = new List<ChainBlock>()
        });
        EditorUtility.SetDirty(cfg);
    }

    private void MoveChain(HeroineScheduleConfig cfg, int index, int direction)
    {
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= cfg.dailyChains.Count) return;
        Undo.RecordObject(cfg, "Reorder Chain");
        var tmp = cfg.dailyChains[index];
        cfg.dailyChains[index] = cfg.dailyChains[newIndex];
        cfg.dailyChains[newIndex] = tmp;
        EditorUtility.SetDirty(cfg);
    }

    private void ShowChainAdvancedMenu(HeroineScheduleConfig cfg, DailyChain chain)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("編輯 Flag 條件..."), false, () =>
            ChainFlagPopup.Show(cfg, chain));

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("複製鍊"), false, () =>
        {
            Undo.RecordObject(cfg, "Duplicate Chain");
            var copy = new DailyChain
            {
                chainName = chain.chainName + "_Copy",
                dayType = chain.dayType,
                probabilityWeight = chain.probabilityWeight,
                requiredFlag = chain.requiredFlag,
                forbiddenFlag = chain.forbiddenFlag,
                blocks = new List<ChainBlock>()
            };
            if (chain.blocks != null)
            {
                foreach (var b in chain.blocks)
                {
                    var newBlock = new ChainBlock
                    {
                        timeName = b.timeName,
                        phaseIndex = b.phaseIndex,
                        slotIndex = b.slotIndex,
                        possibleScenarios = new List<ScheduleScenario>()
                    };
                    if (b.possibleScenarios != null)
                        foreach (var s in b.possibleScenarios)
                            newBlock.possibleScenarios.Add(DeepCopyScenario(s));
                    copy.blocks.Add(newBlock);
                }
            }
            cfg.dailyChains.Add(copy);
            EditorUtility.SetDirty(cfg);
        });

        menu.AddItem(new GUIContent("刪除鍊"), false, () =>
        {
            if (EditorUtility.DisplayDialog("確認刪除鍊",
                $"要刪除「{cfg.heroineID}」的鍊「{chain.chainName}」嗎?此動作不可還原。",
                "刪除", "取消"))
            {
                Undo.RecordObject(cfg, "Delete Chain");
                cfg.dailyChains.Remove(chain);
                EditorUtility.SetDirty(cfg);
                if (_editTarget != null && _editTarget.chain == chain) _editTarget = null;
            }
        });

        menu.ShowAsContext();
    }

    // ==========================================================
    // 鍊格子右鍵選單(含貼上)
    // ==========================================================
    private void ShowChainCellContextMenu(HeroineScheduleConfig cfg, DailyChain chain, ChainBlock block, int phase, int slot)
    {
        var menu = new GenericMenu();

        // 複製此格(必須有資料)
        if (block != null && block.possibleScenarios != null && block.possibleScenarios.Count > 0)
        {
            menu.AddItem(new GUIContent("📋 複製此格"), false, () =>
            {
                QuickCopyBlock(cfg, block.possibleScenarios,
                    $"[{cfg.heroineID}] {chain.chainName} P{phase}S{slot}");
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("📋 複製此格(空格)"));
        }

        // 貼上(必須剪貼簿有內容)
        if (_blockClipboard != null)
        {
            menu.AddItem(new GUIContent($"貼上覆蓋 (← {_blockClipboardSource})"), false, () =>
            {
                QuickPasteToChain(cfg, chain, phase, slot);
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("貼上(剪貼簿為空)"));
        }

        menu.AddSeparator("");

        if (block != null)
        {
            menu.AddItem(new GUIContent("清除此格"), false, () =>
            {
                Undo.RecordObject(cfg, "Clear Block");
                chain.blocks.Remove(block);
                EditorUtility.SetDirty(cfg);
                if (_editTarget != null && _editTarget.chainBlock == block) _editTarget = null;
            });
        }

        menu.ShowAsContext();
    }

    private void ShowFallbackCellContextMenu(HeroineScheduleConfig cfg, ScheduleBlock block, int phase, int slot, ScheduleDayType dayType)
    {
        var menu = new GenericMenu();

        if (block != null && block.possibleScenarios != null && block.possibleScenarios.Count > 0)
        {
            var tag = GetDayTypeLabels(dayType).tag;
            menu.AddItem(new GUIContent("📋 複製此格"), false, () =>
            {
                QuickCopyBlock(cfg, block.possibleScenarios,
                    $"[{cfg.heroineID}] Fallback{tag} P{phase}S{slot}");
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("📋 複製此格(空格)"));
        }

        if (_blockClipboard != null)
        {
            menu.AddItem(new GUIContent($"貼上覆蓋 (← {_blockClipboardSource})"), false, () =>
            {
                QuickPasteToFallback(cfg, phase, slot, dayType);
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("貼上(剪貼簿為空)"));
        }

        menu.AddSeparator("");

        if (block != null)
        {
            menu.AddItem(new GUIContent("清除此格"), false, () =>
            {
                Undo.RecordObject(cfg, "Clear Fallback Block");
                cfg.schedule.Remove(block);
                EditorUtility.SetDirty(cfg);
                if (_editTarget != null && _editTarget.scheduleBlock == block) _editTarget = null;
            });
        }

        menu.ShowAsContext();
    }

    // ==========================================================
    // 展開/建立
    // ==========================================================
    private void OpenChainEditor(HeroineScheduleConfig cfg, DailyChain chain, int phase, int slot, ChainBlock block)
    {
        _editTarget = new BlockEditTarget
        {
            config = cfg,
            chain = chain,
            chainBlock = block,
            scheduleBlock = null,
            phaseIndex = phase,
            slotIndex = slot
        };
    }

    private void OpenFallbackEditor(HeroineScheduleConfig cfg, int phase, int slot, ScheduleBlock block, ScheduleDayType dayType)
    {
        _editTarget = new BlockEditTarget
        {
            config = cfg,
            chain = null,
            chainBlock = null,
            scheduleBlock = block,
            phaseIndex = phase,
            slotIndex = slot,
            fallbackDayType = dayType
        };
    }

    private void CreateChainBlockForTarget()
    {
        if (_editTarget == null) return;
        Undo.RecordObject(_editTarget.config, "Create Chain Block");
        if (_editTarget.chain.blocks == null) _editTarget.chain.blocks = new List<ChainBlock>();
        var newBlock = new ChainBlock
        {
            timeName = $"P{_editTarget.phaseIndex}S{_editTarget.slotIndex}",
            phaseIndex = _editTarget.phaseIndex,
            slotIndex = _editTarget.slotIndex,
            possibleScenarios = new List<ScheduleScenario>
            {
                new ScheduleScenario { probabilityWeight = 100 }
            }
        };
        _editTarget.chain.blocks.Add(newBlock);
        _editTarget.chainBlock = newBlock;
        EditorUtility.SetDirty(_editTarget.config);
    }

    private void CreateFallbackBlockForTarget()
    {
        if (_editTarget == null) return;
        Undo.RecordObject(_editTarget.config, "Create Fallback Block");
        if (_editTarget.config.schedule == null) _editTarget.config.schedule = new List<ScheduleBlock>();
        var newBlock = new ScheduleBlock
        {
            timeName = $"P{_editTarget.phaseIndex}S{_editTarget.slotIndex}",
            phaseIndex = _editTarget.phaseIndex,
            slotIndex = _editTarget.slotIndex,
            dayType = _editTarget.fallbackDayType,  // ← 從 target 帶入
            possibleScenarios = new List<ScheduleScenario>
            {
                new ScheduleScenario { probabilityWeight = 100 }
            }
        };
        _editTarget.config.schedule.Add(newBlock);
        _editTarget.scheduleBlock = newBlock;
        EditorUtility.SetDirty(_editTarget.config);
    }

    // ==========================================================
    // 工具
    // ==========================================================
    private static string Truncate(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "—";
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }

    private static void DrawRectBorder(Rect r, Color c, float thickness)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, thickness), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, thickness, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
    }
}

// ==========================================================
// 子視窗:編輯鍊的 Flag 條件
// ==========================================================
public class ChainFlagPopup : EditorWindow
{
    private HeroineScheduleConfig _cfg;
    private DailyChain _chain;

    public static void Show(HeroineScheduleConfig cfg, DailyChain chain)
    {
        var w = GetWindow<ChainFlagPopup>(true, "鍊的 Flag 條件", true);
        w._cfg = cfg;
        w._chain = chain;
        w.minSize = new Vector2(360, 170);
        w.maxSize = new Vector2(360, 170);
        w.ShowUtility();
    }

    private void OnGUI()
    {
        if (_chain == null) { Close(); return; }

        EditorGUILayout.LabelField($"鍊「{_chain.chainName}」", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        _chain.requiredFlag = (ProgressFlagDefinition)EditorGUILayout.ObjectField(
            "必要 Flag(空=不限)", _chain.requiredFlag, typeof(ProgressFlagDefinition), false);
        _chain.forbiddenFlag = (ProgressFlagDefinition)EditorGUILayout.ObjectField(
            "禁止 Flag(空=不限)", _chain.forbiddenFlag, typeof(ProgressFlagDefinition), false);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(_cfg);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "必要 Flag:此 flag 啟用時鍊才有資格被抽到(空=不限制)\n" +
            "禁止 Flag:此 flag 啟用時鍊不能被抽到(空=不限制)",
            MessageType.Info);
    }
}
#endif