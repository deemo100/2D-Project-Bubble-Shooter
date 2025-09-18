using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject mGameOverPanel; // 게임오버 UI들을 담고 있는 부모 Panel
    [SerializeField] private Button mRestartButton;   // 다시 시작 버튼

    private void Start()
    {
        // 시작할 때 게임오버 UI를 숨김
        if (mGameOverPanel != null)
        {
            mGameOverPanel.SetActive(false);
        }

        // GameLoopManager의 게임오버 이벤트에 구독
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.OnGameOver += ShowGameOverUI;
        }

        // 다시 시작 버튼에 리스너 추가
        if (mRestartButton != null)
        {
            mRestartButton.onClick.AddListener(RestartGame);
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 구독 해제
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.OnGameOver -= ShowGameOverUI;
        }

        if (mRestartButton != null)
        {
            mRestartButton.onClick.RemoveListener(RestartGame);
        }
    }

    private void ShowGameOverUI()
    {
        // 게임오버 UI를 활성화
        if (mGameOverPanel != null)
        {
            mGameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        // 현재 씬을 다시 로드하여 게임을 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
