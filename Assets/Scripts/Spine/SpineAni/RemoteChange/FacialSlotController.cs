using UnityEngine;
using HutongGames.PlayMaker;
using MySpineSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// 【多插槽表情控制器】
/// 負責根據 FSM 情緒，同時控制多個 Spine Slot (Blush, Sweat, Special...) 的 Attachment 切換。
/// </summary>
public class FacialSlotController : MonoBehaviour
{
    // 定義映射類別，讓你在 Inspector 把 Enum 對應到實際的 Spine Slot 名稱
    [Serializable]
    public class SlotMapping
    {
        public ExpressionSlotType type;
        [UnityEngine.Tooltip("Spine 軟體中實際的插槽名稱")]
        public string spineSlotName;
    }

    [Header("1. 依賴組件 (Dependencies)")]
    [UnityEngine.Tooltip("掛載 FSM 的 GameObject")]
    public GameObject fsmOwnerObject;
    public string targetFsmName = "HeroineEmotionFSM";
    public SlotDatabase slotDatabase;

    [Header("2. 插槽映射設定 (Slot Mapping)")]
    [UnityEngine.Tooltip("在這裡定義 Blush, Sweat 等類型分別對應 Spine 哪個 Slot")]
    public List<SlotMapping> slotMappings = new List<SlotMapping>();

    [Header("3. 隨機計數邏輯 (Random Counter)")]
    public int minRemoteCount = 1;
    public int maxRemoteCount = 5;

    // --- 私有變數 ---
    private PlayMakerFSM myOwnerFSM;
    private SpineAnimationController spineController;
    private int currentRemoteCounter;
    private const string EMOTION_VARIABLE_NAME = "MyCurrentEmotion";

    void Start()
    {
        // 1. 初始化 FSM 參考
        if (fsmOwnerObject != null)
        {
            PlayMakerFSM[] allFsms = fsmOwnerObject.GetComponents<PlayMakerFSM>();
            foreach (var fsm in allFsms)
            {
                if (fsm.FsmName == targetFsmName)
                {
                    myOwnerFSM = fsm;
                    break;
                }
            }
        }

        if (myOwnerFSM == null)
        {
            Debug.LogError($"[FacialSlotController] 找不到 FSM: {targetFsmName}，腳本已停用。", this);
            this.enabled = false;
            return;
        }

        // 2. 獲取父層的 Spine 控制器
        spineController = GetComponentInParent<SpineAnimationController>();
        if (spineController == null)
        {
            Debug.LogError($"[FacialSlotController] 找不到 SpineAnimationController，腳本已停用。", this);
            this.enabled = false;
            return;
        }

        // 3. 檢查資料庫
        if (slotDatabase == null)
        {
            Debug.LogError($"[FacialSlotController] 尚未指定 SlotDatabase！", this);
            this.enabled = false;
            return;
        }

        ResetCounter();
    }

    /// <summary>
    /// 由 FSM 通過 SendMessage (Broadcast) 呼叫
    /// </summary>
    public void OnRemoteEvent()
    {
        currentRemoteCounter--;
        if (currentRemoteCounter <= 0)
        {
            ApplyMultiSlotChange();
            ResetCounter();
        }
    }

    /// <summary>
    /// 核心邏輯：讀取情緒，並將所有對應插槽換圖
    /// </summary>
    private void ApplyMultiSlotChange()
    {
        // A. 讀取情緒
        FsmEnum fsmEnumVar = myOwnerFSM.FsmVariables.GetFsmEnum(EMOTION_VARIABLE_NAME);
        HeroineEmotionType currentEmotion = (fsmEnumVar != null && fsmEnumVar.Value is HeroineEmotionType)
            ? (HeroineEmotionType)fsmEnumVar.Value : HeroineEmotionType.Idle;

        // B. 從資料庫讀取該情緒的所有設定 (SlotExpressionSet)
        // 這裡會調用資料庫獲取對應情緒的配置
        SlotExpressionSet resultSet = slotDatabase.GetSet(currentEmotion);
        if (resultSet == null || resultSet.slotSettings == null) return;

        // C. 遍歷該情緒下所有的插槽設定
        foreach (var setting in resultSet.slotSettings)
        {
            // 從映射表中找出對應的 Spine Slot 名稱
            string actualSpineSlot = GetSpineSlotName(setting.slotType);
            if (string.IsNullOrEmpty(actualSpineSlot)) continue;

            // 隨機抽選一張貼圖名稱
            string attachmentToApply = setting.GetRandomName();

            // 呼叫你的標準版 SetSlotAttachment
            spineController.SetSlotAttachment(actualSpineSlot, attachmentToApply);
        }
    }

    private string GetSpineSlotName(ExpressionSlotType type)
    {
        SlotMapping mapping = slotMappings.Find(m => m.type == type);
        return mapping != null ? mapping.spineSlotName : null;
    }

    private void ResetCounter()
    {
        currentRemoteCounter = UnityEngine.Random.Range(minRemoteCount, maxRemoteCount + 1);
    }
}