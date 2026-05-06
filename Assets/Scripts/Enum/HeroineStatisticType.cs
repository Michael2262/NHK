/// <summary>
/// 定義所有可追蹤的女主角統計項目。
/// 
/// ★ 每個項目都有手動指定的數字，可自由調換順序、新增不影響存檔。
/// ★ 規則：同區塊內挑一個沒用過的數字即可，千萬不要重複。
/// </summary>
public enum HeroineStatisticType
{
    // ══════════════════════════════════════
    //  性行為總計 (0~99)
    // ══════════════════════════════════════
    TotalSexCount = 0,    // 總H次數
    OralSexCount = 1,    // 口交次數
    CreampieCount = 2,    // 內射次數
    FacialCount = 3,    // 顏射次數
    KissCount = 4,    // 接吻次數
    OralCreampieCount = 5,    // 口內射精次數
    ExternalEjaculationCount = 6,    // 體外射精次數
    NightCrawlCount = 7,    // 夜襲次數
    InitiatedByHerCount = 8,    // 主動要求H的次數
    SexWithFamilyNearby = 9,    // 有家人的狀況下H次數
    SexualHarassmentCount = 10,   // 性騷擾的次數
    BathTogetherCount = 11,   // 一起洗澡的次數

    // ══════════════════════════════════════
    //  依地點分類 — H次數 (100~149)
    // ══════════════════════════════════════
    SexInBedroom = 100,  // 臥室H次數
    SexInLivingRoom = 101,  // 客廳H次數
    SexInBathroom = 102,  // 浴室H次數
    SexInToilet = 103,  // 廁所H次數
    // SexInKitchen             = 104,  // 廚房H次數
    // SexInSchool              = 105,  // 學校H次數

    // ══════════════════════════════════════
    //  依地點分類 — 性騷擾次數 (150~199)
    // ══════════════════════════════════════
    HarassInToilet = 150,  // 廁所性騷擾的次數
    HarassOnSofa = 151,  // 沙發性騷擾的次數
    HarassInBedroom = 152,  // 臥室性騷擾的次數
    // HarassInBathroom         = 153,  // 浴室性騷擾的次數

    // ══════════════════════════════════════
    //  液體量 (200~249)
    // ══════════════════════════════════════
    SwallowedMl = 200,  // 喝下的精液量 (ml)
    ExternalEjaculationMl = 201,  // 體外射精精液量 (ml)
    CreampieMl = 202,  // 內射的精液量 (ml)

    // ══════════════════════════════════════
    //  高潮 / 射精統計 (300~399)
    // ══════════════════════════════════════
    TotalOrgasmCount = 300,  // 累積高潮次數
    MaxOrgasmInOneSession = 301,  // 單次最多高潮次數 (取最大值)
    MaxConsecutiveEjaculation = 302,  // 連續射精次數 (取最大值)
}