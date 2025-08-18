using System.Collections.Generic;
using System.Threading.Tasks;

public interface IBackendService
{
    Task InitializeAsync();
    Task<string> StartRunAsync(SRunStartData data);
    Task SubmitAsync(SRunEndData data);
    Task<List<SLeaderboardEntry>> GetLeaderboardAsync(string stageId, int limit);
}