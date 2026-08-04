using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 請求表演佇列管理器（惰性自動建立，不需在場景擺放）。
///
/// 每個 Request(...) 指令把自己的表演丟進佇列，管理器依序播放。
/// 「一批」= 從佇列空閒開始、連續（不中斷）排進來的所有表演；
/// 只有排入時佇列正好空閒的那個指令是「批次擁有者」，負責整批演完後發一次 RequestDone。
/// 只要表演還在播、後續 Request 就會併入同一批（同幀或跨幀皆可）。
/// </summary>
public class RequestPerformanceManager : MonoBehaviour
{
    private static RequestPerformanceManager instance;

    public static RequestPerformanceManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[RequestPerformanceManager]");
                instance = go.AddComponent<RequestPerformanceManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private struct Item
    {
        public RequestPerformanceConfig Config;
        public string HeroineID;
        public bool Pass;
        public string[] Args;
    }

    private readonly Queue<Item> queue = new Queue<Item>();
    private bool isPlaying;

    /// <summary>佇列已空且沒有表演在播 = 閒置。</summary>
    public bool IsIdle => !isPlaying && queue.Count == 0;

    /// <summary>
    /// 把表演排入佇列。回傳 true 表示排入時正好閒置（= 這是新一批的第一個，
    /// 呼叫端負責在整批演完後發 RequestDone）。
    /// </summary>
    public bool Enqueue(RequestPerformanceConfig config, string heroineID, bool pass, string[] args)
    {
        bool startedNewBatch = IsIdle;
        queue.Enqueue(new Item { Config = config, HeroineID = heroineID, Pass = pass, Args = args });
        if (!isPlaying)
            StartCoroutine(PlayLoop());
        return startedNewBatch;
    }

    private IEnumerator PlayLoop()
    {
        isPlaying = true;
        while (queue.Count > 0)
        {
            Item item = queue.Dequeue();
            bool done = false;

            if (item.Config != null)
                item.Config.Play(this, item.HeroineID, item.Pass, item.Args, () => done = true);
            else
                done = true;

            while (!done) yield return null;
        }
        isPlaying = false;
    }
}
