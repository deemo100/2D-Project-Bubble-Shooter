using UnityEngine;
using UnityEngine.UI;

public class ShooterUI : MonoBehaviour
{
    [Header("참조 연결")]
    [SerializeField] private Shooter mShooter;
    [SerializeField] private Image mCurrentImage;
    [SerializeField] private Image mNextImage;

    private void Awake()
    {
        if (mShooter == null)
        {
            Debug.LogError("Shooter 참조가 없습니다!");
            enabled = false;
            return;
        }

        // Shooter의 이벤트에 구독하여 UI 업데이트 함수를 연결
        mShooter.OnBubbleQueueChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
        if (mShooter != null)
        {
            mShooter.OnBubbleQueueChanged -= UpdateUI;
        }
    }

    private void Start()
    {
        // 시작할 때 첫 UI 상태를 즉시 반영
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Shooter로부터 현재/다음 버블 색상 정보를 가져옴
        EBubbleColor current = mShooter.CurrentBubbleColor;
        EBubbleColor next = mShooter.NextBubbleColor;

        // UI 이미지의 색상과 활성 상태를 업데이트
        if (mCurrentImage != null)
        {
            mCurrentImage.color = BubbleColorUtil.GetColor(current);
            mCurrentImage.enabled = true; // 항상 보이게
        }

        if (mNextImage != null)
        {
            mNextImage.color = BubbleColorUtil.GetColor(next);
            mNextImage.enabled = true; // 항상 보이게
        }
    }
}
