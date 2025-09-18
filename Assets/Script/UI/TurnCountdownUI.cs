using UnityEngine;
using TMPro;

public class TurnCountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mCountdownText;

    void Update()
    {
        if (GameLoopManager.Instance != null && !GameLoopManager.Instance.IsGameOver)
        {
            // GameLoopManager로부터 턴 정보를 가져와 남은 턴 수를 계산
            int turnsRemaining = GameLoopManager.Instance.TurnsPerDrop - GameLoopManager.Instance.TurnsTaken;
            mCountdownText.text = $"Drop:{turnsRemaining}";
        }
        else if (mCountdownText.text != "")
        {
            // 게임오버 시 텍스트를 비움
            mCountdownText.text = "";
        }
    }
}
