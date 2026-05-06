using UnityEngine;
using System;
using System.Collections.Generic;

// 這裡改成通用名稱，這樣你在建立 Asset 時會更直觀
[CreateAssetMenu(fileName = "NewUIStageSettings", menuName = "GameConfig/UIStageSettings")]
public class UIStageSettings : ScriptableObject
{
    [Serializable]
    public class VisualStage
    {
        public string label = "新階段";

        [Tooltip("觸發此階段的最低門檻")]
        public float threshold;

        [Tooltip("到達此門檻時的目標顏色")]
        public Color stageColor = Color.white;

        [Tooltip("變化時的彈跳縮放倍率")]
        public float punchScale = 1.2f;

        [Tooltip("平時的基礎縮放倍率")]
        public float baseScale = 1.0f;
    }

    [Header("階段視覺配置")]
    public List<VisualStage> stages = new List<VisualStage>();
}