using System;

public static class ScoreEventBus
{
    public static event Action<SScoreParams> OnScore;
    public static void Publish(in SScoreParams p) => OnScore?.Invoke(p);
}