using PublisherStudio.BusinessObjects;
using System.Globalization;
using System.Text;
using PublisherStudio.Services.Configuration;

// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Represents a word art path geometry.
/// </summary>
public sealed class WordArtPathGeometry(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<WordArtPathGeometry> logger)
{
    /// <summary>
    /// Creates preset.
    /// </summary>
    public List<WordArtPathPoint> CreatePreset(string key)
    {
    try
    {
            logger.LogTrace("Creating WordArt path preset {PresetKey}.", key);
            return key switch
        {
            "Straight" => Points((60, 150), (940, 150)),
            "Rise" => Points((60, 235), (940, 65)),
            "Fall" => Points((60, 65), (940, 235)),
            "ArchUp" => Points((60, 225), (250, 70), (500, 25), (750, 70), (940, 225)),
            "ArchDown" => Points((60, 75), (250, 230), (500, 275), (750, 230), (940, 75)),
            "SCurve" => Points((60, 215), (220, 45), (405, 55), (500, 150), (595, 245), (780, 255), (940, 85)),
            "CircleArc" => Points((90, 230), (260, 55), (500, 15), (740, 55), (910, 230)),
            _ => Points((50, 170), (220, 55), (385, 245), (555, 75), (730, 230), (950, 135))
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(CreatePreset)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(CreatePreset)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the build operation.
    /// </summary>
    public string Build(WordArtElement item)
    {
    try
    {
            if (item.Warp != WordArtWarp.Custom)
            {
                return item.Warp switch
                {
                    WordArtWarp.ArchUp => "M 80 225 Q 500 5 920 225",
                    WordArtWarp.ArchDown => "M 80 75 Q 500 295 920 75",
                    WordArtWarp.Wave => "M 40 165 C 220 20 350 290 520 145 C 690 0 810 280 960 125",
                    _ => "M 50 150 L 950 150"
                };
            }

            return Build(item.CustomPathPoints);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Build)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Build)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the build operation.
    /// </summary>
    public string Build(IReadOnlyList<WordArtPathPoint>? points)
    {
    try
    {
            var safe = Normalize(points);
            if (safe.Count == 2)
                return FormattableString.Invariant($"M {safe[0].X:0.###} {safe[0].Y:0.###} L {safe[1].X:0.###} {safe[1].Y:0.###}");

            var builder = new StringBuilder();
            builder.Append("M ").Append(Inv(safe[0].X)).Append(' ').Append(Inv(safe[0].Y));
            for (var index = 0; index < safe.Count - 1; index++)
            {
                var previous = index == 0 ? safe[index] : safe[index - 1];
                var current = safe[index];
                var next = safe[index + 1];
                var following = index + 2 < safe.Count ? safe[index + 2] : next;

                var control1X = current.X + (next.X - previous.X) / 6d;
                var control1Y = current.Y + (next.Y - previous.Y) / 6d;
                var control2X = next.X - (following.X - current.X) / 6d;
                var control2Y = next.Y - (following.Y - current.Y) / 6d;

                builder.Append(" C ")
                    .Append(Inv(control1X)).Append(' ').Append(Inv(control1Y)).Append(' ')
                    .Append(Inv(control2X)).Append(' ').Append(Inv(control2Y)).Append(' ')
                    .Append(Inv(next.X)).Append(' ').Append(Inv(next.Y));
            }
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Build)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Build)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the normalize operation.
    /// </summary>
    public List<WordArtPathPoint> Normalize(IReadOnlyList<WordArtPathPoint>? points)
    {
    try
    {
            if (points is null || points.Count < 2)
                return CreatePreset("Straight");

            var normalized = points
                .Where(point => point is not null && double.IsFinite(point.X) && double.IsFinite(point.Y))
                .Select(point => new WordArtPathPoint
                {
                    X = Math.Clamp(point.X, 0, runtimePolicy.WordArtViewWidth),
                    Y = Math.Clamp(point.Y, 0, runtimePolicy.WordArtViewHeight)
                })
                .Take(32)
                .ToList();

            return normalized.Count >= 2 ? normalized : CreatePreset("Straight");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Normalize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Normalize)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the reverse operation.
    /// </summary>
    public List<WordArtPathPoint> Reverse(IReadOnlyList<WordArtPathPoint>? points) {
    try
    {
        return Normalize(points).AsEnumerable().Reverse().Select(point => point.Clone()).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Reverse)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Reverse)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the reduce operation.
    /// </summary>
    public List<WordArtPathPoint> Reduce(IReadOnlyList<WordArtPathPoint>? points, int maximum = 10)
    {
    try
    {
            var normalized = Normalize(points);
            if (normalized.Count <= maximum) return normalized.Select(point => point.Clone()).ToList();

            var result = new List<WordArtPathPoint>(maximum) { normalized[0].Clone() };
            for (var index = 1; index < maximum - 1; index++)
            {
                var sourceIndex = (int)Math.Round(index * (normalized.Count - 1d) / (maximum - 1d));
                result.Add(normalized[sourceIndex].Clone());
            }
            result.Add(normalized[^1].Clone());
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Reduce)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Reduce)} failed.");
        throw;
    }
}

    private List<WordArtPathPoint> Points(params (double X, double Y)[] values) {
    try
    {
        return values.Select(value => new WordArtPathPoint { X = value.X, Y = value.Y }).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Points)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Points)} failed.");
        throw;
    }
}

    private string Inv(double value) {
    try
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Inv)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(WordArtPathGeometry)}.{nameof(Inv)} failed.");
        throw;
    }
}
}
