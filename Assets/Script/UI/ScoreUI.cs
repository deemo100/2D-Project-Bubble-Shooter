using UnityEngine;
using TMPro;

// ScoreManager를 찾아와서 현재 점수를 TextMeshProUGUI에 표시하는 역할을 합니다.
public class ScoreUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TextMeshProUGUI m_scoreText;

    [Header("Logic")]
    [SerializeField]
    private ScoreManager m_scoreManager;

    private void Awake()
    {
        // 컴포넌트가 할당되지 않았으면 자동으로 찾아옵니다.
        if (m_scoreText == null)
        {
            m_scoreText = GetComponent<TextMeshProUGUI>();
        }

        // ScoreManager가 할당되지 않았으면 씬에서 찾아옵니다.
        if (m_scoreManager == null)
        {
            #if UNITY_2023_1_OR_NEWER
            m_scoreManager = FindAnyObjectByType<ScoreManager>();
            #else
            m_scoreManager = FindObjectOfType<ScoreManager>();
            #endif

            if (m_scoreManager == null)
            {
                Debug.LogError("ScoreUI: ScoreManager를 씬에서 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }
    }

    private void Update()
    {
        // 매 프레임 ScoreManager로부터 현재 점수를 가져와 UI 텍스트를 업데이트합니다.
        if (m_scoreManager != null && m_scoreText != null)
        {
            m_scoreText.text = "Score: " + m_scoreManager.CurrentScore;
        }
    }
}
