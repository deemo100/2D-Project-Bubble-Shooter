using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    public enum EGameState
    {
        Playing,
        GameOver
    }

    [SerializeField] private Shooter mShooter;
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private int mTurnsPerDrop = 5;

    private int mTurnsTaken = 0;
    private EGameState mState = EGameState.Playing;

    public bool IsGameOver => mState == EGameState.GameOver;

    private void Start()
    {
        if (mShooter != null)
        {
            mShooter.OnShotFired += OnShotFired;
        }
    }

    private void OnDestroy()
    {
        if (mShooter != null)
        {
            mShooter.OnShotFired -= OnShotFired;
        }
    }

    private void OnShotFired()
    {
        if (IsGameOver) return;

        mTurnsTaken++;
        if (mTurnsTaken >= mTurnsPerDrop)
        {
            mTurnsTaken = 0;
            if (mGrid != null)
            {
                mGrid.Descend();
            }
        }
    }

    public void TriggerGameOver()
    {
        if (IsGameOver) return;

        mState = EGameState.GameOver;
        Debug.Log("GAME OVER!");

        if (mGrid != null)
        {
            mGrid.ClearAllBubbles();
        }
        
        // 여기에 나중에 게임오버 UI를 표시하는 로직을 추가할 수 있습니다.
    }
}
