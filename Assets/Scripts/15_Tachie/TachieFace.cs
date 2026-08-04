using System;
using UnityEngine;

/// <summary>
/// 一張立繪臉：一個表情封包（TachieExpressionConfig 的 preset 名稱／ID）＋一個身體。
/// 透過 TachieController.ChangeExpression / ChangeBody 套用；兩者留空則不動該部位。
///
/// （原名 EmotionDrawFace，改為通用的 TachieFace，供情緒抽選、請求表演等共用。）
/// </summary>
[Serializable]
public class TachieFace
{
    [Tooltip("表情封包：TachieExpressionConfig 的 preset 名稱（ID）。留空 = 不變更表情。")]
    public string ExpressionID = "";

    [Tooltip("身體：TachieActor 的身體圖片名稱。留空 = 不變更身體。")]
    public string Body = "";
}
