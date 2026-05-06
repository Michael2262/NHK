using UnityEngine;

/// <summary>
/// 呼叫 GiftUI 的轉接腳本。
/// 用於「按鈕所在的 Scene ≠ GiftUI 所在的 Scene」的情境:
/// GiftUI 放在不卸載的 Scene 上,此腳本掛在其他 Scene 的按鈕所在物件上,
/// 按鈕 OnClick 綁定此腳本的方法即可呼叫。
/// </summary>
public class GiftUIOpener : MonoBehaviour
{
    [Tooltip("預設的目標女主角 ID。使用 Open() 時會用這個;OpenFor(string) 可覆蓋。")]
    [SerializeField] private string _defaultHeroineID;

    /// <summary>
    /// 【給 Button OnClick 用】使用 Inspector 設定的 _defaultHeroineID 開啟 GiftUI。
    /// </summary>
    public void Open()
    {
        OpenFor(_defaultHeroineID);
    }

    /// <summary>
    /// 【給 Button OnClick(string) 用】指定 HeroineID 開啟 GiftUI。
    /// Button OnClick 綁定時可直接在 Inspector 填入 ID 字串。
    /// </summary>
    public void OpenFor(string heroineID)
    {
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogError($"[GiftUIOpener] '{name}' HeroineID 為空!", this);
            return;
        }

        if (GiftUI.Instance == null)
        {
            Debug.LogError("[GiftUIOpener] GiftUI.Instance 為 null,請確認 GiftUI 所在的 Scene 已載入。", this);
            return;
        }

        GiftUI.Instance.ShowForHeroine(heroineID);
    }
}