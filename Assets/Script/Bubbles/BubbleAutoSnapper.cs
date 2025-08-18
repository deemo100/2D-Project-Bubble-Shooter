using UnityEngine;

[RequireComponent(typeof(BubblePiece))]
[RequireComponent(typeof(Rigidbody2D))]
public class BubbleAutoSnapper : MonoBehaviour
{
    [SerializeField] private float mMinSpeedToSnap = 1.0f;
    [SerializeField] private bool mbUseGlobalFallback = false; // 기본 OFF
    
    private BubblePiece mPiece;
    private Rigidbody2D mRb;
    private BubbleGrid mGrid;
    private bool mbSnapped = false;

    private void Awake()
    {
        mPiece = GetComponent<BubblePiece>();
        mRb = GetComponent<Rigidbody2D>();
        mGrid = FindObjectOfType<BubbleGrid>();
    }
    
    private void OnCollisionEnter2D(Collision2D c)
    {
        if (mbSnapped || mPiece.State != EBubbleState.Dynamic) return;

        var other = c.collider.GetComponent<BubblePiece>();
        if (other == null) return; // 버블에 닿을 때만

        // 1) 충돌한 버블의 이웃(반경1~2)에서만 스냅 시도 ← 메인 경로
        if (mGrid.TryFindNearestEmptyAround(other.GridX, other.GridY, transform.position, out int sx, out int sy))
        {
            SnapToCell(sx, sy);
            return;
        }

        // 2) (드문 케이스) 이웃에 빈칸이 없을 때만 전역 폴백 사용
        if (mbUseGlobalFallback)
            TrySnapToNearestEmptyGlobal();
    }
    
    private void Update()
    {
        if (!mbSnapped && mPiece.State == EBubbleState.Dynamic)
            TrySnapWhenSlow(); // 버블 사이에서 멈췄을 때 보정 스냅
    }
    
    private void TrySnapWhenSlow()
    {
        if (mbSnapped || mPiece.State != EBubbleState.Dynamic) return;
        if (mRb.linearVelocity.sqrMagnitude > mMinSpeedToSnap * mMinSpeedToSnap) return;

        // 가장 가까운 점유 버블을 찾아 그 이웃에서 스냅
        var near = FindClosestOccupied();
        if (near != null &&
            mGrid.TryFindNearestEmptyAround(near.GridX, near.GridY, transform.position, out int sx, out int sy))
        {
            SnapToCell(sx, sy);
            return;
        }

        if (mbUseGlobalFallback)
            TrySnapToNearestEmptyGlobal();
    }
    
    
    private BubblePiece FindClosestOccupied()
    {
        float best = float.MaxValue;
        BubblePiece bestP = null;
        // 간단 스캔: 그리드 전수 (필요하면 영역 제한·캐시화 가능)
        for (int y = 0; y < mGrid.Height; y++)
        for (int x = 0; x < mGrid.Width; x++)
        {
            var p = mGrid.Get(x, y);
            if (p == null) continue;
            float d = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < best) { best = d; bestP = p; }
        }
        return bestP;
    }
    
    private void SnapToCell(int x, int y)
    {
        // 정착: 물리 속도 정지 → 그리드 배치(내부에서 BindGrid 호출해야 함)
        mRb.linearVelocity = Vector2.zero;
        mRb.angularVelocity = 0f;
        mPiece.SettleToCell(mGrid, x, y);
        mbSnapped = true;
    }
    
    private void TrySnapToNearestEmptyGlobal() // ← 기존 함수, 폴백 전용
    {
        if (mGrid == null) return;

        int bestX = -1, bestY = -1;
        float best = float.MaxValue;
        Vector2 p = transform.position;

        for (int y = 0; y < mGrid.Height; y++)
        for (int x = 0; x < mGrid.Width; x++)
        {
            if (mGrid.Get(x, y) != null) continue;
            Vector2 w = mGrid.CellToWorld(x, y);
            float d = (w - p).sqrMagnitude;
            if (d < best) { best = d; bestX = x; bestY = y; }
        }

        if (bestX >= 0) SnapToCell(bestX, bestY);
    }
}