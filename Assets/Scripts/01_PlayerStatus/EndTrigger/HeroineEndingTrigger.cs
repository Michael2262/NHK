using UnityEngine;
using UnityEngine.Events;

/// <summary>已退役的等級結局觸發器；保留欄位讓舊場景可以手動遷移，不再觸發結局。</summary>
public class HeroineEndingTrigger : MonoBehaviour
{
    public enum HeroineCheckType
    {
        LewdnessReached,             // 開發度 >= 門檻
        AffinityReached,             // 親密度 >= 門檻
        LewdnessAndAffinityReached   // 開發度 + 親密度 同時達標
    }

    [Header("目標女主角")]
    [Tooltip("要監看的女主角 ID（對應 HeroineStatusConfig 中的 ID）。")]
    [SerializeField] private string heroineID;

    [Header("觸發條件")]
    [Tooltip("檢查模式：單一條件或雙條件同時達標。")]
    [SerializeField] private HeroineCheckType checkType = HeroineCheckType.LewdnessReached;

    [Tooltip("開發度門檻。當 checkType 包含 Lewdness 時生效。")]
    [SerializeField] private int lewdnessThreshold = 5;

    [Tooltip("親密度門檻。當 checkType 包含 Affinity 時生效。")]
    [SerializeField] private int affinityThreshold = 5;

    [Header("結局事件")]
    [Tooltip("條件達成時觸發。內容請在 Inspector 自行安排（場景切換、CG、對白等）。")]
    [SerializeField] private UnityEvent onEndingTriggered;

    [Header("排他設定（選填）")]
    [Tooltip("檢查 ProgressFlags：列表中只要任一 Flag 為 true（含數值 > 0），本次就跳過、不觸發 UnityEvent。\n" +
             "用途：避免結局互踩。例如此處拉入結局 A、B 的標記 Flag Asset，當 A 或 B 已觸發過時，C 就不會再觸發。\n" +
             "搭配方式：在 onEndingTriggered 裡呼叫 ProgressFlags.AddPersistentFlag(\"ENDING_X_DONE\") 來標記自己已觸發。")]
    [SerializeField] private ProgressFlagDefinition[] skipIfAnyFlag;


    private void Start()
    { Debug.LogWarning("[HeroineEndingTrigger] 舊女主角等級結局條件已退役，請重新配置結局條件。原事件綁定仍保留。", this); enabled = false; }

}