// 모든 버블 공통 상태
public enum EBubbleState
{
    Static,   // 그리드에 정착(물리 OFF)
    Dynamic,  // 발사/이동 중(물리 ON)
    Falling   // 연결 끊겨 낙하 중(물리 ON)
}