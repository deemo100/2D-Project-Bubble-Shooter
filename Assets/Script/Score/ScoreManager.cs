using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private float mComboWindow = 3f;

    private int mScore;
    private int mCombo;
    private float mLastEventTime = -999f;
    private bool mbInitialized;

    public int CurrentScore => mScore;
    public int CurrentCombo => mCombo;

    private void Awake()
    {
        ScoreEventBus.OnScore += OnScoreEvent;
        mbInitialized = true;
    }

    private void OnDestroy() { ScoreEventBus.OnScore -= OnScoreEvent; }

    private void OnScoreEvent(SScoreParams p)
    {
        if (!mbInitialized) return;

        float now = Time.time;
        if (now - mLastEventTime <= mComboWindow) mCombo++;
        else mCombo = 0;
        mLastEventTime = now;

        int basePer = (p.Type == EScoreEventType.Pop) ? 10 : 15;
        int baseSum = basePer * p.Count;
        float mult = 1f + 0.2f * Mathf.Max(0, mCombo);

        mScore += Mathf.RoundToInt(baseSum * mult);
    }

    public void ResetScore()
    {
        mScore = 0;
        mCombo = 0;
        mLastEventTime = -999f;
    }
}