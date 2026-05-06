using UnityEngine;
using System.Collections;

/// <summary>
/// 所有「場景準備任務」的抽象基礎類別。
/// </summary>
public abstract class SceneReadyTaskBase : MonoBehaviour
{
    /// <summary>
    /// 執行此任務的具體邏輯。
    /// </summary>
    /// <param name="entryID">從 Coordinator 傳入的當前場景入口ID。</param>
    /// <returns>協程 IEnumerator</returns>
    public abstract IEnumerator ExecuteTask(string entryID);
}