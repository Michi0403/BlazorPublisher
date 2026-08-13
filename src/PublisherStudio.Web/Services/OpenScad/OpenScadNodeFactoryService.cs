using System.Globalization;
using System.Text.Json;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>Creates catalog-backed nodes with typed defaults for property panels and future visual builders.</summary>
/// <param name="catalog">Open openscad catalog service dependency used by the open OpenSCAD node factory workflow to provide the corresponding application capability.</param>
public sealed class OpenScadNodeFactoryService(IOpenScadCatalogService catalog) : IOpenScadNodeFactoryService
{
    /// <summary>
    /// Performs create as part of the open OpenSCAD node factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD node factory operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node produced by the operation.</returns>
    public OpenScadNode Create(string kind)
    {
    try
    {
            var definition = catalog.Find(kind) ?? throw new ArgumentException($"Unknown OpenSCAD node kind '{kind}'.", nameof(kind));
            var node = new OpenScadNode { Kind = definition.Kind, Name = definition.DisplayName };
            foreach (var parameter in definition.Parameters)
            {
                var value = ParseDefault(parameter);
                if (value is not null) node.Parameters[parameter.Name] = value;
            }
            return node;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadNodeFactoryService.Create failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses default as part of the open OpenSCAD node factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="parameter">Parameter value supplied to the open OpenSCAD node factory operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD value produced by the operation.</returns>
    private OpenScadValue? ParseDefault(OpenScadParameterDefinition parameter)
    {
    try
    {
            var expression = parameter.DefaultExpression?.Trim() ?? string.Empty;
            if (expression.Length == 0 || string.Equals(expression, "undef", StringComparison.OrdinalIgnoreCase)) return null;
            return parameter.Type switch
            {
                OpenScadParameterType.Number when double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => new() { Type = parameter.Type, Number = number },
                OpenScadParameterType.Integer when int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) => new() { Type = parameter.Type, Integer = integer },
                OpenScadParameterType.Boolean when bool.TryParse(expression, out var boolean) => new() { Type = parameter.Type, Boolean = boolean },
                OpenScadParameterType.String or OpenScadParameterType.FilePath => new() { Type = parameter.Type, Text = Unquote(expression) },
                OpenScadParameterType.Vector2 or OpenScadParameterType.Vector3 or OpenScadParameterType.Vector4 => new() { Type = parameter.Type, Vector = ParseDoubleRows(expression).FirstOrDefault() ?? [] },
                OpenScadParameterType.Matrix4 => new() { Type = parameter.Type, Matrix = ParseDoubleRows(expression) },
                OpenScadParameterType.Points2D => new() { Type = parameter.Type, Points = ParseDoubleRows(expression) },
                OpenScadParameterType.Faces => new() { Type = parameter.Type, Faces = ParseIntegerRows(expression) },
                _ => new() { Type = OpenScadParameterType.Expression, Text = expression }
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadNodeFactoryService.ParseDefault failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs unquote as part of the open OpenSCAD node factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expression">Expression value supplied to the open OpenSCAD node factory operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Unquote(string expression)
    {
    try
    {
            if (expression.Length >= 2 && expression[0] == '"' && expression[^1] == '"')
            {
                try { return JsonSerializer.Deserialize<string>(expression) ?? string.Empty; }
                catch (JsonException) { return expression[1..^1]; }
            }
            return expression;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadNodeFactoryService.Unquote failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses double rows as part of the open OpenSCAD node factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expression">Expression value supplied to the open OpenSCAD node factory operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<List<double>> ParseDoubleRows(string expression)
    {
    try
    {
            try
            {
                if (expression.StartsWith("[[", StringComparison.Ordinal)) return JsonSerializer.Deserialize<List<List<double>>>(expression) ?? [];
                return [JsonSerializer.Deserialize<List<double>>(expression) ?? []];
            }
            catch (JsonException) { return []; }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadNodeFactoryService.ParseDoubleRows failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses integer rows as part of the open OpenSCAD node factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expression">Expression value supplied to the open OpenSCAD node factory operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<List<int>> ParseIntegerRows(string expression)
    {
    try
    {
            try { return JsonSerializer.Deserialize<List<List<int>>>(expression) ?? []; }
            catch (JsonException) { return []; }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadNodeFactoryService.ParseIntegerRows failed: {__serviceMethodException}");
        throw;
    }
}
}
