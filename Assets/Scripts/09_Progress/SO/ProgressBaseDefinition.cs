using UnityEngine;

// 所有進度定義的基類
public abstract class ProgressBaseDefinition : ScriptableObject
{
    [Tooltip("描述用途")]
    [SerializeField, TextArea] protected string description;

    
    public string FlagID => name;
}