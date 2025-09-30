// BubblePiece.cs
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]

public class BubblePiece : MonoBehaviour
{
    public event System.Action OnResolved;

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
    public bool IsBomb = false;
    public bool IsRocket = false;

    public void SetAsRocket()
    {
        IsRocket = true;
        GetComponent<CircleCollider2D>().isTrigger = true; // 트리거로 설정하여 관통 가능하게 함
    }
    
    
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
            case EBubbleColor.Stone:  return new Color(0.5f, 0.5f, 0.5f); // Stone 색상은 회색
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

    public void DetachFromGrid(bool isPopped = false, Vector2? clusterCenter = null)
    {
        if (!mbBoundToGrid) return;
        mbBoundToGrid = false;
        mGrid.RemoveAt(mX, mY);
        mState = EBubbleState.Falling;
        SetPhysicsEnabled(true);
        mRb.gravityScale = 1f;        // 낙하시 중력 1

        if (isPopped)
        {
            // 터져서 떨어지는 버블은 다른 버블을 통과하도록 트리거로 만듭니다.
            GetComponent<CircleCollider2D>().isTrigger = true;

            // 렌더링 순서를 높여 다른 버블보다 앞에 보이게 합니다.
            if (mRenderer) mRenderer.sortingOrder = 1;

            // 폭발 연출: 클러스터 중심으로부터 바깥으로 흩어지는 힘을 가합니다.
            if (clusterCenter.HasValue)
            {
                Vector2 direction = (((Vector2)transform.position - clusterCenter.Value).normalized + Vector2.up * 0.5f).normalized;
                // 버블이 정확히 중심에 있을 경우, 임의의 방향으로 힘을 줍니다.
                if (direction.sqrMagnitude < 0.001f)
                {
                    direction = Random.insideUnitCircle.normalized;
                }

                // 이 값들은 인스펙터에서 조절 가능하게 만들거나, 테스트하며 튜닝할 수 있습니다.
                float scatterForce = 2.5f;
                float randomTorque = 10f;

                mRb.AddForce(direction * scatterForce, ForceMode2D.Impulse);
                mRb.AddTorque(Random.Range(-randomTorque, randomTorque));
            }
        }
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
        // 로켓 버블이 벽과 충돌했을 때만 처리
        if (IsRocket)
        {
            var otherBubble = c.collider.GetComponent<BubblePiece>();
            if (otherBubble == null) // 버블이 아닌 다른 오브젝트(벽 등)와 충돌
            {
                // 충돌한 오브젝트의 레이어가 'Wall' 레이어인지 확인
                if (c.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    Destroy(gameObject); // 로켓 자신을 파괴
                    return;
                }
            }
            // 로켓은 버블과 충돌 시 트리거로 처리되므로, 이 부분은 실행되지 않음
            // 만약 여기에 도달했다면, 로켓이 버블과 비트리거 충돌한 경우인데, 이는 의도치 않은 동작
        }

        // 일반 버블 또는 로켓이 아닌 버블의 충돌 처리 (기존 로직)
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
            if (mGrid != null) mGrid.GridConnectivityCheckAfterWreck();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 로켓 버블이 다른 버블을 관통하며 터뜨리는 처리
        if (IsRocket)
        {
            var otherBubble = other.GetComponent<BubblePiece>();
            if (otherBubble != null)
            {
                // 충돌한 버블을 터뜨립니다.
                otherBubble.DetachFromGrid(true);
                // 로켓은 계속 진행합니다. (Destroy(gameObject) 호출 안 함)
            }
        }
    }

    // 정착(Shot 끝) 호출: 근접한 빈 셀에 스냅
    public void SettleToCell(BubbleGrid grid, int x, int y)
    {
        if (grid.TryPlace(this, x, y))
        {
            OnResolved?.Invoke();

            if (IsBomb)
            {
                Debug.Log("Bomb landed! Detonating with wider range...");
                var cellsToPop = new System.Collections.Generic.HashSet<(int, int)>();
                var ring1_neighbors = new System.Collections.Generic.List<(int, int)>();
                var ring2_neighbors = new System.Collections.Generic.List<(int, int)>();

                // 1. 폭탄 자신의 셀 추가
                cellsToPop.Add((x, y));

                // 2. 1차 이웃들 추가
                grid.GetAllNeighbors(x, y, ring1_neighbors);
                foreach (var neighbor in ring1_neighbors)
                {
                    cellsToPop.Add(neighbor);
                }

                // 3. 2차 이웃들(1차 이웃의 이웃들) 추가
                foreach (var neighbor in ring1_neighbors)
                {
                    grid.GetAllNeighbors(neighbor.Item1, neighbor.Item2, ring2_neighbors);
                    foreach (var neighbor_of_neighbor in ring2_neighbors)
                    {
                        cellsToPop.Add(neighbor_of_neighbor);
                    }
                }

                Vector2 bombPosition = grid.CellToWorld(x, y);

                foreach (var cell in cellsToPop)
                {
                    var bp = grid.Get(cell.Item1, cell.Item2);
                    if (bp != null)
                    {
                        bp.DetachFromGrid(true, bombPosition);
                    }
                }

                ScoreEventBus.Publish(new SScoreParams { Type = EScoreEventType.Pop, Count = cellsToPop.Count, Combo = 0 });
                grid.GridConnectivityCheckAfterWreck();

                // 폭탄을 사용했으므로 연속 팝 카운트는 초기화
                if (GameLoopManager.Instance != null)
                {
                    GameLoopManager.Instance.RecordPop(false);
                }
                return; // 폭탄 로직 후 일반 로직 실행 방지
            }

            // --- 일반 버블 로직 ---
            // 자기 자신은 Stone 타입이 아니므로, 색상 매칭 검사만 수행
            if (mColor == EBubbleColor.Stone) return;

            var same = new System.Collections.Generic.List<(int,int)>();
            grid.FloodSameColor(x, y, mColor, same);

            bool didPop = same.Count >= 3;

            if (didPop)
            {
                // --- 장애물 버블 변신 로직 ---
                var neighborsOfPoppedCluster = new System.Collections.Generic.HashSet<(int, int)>();
                var individualNeighbors = new System.Collections.Generic.List<(int, int)>();
                foreach (var poppedCell in same)
                {
                    grid.GetAllNeighbors(poppedCell.Item1, poppedCell.Item2, individualNeighbors);
                    foreach (var neighbor in individualNeighbors)
                    {
                        neighborsOfPoppedCluster.Add(neighbor);
                    }
                }

                EBubbleColor poppedColor = mColor; // 터진 버블들의 색상

                foreach (var neighborCell in neighborsOfPoppedCluster)
                {
                    var neighborPiece = grid.Get(neighborCell.Item1, neighborCell.Item2);
                    // 이웃이 Stone 버블이면, 터진 버블의 색상으로 변신시킴
                    if (neighborPiece != null && neighborPiece.Color == EBubbleColor.Stone)
                    {
                        neighborPiece.SetColor(poppedColor);
                    }
                }
                // --- 변신 로직 끝 ---

                // 6개 이상 터뜨렸을 때 로켓 아이템 지급
                if (same.Count >= 6)
                {
                    Debug.Log("6+ pop! Rocket awarded.");
                    if (ItemManager.Instance != null) ItemManager.Instance.AddItem(EItemType.Rocket);
                }

                // 1. 폭발 중심점 계산
                Vector2 clusterCenter = Vector2.zero;
                foreach (var c in same)
                {
                    clusterCenter += grid.CellToWorld(c.Item1, c.Item2);
                }
                clusterCenter /= same.Count;

                // 2. 각 버블을 폭발 중심으로부터 흩어지게 함
                foreach (var c in same)
                {
                    var bp = grid.Get(c.Item1, c.Item2);
                    if (bp != null)
                    {
                        bp.DetachFromGrid(true, clusterCenter);
                    }
                }
                ScoreEventBus.Publish(new SScoreParams { Type = EScoreEventType.Pop, Count = same.Count, Combo = 0 });

                // Pop 후 연결성 검사로 낙하 처리
                grid.GridConnectivityCheckAfterWreck();
            }
            
            // 연속 팝 기록
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.RecordPop(didPop);
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

    private void OnDestroy()
    {
        // 로켓이 파괴될 때(예: 벽에 부딪혔을 때) 연결성 검사를 수행합니다.
        // 이렇게 하면 로켓으로 인해 연결이 끊어진 버블들이 즉시 떨어집니다.
        if (IsRocket)
        {
            // mGrid는 발사된 버블에 바인딩되어 있지 않으므로, 씬에서 직접 찾아야 합니다.
            #if UNITY_2023_1_OR_NEWER
            var grid = FindAnyObjectByType<BubbleGrid>();
            #else
            var grid = FindObjectOfType<BubbleGrid>();
            #endif
            
            if (grid != null)
            {
                // 로켓의 OnTriggerEnter2D에서 버블들이 제거된 후,
                // 남아있는 버블들의 연결 상태를 검사합니다.
                grid.GridConnectivityCheckAfterWreck();
            }
        }
        
        OnResolved?.Invoke();
    }
}