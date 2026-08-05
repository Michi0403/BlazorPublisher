namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a story page layout.
/// </summary>
public sealed record StoryPageLayout(
    double PageWidthMm,
    double PageHeightMm,
    double MarginTopMm,
    double MarginRightMm,
    double MarginBottomMm,
    double MarginLeftMm)
{
    /// <summary>
    /// Gets content width millimetres.
    /// </summary>
    public double ContentWidthMm => Math.Max(1, PageWidthMm - MarginLeftMm - MarginRightMm);
    /// <summary>
    /// Gets content height millimetres.
    /// </summary>
    public double ContentHeightMm => Math.Max(1, PageHeightMm - MarginTopMm - MarginBottomMm);
    /// <summary>
    /// Gets is landscape.
    /// </summary>
    public bool IsLandscape => PageWidthMm > PageHeightMm;
}
