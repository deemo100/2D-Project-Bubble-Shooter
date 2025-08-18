public class TimeOverEnd : IEndCondition
{
    private readonly System.Func<float> mRemainTimeGetter;
    public TimeOverEnd(System.Func<float> remainTimeGetter) { mRemainTimeGetter = remainTimeGetter; }
    public EEndStatus Evaluate() => mRemainTimeGetter() <= 0f ? EEndStatus.Lose : EEndStatus.None;
}