/// <summary>女主角依 Awake 推算的互斥睡眠狀態；區間統一由 HeroineStatusModel 設定。</summary>
public enum HeroineSleepState
{
    DeepSleep = 0,
    SoundSleep = 1,
    Sleep = 2,
    HalfAwake = 3,
    Dazed = 4,
    Wake = 5
}
