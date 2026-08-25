// Copyright (c) NHK Project. All rights reserved.
//
// 掛在「回應選單面板」(Bubble Response Menu Panel / StandardUIMenuPanel) 的 GameObject 上。
//
// 作用：
//   選單「開啟」時，把你指定的物件（通常是 MainPanel 的背景子物件）藏起來；
//   選單「關閉」時再還原。讓你逐一決定「哪個選單面板要藏 MainPanel 背景」。
//
// 為什麼需要它：
//   指定了 MainPanel 之後，MainPanel 整段對話都開著，平常被字幕 bubble 蓋住看不到。
//   選單要出現時，Dialogue System 會呼叫 HideOnResponseMenu() 把字幕 bubble 藏掉，
//   於是背後的 MainPanel 背景就露出來了。本元件在選單開/關的當下主動開關背景物件，
//   把它壓回去。
//
// 與 SetPanel 無關：
//   SetPanel(actor, n) 只是換「字幕面板(bubble)」用第幾號，不動 MainPanel。
//   本元件開關的是「你指定的 MainPanel 背景物件」（固定參照），
//   跟當下用哪個字幕面板無關，所以不管怎麼 SetPanel 都照樣生效。
//
// 使用方式：
//   1. 把本元件掛到 Bubble Response Menu Panel 上（同物件必須有 UIPanel / StandardUIMenuPanel）。
//   2. 在 Inspector 的 Targets To Hide 拖入要隱藏的物件（MainPanel 的背景子物件，可放多個）。
//   3. 若某個選單面板不想隱藏，取消勾選 Hide While Menu Open 即可（或不要掛本元件）。
//   4. 若你會用 SetMenuPanel 換成別的選單面板，那個面板也要各自掛一份。

using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/NHK/Menu Panel Background Hider")]
    [RequireComponent(typeof(UIPanel))]
    public class MenuPanelBackgroundHider : MonoBehaviour
    {
        [Tooltip("選單開啟時要隱藏、關閉時還原的物件（通常是 MainPanel 的背景子物件）。可放多個。")]
        [SerializeField] private List<GameObject> targetsToHide = new List<GameObject>();

        [Tooltip("勾選才會在本選單開啟時隱藏背景；取消勾選則本面板不動 MainPanel 背景。")]
        [SerializeField] private bool hideWhileMenuOpen = true;

        private UIPanel _panel;

        private void Awake()
        {
            _panel = GetComponent<UIPanel>();
        }

        private void OnEnable()
        {
            if (_panel == null) return;
            // 先移除再加入，避免面板快速開關造成重複註冊
            _panel.onOpen.RemoveListener(HandleMenuOpen);
            _panel.onClose.RemoveListener(HandleMenuClose);
            _panel.onOpen.AddListener(HandleMenuOpen);
            _panel.onClose.AddListener(HandleMenuClose);
        }

        private void OnDisable()
        {
            if (_panel != null)
            {
                _panel.onOpen.RemoveListener(HandleMenuOpen);
                _panel.onClose.RemoveListener(HandleMenuClose);
            }
            // 保險：面板被停用時把背景還原，避免卡在隱藏狀態
            SetTargetsActive(true);
        }

        private void HandleMenuOpen()
        {
            if (!hideWhileMenuOpen) return;
            SetTargetsActive(false);
        }

        private void HandleMenuClose()
        {
            if (!hideWhileMenuOpen) return;
            SetTargetsActive(true);
        }

        private void SetTargetsActive(bool active)
        {
            for (int i = 0; i < targetsToHide.Count; i++)
            {
                if (targetsToHide[i] != null) targetsToHide[i].SetActive(active);
            }
        }
    }
}
