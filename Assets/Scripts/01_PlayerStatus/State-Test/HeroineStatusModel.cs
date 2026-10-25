namespace NHK.StateTest
{
    /// <summary>舊型別相容名稱；遊戲資料統一由 GameStatusService.Heroines 持有。</summary>
    [System.Obsolete("請使用正式 HeroineStatusModel 與 GameStatusService.GetHeroine。")]
    public class HeroineStatusModel : global::HeroineStatusModel
    {
        public HeroineStatusModel(HeroineStat stat, HeroineStatusConfig config) : base(stat, config) { }
    }
}
