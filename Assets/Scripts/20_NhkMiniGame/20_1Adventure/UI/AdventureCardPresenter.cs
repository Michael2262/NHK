using System;
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
    /// <summary>
    /// 場上唯一的 Presenter，供 Sequencer Command / 外部快速取用。
    /// 這個元件放在會卸載的場景上，所以離場時會把參照清掉。
    /// </summary>
    public static AdventureCardPresenter Instance { get; private set; }

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

    [Tooltip("勾選：結果呈現後牌會一直留著，直到外部呼叫 DismissCard()（Adventure(Dismiss)）才淡出。\n" +
             "適合由對話決定收牌時機，不用跟 Wait After Outcome 的秒數賽跑。\n" +
             "勾選時 Wait After Outcome 不生效。")]
    [SerializeField] private bool _holdUntilDismissed = false;

    [SerializeField] private float _fadeDuration = 0.35f;

    [Header("音效（AudioManager 的音效 ID；留空 = 不播）")]
    [Tooltip("發牌（牌飛入畫面）時播的音效 ID")]
    [SerializeField] private string _dealSoundKey = "woosh";

    [Tooltip("翻面時播的音效 ID")]
    [SerializeField] private string _flipSoundKey = "flipCard";

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

    // 「結果已呈現、正在等待淡出」的窗口 —— 這段可以被 DismissCard() 提前切斷
    private bool _canCutWait;
    private bool _dismissRequested;

    // ============================================================
    // 對外入口
    // ============================================================

    /// <summary>
    /// 發下一張牌（依牌池抽）並演到「必有效果」為止。
    /// 「繼續前進」和「繞遠路」都呼叫這支 —— 畫面上還有舊牌的話會先淡出清掉。
    /// </summary>
    public void PlayDraw()
    {
        if (!CanStartDraw()) return;
        StartCoroutine(DrawSequence(() => _controller.DrawNext()));
    }

    /// <summary>發「指定 ID」的牌（略過牌池）並演到必有效果為止。給劇情腳本用。</summary>
    public void PlayDrawByID(string cardID)
    {
        if (!CanStartDraw()) return;
        StartCoroutine(DrawSequence(() => _controller.DrawNextByID(cardID)));
    }

    private bool CanStartDraw()
    {
        if (IsPlaying) return false;
        if (_controller == null || _cardPrefab == null || _cardParent == null)
        {
            Debug.LogError("[AdventureCardPresenter] 參照未設定完整（Controller / CardPrefab / CardParent）。");
            return false;
        }
        return true;
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

    /// <summary>
    /// 把當前這張牌立刻收掉。依牌停在哪個狀態有兩種收法：
    ///
    /// ① 停著等挑戰/繞遠路（IsAwaitingOutcome）
    ///    → 直接撤牌，不判定成敗、不發 onSequenceComplete。
    ///      未結算的結果會在下次 PlayDraw() 時被 Model 丟棄（已生效的必有效果不回復）。
    ///
    /// ② 結果已呈現、正在等待淡出（_canCutWait）
    ///    → 把 Wait After Outcome 切短，立刻淡出。流程照常走完，onSequenceComplete 仍會發。
    ///
    /// 其他時機（飛入 / 翻面 / 判定進行中）呼叫一律忽略，不破壞狀態。
    /// </summary>
    public void DismissCard()
    {
        // ② 等待淡出中 → 切短等待
        if (_canCutWait)
        {
            _dismissRequested = true;
            return;
        }

        // ① 停著等挑戰/繞遠路 → 撤牌
        if (!IsPlaying && IsAwaitingOutcome)
        {
            StartCoroutine(WithdrawSequence());
            return;
        }

        Debug.LogWarning("[AdventureCardPresenter] DismissCard() 被忽略：目前沒有可收的牌。" +
                         $"（IsPlaying={IsPlaying}, IsAwaitingOutcome={IsAwaitingOutcome}, 等待淡出={_canCutWait}）");
    }

    /// <summary>撤牌：淡出當前牌、清掉等待挑戰狀態，不判定成敗、不發 onSequenceComplete。</summary>
    private IEnumerator WithdrawSequence()
    {
        IsPlaying = true;
        IsAwaitingOutcome = false;
        yield return ClearCurrentCard(); // 有牌就淡出；NoFlip 無牌則直接跳過
        IsPlaying = false;
        // 刻意不發 onSequenceComplete —— 撤牌是中途操作，後續由呼叫端接（通常再 PlayDraw）
    }

    /// <summary>目前是否可以用 DismissCard() 提前收牌。</summary>
    public bool CanDismissNow => _canCutWait;

    /// <summary>
    /// 等待指定秒數，但期間若被 DismissCard() 請求提前結束就立刻返回。
    /// </summary>
    private IEnumerator WaitOrDismiss(float seconds)
    {
        _canCutWait = true;
        _dismissRequested = false;

        if (_holdUntilDismissed)
        {
            // 牌一直留著，直到外部呼叫 DismissCard()
            while (!_dismissRequested) yield return null;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < seconds && !_dismissRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        _canCutWait = false;
        _dismissRequested = false;
    }

    // ============================================================
    // 演出
    // ============================================================

    private IEnumerator DrawSequence(Func<AdventureCardData> drawFunc)
    {
        IsPlaying = true;
        IsAwaitingOutcome = false;

        // 畫面上還有上一張牌（繞遠路）→ 先淡出清掉
        yield return ClearCurrentCard();

        // 發牌。Model 會在這裡丟棄上一張未結算的結果
        var card = drawFunc();
        if (card == null)
        {
            IsPlaying = false;
            yield break;
        }

        // NoFlip 只拿掉「視覺」，邏輯流程（必有 → 停頓等挑戰 → 判定）完全一樣
        bool visual = !card.NoFlipCardAnimation;

        if (visual)
        {
            // 生成牌背在畫面外 → 飛入 → 翻面，露出預設插圖
            _current = Instantiate(_cardPrefab, _cardParent);
            _current.Rect.anchoredPosition = _spawnPosition;
            _current.ShowBack();

            PlaySfx(_dealSoundKey); // 發牌
            yield return _current.FlyTo(_centerPosition, _flyDuration).WaitForCompletion();

            PlaySfx(_flipSoundKey); // 翻牌
            yield return _current.FlipTo(card.Illustration, _flipDuration).WaitForCompletion();

            // 等 X → 換必有插圖
            yield return new WaitForSeconds(_waitBeforeAlways);
            _current.SetSprite(card.GetAlwaysIllustration());
        }

        // 觸發必有效果（有無演出都一樣）
        _controller.ApplyAlways();

        if (card.OutcomeMode == AdventureOutcomeMode.AlwaysOnly)
        {
            // 這種牌沒有挑戰階段：直接收尾
            _controller.ResolveOutcome();

            if (visual) yield return WaitOrDismiss(_waitAfterOutcome); // 可被 DismissCard() 提前切斷
            yield return FinishCardSequence();
        }
        else
        {
            // 停在這裡，等外部呼叫 PlayOutcome()（挑戰）或 PlayDraw()（繞遠路）—— 有無演出都會停
            IsPlaying = false;
            IsAwaitingOutcome = true;
            onAwaitingOutcome?.Invoke();
        }
    }

    private IEnumerator OutcomeSequence()
    {
        IsPlaying = true;
        IsAwaitingOutcome = false;

        bool visual = _current != null; // NoFlip 卡沒有生成牌

        if (visual) yield return new WaitForSeconds(_waitBeforeOutcome);

        var result = _controller.ResolveOutcome();
        if (result != null && _current != null)
            _current.SetSprite(result.ResultIllustration);

        if (visual) yield return WaitOrDismiss(_waitAfterOutcome); // 可被 DismissCard() 提前切斷
        yield return FinishCardSequence();
    }

    /// <summary>
    /// 一張牌演完後的共同收尾：收牌 → 開下一步選單（onSequenceComplete）。
    /// 若這張牌的效果已結束了大冒險（End Adventure），則 onRunEnded 已發，不再開選單。
    /// </summary>
    private IEnumerator FinishCardSequence()
    {
        yield return ClearCurrentCard();

        IsPlaying = false;

        // 牌上的效果結束了大冒險 → onRunEnded 已發，不開下一步選單
        if (_controller.IsRunning)
            onSequenceComplete?.Invoke();
    }

    private IEnumerator ClearCurrentCard()
    {
        if (_current == null) yield break;

        yield return _current.FadeOut(_fadeDuration).WaitForCompletion();
        Destroy(_current.gameObject);
        _current = null;
    }

    /// <summary>播一個 AudioManager 音效（key 為空或找不到 AudioManager 就跳過）。</summary>
    private static void PlaySfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(key);
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;

        // 場景卸載 / 物件關閉時，避免留下半途的 clone
        StopAllCoroutines();
        if (_current != null)
        {
            Destroy(_current.gameObject);
            _current = null;
        }
        IsPlaying = false;
        IsAwaitingOutcome = false;
        _canCutWait = false;
        _dismissRequested = false;
    }
}
