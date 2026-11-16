using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Transition")]
    [Tooltip("立即開啟或關閉共用的壓灰圖層。")]
    public class SetGrayOverlay : FsmStateAction
    {
        [RequiredField]
        [Tooltip("勾選時開啟，取消勾選時關閉。可指定 Bool 變數。")]
        public FsmBool visible;

        public override void Reset()
        {
            visible = true;
        }

        public override void OnEnter()
        {
            GrayOverlay.SetVisible(visible.Value);
            Finish();
        }
    }
}
