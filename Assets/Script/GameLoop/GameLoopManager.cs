using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    public event System.Action OnGameOver;

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
    
    private int mConsecutivePops = 0;

    public bool IsGameOver => mState == EGameState.GameOver;
    public int TurnsPerDrop => mTurnsPerDrop;
    public int TurnsTaken => mTurnsTaken;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

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
    
    public void RecordPop(bool didPop)
    {
        if (didPop)
        {
            mConsecutivePops++;
            Debug.Log($"Consecutive pops: {mConsecutivePops}");
        }
        else
        {
            mConsecutivePops = 0;
            Debug.Log("Consecutive pops reset.");
        }

        if (mConsecutivePops >= 3)
        {
            Debug.Log("3 consecutive pops! Bomb awarded.");
            ItemManager.Instance.AddItem(EItemType.Bomb);
            mConsecutivePops = 0; // Reset after awarding
        }
    }

    public bool IsBombArmed { get; private set; }

    public void ArmBomb()
    {
        if (ItemManager.Instance.UseItem(EItemType.Bomb))
        {
            IsBombArmed = true;
            Debug.Log("Bomb Armed!");
        }
    }

    public void ConsumeBomb()
    {
        IsBombArmed = false;
        Debug.Log("Bomb Consumed.");
    }

    public bool IsRocketArmed { get; private set; }

    public void ArmRocket()
    {
        if (ItemManager.Instance.UseItem(EItemType.Rocket))
        {
            IsRocketArmed = true;
            Debug.Log("Rocket Armed!");
        }
    }

    public void ConsumeRocket()
    {
        IsRocketArmed = false;
        Debug.Log("Rocket Consumed.");
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
        
        OnGameOver?.Invoke();
    }
}