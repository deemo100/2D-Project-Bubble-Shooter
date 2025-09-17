using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BombUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button mBombButton;
    [SerializeField] private TextMeshProUGUI mBombCountText;

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
        mBombButton.onClick.AddListener(UseBomb);

        // 초기 UI 상태 설정
        UpdateUI(EItemType.Bomb, ItemManager.Instance.GetItemCount(EItemType.Bomb));
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemCountChanged -= UpdateUI;
        }
        
        if (mBombButton != null)
        {
            mBombButton.onClick.RemoveListener(UseBomb);
        }
    }

    private void UpdateUI(EItemType type, int count)
    {
        if (type == EItemType.Bomb)
        {
            bool hasBombs = count > 0;
            mBombButton.gameObject.SetActive(hasBombs);
            mBombCountText.text = count.ToString();
        }
    }

    private void UseBomb()
    {
        GameLoopManager.Instance.ArmBomb();
    }
}
