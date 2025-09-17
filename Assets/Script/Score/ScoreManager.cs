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

        int scoreToAdd = 0;
        if (p.Type == EScoreEventType.Pop)
        {
            // 팝: 기본 100점 + 3개 초과 버블당 50점
            int baseScore = 100;
            int bonusPerBubble = 50;
            int bonusBubbles = Mathf.Max(0, p.Count - 3);
            scoreToAdd = baseScore + (bonusBubbles * bonusPerBubble);
        }
        else // EScoreEventType.Drop
        {
            // 드랍: 기존 로직 유지 (개당 15점)
            int basePer = 15;
            scoreToAdd = basePer * p.Count;
        }

        // 콤보 보너스 적용
        float mult = 1f + 0.2f * Mathf.Max(0, mCombo);
        mScore += Mathf.RoundToInt(scoreToAdd * mult);
    }

    public void ResetScore()
    {
        mScore = 0;
        mCombo = 0;
        mLastEventTime = -999f;
    }
}