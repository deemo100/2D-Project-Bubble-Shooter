public class BottomReachedEnd : IEndCondition
{
    private readonly BubbleGrid mGrid;
    public BottomReachedEnd(BubbleGrid grid) { mGrid = grid; }
    public EEndStatus Evaluate() =>
        (mGrid != null && mGrid.HasAnyAtRow(mGrid.Height - 1)) ? EEndStatus.Lose : EEndStatus.None;
}