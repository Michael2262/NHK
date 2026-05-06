using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 依據 CurrentScenarioModel 中「女主角」或「Risk」的即時狀態來切換圖片。
/// 條件可組合：角色ID、是否在某地點、Action名稱（多選任一符合）。
/// 
/// ■ 圖片切換：純粹依據 Scenario 條件結果。
/// ■ 顏色切換：Scenario 條件符合 + 額外 Flag 條件符合（若有設定）才變色。
///   - ChangeColorIfMet = true 且 ColorFlagIfMet = None → Scenario 符合就變色
///   - ChangeColorIfMet = true 且 ColorFlagIfMet 有值 → Scenario 符合 + Flag 也符合才變色
/// </summary>
public class ImageChangeByScenario : MonoBehaviour
{
    // ============================================================
    // 列舉 & 條件定義
    // ============================================================

    public enum TargetType { Heroine, Risk }

    public enum LocationCheckMode
    {
        DontCheck,          // 不檢查地點（僅靠角色ID / Action 判斷）
        IsAtLocation,       // 角色「在」指定地點
        IsNotAtLocation     // 角色「不在」指定地點
    }

    public enum LogicType { All_Conditions_Met, Any_Condition_Met }

    [Serializable]
    public class ScenarioConditionEntry
    {
        [Tooltip("僅供 Inspector 辨識用")]
        public string label;

        [Header("目標類型")]
        public TargetType targetType = TargetType.Heroine;

        [Header("角色 ID")]
        [Tooltip("填寫 heroineID 或 agentID（例如 sister、mother）")]
        public string characterID;

        [Header("地點檢查")]
        public LocationCheckMode locationCheck = LocationCheckMode.DontCheck;

        [Tooltip("當 locationCheck 不是 DontCheck 時，填寫要比對的地點 ID")]
        public string locationID;

        [Header("Action / Activity 檢查（多選任一符合即可）")]
        [Tooltip("留空 = 不檢查 Action。填寫多筆時，符合其中任何一筆即算通過")]
        public List<string> matchActions = new List<string>();
    }

    // ============================================================
    // Inspector 設定
    // ============================================================

    [Header("目標 UI 圖片")]
    public Image TargetImage;

    [Header("核心邏輯設定")]
    public LogicType Logic = LogicType.All_Conditions_Met;
    public List<ScenarioConditionEntry> Conditions = new List<ScenarioConditionEntry>();

    [Header("符合條件時 (True)")]
    public Sprite SpriteIfMet;
    public bool ChangeColorIfMet = false;
    [Tooltip("額外 Flag 條件：留空 = Scenario 符合就變色；有填 = Scenario + Flag 都符合才變色")]
    public ProgressFlagDefinition ColorFlagIfMet;
    [Tooltip("Flag 開啟時才變色 (true) 或 Flag 關閉時才變色 (false)")]
    public bool ColorFlagActiveState = true;
    public Color ColorIfMet = Color.white;
    public bool ChangeRotationIfMet = false;
    public float RotationIfMet = 0f;

    [Header("不符合條件時 (False)")]
    public Sprite SpriteIfNotMet;
    public bool ChangeColorIfNotMet = false;
    [Tooltip("額外 Flag 條件：留空 = Scenario 不符合就變色；有填 = Scenario 不符合 + Flag 都符合才變色")]
    public ProgressFlagDefinition ColorFlagIfNotMet;
    [Tooltip("Flag 開啟時才變色 (true) 或 Flag 關閉時才變色 (false)")]
    public bool ColorFlagNotMetActiveState = true;
    public Color ColorIfNotMet = Color.white;
    public bool ChangeRotationIfNotMet = false;
    public float RotationIfNotMet = 0f;

    // ============================================================
    // 內部狀態
    // ============================================================

    private CurrentScenarioModel _scenario;
    private ProgressFlagModel _flagModel;
    private Sprite _initialSprite;
    private Color _initialColor;
    private float _initialRotationZ;

    // ============================================================
    // 生命週期
    // ============================================================

    private void Awake()
    {
        if (TargetImage == null) TargetImage = GetComponent<Image>();
        if (TargetImage != null)
        {
            _initialSprite = TargetImage.sprite;
            _initialColor = TargetImage.color;
            _initialRotationZ = TargetImage.rectTransform.localEulerAngles.z;
        }
    }

    private void OnEnable()
    {
        if (GameStatusService.Instance != null)
        {
            _scenario = GameStatusService.Instance.Scenario;
            _flagModel = GameStatusService.Instance.ProgressFlags;
        }

        if (_scenario == null) return;

        // 訂閱角色移動事件
        _scenario.OnHeroineMoved += OnHeroineMovedHandler;
        _scenario.OnRiskMoved += OnRiskMovedHandler;

        // 訂閱場景整體重算事件（時間推進後會觸發）
        _scenario.OnScenarioRecalculated += Evaluate;

        // 訂閱 Flag 變化事件（顏色可能依賴 Flag）
        if (_flagModel != null)
            _flagModel.OnFlagChanged += OnFlagChangedHandler;

        Evaluate();
    }

    private void OnDisable()
    {
        if (_scenario != null)
        {
            _scenario.OnHeroineMoved -= OnHeroineMovedHandler;
            _scenario.OnRiskMoved -= OnRiskMovedHandler;
            _scenario.OnScenarioRecalculated -= Evaluate;
        }
        if (_flagModel != null)
            _flagModel.OnFlagChanged -= OnFlagChangedHandler;
    }

    // ============================================================
    // 事件處理
    // ============================================================

    private void OnHeroineMovedHandler(string heroineID, string oldLoc, string newLoc, string newActivity)
    {
        if (IsRelevantCharacter(heroineID, TargetType.Heroine))
            Evaluate();
    }

    private void OnRiskMovedHandler(string agentID, string oldLoc, string newLoc, string actionID)
    {
        if (IsRelevantCharacter(agentID, TargetType.Risk))
            Evaluate();
    }

    private void OnFlagChangedHandler(string flagID, bool value)
    {
        // 只在相關的 ColorFlag 變動時才重新評估
        bool relevant = false;
        if (ColorFlagIfMet != null && ColorFlagIfMet.FlagID == flagID) relevant = true;
        if (ColorFlagIfNotMet != null && ColorFlagIfNotMet.FlagID == flagID) relevant = true;
        if (relevant) Evaluate();
    }

    private bool IsRelevantCharacter(string changedID, TargetType type)
    {
        foreach (var cond in Conditions)
        {
            if (cond.targetType == type &&
                cond.characterID.Equals(changedID, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ============================================================
    // 核心評估
    // ============================================================

    [ContextMenu("Force Evaluate")]
    public void Evaluate()
    {
        if (Conditions.Count == 0 || TargetImage == null) return;

        if (_scenario == null && GameStatusService.Instance != null)
            _scenario = GameStatusService.Instance.Scenario;
        if (_flagModel == null && GameStatusService.Instance != null)
            _flagModel = GameStatusService.Instance.ProgressFlags;
        if (_scenario == null) return;

        bool finalResult = (Logic == LogicType.All_Conditions_Met);

        foreach (var cond in Conditions)
        {
            bool isMet = CheckCondition(cond);

            if (Logic == LogicType.All_Conditions_Met)
            {
                if (!isMet) { finalResult = false; break; }
            }
            else // Any_Condition_Met
            {
                if (isMet) { finalResult = true; break; }
            }
        }

        ApplyResult(finalResult);
    }

    // ============================================================
    // 條件判斷
    // ============================================================

    private bool CheckCondition(ScenarioConditionEntry entry)
    {
        if (string.IsNullOrEmpty(entry.characterID)) return false;

        if (entry.targetType == TargetType.Heroine)
            return CheckHeroine(entry);
        else
            return CheckRisk(entry);
    }

    private bool CheckHeroine(ScenarioConditionEntry entry)
    {
        string foundLocID = null;
        string foundActivity = null;

        foreach (var kvp in _scenario.AllLocationStates)
        {
            var match = kvp.Value.Heroines.Find(
                h => h.HeroineID.Equals(entry.characterID, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundActivity = match.Activity;
                break;
            }
        }

        if (!CheckLocation(entry, foundLocID)) return false;
        if (!CheckAction(entry, foundActivity)) return false;
        return true;
    }

    private bool CheckRisk(ScenarioConditionEntry entry)
    {
        string foundLocID = null;
        string foundActionID = null;

        foreach (var kvp in _scenario.AllLocationStates)
        {
            var match = kvp.Value.Risks.Find(r =>
                !string.IsNullOrEmpty(r.inspectionTypeID) &&
                r.inspectionTypeID.IndexOf(entry.characterID, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundActionID = match.inspectionTypeID;
                break;
            }
        }

        if (!CheckLocation(entry, foundLocID)) return false;
        if (!CheckAction(entry, foundActionID)) return false;
        return true;
    }

    // ============================================================
    // 共用檢查
    // ============================================================

    private bool CheckLocation(ScenarioConditionEntry entry, string foundLocID)
    {
        switch (entry.locationCheck)
        {
            case LocationCheckMode.DontCheck:
                return foundLocID != null;

            case LocationCheckMode.IsAtLocation:
                return foundLocID != null &&
                       foundLocID.Equals(entry.locationID, StringComparison.OrdinalIgnoreCase);

            case LocationCheckMode.IsNotAtLocation:
                return foundLocID == null ||
                       !foundLocID.Equals(entry.locationID, StringComparison.OrdinalIgnoreCase);

            default:
                return true;
        }
    }

    private bool CheckAction(ScenarioConditionEntry entry, string currentAction)
    {
        if (entry.matchActions == null || entry.matchActions.Count == 0)
            return true;

        if (string.IsNullOrEmpty(currentAction))
            return false;

        return entry.matchActions.Any(
            a => currentAction.Equals(a, StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // Flag 輔助判斷
    // ============================================================

    /// <summary>
    /// 檢查額外的 Flag 條件是否通過。
    /// flagDef 為 null → 視為無條件通過（不檢查）。
    /// </summary>
    private bool IsColorFlagSatisfied(ProgressFlagDefinition flagDef, bool requiredActiveState)
    {
        if (flagDef == null) return true; // 沒設定 = 不檢查 = 直接通過
        if (_flagModel == null) return false;

        bool isActive = _flagModel.Contains(flagDef.FlagID);
        return isActive == requiredActiveState;
    }

    // ============================================================
    // 視覺套用
    // ============================================================

    private void ApplyResult(bool scenarioPassed)
    {
        if (TargetImage == null) return;

        // --- 圖片切換：純粹依據 Scenario 結果 ---
        Sprite nextSprite = scenarioPassed ? SpriteIfMet : SpriteIfNotMet;
        TargetImage.sprite = nextSprite != null ? nextSprite : _initialSprite;

        // --- 顏色切換：Scenario 結果 + 額外 Flag 條件 ---
        bool applyColor = false;

        if (scenarioPassed && ChangeColorIfMet)
        {
            // Scenario 符合 + Flag 也符合（或沒設定 Flag）→ 變色
            applyColor = IsColorFlagSatisfied(ColorFlagIfMet, ColorFlagActiveState);
        }
        else if (!scenarioPassed && ChangeColorIfNotMet)
        {
            // Scenario 不符合 + Flag 也符合（或沒設定 Flag）→ 變色
            applyColor = IsColorFlagSatisfied(ColorFlagIfNotMet, ColorFlagNotMetActiveState);
        }

        if (applyColor)
            TargetImage.color = scenarioPassed ? ColorIfMet : ColorIfNotMet;
        else
            TargetImage.color = _initialColor;

        // --- 旋轉切換：純粹依據 Scenario 結果 ---
        bool changeRot = scenarioPassed ? ChangeRotationIfMet : ChangeRotationIfNotMet;
        float targetZ = changeRot ? (scenarioPassed ? RotationIfMet : RotationIfNotMet) : _initialRotationZ;

        Vector3 rot = TargetImage.rectTransform.localEulerAngles;
        if (!Mathf.Approximately(rot.z, targetZ))
        {
            rot.z = targetZ;
            TargetImage.rectTransform.localEulerAngles = rot;
        }
    }
}