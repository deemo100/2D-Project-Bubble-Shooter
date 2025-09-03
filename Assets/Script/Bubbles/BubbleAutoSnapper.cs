using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BubblePiece))]
[RequireComponent(typeof(Rigidbody2D))]
public class BubbleAutoSnapper : MonoBehaviour
{
    [SerializeField] private float mMinSpeedToSnap = 1.0f;
    [SerializeField] private float mSnapMaxDistFactor = 1.10f; // 후보 셀 중심까지 허용 거리(셀 반지름 × 계수)
    [SerializeField] private bool mbUseRing2Fallback = false;  // 반경2까지 확장할지(기본 OFF)
    [SerializeField] private bool mbUseGlobalFallback = false; // 마지막 수단(전역 스캔)

    private BubblePiece mPiece;
    private Rigidbody2D mRb;
    private BubbleGrid mGrid;
    private bool mbSnapped;

    private readonly List<(int,int)> mTmpCells = new List<(int,int)>(); // GC 줄이기용

    private void Awake()
    {
        mPiece = GetComponent<BubblePiece>();
        mRb = GetComponent<Rigidbody2D>();
        #if UNITY_2023_1_OR_NEWER
        mGrid = Object.FindAnyObjectByType<BubbleGrid>();
        #else
        mGrid = FindObjectOfType<BubbleGrid>();
        #endif
        if (mGrid == null) Debug.LogError("BubbleAutoSnapper: BubbleGrid를 찾지 못했습니다.");
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (mbSnapped || mPiece.State != EBubbleState.Dynamic) return;

        var other = c.collider.GetComponent<BubblePiece>();
        if (other == null) return; // 버블끼리만

        // ① 충돌 상대의 '이웃 6칸' 중에서만 후보 선정
        mGrid.GetEmptyNeighbors(other.GridX, other.GridY, mTmpCells);
        if (TrySnapFromCandidates(mTmpCells, mSnapMaxDistFactor)) return;

        // ② (선택) 반경2까지 확장
        if (mbUseRing2Fallback)
        {
            if (mGrid.TryFindNearestEmptyAround(other.GridX, other.GridY, transform.position, out int sx, out int sy))
            {
                if (WithinGate(sx, sy, mSnapMaxDistFactor * 1.1f)) { SnapToCell(sx, sy); return; }
            }
        }

        // ③ (선택) 전역 폴백
        if (mbUseGlobalFallback) TrySnapToNearestEmptyGlobal();
    }

    private void Update()
    {
        // 버블 사이에서 멈춰 떨 때 보정 스냅
        if (!mbSnapped && mPiece.State != EBubbleState.Dynamic)
            TrySnapWhenSlow();
    }

    private void TrySnapWhenSlow()
    {
        if (mbSnapped || mPiece.State != EBubbleState.Dynamic) return;
        if (mRb.linearVelocity.sqrMagnitude > mMinSpeedToSnap * mMinSpeedToSnap) return;

        // 가장 가까운 '점유 버블'을 찾아 그 이웃 6칸에서만 스냅
        var near = FindClosestOccupied();
        if (near != null)
        {
            mGrid.GetEmptyNeighbors(near.GridX, near.GridY, mTmpCells);
            if (TrySnapFromCandidates(mTmpCells, mSnapMaxDistFactor)) return;

            if (mbUseRing2Fallback)
            {
                if (mGrid.TryFindNearestEmptyAround(near.GridX, near.GridY, transform.position, out int sx, out int sy))
                {
                    if (WithinGate(sx, sy, mSnapMaxDistFactor * 1.1f)) { SnapToCell(sx, sy); return; }
                }
            }
        }

        if (mbUseGlobalFallback) TrySnapToNearestEmptyGlobal();
    }

    // ---- Helper ----

    private bool TrySnapFromCandidates(List<(int x,int y)> candidates, float gateFactor)
    {
        if (candidates.Count == 0) return false;

        int bestX = -1, bestY = -1;
        float best = float.MaxValue;
        Vector2 p = transform.position;

        // 6칸 중 '현재 위치에 제일 가까운' 셀
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            Vector2 w = mGrid.CellToWorld(c.x, c.y);
            float d = (w - p).sqrMagnitude;
            if (d < best) { best = d; bestX = c.x; bestY = c.y; }
        }

        if (bestX < 0) return false;

        if (WithinGate(bestX, bestY, gateFactor))
        {
            SnapToCell(bestX, bestY);
            return true;
        }
        return false;
    }

    private bool WithinGate(int x, int y, float factor)
    {
        float gate = mGrid.CellRadius * factor;
        Vector2 target = mGrid.CellToWorld(x, y);
        return ((Vector2)transform.position - target).sqrMagnitude <= gate * gate;
    }

    private BubblePiece FindClosestOccupied()
    {
        float best = float.MaxValue;
        BubblePiece bestP = null;
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
        mRb.linearVelocity = Vector2.zero;
        mRb.angularVelocity = 0f;
        mPiece.SettleToCell(mGrid, x, y);
        mbSnapped = true;
        enabled = false; // 이중 스냅 방지
    }

    // 전역 폴백(가능하면 비활성 유지)
    private void TrySnapToNearestEmptyGlobal()
    {
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

        if (bestX >= 0 && WithinGate(bestX, bestY, mSnapMaxDistFactor * 1.2f))
            SnapToCell(bestX, bestY);
    }
}