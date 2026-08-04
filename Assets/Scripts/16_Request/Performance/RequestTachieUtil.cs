using System;
using UnityEngine;

/// <summary>
/// 請求表演用的立繪套用工具：把一張 TachieFace（表情 + 身體）套到指定 group。
/// 失敗只記錄，絕不拋例外中斷表演協程（否則 onDone 不會呼叫、對話會卡死）。
/// </summary>
public static class RequestTachieUtil
{
    public static void Apply(TachieFace face, string groupID)
    {
        if (face == null) return;

        bool hasExpr = !string.IsNullOrEmpty(face.ExpressionID);
        bool hasBody = !string.IsNullOrEmpty(face.Body);
        if (!hasExpr && !hasBody) return;

        var tc = TachieController.Instance;
        if (tc == null)
        {
            Debug.LogWarning("[RequestTachieUtil] TachieController.Instance is null。");
            return;
        }

        try
        {
            if (hasExpr) tc.ChangeExpression(groupID, face.ExpressionID);
            if (hasBody) tc.ChangeBody(groupID, face.Body);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RequestTachieUtil] 套用立繪失敗（expr='{face.ExpressionID}', body='{face.Body}'）：{e}");
        }
    }
}
