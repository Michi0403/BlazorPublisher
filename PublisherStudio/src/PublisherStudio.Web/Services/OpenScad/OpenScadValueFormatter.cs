using System.Globalization;
using System.Text;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

public sealed class OpenScadValueFormatter : IOpenScadValueFormatter
{
    public string Format(OpenScadValue? value, string fallbackExpression = "undef")
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

    public string Quote(string value)
    {
        var escaped = (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return "\"" + escaped + "\"";
    }

    public string Identifier(string value, string fallback = "part")
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var builder = new StringBuilder(source.Length + 1);
        if (!char.IsLetter(source[0]) && source[0] != '_') builder.Append('_');
        foreach (var character in source)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        return builder.Length == 0 ? fallback : builder.ToString();
    }

    private string Vector(IEnumerable<double> values) => "[" + string.Join(", ", values.Select(Number)) + "]";
    private string Matrix(IEnumerable<IEnumerable<double>> rows) => "[" + string.Join(", ", rows.Select(Vector)) + "]";

    private string Faces(IEnumerable<IEnumerable<int>> rows)
    {
        var formattedRows = rows.Select(row => "[" + string.Join(", ", row) + "]");
        return "[" + string.Join(", ", formattedRows) + "]";
    }
    private string Number(double value) => double.IsFinite(value) ? value.ToString("0.######", CultureInfo.InvariantCulture) : "0";
}
