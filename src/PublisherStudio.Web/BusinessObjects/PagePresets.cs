namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a page preset application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Key">Key value supplied to the page preset operation and used when producing its result.</param>
/// <param name="Name">Name value supplied to the page preset operation and used when producing its result.</param>
/// <param name="WidthMm">Width mm value supplied to the page preset operation and used when producing its result.</param>
/// <param name="HeightMm">Height mm value supplied to the page preset operation and used when producing its result.</param>
public sealed record PagePreset(string Key, string Name, double WidthMm, double HeightMm);
