using UnityEngine;

public static class BubbleColorUtil
{
    // EBubbleColor enum 값을 실제 Color 값으로 변환합니다.
    public static Color GetColor(EBubbleColor bubbleColor)
    {
        switch (bubbleColor)
        {
            case EBubbleColor.Red:    return new Color(0.95f, 0.25f, 0.25f);
            case EBubbleColor.Blue:   return new Color(0.30f, 0.55f, 0.95f);
            case EBubbleColor.Green:  return new Color(0.35f, 0.85f, 0.45f);
            case EBubbleColor.Yellow: return new Color(0.98f, 0.90f, 0.25f);
            case EBubbleColor.Purple: return new Color(0.70f, 0.45f, 0.95f);
            case EBubbleColor.Orange: return new Color(0.98f, 0.60f, 0.20f);
            default:                  return Color.white;
        }
    }
}