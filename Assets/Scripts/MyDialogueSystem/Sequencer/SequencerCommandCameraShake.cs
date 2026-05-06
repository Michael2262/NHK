using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandCameraShake : SequencerCommand
    {
        private Transform target;
        private float duration;
        private float amount;
        private Vector3 originalPos;
        private float startTime;

        public void Awake()
        {
            // 讀取參數
            duration = GetParameterAsFloat(0, 0.5f);
            amount = GetParameterAsFloat(1, 0.2f);
            target = GetSubject(2);

            // 自動抓取 Main Camera
            if (target == null) target = (Camera.main != null) ? Camera.main.transform : null;
            if (target == null)
            {
                GameObject camObj = GameObject.Find("Main Camera");
                if (camObj != null) target = camObj.transform;
            }

            if (target == null)
            {
                Debug.LogError("搖動失敗：找不到 Main Camera！");
                Stop();
                return;
            }

            // --- 除錯用：如果在 Console 看到這行，代表指令有成功啟動 ---
            Debug.Log($"開始搖動物件: {target.name}，持續 {duration} 秒");

            originalPos = target.localPosition;
            startTime = DialogueTime.time;
        }

        public void Update()
        {
            if (DialogueTime.time < startTime + duration)
            {
                target.localPosition = originalPos + Random.insideUnitSphere * amount;
            }
            else
            {
                Stop();
            }
        }

        public void OnDestroy()
        {
            if (target != null)
            {
                target.localPosition = originalPos;
                Debug.Log("搖動結束，恢復位置。");
            }
        }
    }
}