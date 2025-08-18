using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LocalBackendService : MonoBehaviour, IBackendService
{
    private static readonly List<SLeaderboardEntry> sBoard = new List<SLeaderboardEntry>();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task<string> StartRunAsync(SRunStartData data)
    {
        string runId = System.Guid.NewGuid().ToString("N");
        return Task.FromResult(runId);
    }

    public Task SubmitAsync(SRunEndData data)
    {
        sBoard.Add(new SLeaderboardEntry
        {
            PlayerId = data.PlayerId,
            StageId = data.StageId,
            Score = data.Score,
            Timestamp = System.DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task<List<SLeaderboardEntry>> GetLeaderboardAsync(string stageId, int limit)
    {
        var list = new List<SLeaderboardEntry>(sBoard);
        if (!string.IsNullOrEmpty(stageId))
            list = list.FindAll(e => e.StageId == stageId);
        list.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (list.Count > limit) list.RemoveRange(limit, list.Count - limit);
        return Task.FromResult(list);
    }
}