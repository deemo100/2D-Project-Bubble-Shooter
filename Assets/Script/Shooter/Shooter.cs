using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class Shooter : MonoBehaviour
{
    [SerializeField] private GameLoopManager mGameLoopManager;
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private GameObject mPrefab; 
    [SerializeField] private float mShotSpeed = 12f;
    [SerializeField] private int mQueueSize = 2; // 대기열 크기

    // --- Public API for UI ---
    public EBubbleColor CurrentBubbleColor => mBubbleQueue.Count > 0 ? mBubbleQueue[0] : default;
    public EBubbleColor NextBubbleColor => mBubbleQueue.Count > 1 ? mBubbleQueue[1] : default;
    public event Action OnBubbleQueueChanged;
    public event Action OnShotFired;
    
    private readonly List<EBubbleColor> mBubbleQueue = new List<EBubbleColor>();

    private void Start()
    {
        InitializeQueue();
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
            return;
        }

        // 샷이 진행 중이 아닐 때만 발사 가능
        if (!m_isShotInProgress && Input.GetMouseButtonDown(0))
        {
            if (mBubbleQueue.Count == 0) return;

            m_isShotInProgress = true; // 발사 시작, 플래그 설정

            var go = Instantiate(mPrefab, transform.position, Quaternion.identity);
            var piece = go.GetComponentInChildren<BubblePiece>();
            if (piece == null)
            {
                Debug.LogError("BubblePiece가 프리팹(또는 자식)에 없습니다.");
                Destroy(go);
                m_isShotInProgress = false; // 발사 실패, 플래그 리셋
                return;
            }

            // C#의 클로저(closure) 기능을 사용하여, 생성된 piece 인스턴스에 대한 이벤트 핸들러를 등록합니다.
            // 이렇게 하면 여러 버블이 있어도 정확히 해당 버블의 OnResolved 이벤트에만 반응할 수 있습니다.
            System.Action onResolvedHandler = null;
            onResolvedHandler = () => {
                m_isShotInProgress = false;
                piece.OnResolved -= onResolvedHandler; // 이벤트 구독 해제 (메모리 누수 방지)
            };
            piece.OnResolved += onResolvedHandler;

            // 1. 대기열의 첫 버블 색상 사용
            piece.SetColor(CurrentBubbleColor);
            
            var mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = ((Vector2)mp - (Vector2)transform.position).normalized;
            piece.SetDynamicForShot(dir * mShotSpeed);

            // 2. 대기열 갱신
            mBubbleQueue.RemoveAt(0);
            mBubbleQueue.Add(PickPlayableColor());
            
            // 3. UI 갱신을 위한 이벤트 호출
            OnBubbleQueueChanged?.Invoke();

            OnShotFired?.Invoke();
        }
    }
    
    private EBubbleColor PickPlayableColor()
    {
        var available = new HashSet<EBubbleColor>();
        mGrid.CollectColorsPresent(available);
        if (available.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, available.Count);
            return available.ElementAt(idx);
        }
        return (EBubbleColor)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(EBubbleColor)).Length);
    }
}
