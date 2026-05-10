// 檔案：HeroineDebugUI.cs
using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 職責：顯示「單一」女主角的 NHK 除錯資訊，並訂閱其數據變更事件。
/// 顯示項目：名稱、主導情緒、大宗情緒、情緒卡池分佈、性慾、H次數。
/// </summary>
public class HeroineDebugUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("對應 HeroineStatusConfig 中的角色 ID")]
    [SerializeField] private string heroineID;

    [Header("Identity")]
    [SerializeField] private TextMeshProUGUI textName;

    [Header("Emotions")]
    [SerializeField] private TextMeshProUGUI textCurrentEmotion;
    [SerializeField] private TextMeshProUGUI textDominantEmotion;
    [SerializeField] private TextMeshProUGUI textEmotionDeck;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI textLibido;
    [SerializeField] private TextMeshProUGUI textHCount;

    private HeroineStatusModel _model;

    // 快取 delegate 以便正確取消訂閱
    private Action _onDeckChanged;
    private Action<HeroineEmotionCardType> _onCurrentEmotionChanged;
    private Action<HeroineEmotionCardType> _onDominantEmotionChanged;
    private Action<int> _onLibidoChanged;
    private Action<int> _onHCountChanged;

    /// <summary>
    /// 若 Inspector 有填 heroineID，自動從 GameStatusService 取得對應 Model。
    /// 也可由外部呼叫 Initialize() 手動指定。
    /// </summary>
    private void Start()
    {
        if (_model != null) return; // 已經由外部 Initialize 過了

        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning($"[HeroineDebugUI] heroineID 未填寫，無法自動初始化。", this);
            return;
        }

        var service = GameStatusService.Instance;
        if (service == null)
        {
            Debug.LogError($"[HeroineDebugUI] GameStatusService.Instance 為 null。", this);
            return;
        }

        if (service.Heroines.TryGetValue(heroineID, out var model))
        {
            Initialize(model);
        }
        else
        {
            Debug.LogError($"[HeroineDebugUI] 找不到 heroineID=\"{heroineID}\" 的女主角。", this);
        }
    }

    /// <summary>
    /// 從外部初始化，告訴這個 UI 要監聽哪一位女主角。
    /// </summary>
    public void Initialize(HeroineStatusModel model)
    {
        // 若已訂閱舊 model，先解除
        Unsubscribe();

        _model = model;

        // 建立 delegate 快取
        _onDeckChanged = RefreshUI;
        _onCurrentEmotionChanged = _ => RefreshUI();
        _onDominantEmotionChanged = _ => RefreshUI();
        _onLibidoChanged = _ => RefreshUI();
        _onHCountChanged = _ => RefreshUI();

        // 訂閱 NHK 核心事件
        _model.OnEmotionDeckChanged += _onDeckChanged;
        _model.OnCurrentEmotionChanged += _onCurrentEmotionChanged;
        _model.OnDominantEmotionChanged += _onDominantEmotionChanged;
        _model.OnLibidoChanged += _onLibidoChanged;
        _model.OnHCountChanged += _onHCountChanged;

        Debug.Log($"<color=lime>[HeroineDebugUI] 訂閱成功: {_model.Name} ({_model.HeroineID})</color>", this);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_model == null) return;
        Debug.Log($"<color=yellow>[HeroineDebugUI] RefreshUI 被呼叫 — " +
                  $"Current={_model.CurrentEmotion}, Dominant={_model.DominantEmotion}, " +
                  $"Libido={_model.Libido}, HCount={_model.HCount}</color>", this);

        if (textName != null)
            textName.text = _model.Name;

        if (textCurrentEmotion != null)
            textCurrentEmotion.text = $"主導情緒: {_model.CurrentEmotion}";

        if (textDominantEmotion != null)
            textDominantEmotion.text = $"大宗情緒: {_model.DominantEmotion}";

        if (textEmotionDeck != null)
            textEmotionDeck.text = BuildDeckString();

        if (textLibido != null)
            textLibido.text = $"性慾: {_model.Libido} / {HeroineStatusModel.LibidoMax}";

        if (textHCount != null)
            textHCount.text = $"H回數: {_model.HCount}";
    }

    private string BuildDeckString()
    {
        int angry = _model.GetCardCount(HeroineEmotionCardType.Angry);
        int shy = _model.GetCardCount(HeroineEmotionCardType.Shy);
        int worried = _model.GetCardCount(HeroineEmotionCardType.Worried);
        int maternal = _model.GetCardCount(HeroineEmotionCardType.Maternal);
        int relaxed = _model.GetCardCount(HeroineEmotionCardType.Relaxed);
        int disapp = _model.GetCardCount(HeroineEmotionCardType.Disappointed);

        return $"卡池 [{_model.GetEmotionDeckCount()}/{_model.EmotionDeckMaxCount}]\n"
             + $"  怒{angry} 羞{shy} 憂{worried} 母{maternal} 鬆{relaxed} 失{disapp}";
    }

    private void Unsubscribe()
    {
        if (_model == null) return;

        _model.OnEmotionDeckChanged -= _onDeckChanged;
        _model.OnCurrentEmotionChanged -= _onCurrentEmotionChanged;
        _model.OnDominantEmotionChanged -= _onDominantEmotionChanged;
        _model.OnLibidoChanged -= _onLibidoChanged;
        _model.OnHCountChanged -= _onHCountChanged;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}