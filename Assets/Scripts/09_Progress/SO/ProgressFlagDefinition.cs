using UnityEngine;

[CreateAssetMenu(menuName = "Game/Progress/Flag Definition", fileName = "Flag_NewFlag")]
public class ProgressFlagDefinition : ProgressBaseDefinition
{
    [Tooltip("建立時的預設狀態（開/關）")]
    public bool DefaultValue = false;
}