using UnityEngine;

/// <summary>放在 Resources/InsertCGRefreshSettings 時，所有插圖演出共用這份刷新設定。</summary>
[CreateAssetMenu(menuName = "NHK/InsertCG/共用刷新設定", fileName = "InsertCGRefreshSettings")]
public sealed class InsertCGRefreshSettings : ScriptableObject
{
    internal const float DefaultRefreshTime = 1.5f;

    [Tooltip("開啟時按固定間隔顯示進度，關閉時每幀更新。滿格與歸零都會立即顯示。")]
    public bool nonRealtimeRefresh = true;
    [Min(0.01f)] public float refreshTime = DefaultRefreshTime;
}
