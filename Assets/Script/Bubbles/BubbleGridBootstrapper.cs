using UnityEngine;

public class BubbleGridBootstrapper : MonoBehaviour
{
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private GameObject mPrefab; 
    [SerializeField] private int mRows = 5; // 채울 줄 수

    // 스폰에 사용할 색 목록
    [SerializeField] private EBubbleColor[] mSpawnColors =
        { EBubbleColor.Red, EBubbleColor.Blue, EBubbleColor.Green, EBubbleColor.Yellow, EBubbleColor.Purple };
    
    private void Start()
    {
        int rows = Mathf.Min(mRows, mGrid.Height);

        for (int y = 0; y < rows; y++)
        for (int x = 0; x < mGrid.Width; x++)
        {
            var go = Instantiate(mPrefab);
            var piece = go.GetComponentInChildren<BubblePiece>();
            if (piece == null)
            {
                Debug.LogError("BubbleGridBootstrapper: 프리팹(또는 자식)에 BubblePiece가 없습니다.");
                Destroy(go);
                continue;
            }

            piece.SetColor(RandomColor());
            mGrid.TryPlace(piece, x, y);   // TryPlace 내부에서 BindGrid 호출되게 해둔 버전
        }
    }
    
    private EBubbleColor RandomColor()
    {
        if (mSpawnColors == null || mSpawnColors.Length == 0)
            return EBubbleColor.Red;
        int i = Random.Range(0, mSpawnColors.Length);
        return mSpawnColors[i];
    }
}