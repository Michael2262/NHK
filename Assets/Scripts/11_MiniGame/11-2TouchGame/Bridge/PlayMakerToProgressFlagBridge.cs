using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;

public class PlayMakerToProgressFlagBridge : MonoBehaviour
{
    [Header("目標設定")]
    [UnityEngine.Tooltip("目標物件 (如果不拉取，預設為此腳本所在的物件)")]
    public GameObject targetObject;

    [UnityEngine.Tooltip("指定 FSM 的名稱。若留空，則自動抓取該物件第一個找到的 FSM")]
    public string fsmName;

    [UnityEngine.Tooltip("FSM 內的變數名稱 (支援 Int 或 Float)")]
    public string variableName;

    public enum VarType { Int, Float }
    public VarType variableType = VarType.Int;

    [Header("旗標設定")]
    [UnityEngine.Tooltip("開啟 Flag 的生命週期")]
    public FlagLifetime flagLifetime = FlagLifetime.Persistent;

    [Serializable]
    public struct ThresholdEntry
    {
        public float threshold;
        [UnityEngine.Tooltip("直接拉入 ProgressFlagDefinition 資源檔")]
        public ProgressFlagDefinition flagSO;
    }

    [UnityEngine.Tooltip("設定門檻與對應的 Flag SO")]
    public List<ThresholdEntry> thresholds = new List<ThresholdEntry>();

    private PlayMakerFSM _activeFSM;
    private FsmInt _fsmInt;
    private FsmFloat _fsmFloat;
    private int _nextIndex = 0;

    void Start()
    {
        // 1. 初始化目標 FSM 與物件
        if (targetObject == null) targetObject = this.gameObject;

        PlayMakerFSM[] allFSMs = targetObject.GetComponents<PlayMakerFSM>();
        if (allFSMs.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] 找不到 FSM 元件。");
            enabled = false;
            return;
        }

        if (string.IsNullOrEmpty(fsmName))
        {
            _activeFSM = allFSMs[0];
        }
        else
        {
            _activeFSM = allFSMs.FirstOrDefault(f => f.FsmName == fsmName);
            if (_activeFSM == null) _activeFSM = allFSMs[0];
        }

        // 2. 連結 PlayMaker 變數
        if (variableType == VarType.Int)
            _fsmInt = _activeFSM.FsmVariables.GetFsmInt(variableName);
        else
            _fsmFloat = _activeFSM.FsmVariables.GetFsmFloat(variableName);

        // 3. 排序門檻並初步檢查進度
        thresholds = thresholds.OrderBy(t => t.threshold).ToList();
        RefreshNextIndex();
    }

    void Update()
    {
        if (_activeFSM == null || _nextIndex >= thresholds.Count) return;

        float currentValue = (variableType == VarType.Int) ? _fsmInt.Value : _fsmFloat.Value;
        var nextTarget = thresholds[_nextIndex];

        // 檢查 SO 是否遺失
        if (nextTarget.flagSO == null)
        {
            _nextIndex++;
            return;
        }

        if (currentValue >= nextTarget.threshold)
        {
            TriggerFlag(nextTarget.flagSO.name); // 使用 SO 的檔名作為 Flag ID
            _nextIndex++;
        }
    }

    private void TriggerFlag(string flagId)
    {
        if (GameStatusService.Instance?.ProgressFlags != null)
        {
            Debug.Log($"<color=yellow>[Flag Bridge]</color> 門檻達成！觸發標記：{flagId}");
            GameStatusService.Instance.ProgressFlags.AddFlag(flagId, flagLifetime);
        }
    }

    public void RefreshNextIndex()
    {
        if (GameStatusService.Instance?.ProgressFlags == null) return;

        for (int i = 0; i < thresholds.Count; i++)
        {
            if (thresholds[i].flagSO != null)
            {
                // 檢查存檔模型中是否已經包含這個 SO 的名稱
                if (GameStatusService.Instance.ProgressFlags.Contains(thresholds[i].flagSO.name))
                {
                    _nextIndex = i + 1;
                }
            }
        }
    }
}