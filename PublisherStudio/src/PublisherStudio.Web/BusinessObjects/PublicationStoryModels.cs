namespace PublisherStudio.BusinessObjects;

public sealed record StoryPageLayout(
    double PageWidthMm,
    double PageHeightMm,
    double MarginTopMm,
    double MarginRightMm,
    double MarginBottomMm,
    double MarginLeftMm)
{
    public double ContentWidthMm => Math.Max(1, PageWidthMm - MarginLeftMm - MarginRightMm);
    public double ContentHeightMm => Math.Max(1, PageHeightMm - MarginTopMm - MarginBottomMm);
    public bool IsLandscape => PageWidthMm > PageHeightMm;
}
