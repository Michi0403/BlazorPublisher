namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a publication point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="X">X value supplied to the publication point operation and used when producing its result.</param>
/// <param name="Y">Y value supplied to the publication point operation and used when producing its result.</param>
public readonly record struct PublicationPoint(double X, double Y);

/// <summary>
/// Represents a word art path point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class WordArtPathPoint
{
    /// <summary>
    /// Gets or sets the x value that forms part of the word art path point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="WordArtPathPoint"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the word art path point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="WordArtPathPoint"/>.</value>
    public double Y { get; set; }

    /// <summary>
    /// Performs clone for <see cref="WordArtPathPoint"/>, keeping the operation consistent with the state and invariants of the surrounding word art path point workflow.
    /// </summary>
    /// <returns>The word art path point produced by the operation.</returns>
    public WordArtPathPoint Clone() => new() { X = X, Y = Y };
}
