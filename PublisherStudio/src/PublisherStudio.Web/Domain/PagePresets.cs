namespace PublisherStudio.Domain;

public sealed record PagePreset(string Key, string Name, double WidthMm, double HeightMm)
{
    public static IReadOnlyList<PagePreset> All { get; } =
    [
        new("a3-p", "A3 portrait", 297, 420),
        new("a3-l", "A3 landscape", 420, 297),
        new("a4-p", "A4 portrait", 210, 297),
        new("a4-l", "A4 landscape", 297, 210),
        new("a5-p", "A5 portrait", 148, 210),
        new("a5-l", "A5 landscape", 210, 148),
        new("letter-p", "Letter portrait", 215.9, 279.4),
        new("letter-l", "Letter landscape", 279.4, 215.9),
        new("legal-p", "Legal portrait", 215.9, 355.6),
        new("tabloid-l", "Tabloid landscape", 431.8, 279.4),
        new("business-eu", "Business card 85 × 55 mm", 85, 55),
        new("square", "Square 210 × 210 mm", 210, 210)
    ];

    public static PagePreset? Find(string? key) => All.FirstOrDefault(item => item.Key == key);
}
