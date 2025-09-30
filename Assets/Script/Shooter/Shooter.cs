using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.EventSystems;

public class Shooter : MonoBehaviour
{
    [Header("Game Logic")]
    [SerializeField] private GameLoopManager mGameLoopManager;
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private GameObject mPrefab;
    [SerializeField] private float mShotSpeed = 12f;
    [SerializeField] private int mQueueSize = 2; // 대기열 크기

    [Header("Aiming")]
    [SerializeField] private float mMinAngle = 30f; // 최소 조준 각도
    [SerializeField] private float mMaxAngle = 150f; // 최대 조준 각도

    [Header("Trajectory Prediction")]
    [SerializeField] private LineRenderer mTrajectoryLine;
    [SerializeField] private int mMaxBounce = 1; // 예측할 최대 반사 횟수

    // --- Public API for UI ---
    public EBubbleColor CurrentBubbleColor => mBubbleQueue.Count > 0 ? mBubbleQueue[0] : default;
    public EBubbleColor NextBubbleColor => mBubbleQueue.Count > 1 ? mBubbleQueue[1] : default;
    public event Action OnBubbleQueueChanged;
    public event Action OnShotFired;

    private readonly List<EBubbleColor> mBubbleQueue = new List<EBubbleColor>();

    private void Start()
    {
        InitializeQueue();
        if (mTrajectoryLine != null)
        {
            mTrajectoryLine.positionCount = 0;
            mTrajectoryLine.enabled = false;
        }
    }

    private void InitializeQueue()
    {
        mBubbleQueue.Clear();
        for (int i = 0; i < mQueueSize; i++)
        {
            mBubbleQueue.Add(PickPlayableColor());
        }

        OnBubbleQueueChanged?.Invoke();
    }

    private bool m_isShotInProgress = false;

    private void Update()
    {
        if (mGameLoopManager != null && mGameLoopManager.IsGameOver)
        {
            if (mTrajectoryLine != null && mTrajectoryLine.enabled)
            {
                mTrajectoryLine.enabled = false;
            }
            return;
        }

        // --- 조준 로직 ---
        Vector3 mousePos = Input.mousePosition;
        if (float.IsInfinity(mousePos.x) || float.IsInfinity(mousePos.y))
        {
            return;
        }

        var mp = Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 rawDir = ((Vector2)mp - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(rawDir.y, rawDir.x) * Mathf.Rad2Deg;

        // 각도 제한
        angle = Mathf.Clamp(angle, mMinAngle, mMaxAngle);

        // 제한된 각도로부터 최종 방향 벡터를 다시 계산 (보이는 것과 실제 발사 방향을 일치시키기 위함)
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        // 슈터 회전 적용
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        // --- 궤적 표시 로직 ---
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (mTrajectoryLine != null)
            {
                // 현재 버블 색상을 가져와 라인 렌더러의 색상으로 설정
                Color bubbleColor = BubbleColorUtil.GetColor(CurrentBubbleColor);
                mTrajectoryLine.startColor = bubbleColor;
                mTrajectoryLine.endColor = bubbleColor;

                mTrajectoryLine.enabled = true;
                PredictAndDrawTrajectory(dir); // 제한된 방향(dir)으로 궤적을 그림
            }
        }
        else
        {
            if (mTrajectoryLine != null)
            {
                mTrajectoryLine.enabled = false;
            }
        }

        // --- 발사 로직 ---
        if (!m_isShotInProgress && Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (mBubbleQueue.Count == 0) return;

            m_isShotInProgress = true;

            var go = Instantiate(mPrefab, transform.position, Quaternion.identity);
            var piece = go.GetComponentInChildren<BubblePiece>();
            if (piece == null)
            {
                Debug.LogError("BubblePiece가 프리팹(또는 자식)에 없습니다.");
                Destroy(go);
                m_isShotInProgress = false;
                return;
            }

            System.Action onResolvedHandler = null;
            onResolvedHandler = () =>
            {
                m_isShotInProgress = false;
                piece.OnResolved -= onResolvedHandler;
            };
            piece.OnResolved += onResolvedHandler;

            if (GameLoopManager.Instance.IsBombArmed)
            {
                piece.IsBomb = true;
                GameLoopManager.Instance.ConsumeBomb();
            }
            else if (GameLoopManager.Instance.IsRocketArmed)
            {
                piece.SetAsRocket();
                GameLoopManager.Instance.ConsumeRocket();
            }
            else
            {
                piece.SetColor(CurrentBubbleColor);
            }

            piece.SetDynamicForShot(dir * mShotSpeed); // 제한된 방향(dir)으로 발사

            mBubbleQueue.RemoveAt(0);
            mBubbleQueue.Add(PickPlayableColor());

            OnBubbleQueueChanged?.Invoke();
            OnShotFired?.Invoke();
        }
    }

    private void PredictAndDrawTrajectory(Vector2 direction)
    {
        var points = new List<Vector2>();
        points.Add(transform.position);

        Vector2 currentPos = transform.position;
        Vector2 currentDir = direction;

        for (int i = 0; i <= mMaxBounce; i++)
        {
            // 시작점 바로 앞에서 레이를 쏴서 자기 자신과 충돌하는 것을 방지
            RaycastHit2D hit = Physics2D.Raycast(currentPos + currentDir * 0.1f, currentDir);
            if (hit.collider != null)
            {
                points.Add(hit.point);
                // 벽에 부딪혔고, 최대 반사 횟수에 도달하지 않았다면 경로를 계속 계산
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall") && i < mMaxBounce)
                {
                    currentPos = hit.point;
                    currentDir = Vector2.Reflect(currentDir, hit.normal);
                }
                else
                {
                    break; // 버블이나 다른 것에 부딪혔거나, 최대 반사 횟수에 도달하면 중단
                }
            }
            else
            {
                // 아무것도 맞지 않으면 긴 선을 그림
                points.Add(currentPos + currentDir * 30f);
                break;
            }
        }

        mTrajectoryLine.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            mTrajectoryLine.SetPosition(i, points[i]);
        }
    }

    private EBubbleColor PickPlayableColor()
    {
        var available = new HashSet<EBubbleColor>();
        mGrid.CollectColorsPresent(available);
        available.Remove(EBubbleColor.Stone); // 발사할 색상에서 Stone(장애물)은 제외

        if (available.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, available.Count);
            return available.ElementAt(idx);
        }

        // 그리드에 플레이 가능한 색이 하나도 없을 경우(예: Stone 버블만 남은 경우) 대비
        // 기본 색상들 중에서 랜덤으로 하나를 선택하여 발사
        var fallbackColors = new List<EBubbleColor> 
            { EBubbleColor.Red, EBubbleColor.Blue, EBubbleColor.Green, EBubbleColor.Yellow };
        return fallbackColors[UnityEngine.Random.Range(0, fallbackColors.Count)];
    }
}