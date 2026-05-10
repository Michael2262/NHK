using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Flags")]
    [Tooltip("從清單中「已開啟」的旗標裡隨機保留 X 個，其餘全部關閉。本來就是關閉的旗標不受影響。")]
    public class KeepRandomFlags : FsmStateAction
    {
        [RequiredField]
        [Tooltip("要保留幾個開啟的旗標")]
        public FsmInt keepCount;

        [Tooltip("手動指定候選旗標 key 清單")]
        [ArrayEditor(VariableType.String)]
        public FsmString[] flags;

        [Tooltip("額外引用的 FlagBundle（可為空）")]
        public FlagBundle[] bundles;

        [UIHint(UIHint.Variable)]
        [Tooltip("（可選）將實際保留的數量存入此變數")]
        public FsmInt storeKeptCount;

        public override void OnEnter()
        {
            var model = GameStatusService.Instance.ProgressFlags;

            // 1. 收集所有候選 key（去重）
            var allKeys = new HashSet<string>();

            if (flags != null)
            {
                foreach (var f in flags)
                {
                    if (!f.IsNone && !string.IsNullOrEmpty(f.Value))
                        allKeys.Add(f.Value);
                }
            }

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle != null)
                    {
                        foreach (var id in bundle.GetFlagIDs())
                            allKeys.Add(id);
                    }
                }
            }

            // 2. 篩選出目前已開啟的
            var activeFlags = new List<string>();
            foreach (var key in allKeys)
            {
                if (model.Contains(key))
                    activeFlags.Add(key);
            }

            // 3. Fisher-Yates 洗牌
            int n = activeFlags.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (activeFlags[i], activeFlags[j]) = (activeFlags[j], activeFlags[i]);
            }

            // 4. 前 keepCount 個保留，其餘關閉
            int keep = Mathf.Clamp(keepCount.Value, 0, n);

            for (int i = keep; i < n; i++)
            {
                model.RemoveFlag(activeFlags[i]);
            }

            // 5. 儲存實際保留數量
            if (!storeKeptCount.IsNone)
                storeKeptCount.Value = Mathf.Min(keep, n);

            Finish();
        }
    }
}
