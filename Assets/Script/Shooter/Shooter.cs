using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Shooter : MonoBehaviour
{
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private GameObject mPrefab; 
    [SerializeField] private float mShotSpeed = 12f;

    private readonly HashSet<EBubbleColor> mAvailable = new HashSet<EBubbleColor>();

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var go = Instantiate(mPrefab, transform.position, Quaternion.identity);

            // 자식까지 포함하여 BubblePiece 검색
            var piece = go.GetComponentInChildren<BubblePiece>();
            if (piece == null)
            {
                Debug.LogError("BubblePiece가 프리팹(또는 자식)에 없습니다.");
                return;
            }

            piece.SetColor(PickPlayableColor());
            var mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = ((Vector2)mp - (Vector2)transform.position).normalized;
            piece.SetDynamicForShot(dir * mShotSpeed);
        }
    }
    
    private EBubbleColor PickPlayableColor()
    {
        // 그리드에 존재하는 색만 뽑아주면 '매치 불가' 상황 방지
        mGrid.CollectColorsPresent(mAvailable);
        if (mAvailable.Count > 0)
        {
            int idx = Random.Range(0, mAvailable.Count);
            return mAvailable.ElementAt(idx);
        }
        // 비어 있으면 아무 색이나
        return (EBubbleColor)Random.Range(0, System.Enum.GetValues(typeof(EBubbleColor)).Length);
    }
    
}
