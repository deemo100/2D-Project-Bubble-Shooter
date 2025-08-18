using System.Collections.Generic;
using UnityEngine;

public class BubbleGrid : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int mWidth = 9;
    [SerializeField] private int mHeight = 12;

    [Header("Layout")]
    [SerializeField] private float mCellRadius = 0.5f;   // 버블 반지름(육각 격자 기준)
    [SerializeField] private Vector2 mOrigin = Vector2.zero; // 월드 기준 오프셋
    
    private BubblePiece[,] mCells;

    public int Width => mWidth;
    public int Height => mHeight;

    private void Awake()
    {
        mCells = new BubblePiece[mWidth, mHeight];
    }

    public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < mWidth && y < mHeight;

    public bool TryPlace(BubblePiece p, int x, int y)
    {
        if (!IsInside(x, y) || mCells[x, y] != null) return false;
        mCells[x, y] = p;
        p.BindGrid(this, x, y);   // ← 필수: 바인딩 + 위치 스냅 + 물리 OFF
        return true;
    }

    public void RemoveAt(int x, int y)
    {
        if (IsInside(x, y)) mCells[x, y] = null;
    }

    public BubblePiece Get(int x, int y)
    {
        if (!IsInside(x, y)) return null;
        return mCells[x, y];
    }

    public int CountAll()
    {
        int cnt = 0;
        for (int y = 0; y < mHeight; y++)
        for (int x = 0; x < mWidth; x++)
            if (mCells[x, y] != null) cnt++;
        return cnt;
    }

    public bool HasAnyAtRow(int row)
    {
        if (row < 0 || row >= mHeight) return false;
        for (int x = 0; x < mWidth; x++)
            if (mCells[x, row] != null) return true;
        return false;
    }
    public Vector2 CellToWorld(int x, int y)
    {
        // 가로 간격 = 지름(2r), 세로 간격 = r * sqrt(3)
        float xOffset = (y % 2 == 0) ? 0f : mCellRadius;
        float wx = mOrigin.x + (x * 2f * mCellRadius) + xOffset;
        float wy = mOrigin.y - (y * (mCellRadius * 1.73205080757f)); // sqrt(3) ≈ 1.732
        return new Vector2(wx, wy);
    }
    
    private static readonly (int dx, int dy)[] sEvenRow =
    {
        (-1,0),(1,0),(0,-1),(0,1),(-1,-1),(-1,1)
    };
    private static readonly (int dx, int dy)[] sOddRow =
    {
        (-1,0),(1,0),(0,-1),(0,1),(1,-1),(1,1)
    };

    public void GetNeighbors(int x, int y, List<(int nx,int ny)> results)
    {
        results.Clear();
        var dirs = (y % 2 == 0) ? sEvenRow : sOddRow;
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = x + dirs[i].dx;
            int ny = y + dirs[i].dy;
            if (IsInside(nx, ny) && mCells[nx, ny] != null)
                results.Add((nx, ny));
        }
    }
    
    public void FloodSameColor(int sx, int sy, EBubbleColor color, List<(int,int)> outList)
    {
        outList.Clear();
        var visited = new HashSet<(int,int)>();
        var q = new Queue<(int,int)>();
        q.Enqueue((sx, sy));
        visited.Add((sx, sy));

        var neigh = new List<(int,int)>();
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            outList.Add(c);

            GetNeighbors(c.Item1, c.Item2, neigh);
            foreach (var n in neigh)
            {
                var bp = Get(n.Item1, n.Item2);
                if (bp != null && bp.Color == color && !visited.Contains(n))
                {
                    visited.Add(n);
                    q.Enqueue(n);
                }
            }
        }
    }
    
    public void CollectDisconnected(List<(int,int)> outDisconnected)
    {
        outDisconnected.Clear();
        var visited = new HashSet<(int,int)>();
        var q = new Queue<(int,int)>();

        // 천장 라인에서 시작
        for (int x = 0; x < mWidth; x++)
        {
            if (mCells[x, 0] != null)
            {
                q.Enqueue((x, 0));
                visited.Add((x, 0));
            }
        }

        var neigh = new List<(int,int)>();
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            GetNeighbors(c.Item1, c.Item2, neigh);
            foreach (var n in neigh)
                if (!visited.Contains(n))
                {
                    visited.Add(n);
                    q.Enqueue(n);
                }
        }

        for (int y = 0; y < mHeight; y++)
        for (int x = 0; x < mWidth; x++)
            if (mCells[x, y] != null && !visited.Contains((x, y)))
                outDisconnected.Add((x, y));
    }
    
    public void CollectColorsPresent(System.Collections.Generic.HashSet<EBubbleColor> outSet)
    {
        outSet.Clear();
        for (int y = 0; y < mHeight; y++)
        for (int x = 0; x < mWidth; x++)
        {
            var p = mCells[x, y];
            if (p != null) outSet.Add(p.Color);
        }
    }
    
    public void GetEmptyNeighbors(int x, int y, System.Collections.Generic.List<(int,int)> results)
    {
        results.Clear();
        var dirs = (y % 2 == 0) ? sEvenRow : sOddRow;
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = x + dirs[i].dx;
            int ny = y + dirs[i].dy;
            if (IsInside(nx, ny) && mCells[nx, ny] == null)
                results.Add((nx, ny));
        }
    }

    // 충돌 지점 기준, (cx,cy) 주변에서 '가장 가까운 빈칸' 찾기 (반경 1 → 2 레벨)
    public bool TryFindNearestEmptyAround(int cx, int cy, Vector2 from, out int outX, out int outY)
    {
        outX = outY = -1;
        float best = float.MaxValue;
    
        var cand = new System.Collections.Generic.List<(int,int)>();
        GetEmptyNeighbors(cx, cy, cand);
        SelectNearest(cand, from, ref outX, ref outY, ref best);
    
        if (outX != -1) return true; // 반경1에서 찾았으면 끝
    
        // 반경 2까지 확장(BFS)
        var visited = new System.Collections.Generic.HashSet<(int,int)>();
        var q = new System.Collections.Generic.Queue<((int x,int y) cell, int depth)>();
        visited.Add((cx, cy));
        q.Enqueue(((cx, cy), 0));
    
        while (q.Count > 0)
        {
            var item = q.Dequeue();
            if (item.depth >= 2) continue;
    
            var neigh = new System.Collections.Generic.List<(int,int)>();
            GetNeighbors(item.cell.x, item.cell.y, neigh); // 점유된 이웃
            foreach (var n in neigh)
            {
                if (!visited.Add(n)) continue;
    
                // 이웃의 빈칸 후보만 모아서 평가
                GetEmptyNeighbors(n.Item1, n.Item2, cand);
                SelectNearest(cand, from, ref outX, ref outY, ref best);
    
                q.Enqueue((n, item.depth + 1));
            }
        }
    
        return outX != -1;
    
        // 로컬 함수: 가장 가까운 후보 선택
        void SelectNearest(System.Collections.Generic.List<(int,int)> list, Vector2 origin,
                           ref int bx, ref int by, ref float bdist)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                Vector2 w = CellToWorld(c.Item1, c.Item2);
                float d = (w - origin).sqrMagnitude;
                if (d < bdist)
                {
                    bdist = d; bx = c.Item1; by = c.Item2;
                }
            }
        }
    }
}