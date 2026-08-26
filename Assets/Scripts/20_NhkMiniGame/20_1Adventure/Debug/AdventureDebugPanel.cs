using UnityEngine;

/// <summary>
/// 大冒險除錯面板（OnGUI）。不需要任何 UI 美術即可試玩核心流程：
/// 開始 → 發牌 → 翻牌 → 繼續/行動-1/回家。
/// 掛在場景任一 GameObject，拖入 AdventureController 與要測的 Dungeon 即可。
/// </summary>
public class AdventureDebugPanel : MonoBehaviour
{
    [SerializeField] private AdventureController _controller;
    [SerializeField] private AdventureDungeonData _dungeon;

    [Tooltip("面板螢幕位置")]
    [SerializeField] private Vector2 _origin = new Vector2(10, 10);

    private string _lastResult = "（尚未翻牌）";

    private void Start()
    {
        if (_controller == null) return;
        _controller.onFlipResolved.AddListener(OnFlip);
        _controller.onRunEnded.AddListener(OnEnded);
    }

    private void OnFlip(AdventureFlipResult r)
    {
        string outcome = r.OutcomeResolved ? (r.Success ? "★成功" : "✗失敗") : "（必有效果直接結束）";

        _lastResult = outcome
                    + Format("必有", r.AlwaysChanges)
                    + Format("結果", r.Changes);
    }

    private static string Format(string label, System.Collections.Generic.List<AdventureChangeRecord> list)
    {
        if (list == null || list.Count == 0) return "";
        string s = $"\n  {label}:";
        foreach (var c in list)
            s += $" [{c.LabelKey}{(c.Amount >= 0 ? "+" : "")}{c.Amount}]";
        return s;
    }

    private void OnEnded() => _lastResult = "── 大冒險結束 ──";

    private void OnGUI()
    {
        if (_controller == null) return;

        GUILayout.BeginArea(new Rect(_origin.x, _origin.y, 380, 520), GUI.skin.box);
        GUILayout.Label("<b>大冒險 Debug 面板</b>", RichLabel());

        var run = _controller.Run;
        var p = GameStatusService.Instance != null ? GameStatusService.Instance.Protagonist : null;

        if (run == null || run.IsEnded)
        {
            GUILayout.Label(run == null ? "尚未開始" : _lastResult);
            if (GUILayout.Button("開始大冒險", Big()))
                _controller.StartAdventure(_dungeon);
            GUILayout.EndArea();
            return;
        }

        // ── 狀態列 ──
        GUILayout.Space(4);
        GUILayout.Label($"地點：{run.Dungeon?.DungeonID}");
        int maxMoves = run.Dungeon != null ? run.Dungeon.MaxMoves : 0;
        GUILayout.Label($"行動次數：{run.MovesRemaining} / {maxMoves}    已散步：{run.ActionsTaken} 次");

        // 下一次散步的特色機率（把「一趟只出一次」規則算進去）
        if (run.Dungeon != null)
        {
            float nextChance = run.Dungeon.GetSpecialChance(run.ActionsTaken);
            if (run.Dungeon.SpecialOnlyOncePerRun && run.SpecialHappened) nextChance = 0f;
            GUILayout.Label($"下次特色機率：{nextChance:0}%   （已出過特色：{(run.SpecialHappened ? "是" : "否")}）");
        }
        if (p != null)
            GUILayout.Label($"壓力：{p.Stress}   社會性：{p.Sociality}   生活力：{p.LifePower}   $：{p.Money}");

        // ── 目前的牌 ──
        GUILayout.Space(4);
        if (run.CurrentCard != null)
        {
            float rate = run.CurrentCard.CalcSuccessRate(p);
            string kind = run.LastDrawWasSpecial ? "特色" : "普通";
            GUILayout.Label($"當前牌：{run.CurrentCard.CardID}（{kind}・{run.CurrentCard.Mode}）  成功率 {rate:0}%");
        }
        else
        {
            GUILayout.Label("當前牌：（尚未發牌）");
        }

        GUILayout.Space(4);
        GUILayout.Label(_lastResult);

        // ── 動作按鈕 ──
        GUILayout.Space(8);
        if (GUILayout.Button("發牌 + 翻牌（繼續前進）", Big())) _controller.DrawAndResolve();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("只發牌", Big())) _controller.DrawNext();
        GUI.enabled = run.CurrentCard != null;
        if (GUILayout.Button("只翻牌", Big())) _controller.Flip();
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"行動-1（剩 {run.MovesRemaining}）", Big())) _controller.AddMoves(-1);
        if (GUILayout.Button("回家", Big())) _controller.GoHome();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private static GUIStyle _rich;
    private static GUIStyle RichLabel()
    {
        if (_rich == null) _rich = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
        return _rich;
    }

    private static GUIStyle _big;
    private static GUIStyle Big()
    {
        if (_big == null) _big = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 34 };
        return _big;
    }
}
