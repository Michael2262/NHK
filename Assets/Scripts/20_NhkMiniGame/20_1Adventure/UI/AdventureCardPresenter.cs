using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 抽牌演出的節奏控制。這裡是「演出驅動邏輯」——
/// 由這支決定什麼時候去呼叫 AdventureController 的各階段，讓效果在畫面對得上的時間點觸發。
///
/// 演出切成兩段，中間會停下來等外部觸發：
///
///   PlayDraw()   發牌 → 生成牌背在畫面外 → 飛到中央 → 翻面(預設插圖)
///                → 等 X 秒 → 換必有插圖 + 觸發必有效果
///                → 停下，發 onAwaitingOutcome
///
///   〔這裡由你接對話：「要挑戰」或「繞遠路」〕
///
///   PlayOutcome() 要挑戰 → 等 Y 秒 → 擲骰判定 + 換成功/失敗插圖
///                → 等 Z 秒 → 淡出銷毀 → 發 onSequenceComplete
///
///   繞遠路 → 直接再呼叫一次 PlayDraw()，舊牌會淡出、未結算的結果被丟棄
///           （必有效果已經生效的部分不會回復）
///
/// AlwaysOnly 的牌沒有挑戰階段，PlayDraw() 會自己收尾跑完整段。
/// </summary>
public class AdventureCardPresenter : MonoBehaviour
{
    [Header("參照")]
    [SerializeField] private AdventureController _controller;

    [Tooltip("牌的 prefab（需掛 AdventureCardView）")]
    [SerializeField] private AdventureCardView _cardPrefab;

    [Tooltip("生出來的牌要放在哪個 UI 容器底下")]
    [SerializeField] private RectTransform _cardParent;

    [Header("位置（anchoredPosition）")]
    [Tooltip("牌的起始位置，設在畫面外")]
    [SerializeField] private Vector2 _spawnPosition = new Vector2(-1400f, 0f);

    [Tooltip("牌停下來的位置，通常是畫面正中央")]
    [SerializeField] private Vector2 _centerPosition = Vector2.zero;

    [Header("時間")]
    [SerializeField] private float _flyDuration = 0.45f;
    [SerializeField] private float _flipDuration = 0.35f;

    [Tooltip("X：翻面後，等多久才觸發必有效果")]
    [SerializeField] private float _waitBeforeAlways = 0.5f;

    [Tooltip("Y：按下「挑戰」後，等多久才擲骰判定。不需要停頓就設 0")]
    [SerializeField] private float _waitBeforeOutcome = 0.3f;

    [Tooltip("Z：結果呈現後，等多久才讓牌淡出")]
    [SerializeField] private float _waitAfterOutcome = 0.8f;

    [SerializeField] private float _fadeDuration = 0.35f;

    [Header("事件")]
    [Tooltip("必有效果已觸發，停下來等玩家決定。接這裡去開你的「挑戰 / 繞遠路」對話")]
    public UnityEvent onAwaitingOutcome;

    [Tooltip("整段演出播完（牌已消失）。接這裡去開玩家的【繼續前進 / 休息 / 回家】選單")]
    public UnityEvent onSequenceComplete;

    /// <summary>有一段演出正在播（動畫進行中，別讓玩家插隊）。</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>牌停在畫面上，等玩家決定要不要挑戰。</summary>
    public bool IsAwaitingOutcome { get; private set; }

    private AdventureCardView _current;

    // ============================================================
    // 對外入口
    // ============================================================

    /// <summary>
    /// 發下一張牌並演到「必有效果」為止。
    /// 「繼續前進」和「繞遠路」都呼叫這支 —— 畫面上還有舊牌的話會先淡出清掉。
    /// </summary>
    public void PlayDraw()
    {
        if (IsPlaying) return;
        if (_controller == null || _cardPrefab == null || _cardParent == null)
        {
            Debug.LogError("[AdventureCardPresenter] 參照未設定完整（Controller / CardPrefab / CardParent）。");
            return;
        }
        StartCoroutine(DrawSequence());
    }

    /// <summary>
    /// 「要挑戰」：跑成功率判定並演出結果，然後收牌。
    /// 只有在 IsAwaitingOutcome 為 true 時有效。
    /// </summary>
    public void PlayOutcome()
    {
        if (IsPlaying || !IsAwaitingOutcome) return;
        StartCoroutine(OutcomeSequence());
    }

    // ============================================================
    // 演出
    // ============================================================

    private IEnumerator DrawSequence()
    {
        IsPlaying = true;
        IsAwaitingOutcome = false;

        // 畫面上還有上一張牌（繞遠路）→ 先淡出清掉
        yield return ClearCurrentCard();

        // 發牌。Model 會在這裡丟棄上一張未結算的結果
        var card = _controller.DrawNext();
        if (card == null)
        {
            IsPlaying = false;
            yield break;
        }

        // 生成牌背在畫面外
        _current = Instantiate(_cardPrefab, _cardParent);
        _current.Rect.anchoredPosition = _spawnPosition;
        _current.ShowBack();

        // 飛入 → 翻面，露出預設插圖
        yield return _current.FlyTo(_centerPosition, _flyDuration).WaitForCompletion();
        yield return _current.FlipTo(card.Illustration, _flipDuration).WaitForCompletion();

        // 等 X → 換必有插圖 + 觸發必有效果
        yield return new WaitForSeconds(_waitBeforeAlways);
        _current.SetSprite(card.GetAlwaysIllustration());
        _controller.ApplyAlways();

        if (card.OutcomeMode == AdventureOutcomeMode.AlwaysOnly)
        {
            // 這種牌沒有挑戰階段：直接收尾、收牌
            _controller.ResolveOutcome();

            yield return new WaitForSeconds(_waitAfterOutcome);
            yield return ClearCurrentCard();

            IsPlaying = false;
            onSequenceComplete?.Invoke();
        }
        else
        {
            // 停在這裡，等外部呼叫 PlayOutcome()（挑戰）或 PlayDraw()（繞遠路）
            IsPlaying = false;
            IsAwaitingOutcome = true;
            onAwaitingOutcome?.Invoke();
        }
    }

    private IEnumerator OutcomeSequence()
    {
        IsPlaying = true;
        IsAwaitingOutcome = false;

        yield return new WaitForSeconds(_waitBeforeOutcome);

        var result = _controller.ResolveOutcome();
        if (result != null && _current != null)
            _current.SetSprite(result.ResultIllustration);

        yield return new WaitForSeconds(_waitAfterOutcome);
        yield return ClearCurrentCard();

        IsPlaying = false;
        onSequenceComplete?.Invoke();
    }

    private IEnumerator ClearCurrentCard()
    {
        if (_current == null) yield break;

        yield return _current.FadeOut(_fadeDuration).WaitForCompletion();
        Destroy(_current.gameObject);
        _current = null;
    }

    private void OnDisable()
    {
        // 場景卸載 / 物件關閉時，避免留下半途的 clone
        StopAllCoroutines();
        if (_current != null)
        {
            Destroy(_current.gameObject);
            _current = null;
        }
        IsPlaying = false;
        IsAwaitingOutcome = false;
    }
}
