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
    
    private void Update()
    {
        if (mGameLoopManager != null && mGameLoopManager.IsGameOver)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (mBubbleQueue.Count == 0) return;

            var go = Instantiate(mPrefab, transform.position, Quaternion.identity);
            var piece = go.GetComponentInChildren<BubblePiece>();
            if (piece == null)
            {
                Debug.LogError("BubblePiece가 프리팹(또는 자식)에 없습니다.");
                Destroy(go);
                return;
            }

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
