public enum GirlFeeling { Normal, Angry,Happy, Sad, Sexy, Orgasm }
public enum OralState { Default, Lick, Hold, Oral, Deep ,LickAll }

public enum GirlBodyStateId
{
    // 無用的預設
    All_Zero,
    All_Empty,

    // LeftLeg
    LeftLeg_Open,
    LeftLeg_Close,

    // Underwear
    Underwear_Aside,
    Underwear_Normal,
    Underwear_Out,

    Hand_On,
    Hand_Off,

    ClothesOff_Default,
    ClothesOff_Half,
    ClothesOff_Off,

    ClothesLift_Default,
    ClothesLift_Boy,
    ClothesLift_Girl,

    BlockAnimation_noBlock,
    BlockAnimation_blockA,
    BlockAnimation_blockB,

    // RightLeg
    RightLeg_Open,
    RightLeg_Close,

    Oral_Default,
    Oral_Lick,
    Oral_LickFast,
    Oral_HandJob,
    Oral_HandJobFast,
    Oral_Oral,
    Oral_OralFast,
    Oral_Deep,
    Oral_DeepFast,
    Oral_Cum,
    Oral_OralWait,

    OralWait_Default,
    OralWait_Oral,
    OralWait_OralFast,
    
    Sex_Default,
    Sex_Slow,
    Sex_Fast,

    SexCondom_No,
    SexCondom_Yes,

    Sex_AfterCum,
    Sex_CanPutIn,

    OralWait_OpenMouth,

    SexHand_Default,
    SexHand_Pull,
    SexHand_Wall,



    // 再往下加新條目即可……
}

public static class GirlBodyEnumExt //小工具：把 GirlBodyStateId 拆成 category 與 state 字串
{
    /// <returns>(category, state)</returns>
    public static (string category, string state) Split(this GirlBodyStateId id)
    {
        string s = id.ToString();               // 例：Underwear_Out
        int idx = s.IndexOf('_');
        return (s[..idx], s[(idx + 1)..]);
    }
}

