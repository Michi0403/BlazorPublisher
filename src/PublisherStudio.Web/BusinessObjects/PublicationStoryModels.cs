namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a story page layout application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="PageWidthMm">Page width mm value supplied to the story page layout operation and used when producing its result.</param>
/// <param name="PageHeightMm">Page height mm value supplied to the story page layout operation and used when producing its result.</param>
/// <param name="MarginTopMm">Margin top mm value supplied to the story page layout operation and used when producing its result.</param>
/// <param name="MarginRightMm">Margin right mm value supplied to the story page layout operation and used when producing its result.</param>
/// <param name="MarginBottomMm">Margin bottom mm value supplied to the story page layout operation and used when producing its result.</param>
/// <param name="MarginLeftMm">Margin left mm value supplied to the story page layout operation and used when producing its result.</param>
public sealed record StoryPageLayout(
    double PageWidthMm,
    double PageHeightMm,
    double MarginTopMm,
    double MarginRightMm,
    double MarginBottomMm,
    double MarginLeftMm)
{
    /// <summary>
    /// Gets the content width mm value that forms part of the story page layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content width mm value exposed by <see cref="StoryPageLayout"/>.</value>
    public double ContentWidthMm => Math.Max(1, PageWidthMm - MarginLeftMm - MarginRightMm);
    /// <summary>
    /// Gets the content height mm value that forms part of the story page layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content height mm value exposed by <see cref="StoryPageLayout"/>.</value>
    public double ContentHeightMm => Math.Max(1, PageHeightMm - MarginTopMm - MarginBottomMm);
    /// <summary>
    /// Gets a value indicating whether landscape applies to the story page layout state.
    /// </summary>
    /// <value>The is landscape value exposed by <see cref="StoryPageLayout"/>.</value>
    public bool IsLandscape => PageWidthMm > PageHeightMm;
}
