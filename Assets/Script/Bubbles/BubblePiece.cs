// BubblePiece.cs
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]

public class BubblePiece : MonoBehaviour
{
    [SerializeField] private EBubbleColor mColor;
    [SerializeField] private SpriteRenderer mRenderer; // <- 인스펙터에 연결 (없으면 GetComponent)
    
    [SerializeField] private float mWreckBreakSpeed = 3.0f;

    [SerializeField] private bool mbWreckOnlyWhenFalling = true; // 기본: 낙하 때만 레킹
    [SerializeField] private float mWreckGraceTime = 0.08f;      // 발사 직후 레킹 잠금 시간
    
    private Rigidbody2D mRb;
    private BubbleGrid mGrid;
    private int mX, mY;
    
    public int GridX => mX;
    public int GridY => mY;
    
    private EBubbleState mState = EBubbleState.Static;
    private bool mbBoundToGrid = false;
    
    private float mWreckUnlockTime = 0f;
    private bool bWreckLocked => Time.time < mWreckUnlockTime;
    
    public bool IsDynamic => mState == EBubbleState.Dynamic;
    public bool IsFalling => mState == EBubbleState.Falling;
    
    public EBubbleColor Color => mColor;
    public EBubbleState State => mState;
    
    
    public void SetColor(EBubbleColor color)
    {
        mColor = color;
        ApplyColorToRenderer();
    }

    private void ApplyColorToRenderer()
    {
        if (mRenderer == null) mRenderer = GetComponent<SpriteRenderer>();
        // 원하는 팔레트로 지정
        mRenderer.color = ColorFromEnum(mColor);
    }

    private static Color ColorFromEnum(EBubbleColor c)
    {
        // 팔레트
        switch (c)
        {
            case EBubbleColor.Red:    return new Color(0.95f, 0.25f, 0.25f);
            case EBubbleColor.Blue:   return new Color(0.30f, 0.55f, 0.95f);
            case EBubbleColor.Green:  return new Color(0.35f, 0.85f, 0.45f);
            case EBubbleColor.Yellow: return new Color(0.98f, 0.90f, 0.25f);
            case EBubbleColor.Purple: return new Color(0.70f, 0.45f, 0.95f);
            case EBubbleColor.Orange: return new Color(0.98f, 0.60f, 0.20f);
            default:                  return new Color(1, 1, 1);
        }
    }

    private void Awake()
    {
        mRb = GetComponent<Rigidbody2D>();
        ApplyColorToRenderer();   // <- 프리팹 설정 색도 반영
        SetPhysicsEnabled(false);
    }

    public void BindGrid(BubbleGrid grid, int x, int y)
    {
        mGrid = grid; mX = x; mY = y;
        mbBoundToGrid = true;
        mState = EBubbleState.Static;
        SetPhysicsEnabled(false);
        transform.position = grid.CellToWorld(x, y);
    }

    public void DetachFromGrid()
    {
        if (!mbBoundToGrid) return;
        mbBoundToGrid = false;
        mGrid.RemoveAt(mX, mY);
        mState = EBubbleState.Falling;
        SetPhysicsEnabled(true);
        mRb.gravityScale = 1f;        // 낙하시 중력 1
    }

    public void SetDynamicForShot(Vector2 velocity)
    {
        mbBoundToGrid = false;
        mState = EBubbleState.Dynamic;
        SetPhysicsEnabled(true);
        mRb.gravityScale = 0f;
        mRb.linearVelocity = velocity;
        mWreckUnlockTime = Time.time + mWreckGraceTime; // 발사 직후 레킹 금지
    }

    private void SetPhysicsEnabled(bool b)
    {
        mRb.isKinematic = !b;
        mRb.gravityScale = b ? 1f : 0f;
    }

    // 레킹 판정 가드 추가
    private void OnCollisionEnter2D(Collision2D c)
    {
        var other = c.collider.GetComponent<BubblePiece>();
        if (other == null) return;

        // 1) 발사/그레이스 구간이면 레킹 금지(스냅 우선)
        if (IsDynamic || bWreckLocked) return;
        if (other.IsDynamic) return;

        // 2) 낙하 관련 충돌이 아니면 레킹 금지
        if (mbWreckOnlyWhenFalling)
        {
            if (!(IsFalling || other.IsFalling)) return;
        }

        // 3) 여기서만 레킹 수행
        float rel = c.relativeVelocity.magnitude;
        if (rel >= mWreckBreakSpeed)
        {
            if (other.mbBoundToGrid) other.DetachFromGrid();
            GridConnectivityCheckAfterWreck();
        }
    }

    private void GridConnectivityCheckAfterWreck()
    {
        if (mGrid == null) return;

        var disconnected = new System.Collections.Generic.List<(int,int)>();
        mGrid.CollectDisconnected(disconnected);

        int dropCount = 0;
        foreach (var cell in disconnected)
        {
            var bp = mGrid.Get(cell.Item1, cell.Item2);
            if (bp != null)
            {
                bp.DetachFromGrid();
                dropCount++;
            }
        }

        if (dropCount > 0)
        {
            ScoreEventBus.Publish(new SScoreParams
            {
                Type = EScoreEventType.Drop,
                Count = dropCount,
                Combo = 0 // 콤보는 ScoreManager가 갱신
            });
        }
    }

    // 정착(Shot 끝) 호출: 근접한 빈 셀에 스냅
    public void SettleToCell(BubbleGrid grid, int x, int y)
    {
        if (grid.TryPlace(this, x, y))
        {
            // 매치3 검사
            var same = new System.Collections.Generic.List<(int,int)>();
            grid.FloodSameColor(x, y, mColor, same);
            if (same.Count >= 3)
            {
                foreach (var c in same)
                {
                    var bp = grid.Get(c.Item1, c.Item2);
                    if (bp != null) bp.Pop();
                }
                ScoreEventBus.Publish(new SScoreParams { Type = EScoreEventType.Pop, Count = same.Count, Combo = 0 });

                // Pop 후 연결성 검사로 낙하 처리
                GridConnectivityCheckAfterWreck();
            }
        }
        else
        {
            // 배치 실패 시 약간 위 셀을 재시도하는 등 보정 로직 필요
        }
    }

    private void Pop()
    {
        // 비주얼/사운드 후 파괴
        mGrid.RemoveAt(mX, mY);
        Destroy(gameObject);
    }
}
