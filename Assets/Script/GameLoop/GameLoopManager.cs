using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private ScoreManager mScore;
    [SerializeField] private MonoBehaviour mBackendBehaviour; // IBackendService 구현 컴포넌트

    [Header("Game Options")]
    [SerializeField] private string mStageId = "Stage_001";
    [SerializeField] private string mPlayerId = "Player_Local";
    [SerializeField] private bool mbUseTimeLimit = false;
    [SerializeField] private float mTimeLimit = 60f;

    private IBackendService mBackend;
    private readonly List<IEndCondition> mEnders = new List<IEndCondition>();
    private EGameState mState = EGameState.None;

    private string mRunId;
    private float mStartTime;
    private float mElapsed;
    private bool mbInitialized;

    public EGameState State => mState;
    public float RemainingTime => mbUseTimeLimit ? Mathf.Max(0f, mTimeLimit - mElapsed) : float.PositiveInfinity;

    private async void Start()
    {
        mBackend = mBackendBehaviour as IBackendService;
        if (mBackend == null)
        {
            // 씬에 LocalBackendService를 안 붙였으면 자동 생성
            var go = new GameObject("LocalBackendService");
            mBackend = go.AddComponent<LocalBackendService>();
        }

        await SafeInitBackend();
        BuildEndConditions();
        SetState(EGameState.Ready);
    }

    private async Task SafeInitBackend()
    {
        try { await mBackend.InitializeAsync(); }
        catch (System.Exception e) { Debug.LogWarning($"Backend init failed: {e.Message}"); }
        mbInitialized = true;
    }

    private void BuildEndConditions()
    {
        mEnders.Clear();
        mEnders.Add(new ClearAllBubblesEnd(mGrid));
        mEnders.Add(new BottomReachedEnd(mGrid));
        if (mbUseTimeLimit) mEnders.Add(new TimeOverEnd(() => RemainingTime));
    }

    private void Update()
    {
        if (mState != EGameState.Playing) return;

        mElapsed = Time.time - mStartTime;
        var status = EvaluateEnd();
        if (status != EEndStatus.None) EndGame(status);
    }

    private EEndStatus EvaluateEnd()
    {
        for (int i = 0; i < mEnders.Count; i++)
        {
            var s = mEnders[i].Evaluate();
            if (s != EEndStatus.None) return s;
        }
        return EEndStatus.None;
    }

    public async void StartGame()
    {
        if (mState != EGameState.Ready || !mbInitialized) return;

        mScore.ResetScore();
        mElapsed = 0f;
        mStartTime = Time.time;

        try
        {
            mRunId = await mBackend.StartRunAsync(new SRunStartData
            {
                PlayerId = mPlayerId,
                StageId = mStageId,
                StartTime = System.DateTime.UtcNow
            });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"StartRun failed: {e.Message}");
            mRunId = System.Guid.NewGuid().ToString("N");
        }

        SetState(EGameState.Playing);
    }

    private async void EndGame(EEndStatus status)
    {
        if (mState != EGameState.Playing) return;

        SetState(EGameState.GameOver);
        float playTime = Time.time - mStartTime;

        try
        {
            await mBackend.SubmitAsync(new SRunEndData
            {
                RunId = mRunId,
                PlayerId = mPlayerId,
                StageId = mStageId,
                Score = mScore.CurrentScore,
                Status = status,
                PlayTime = playTime,
                EndTime = System.DateTime.UtcNow
            });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Submit failed: {e.Message}");
        }

        Debug.Log($"GameOver | {status} | Score: {mScore.CurrentScore} | Time: {playTime:0.00}s");
    }

    private void SetState(EGameState s) { mState = s; }
}
