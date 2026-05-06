// 我們將 enum 放在自訂的命名空間中，以避免與 Unity 內建的類別名稱衝突。
namespace MySpineSystem
{
    /// <summary>
    /// Spine 動畫軌道語意化定義。
    /// 讓您可以用有意義的名稱（如 Body, Face）而非數字（1, 3）來操作動畫軌道。
    /// </summary>
    public enum AnimationTrack
    {
        Skin = 0,          // 皮膚開關層：很有可能不被設定，用來設定皮膚的眼睛開關
        Body = 1,          // 身體層：主要的動畫層，大多SpinePlayByList會應用於此
        BodyAttach = 2,    // 身體層附加：偏肢體的動畫附加
        LeftHand = 3,          
        RightHand = 4,
        BothHand = 5,
        OverBody1 = 6,        // 後續備用軌道，block目前在此
        OverBody2 = 7,
        OverBody3 = 8,
        Face = 9,           // 臉層：主要用在此
        Eye = 10,   // 臉層附加1：眼睛等等的需求
        Mouth = 11,   // 臉層附加2：嘴巴等等的需求
        Brow = 12,   // 臉層附加3：眉毛等等的需求
        FaceAll = 13,    //臉層附加4：覆蓋全臉(主動)
        FaceAyatem = 14, //臉層附加5：覆蓋全臉(被動)
        SF = 15,
        Track16 = 16


    }
}