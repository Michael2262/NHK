using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 資源變動報告設定檔 (ScriptableObject)
/// 用於配置 ValueChangeReporter 的 Key 規則、忽略類型、顏色反轉等設定
/// </summary>
[CreateAssetMenu(fileName = "ValueChangeReportConfig", menuName = "Config/Value Change Report Config")]
public class ValueChangeReportConfig : ScriptableObject
{
    // ==================================================
    // Key 格式設定
    // ==================================================

    [Header("Key 格式設定")]
    [Tooltip("Text Table Key 的前綴 (例如: Result)")]
    [SerializeField] private string keyPrefix = "Result";

    [Tooltip("Key 的分隔符號 (例如: .)")]
    [SerializeField] private string keySeparator = ".";

    // ==================================================
    // 資源類型對應設定
    // ==================================================

    [Header("資源類型 Key 對應")]
    [Tooltip("自訂資源類型對應的 Key 名稱 (留空則使用 enum 名稱)")]
    [SerializeField] private List<ResourceTypeKeyMapping> resourceTypeKeyMappings = new List<ResourceTypeKeyMapping>();

    [Header("女主角資源類型 Key 對應")]
    [Tooltip("自訂女主角資源類型對應的 Key 名稱 (留空則使用 enum 名稱)")]
    [SerializeField] private List<HeroineResourceTypeKeyMapping> heroineResourceTypeKeyMappings = new List<HeroineResourceTypeKeyMapping>();

    // ==================================================
    // 過濾設定
    // ==================================================

    [Header("過濾設定")]
    [Tooltip("不顯示報告的資源類型")]
    [SerializeField] private List<string> ignoredTypes = new List<string> { "Action" };

    [Tooltip("增加時視為「壞事」的資源類型 (顏色反轉)")]
    [SerializeField] private List<string> inverseGoodBadTypes = new List<string> { "Suspicion" };

    // ==================================================
    // 顏色標籤設定
    // ==================================================

    [Header("顏色標籤設定")]
    [Tooltip("好事使用的顏色標籤 (例如: em3)")]
    [SerializeField] private string goodColorTag = "em3";

    [Tooltip("壞事使用的顏色標籤 (例如: em2)")]
    [SerializeField] private string badColorTag = "em2";

    // ==================================================
    // 公開屬性
    // ==================================================

    public string KeyPrefix => keyPrefix;
    public string KeySeparator => keySeparator;
    public string GoodColorTag => goodColorTag;
    public string BadColorTag => badColorTag;
    public IReadOnlyList<string> IgnoredTypes => ignoredTypes;
    public IReadOnlyList<string> InverseGoodBadTypes => inverseGoodBadTypes;

    // ==================================================
    // 資源類型 Key 對應
    // ==================================================

    [Serializable]
    public class ResourceTypeKeyMapping
    {
        [Tooltip("資源類型")]
        public ValueChangeResult.ResourceType resourceType;

        [Tooltip("對應的 Key 名稱 (留空則使用 enum 名稱)")]
        public string keyName;
    }

    [Serializable]
    public class HeroineResourceTypeKeyMapping
    {
        [Tooltip("女主角資源類型")]
        public ValueChangeResult.HeroineResourceType resourceType;

        [Tooltip("對應的 Key 名稱 (留空則使用 enum 名稱)")]
        public string keyName;
    }

    // ==================================================
    // 查詢方法
    // ==================================================

    /// <summary>
    /// 取得資源類型對應的 Key 名稱
    /// </summary>
    public string GetResourceTypeKey(string enumName)
    {
        // 先查找自訂對應
        foreach (var mapping in resourceTypeKeyMappings)
        {
            if (mapping.resourceType.ToString() == enumName && !string.IsNullOrEmpty(mapping.keyName))
            {
                return mapping.keyName;
            }
        }

        // 查找女主角資源類型
        foreach (var mapping in heroineResourceTypeKeyMappings)
        {
            if (mapping.resourceType.ToString() == enumName && !string.IsNullOrEmpty(mapping.keyName))
            {
                return mapping.keyName;
            }
        }

        // 沒有自訂則返回原名
        return enumName;
    }

    /// <summary>
    /// 檢查是否為忽略的類型
    /// </summary>
    public bool IsIgnoredType(string typeKey)
    {
        return ignoredTypes.Contains(typeKey);
    }

    /// <summary>
    /// 檢查是否為反轉好壞的類型
    /// </summary>
    public bool IsInverseType(string typeKey)
    {
        return inverseGoodBadTypes.Contains(typeKey);
    }

    /// <summary>
    /// 建構完整的 Text Table Key
    /// 格式: {Prefix}{Sep}{Action}{TypeKey}{Sep}{Effect}
    /// 例如: Result.AddMoney.Good
    /// </summary>
    public string BuildKey(string typeKey, bool isAdd, string effect)
    {
        string actionStr = isAdd ? "Add" : "Reduce";
        string mappedTypeKey = GetResourceTypeKey(typeKey);
        return $"{keyPrefix}{keySeparator}{actionStr}{mappedTypeKey}{keySeparator}{effect}";
    }

    /// <summary>
    /// 取得顏色標籤
    /// </summary>
    public string GetColorTag(bool isGood)
    {
        return isGood ? goodColorTag : badColorTag;
    }

    // ==================================================
    // Editor 輔助
    // ==================================================

#if UNITY_EDITOR
    [ContextMenu("填入預設資源類型對應")]
    private void FillDefaultResourceTypeMappings()
    {
        resourceTypeKeyMappings.Clear();
        foreach (ValueChangeResult.ResourceType type in Enum.GetValues(typeof(ValueChangeResult.ResourceType)))
        {
            resourceTypeKeyMappings.Add(new ResourceTypeKeyMapping
            {
                resourceType = type,
                keyName = "" // 留空表示使用 enum 名稱
            });
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("填入預設女主角資源類型對應")]
    private void FillDefaultHeroineResourceTypeMappings()
    {
        heroineResourceTypeKeyMappings.Clear();
        foreach (ValueChangeResult.HeroineResourceType type in Enum.GetValues(typeof(ValueChangeResult.HeroineResourceType)))
        {
            heroineResourceTypeKeyMappings.Add(new HeroineResourceTypeKeyMapping
            {
                resourceType = type,
                keyName = "" // 留空表示使用 enum 名稱
            });
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}