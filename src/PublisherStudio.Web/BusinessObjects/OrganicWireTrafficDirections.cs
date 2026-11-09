namespace PublisherStudio.BusinessObjects;

/// <summary>Names the directions of a journaled 1-Wire packet.</summary>
public sealed class OrganicWireTrafficDirections
{
    /// <summary>A packet PublisherStudio received.</summary>
    public const string Received = "Received";
    /// <summary>A packet PublisherStudio sent.</summary>
    public const string Sent = "Sent";
}
