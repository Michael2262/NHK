using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Request 難度文字的顯示設定。
/// 將內部成功率轉成三級 Text Table Key；不向玩家顯示真實成功率。
/// </summary>
[CreateAssetMenu(
    menuName = "NHK/Request Difficulty Display Config",
    fileName = "RequestDifficultyDisplayConfig")]
public class RequestDifficultyDisplayConfig : ScriptableObject
{
    [Header("成功率級距（由高到低）")]
    [Tooltip("成功率大於等於此值時顯示 Easy。")]
    [Range(0f, 100f)]
    [FormerlySerializedAs("veryEasyMinimumRate")]
    [SerializeField] private float easyRateThreshold = 85f;

    [Tooltip("成功率大於等於此值時顯示 Medium；低於此值顯示 Hard。")]
    [Range(0f, 100f)]
    [SerializeField] private float mediumRateThreshold = 50f;

    [Header("Text Table Keys")]
    [SerializeField] private string easyKey = "CheckDifficulty_Easy";
    [SerializeField] private string mediumKey = "CheckDifficulty_Medium";
    [SerializeField] private string hardKey = "CheckDifficulty_Hard";

    public float EasyMinimumRate => easyRateThreshold;
    public float MediumMinimumRate => mediumRateThreshold;

    public string EasyKey => easyKey;
    public string MediumKey => mediumKey;
    public string HardKey => hardKey;

    private void OnValidate()
    {
        easyRateThreshold = Mathf.Clamp(easyRateThreshold, 0f, 100f);
        mediumRateThreshold = Mathf.Clamp(mediumRateThreshold, 0f, easyRateThreshold);
    }
}
