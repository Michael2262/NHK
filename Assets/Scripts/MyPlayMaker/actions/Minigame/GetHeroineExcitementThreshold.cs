using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("根據女主角 ID 與指定等級，從 HeroineStatusModel 獲取升級所需的興奮度門檻。")]
    public class GetHeroineExcitementThreshold : FsmStateAction
    {
        [RequiredField]
        [Tooltip("女主角的唯一識別 ID (例如: sister)")]
        public FsmString heroineID;

        [RequiredField]
        [Tooltip("要查詢哪個等級的門檻？(例如輸入 0 代表查詢 LV0 升 LV1 所需經驗)")]
        public FsmInt level;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("將結果儲存到此變數中")]
        public FsmInt storeResult;

        [Tooltip("是否每幀執行？通常只需要在進入狀態時執行一次。")]
        public bool everyFrame;

        public override void Reset()
        {
            heroineID = null;
            level = 0;
            storeResult = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoGetThreshold();

            if (!everyFrame)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            DoGetThreshold();
        }

        private void DoGetThreshold()
        {
            if (heroineID == null || string.IsNullOrEmpty(heroineID.Value)) return;
            if (storeResult == null) return;

            // 1. 從 GameStatusService 獲取對應的女主角 Model
            // 注意：確保你的 GameStatusService.Instance 是可以全域訪問的
            if (GameStatusService.Instance != null && GameStatusService.Instance.Heroines.TryGetValue(heroineID.Value, out var heroine))
            {
                // 2. 呼叫我們之前在 Model 中建立的 public 方法
                storeResult.Value = heroine.GetCurrentExcitementThreshold(level.Value);
            }
            else
            {
                Debug.LogWarning($"[FSM Action] 找不到 ID 為 {heroineID.Value} 的女主角數據。");
            }
        }
    }
}