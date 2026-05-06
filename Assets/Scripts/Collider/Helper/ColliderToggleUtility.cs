using UnityEngine;
using System.Collections.Generic; // 引用這個才能使用 List

public class ColliderToggleUtility : MonoBehaviour
{
    // --- 新增的欄位 (符合需求 3 & 4) ---
    [Header("Custom Lists")]
    [Tooltip("將物件拖到這裡，呼叫 EnableListColliders() 時會開啟它們的 Collider2D")]
    public List<GameObject> listToEnable;

    [Tooltip("將物件拖到這裡，呼叫 DisableListColliders() 時會關閉它們的 Collider2D")]
    public List<GameObject> listToDisable;

    // --- 用於快取自己的 Collider (符合需求 1 & 2) ---
    private Collider2D selfCollider;

    private void Awake()
    {
        // 啟動時就抓取自己身上的 Collider 並存起來，效能較好
        selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null)
        {
            Debug.LogWarning("ColliderToggleUtility: " + gameObject.name + " 身上沒有找到 Collider2D。", this);
        }
    }

    // --- 新增的 API (符合需求 1) ---
    public void EnableSelfCollider()
    {
        if (selfCollider != null)
        {
            selfCollider.enabled = true;
        }
    }

    // --- 新增的 API (符合需求 2) ---
    public void DisableSelfCollider()
    {
        if (selfCollider != null)
        {
            selfCollider.enabled = false;
        }
    }

    // --- 新增的 API (符合需求 3) ---
    public void EnableListColliders()
    {
        ToggleListColliders(listToEnable, true);
    }

    // --- 新增的 API (符合需求 4) ---
    public void DisableListColliders()
    {
        ToggleListColliders(listToDisable, false);
    }

    // 抽出來共用的私有方法，用於處理清單
    private void ToggleListColliders(List<GameObject> objectList, bool isEnabled)
    {
        if (objectList == null || objectList.Count == 0)
        {
            return; // 如果清單是空的，就直接返回
        }

        foreach (GameObject obj in objectList)
        {
            // 增加 null 檢查，避免清單中有空欄位導致錯誤
            if (obj != null)
            {
                Collider2D col = obj.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = isEnabled;
                }
                else
                {
                    Debug.LogWarning("ColliderToggleUtility: " + obj.name + " 身上沒有找到 Collider2D。", obj);
                }
            }
        }
    }


    // --- 以下是您原有的腳本 ---

    // 公開方法：啟用所有子物件的 2D Collider
    public void EnableChildColliders()
    {
        SetChildCollidersEnabled(true);
    }

    // 公開方法：禁用所有子物件的 2D Collider
    public void DisableChildColliders()
    {
        SetChildCollidersEnabled(false);
    }

    // 執行邏輯的私有方法
    private void SetChildCollidersEnabled(bool isEnabled)
    {
        // 獲取這個物件 "以及" 其所有子物件中的 Collider2D 組件
        // "true" 參數代表也包含那些目前 Inactive 的子物件
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in colliders)
        {
            // 加上這個判斷，避免開關到這個父物件 "自己" 的 Collider
            if (col.gameObject != this.gameObject)
            {
                col.enabled = isEnabled;
            }
        }
    }
}