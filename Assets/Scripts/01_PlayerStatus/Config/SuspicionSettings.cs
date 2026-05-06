using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SuspicionSettings", menuName = "GameConfig/SuspicionSettings")]
public class SuspicionSettings : ScriptableObject
{
    [Serializable]
    public class SuspicionStage
    {
        public string label = "新階段";

        [Tooltip("觸發此階段的最低門檻 (%)")]
        public float threshold;

        [Tooltip("到達此門檻時，UI 顯示的目標顏色")]
        public Color stageColor = Color.white;

        [Tooltip("當可疑度變化時，文字會「彈」跳到的最高縮放倍率")]
        public float punchScale = 1.2f;

        [Tooltip("文字在此階段平時維持的基礎縮放倍率")]
        public float baseScale = 1.0f;
    }

    [Header("Stage Configurations")]
    [Tooltip("定義不同可疑度區間的視覺表現。請務必按門檻由小到大排列。")]
    public List<SuspicionStage> stages = new List<SuspicionStage>();
}