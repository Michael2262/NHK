
/// <summary>
/// 條件檢查器的通用介面 (合約)。
/// 無論如何都會回傳一個bool CheckCondition
/// /// 任何實作此介面的類別，都必須提供一個 CheckCondition() 方法。
/// </summary>
public interface IConditionChecker
{
    bool CheckCondition();
}