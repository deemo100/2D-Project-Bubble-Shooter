using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RocketUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button mRocketButton;
    [SerializeField] private TextMeshProUGUI mRocketCountText;

    private void Start()
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogError("ItemManager 인스턴스가 없습니다!");
            gameObject.SetActive(false);
            return;
        }

        // 이벤트 구독
        ItemManager.Instance.OnItemCountChanged += UpdateUI;
        mRocketButton.onClick.AddListener(UseRocket);

        // 초기 UI 상태 설정
        UpdateUI(EItemType.Rocket, ItemManager.Instance.GetItemCount(EItemType.Rocket));
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemCountChanged -= UpdateUI;
        }
        
        if (mRocketButton != null)
        {
            mRocketButton.onClick.RemoveListener(UseRocket);
        }
    }

    private void UpdateUI(EItemType type, int count)
    {
        if (type == EItemType.Rocket)
        {
            bool hasRockets = count > 0;
            mRocketButton.gameObject.SetActive(hasRockets);
            mRocketCountText.text = count.ToString();
        }
    }

    private void UseRocket()
    {
        GameLoopManager.Instance.ArmRocket();
    }
}
