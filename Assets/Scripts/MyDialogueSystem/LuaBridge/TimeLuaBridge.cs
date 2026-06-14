using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 TimeSystemModel 的「星期幾 / 假日平日」橋接到 Dialogue System 的 Lua 環境，
/// 供對話條件式判斷使用。掛載於 GameStatusService 同一個 GameObject 上。
///
/// 註：「假日 / 平日」沿用遊戲既有定義（TimeSystemModel.IsWeekend），
/// 目前定義為星期六、星期日是假日。
/// </summary>
public class TimeLuaBridge : MonoBehaviour
{
    void OnEnable()
    {
        RegisterLuaFunctions();
    }

    void OnDisable()
    {
        UnregisterLuaFunctions();
    }

    // ==========================================================
    // Lua 函數註冊 / 取消註冊
    // ==========================================================

    private void RegisterLuaFunctions()
    {
        Lua.RegisterFunction("DayOfWeek", this, typeof(TimeLuaBridge).GetMethod("GetDayOfWeek"));
        Lua.RegisterFunction("IsDayOfWeek", this, typeof(TimeLuaBridge).GetMethod("IsDayOfWeek"));
        Lua.RegisterFunction("IsWeekend", this, typeof(TimeLuaBridge).GetMethod("IsWeekend"));
        Lua.RegisterFunction("IsWeekday", this, typeof(TimeLuaBridge).GetMethod("IsWeekday"));

        Debug.Log("TimeLuaBridge: Lua functions registered.");
    }

    private void UnregisterLuaFunctions()
    {
        Lua.UnregisterFunction("DayOfWeek");
        Lua.UnregisterFunction("IsDayOfWeek");
        Lua.UnregisterFunction("IsWeekend");
        Lua.UnregisterFunction("IsWeekday");

        Debug.Log("TimeLuaBridge: Lua functions unregistered.");
    }

    // ==========================================================
    // 星期相關 Lua 函數
    // ==========================================================

    /// <summary>
    /// 取得今天星期幾的數字（對齊 System.DayOfWeek：0=週日, 1=週一 … 6=週六）。
    /// Lua 用法: DayOfWeek() == 1  → 今天是星期一
    /// </summary>
    public double GetDayOfWeek()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return 0;
        }
        return (int)GameStatusService.Instance.Time.CurrentDayOfWeek;
    }

    /// <summary>
    /// 判斷今天是否為指定星期。可傳英文名稱（"Monday"）或數字字串（"1"）。
    /// Lua 用法: IsDayOfWeek("Monday") 或 IsDayOfWeek("1")
    /// </summary>
    public bool IsDayOfWeek(string day)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }

        DayOfWeek today = GameStatusService.Instance.Time.CurrentDayOfWeek;

        if (int.TryParse(day, out int dayNum))
            return (int)today == dayNum;

        if (Enum.TryParse(day, true, out DayOfWeek target))
            return today == target;

        Debug.LogWarning($"TimeLuaBridge: 無法解析星期參數 \"{day}\"。可用：Sunday~Saturday 或 0~6。");
        return false;
    }

    /// <summary>
    /// 今天是否為假日（星期六、星期日）。
    /// Lua 用法: IsWeekend()
    /// </summary>
    public bool IsWeekend()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }
        return GameStatusService.Instance.Time.IsWeekend;
    }

    /// <summary>
    /// 今天是否為平日（= 非假日）。
    /// Lua 用法: IsWeekday()
    /// </summary>
    public bool IsWeekday()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }
        return !GameStatusService.Instance.Time.IsWeekend;
    }
}
