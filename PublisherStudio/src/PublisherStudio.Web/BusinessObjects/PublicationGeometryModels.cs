namespace PublisherStudio.BusinessObjects;

public readonly record struct PublicationPoint(double X, double Y);

public sealed class WordArtPathPoint
{
    public double X { get; set; }
    public double Y { get; set; }

    public WordArtPathPoint Clone() => new() { X = X, Y = Y };
}
