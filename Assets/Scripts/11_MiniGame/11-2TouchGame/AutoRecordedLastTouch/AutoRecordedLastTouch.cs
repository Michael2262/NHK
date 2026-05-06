using UnityEngine;
using HutongGames.PlayMaker;

[RequireComponent(typeof(PlayMakerFSM))]
public class AutoRecordedLastTouch : MonoBehaviour
{
    [Header("Current Records")]
    [SerializeField] public GameObject lastLeftHand;
    [SerializeField] public GameObject lastRightHand;
    [SerializeField] public GameObject lastSpecial1;
    [SerializeField] public GameObject lastSpecial2;

    private PlayMakerFSM _fsm;

    private void Awake()
    {
        _fsm = GetComponent<PlayMakerFSM>();
    }

    /// <summary>
    /// 觸碰事件入口，記錄最後觸碰者
    /// </summary>
    public void RegisterTouch(GameObject sender, TouchHandType type)
    {
        if (type == TouchHandType.None) return;

        if (type == TouchHandType.RandomHand)
        {
            if (lastLeftHand == null) type = TouchHandType.LeftHand;
            else if (lastRightHand == null) type = TouchHandType.RightHand;
            else type = (Random.value > 0.5f) ? TouchHandType.LeftHand : TouchHandType.RightHand;
        }

        switch (type)
        {
            case TouchHandType.LeftHand:
                lastLeftHand = sender;
                SyncToFsm("Last_LeftHand", sender);
                break;
            case TouchHandType.RightHand:
                lastRightHand = sender;
                SyncToFsm("Last_RightHand", sender);
                break;
            case TouchHandType.Special1:
                lastSpecial1 = sender;
                SyncToFsm("Last_Special1", sender);
                break;
            case TouchHandType.Special2:
                lastSpecial2 = sender;
                SyncToFsm("Last_Special2", sender);
                break;
        }
    }

    /// <summary>
    /// 清除指定類型的「最後觸碰者」
    /// </summary>
    public void ClearLastTouch(TouchHandType type)
    {
        switch (type)
        {
            case TouchHandType.LeftHand:
                lastLeftHand = null;
                SyncToFsm("Last_LeftHand", null);
                break;
            case TouchHandType.RightHand:
                lastRightHand = null;
                SyncToFsm("Last_RightHand", null);
                break;
            case TouchHandType.Special1:
                lastSpecial1 = null;
                SyncToFsm("Last_Special1", null);
                break;
            case TouchHandType.Special2:
                lastSpecial2 = null;
                SyncToFsm("Last_Special2", null);
                break;
        }
    }

    // ───── 內部工具 ─────

    private void SyncToFsm(string varName, GameObject val)
    {
        if (_fsm == null) return;

        var fsmObj = _fsm.FsmVariables.GetFsmGameObject(varName);
        if (fsmObj != null)
            fsmObj.Value = val;
        else
            Debug.LogWarning($"[AutoRecordedLastTouch] 找不到 FSM 變數: {varName}");
    }
}