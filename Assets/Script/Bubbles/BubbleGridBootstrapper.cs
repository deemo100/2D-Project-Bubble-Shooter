using UnityEngine;
using System.Collections.Generic;
using System; // for System.StringSplitOptions

public class BubbleGridBootstrapper : MonoBehaviour
{
    // 인스펙터에서 문자-색상을 매핑하기 위한 구조체
    [System.Serializable]
    public struct ColorMapping
    {
        public char character;
        public EBubbleColor color;
    }

    [Header("Grid Setup")]
    [SerializeField] private BubbleGrid mGrid;
    [SerializeField] private GameObject mPrefab;

    [Header("Level Data (Recommended)")]
    [SerializeField] private TextAsset mLevelData; // <- 새로 추가: 레벨 데이터 파일
    [SerializeField] private ColorMapping[] mColorPalette; // <- 새로 추가: 문자-색상 매핑

    [Header("Fallback Random Generation")]
    [SerializeField] private int mRows = 5; // 채울 줄 수
    [SerializeField] private EBubbleColor[] mSpawnColors =
        { EBubbleColor.Red, EBubbleColor.Blue, EBubbleColor.Green, EBubbleColor.Yellow, EBubbleColor.Purple };

    private Dictionary<char, EBubbleColor> mColorMap;

    private void Awake()
    {
        // 인스펙터에서 설정한 팔레트로 딕셔너리 생성하여 빠른 조회를 위함
        mColorMap = new Dictionary<char, EBubbleColor>();
        if (mColorPalette == null) return;

        foreach (var mapping in mColorPalette)
        {
            if (!mColorMap.ContainsKey(mapping.character))
            {
                mColorMap.Add(mapping.character, mapping.color);
            }
        }
    }

    private void Start()
    {
        // 레벨 데이터 파일이 지정되었다면, 파싱하여 버블 생성
        if (mLevelData != null)
        {
            ParseLevelDataAndCreateBubbles();
        }
        // 파일이 없다면, 기존의 랜덤 생성 방식 사용
        else
        {
            Debug.LogWarning("mLevelData is not assigned in BubbleGridBootstrapper. Falling back to random generation.");
            CreateRandomBubbles();
        }
    }

    private void ParseLevelDataAndCreateBubbles()
    {
        // 텍스트 파일을 줄 단위로 분리
        string[] lines = mLevelData.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int y = 0; y < lines.Length; y++)
        {
            // 그리드의 높이를 초과하는 데이터는 무시
            if (y >= mGrid.Height)
            {
                Debug.LogWarning($"Level data has more rows ({lines.Length}) than grid height ({mGrid.Height}). Clamping.");
                break;
            }

            string line = lines[y];
            for (int x = 0; x < line.Length; x++)
            {
                // 그리드의 너비를 초과하는 데이터는 무시
                if (x >= mGrid.Width)
                {
                    if (y == 0) Debug.LogWarning($"Level data has more columns ({line.Length}) than grid width ({mGrid.Width}). Clamping.");
                    break;
                }

                char character = line[x];
                // 딕셔너리에서 문자에 해당하는 색상을 찾음
                if (mColorMap.TryGetValue(character, out EBubbleColor color))
                {
                    // 해당 위치에 버블 생성
                    CreateBubble(x, y, color);
                }
                // 매핑되지 않은 문자(예: '_' 또는 ' ')는 빈 공간으로 처리
            }
        }
    }

    // (폴백용) 기존 랜덤 생성 로직
    private void CreateRandomBubbles()
    {
        int rows = Mathf.Min(mRows, mGrid.Height);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < mGrid.Width; x++)
            {
                CreateBubble(x, y, RandomColor());
            }
        }
    }

    // 버블을 생성하고 그리드에 배치하는 공통 함수
    private void CreateBubble(int x, int y, EBubbleColor color)
    {
        var go = Instantiate(mPrefab);
        var piece = go.GetComponentInChildren<BubblePiece>();
        if (piece == null)
        {
            Debug.LogError("BubbleGridBootstrapper: 프리팹(또는 자식)에 BubblePiece가 없습니다.");
            Destroy(go);
            return;
        }

        piece.SetColor(color);
        mGrid.TryPlace(piece, x, y);
    }

    // (폴백용) 기존 랜덤 색상 선택 로직
    private EBubbleColor RandomColor()
    {
        if (mSpawnColors == null || mSpawnColors.Length == 0)
            return EBubbleColor.Red;
        int i = UnityEngine.Random.Range(0, mSpawnColors.Length);
        return mSpawnColors[i];
    }
}