using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering; // 선택: zTest 설정에 사용

public class BubbleGrid : MonoBehaviour
{
    [Header("Debug / Gizmos")]
    [SerializeField] private bool mbDrawGizmos = true;
    [SerializeField] private bool mbLabelCoords = false;
    [SerializeField] private bool mbDrawNeighbors = false;
    [SerializeField] private int mDebugX = -1;
    [SerializeField] private int mDebugY = -1;

    [SerializeField] private Color mEmptyColor = new Color(0f, 1f, 0f, 0.18f);
    [SerializeField] private Color mOccupiedColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color mOutlineColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField] private Color mNeighborLineColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    
    [Header("Grid Size")]
    [SerializeField] private int mWidth = 22;
    [SerializeField] private int mHeight = 12;

    [Header("Layout")]
    [SerializeField] private float mCellRadius = 0.5f;   // 버블 반지름(육각 격자 기준)
    [SerializeField] private Vector2 mOrigin = Vector2.zero; // 월드 기준 오프셋

    [Header("Game Loop")]
    [SerializeField] private GameLoopManager mGameLoopManager;
    [SerializeField] private int mGameOverRow = 11; // 12번째 줄

    // row 0이 홀수 오프셋인지 여부 (false: 짝수 기준, true: 홀수 기준)
    [SerializeField] private bool mbRowZeroOdd = false;
    
    private BubblePiece[,] mCells;

    public int Width => mWidth;
    public int Height => mHeight;
    public float CellRadius => mCellRadius;

    private void Awake()
    {
        mCells = new BubblePiece[mWidth, mHeight];
    }

    public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < mWidth && y < mHeight;

    public bool TryPlace(BubblePiece p, int x, int y)
    {
        Debug.Log($"BubbleGrid.TryPlace received coordinates: (x={x}, y={y})");
        if (!IsInside(x, y) || mCells[x, y] != null) return false;
        mCells[x, y] = p;
        p.BindGrid(this, x, y);

        if (y >= mGameOverRow)
        {
            Debug.Log("Game Over condition met in TryPlace at row " + y);
            if (mGameLoopManager != null)
            {
                Debug.Log("GameLoopManager reference is valid. Triggering game over.");
                mGameLoopManager.TriggerGameOver();
            }
            else
            {
                Debug.LogError("GameLoopManager reference on BubbleGrid is NULL! Please assign it in the Inspector.");
            }
        }
        
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
        int parity = mbRowZeroOdd ? 1 : 0;
        float xOffset = (((y + parity) % 2) == 0) ? 0f : mCellRadius;
        float wx = mOrigin.x + (x * 2f * mCellRadius) + xOffset;
        float wy = mOrigin.y - (y * (mCellRadius * 1.73205080757f)); // sqrt(3)
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

    public void GetNeighbors(int x, int y, System.Collections.Generic.List<(int nx,int ny)> results)
    {
        results.Clear();
        int parity = mbRowZeroOdd ? 1 : 0;
        var dirs = (((y + parity) % 2) == 0) ? sEvenRow : sOddRow;
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = x + dirs[i].dx;
            int ny = y + dirs[i].dy;
            if (IsInside(nx, ny) && mCells[nx, ny] != null)
                results.Add((nx, ny));
        }
    }

    public void GetAllNeighbors(int x, int y, System.Collections.Generic.List<(int nx, int ny)> results)
    {
        results.Clear();
        int parity = mbRowZeroOdd ? 1 : 0;
        var dirs = (((y + parity) % 2) == 0) ? sEvenRow : sOddRow;
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = x + dirs[i].dx;
            int ny = y + dirs[i].dy;
            if (IsInside(nx, ny))
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
        int parity = mbRowZeroOdd ? 1 : 0;
        var dirs = (((y + parity) % 2) == 0) ? sEvenRow : sOddRow;
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = x + dirs[i].dx;
            int ny = y + dirs[i].dy;
            if (IsInside(nx, ny) && mCells[nx, ny] == null)
                results.Add((nx, ny));
        }
    }

    public bool TryFindNearestEmptyAround(int cx, int cy, Vector2 from, out int outX, out int outY)
    {
        outX = outY = -1;
        float best = float.MaxValue;
    
        var cand = new System.Collections.Generic.List<(int,int)>();
        GetEmptyNeighbors(cx, cy, cand);
        SelectNearest(cand, from, ref outX, ref outY, ref best);
    
        if (outX != -1) return true;
    
        var visited = new System.Collections.Generic.HashSet<(int,int)>();
        var q = new System.Collections.Generic.Queue<((int x,int y) cell, int depth)>();
        visited.Add((cx, cy));
        q.Enqueue(((cx, cy), 0));
    
        while (q.Count > 0)
        {
            var item = q.Dequeue();
            if (item.depth >= 2) continue;
    
            var neigh = new System.Collections.Generic.List<(int,int)>();
            GetNeighbors(item.cell.x, item.cell.y, neigh);
            foreach (var n in neigh)
            {
                if (!visited.Add(n)) continue;
    
                GetEmptyNeighbors(n.Item1, n.Item2, cand);
                SelectNearest(cand, from, ref outX, ref outY, ref best);
    
                q.Enqueue((n, item.depth + 1));
            }
        }
    
        return outX != -1;
    
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

    [Header("Bubble Prefab")]
    [SerializeField] private GameObject mBubblePrefab;

    public void Descend()
    {
        mbRowZeroOdd = !mbRowZeroOdd;

        for (int y = Height - 1; y >= 1; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                var p = Get(x, y - 1);
                mCells[x, y] = p;
                if (p != null)
                {
                    p.BindGrid(this, x, y);
                    if (y >= mGameOverRow)
                    {
                        if (mGameLoopManager != null) mGameLoopManager.TriggerGameOver();
                        return;
                    }
                }
            }
        }

        for (int x = 0; x < mWidth; x++)
        {
            var go = Instantiate(mBubblePrefab, transform);
            var piece = go.GetComponentInChildren<BubblePiece>();

            if (piece == null)
            {
                Debug.LogError("BubbleGrid: P_Bubble prefab must have a BubblePiece component on itself or a child.");
                Destroy(go);
                mCells[x, 0] = null;
                continue;
            }

            var color = (EBubbleColor)UnityEngine.Random.Range(0, 4);
            piece.SetColor(color);
            mCells[x, 0] = piece;
            piece.BindGrid(this, x, 0);
        }
    }

    public void ClearAllBubbles()
    {
        Vector2 gridCenter = CellToWorld(mWidth / 2, mHeight / 2);

        for (int y = 0; y < mHeight; y++)
        {
            for (int x = 0; x < mWidth; x++)
            {
                if (mCells[x, y] != null)
                {
                    mCells[x, y].DetachFromGrid(true, gridCenter);
                }
            }
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!mbDrawGizmos) return;

        if (mCells == null || mCells.Length != mWidth * mHeight)
            mCells = new BubblePiece[mWidth, mHeight];

        Handles.zTest = CompareFunction.Always;

        for (int y = 0; y < mHeight; y++)
        for (int x = 0; x < mWidth; x++)
        {
            Vector2 wp = CellToWorld(x, y);

            bool bOccupied = mCells[x, y] != null;
            Handles.color = bOccupied ? mOccupiedColor : mEmptyColor;
            Handles.DrawSolidDisc(wp, Vector3.forward, mCellRadius * 0.95f);

            Handles.color = mOutlineColor;
            Handles.DrawWireDisc(wp, Vector3.forward, mCellRadius * 0.98f);

            if (mbLabelCoords)
            {
                var style = new GUIStyle(EditorStyles.miniBoldLabel);
                style.normal.textColor = Color.black;
                Handles.Label(wp + new Vector2(0f, mCellRadius * 0.1f), $"{x},{y}", style);
            }
        }

        if (mbDrawNeighbors && IsInside(mDebugX, mDebugY))
        {
            var neigh = new System.Collections.Generic.List<(int,int)>();
            GetNeighbors(mDebugX, mDebugY, neigh);

            Vector2 center = CellToWorld(mDebugX, mDebugY);
            Handles.color = mNeighborLineColor;
            foreach (var n in neigh)
            {
                Vector2 to = CellToWorld(n.Item1, n.Item2);
                Handles.DrawLine(center, to, 2f);
            }

            Handles.color = Color.red;
            Handles.DrawWireDisc(center, Vector3.forward, mCellRadius * 1.05f);
        }

        Handles.color = Color.magenta;
        Handles.DrawWireDisc(mOrigin, Vector3.forward, mCellRadius * 0.5f);
    }
#endif
}