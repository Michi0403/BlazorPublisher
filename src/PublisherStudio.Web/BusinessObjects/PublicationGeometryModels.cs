namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a publication point.
/// </summary>
public readonly record struct PublicationPoint(double X, double Y);

/// <summary>
/// Represents a word art path point.
/// </summary>
public sealed class WordArtPathPoint
{
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Runs the clone operation.
    /// </summary>
    public WordArtPathPoint Clone() => new() { X = X, Y = Y };
}
