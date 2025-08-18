public class ClearAllBubblesEnd : IEndCondition
{
    private readonly BubbleGrid mGrid;
    public ClearAllBubblesEnd(BubbleGrid grid) { mGrid = grid; }
    public EEndStatus Evaluate() => (mGrid != null && mGrid.CountAll() == 0) ? EEndStatus.Win : EEndStatus.None;
}