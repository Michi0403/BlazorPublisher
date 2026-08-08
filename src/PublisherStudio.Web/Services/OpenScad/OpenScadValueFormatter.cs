using System.Globalization;
using System.Text;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Represents an open scad value formatter.
/// </summary>
public sealed class OpenScadValueFormatter : IOpenScadValueFormatter
{
    /// <summary>
    /// Runs the format operation.
    /// </summary>
    public string Format(OpenScadValue? value, string fallbackExpression = "undef")
    {
    try
    {
            if (value is null) return fallbackExpression;
            return value.Type switch
            {
                OpenScadParameterType.Number => Number(value.Number),
                OpenScadParameterType.Integer => value.Integer.ToString(CultureInfo.InvariantCulture),
                OpenScadParameterType.Boolean => value.Boolean ? "true" : "false",
                OpenScadParameterType.String or OpenScadParameterType.FilePath => Quote(value.Text),
                OpenScadParameterType.Vector2 or OpenScadParameterType.Vector3 or OpenScadParameterType.Vector4 => Vector(value.Vector),
                OpenScadParameterType.Matrix4 => Matrix(value.Matrix),
                OpenScadParameterType.Points2D => Matrix(value.Points),
                OpenScadParameterType.Faces => Faces(value.Faces),
                OpenScadParameterType.Expression => string.IsNullOrWhiteSpace(value.Text) ? fallbackExpression : value.Text.Trim(),
                _ => fallbackExpression
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Format failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the quote operation.
    /// </summary>
    public string Quote(string value)
    {
    try
    {
            var escaped = (value ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            return "\"" + escaped + "\"";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Quote failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the identifier operation.
    /// </summary>
    public string Identifier(string value, string fallback = "part")
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            var builder = new StringBuilder(source.Length + 1);
            if (!char.IsLetter(source[0]) && source[0] != '_') builder.Append('_');
            foreach (var character in source)
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            return builder.Length == 0 ? fallback : builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Identifier failed: {__serviceMethodException}");
        throw;
    }
}

    private string Vector(IEnumerable<double> values) {
    try
    {
        return "[" + string.Join(", ", values.Select(Number)) + "]";
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Vector failed: {__serviceMethodException}");
        throw;
    }
}
    private string Matrix(IEnumerable<IEnumerable<double>> rows) {
    try
    {
        return "[" + string.Join(", ", rows.Select(Vector)) + "]";
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Matrix failed: {__serviceMethodException}");
        throw;
    }
}

    private string Faces(IEnumerable<IEnumerable<int>> rows)
    {
    try
    {
            var formattedRows = rows.Select(row => "[" + string.Join(", ", row) + "]");
            return "[" + string.Join(", ", formattedRows) + "]";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Faces failed: {__serviceMethodException}");
        throw;
    }
}
    private string Number(double value) {
    try
    {
        return double.IsFinite(value) ? value.ToString("0.######", CultureInfo.InvariantCulture) : "0";
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadValueFormatter.Number failed: {__serviceMethodException}");
        throw;
    }
}
}
