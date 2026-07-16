using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class PublicationDataService
{
    public PublicationDataObject CreateSample(string name = "Quarterly Sales")
    {
        var data = new PublicationDataObject
        {
            Name = name,
            SourceKind = PublicationDataSourceKind.DelimitedText,
            Delimiter = ",",
            RawSource = "Quarter,Product,Revenue,Target\nQ1,Print,42,50\nQ1,Digital,31,35\nQ2,Print,58,55\nQ2,Digital,46,42\nQ3,Print,64,60\nQ3,Digital,53,48\nQ4,Print,78,70\nQ4,Digital,66,60"
        };
        ParseInto(data);
        return data;
    }

    public PublicationDataObject Clone(PublicationDataObject source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<PublicationDataObject>(json) ?? CreateSample();
    }

    public void Normalize(PublicationDocument document)
    {
        document.DataObjects ??= [];
        foreach (var data in document.DataObjects)
        {
            data.Name = string.IsNullOrWhiteSpace(data.Name) ? "Data" : data.Name.Trim();
            data.RawSource ??= string.Empty;
            data.Delimiter = string.IsNullOrEmpty(data.Delimiter) ? "," : data.Delimiter[..1];
            data.Columns ??= [];
            data.Rows ??= [];
            foreach (var row in data.Rows)
                row.Values = new Dictionary<string, string>(row.Values ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            if (data.SourceKind != PublicationDataSourceKind.DocumentObjects && (data.Columns.Count == 0 || data.Rows.Count == 0) && !string.IsNullOrWhiteSpace(data.RawSource))
            {
                try { ParseInto(data); }
                catch { /* Preserve malformed user source; the manager displays the parse error on demand. */ }
            }
        }
    }

    public void ParseInto(PublicationDataObject data)
    {
        switch (data.SourceKind)
        {
            case PublicationDataSourceKind.Json:
                ParseJson(data);
                break;
            case PublicationDataSourceKind.DelimitedText:
                ParseDelimited(data);
                break;
            case PublicationDataSourceKind.Xml:
                ParseXml(data);
                break;
            case PublicationDataSourceKind.DocumentObjects:
                data.Columns = DocumentObjectColumns();
                data.Rows = [];
                break;
            default:
                throw new InvalidDataException("Unsupported data source type.");
        }
        data.ModifiedUtc = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<PublicationDataRow> ResolveRows(PublicationDocument document, PublicationDataObject? data, Guid currentPageId)
    {
        if (data is null) return [];
        return data.SourceKind == PublicationDataSourceKind.DocumentObjects
            ? BuildDocumentRows(document, data.DocumentScope, currentPageId)
            : data.Rows;
    }

    public IReadOnlyList<PublicationDataColumn> ResolveColumns(PublicationDataObject? data)
        => data?.SourceKind == PublicationDataSourceKind.DocumentObjects ? DocumentObjectColumns() : data?.Columns ?? [];

    public IReadOnlyList<DataChartPoint> BuildChartPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var rows = ResolveRows(document, data, currentPageId);
        var valueFields = item.ValueFields.Where(field => !string.IsNullOrWhiteSpace(field)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (valueFields.Length == 0) return [];
        var points = new List<DataChartPoint>();
        foreach (var row in rows.Take(500))
        {
            var argument = string.IsNullOrWhiteSpace(item.ArgumentField) ? (points.Count + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField);
            foreach (var valueField in valueFields)
            {
                var groupedSeries = string.IsNullOrWhiteSpace(item.SeriesField) ? string.Empty : row.Get(item.SeriesField);
                var series = !string.IsNullOrWhiteSpace(groupedSeries)
                    ? valueFields.Length > 1 ? $"{groupedSeries} · {valueField}" : groupedSeries
                    : valueFields.Length > 1 ? valueField : item.Title;
                points.Add(new DataChartPoint(string.IsNullOrWhiteSpace(argument) ? "(blank)" : argument,
                    string.IsNullOrWhiteSpace(series) ? valueField : series,
                    row.GetNumber(valueField)));
            }
        }
        return points;
    }

    public IReadOnlyList<DataPiePoint> BuildPiePoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var field = item.ValueFields.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(field)) return [];
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        return ResolveRows(document, data, currentPageId)
            .Take(200)
            .Select((row, index) => new DataPiePoint(
                string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
                row.GetNumber(field)))
            .ToArray();
    }

    public IReadOnlyList<DataSparkPoint> BuildSparkPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var field = item.ValueFields.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(field)) return [];
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        return ResolveRows(document, data, currentPageId)
            .Take(500)
            .Select((row, index) => new DataSparkPoint(
                string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
                row.GetNumber(field)))
            .ToArray();
    }

    public IReadOnlyList<string> GridColumns(PublicationDataObject? data)
        => ResolveColumns(data).Select(column => column.Name).Take(8).ToArray();

    public IReadOnlyList<PublicationGridRow> BuildGridRows(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var columns = GridColumns(data);
        return ResolveRows(document, data, currentPageId)
            .Take(Math.Clamp(item.RowLimit, 1, 100))
            .Select(row => PublicationGridRow.From(row, columns))
            .ToArray();
    }

    private static void ParseJson(PublicationDataObject data)
    {
        using var document = JsonDocument.Parse(data.RawSource, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        var root = document.RootElement;
        var sourceRows = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray().ToArray(),
            JsonValueKind.Object when root.TryGetProperty("data", out var nested) && nested.ValueKind == JsonValueKind.Array => nested.EnumerateArray().ToArray(),
            JsonValueKind.Object => [root],
            _ => throw new InvalidDataException("JSON must contain an object, an array of objects, or a data array.")
        };
        if (sourceRows.Length == 0)
        {
            data.Columns = [];
            data.Rows = [];
            return;
        }
        if (sourceRows.Any(row => row.ValueKind != JsonValueKind.Object))
            throw new InvalidDataException("Every JSON row must be an object with named properties.");

        var names = new List<string>();
        foreach (var row in sourceRows)
            foreach (var property in row.EnumerateObject())
                if (!names.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) names.Add(property.Name);

        data.Rows = sourceRows.Select(row => new PublicationDataRow
        {
            Values = row.EnumerateObject().ToDictionary(property => property.Name, property => JsonText(property.Value), StringComparer.OrdinalIgnoreCase)
        }).ToList();
        data.Columns = names.Select(name => new PublicationDataColumn
        {
            Name = name,
            ValueKind = InferKind(data.Rows.Select(row => row.Get(name)))
        }).ToList();
    }

    private static string JsonText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => value.GetRawText()
    };

    private static void ParseXml(PublicationDataObject data)
    {
        var document = XDocument.Parse(data.RawSource ?? string.Empty, LoadOptions.None);
        var root = document.Root ?? throw new InvalidDataException("XML must contain a root element.");
        var direct = root.Elements().ToList();
        List<XElement> sourceRows;
        if (direct.Count == 0)
        {
            sourceRows = [root];
        }
        else
        {
            var repeated = direct.GroupBy(element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .First();
            if (repeated.Count() > 1)
                sourceRows = repeated.ToList();
            else if (direct.Count == 1 && direct[0].Elements().Any())
                sourceRows = direct[0].Elements().ToList();
            else if (direct.All(element => element.Elements().Any() || element.HasAttributes))
                sourceRows = direct;
            else
                sourceRows = [root];
        }

        var rows = new List<PublicationDataRow>();
        var names = new List<string>();
        foreach (var sourceRow in sourceRows)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in sourceRow.Attributes())
                AddXmlValue(values, names, attribute.Name.LocalName, attribute.Value, attribute: true);
            var children = sourceRow.Elements().ToList();
            if (children.Count == 0)
            {
                var column = sourceRow == root ? root.Name.LocalName : "Value";
                AddXmlValue(values, names, column, sourceRow.Value, attribute: false);
            }
            else
            {
                foreach (var child in children)
                {
                    var value = child.HasElements
                        ? string.Join(" ", child.DescendantNodes().OfType<XText>().Select(node => node.Value).Where(item => !string.IsNullOrWhiteSpace(item))).Trim()
                        : child.Value;
                    AddXmlValue(values, names, child.Name.LocalName, value, attribute: false);
                    foreach (var attribute in child.Attributes())
                        AddXmlValue(values, names, $"{child.Name.LocalName}.@{attribute.Name.LocalName}", attribute.Value, attribute: true);
                }
            }
            rows.Add(new PublicationDataRow { Values = values });
        }

        data.Rows = rows;
        data.Columns = names.Select(name => new PublicationDataColumn
        {
            Name = name,
            ValueKind = InferKind(rows.Select(row => row.Get(name)))
        }).ToList();
    }

    private static void AddXmlValue(Dictionary<string, string> values, List<string> names, string name, string value, bool attribute)
    {
        var basis = string.IsNullOrWhiteSpace(name) ? (attribute ? "Attribute" : "Value") : name.Trim();
        var candidate = basis;
        if (values.ContainsKey(candidate) && attribute) candidate = $"@{basis}";
        var suffix = 2;
        while (values.ContainsKey(candidate)) candidate = $"{basis} {suffix++}";
        values[candidate] = value?.Trim() ?? string.Empty;
        if (!names.Contains(candidate, StringComparer.OrdinalIgnoreCase)) names.Add(candidate);
    }

    private static void ParseDelimited(PublicationDataObject data)
    {
        var delimiter = string.IsNullOrEmpty(data.Delimiter) ? ',' : data.Delimiter[0];
        var rows = ParseDelimitedRows(data.RawSource, delimiter);
        if (rows.Count == 0)
        {
            data.Columns = [];
            data.Rows = [];
            return;
        }
        var width = rows.Max(row => row.Count);
        var header = data.FirstRowContainsHeaders ? rows[0] : Enumerable.Range(1, width).Select(index => $"Column {index}").ToList();
        header = MakeUnique(header.Select((value, index) => string.IsNullOrWhiteSpace(value) ? $"Column {index + 1}" : value.Trim()).ToList());
        var start = data.FirstRowContainsHeaders ? 1 : 0;
        data.Rows = rows.Skip(start).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))).Select(row =>
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < header.Count; index++) values[header[index]] = index < row.Count ? row[index].Trim() : string.Empty;
            return new PublicationDataRow { Values = values };
        }).ToList();
        data.Columns = header.Select(name => new PublicationDataColumn
        {
            Name = name,
            ValueKind = InferKind(data.Rows.Select(row => row.Get(name)))
        }).ToList();
    }

    private static List<List<string>> ParseDelimitedRows(string source, char delimiter)
    {
        var result = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < (source ?? string.Empty).Length; index++)
        {
            var character = source[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < source.Length && source[index + 1] == '"') { value.Append('"'); index++; }
                else if (character == '"') quoted = false;
                else value.Append(character);
                continue;
            }
            if (character == '"') quoted = true;
            else if (character == delimiter) { row.Add(value.ToString()); value.Clear(); }
            else if (character == '\r') { }
            else if (character == '\n') { row.Add(value.ToString()); value.Clear(); result.Add(row); row = []; }
            else value.Append(character);
        }
        if (quoted) throw new InvalidDataException("Delimited text contains an unterminated quoted value.");
        if (value.Length > 0 || row.Count > 0) { row.Add(value.ToString()); result.Add(row); }
        return result;
    }

    private static List<string> MakeUnique(List<string> names)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < names.Count; index++)
        {
            var basis = names[index];
            var candidate = basis;
            var suffix = 2;
            while (!used.Add(candidate)) candidate = $"{basis} {suffix++}";
            names[index] = candidate;
        }
        return names;
    }

    private static PublicationDataValueKind InferKind(IEnumerable<string> values)
    {
        var materialized = values.Where(value => !string.IsNullOrWhiteSpace(value)).Take(100).ToArray();
        if (materialized.Length == 0) return PublicationDataValueKind.Text;
        if (materialized.All(value => double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _) || double.TryParse(value, out _))) return PublicationDataValueKind.Number;
        if (materialized.All(value => bool.TryParse(value, out _))) return PublicationDataValueKind.Boolean;
        if (materialized.All(value => DateTimeOffset.TryParse(value, out _))) return PublicationDataValueKind.DateTime;
        return PublicationDataValueKind.Text;
    }

    private static List<PublicationDataColumn> DocumentObjectColumns() =>
    [
        new() { Name = "Page", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "Object", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "Kind", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "X", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "Y", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "Width", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "Height", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "Rotation", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "Layer", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "Visible", ValueKind = PublicationDataValueKind.Boolean },
        new() { Name = "Locked", ValueKind = PublicationDataValueKind.Boolean }
    ];

    private static IReadOnlyList<PublicationDataRow> BuildDocumentRows(PublicationDocument document, DocumentObjectDataScope scope, Guid currentPageId)
    {
        var pages = scope == DocumentObjectDataScope.CurrentPage
            ? document.Pages.Where(page => page.Id == currentPageId)
            : document.Pages;
        return pages.SelectMany(page => page.Elements.Select(element => new PublicationDataRow
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Page"] = page.Name,
                ["Object"] = element.Name,
                ["Kind"] = element.Kind.ToString(),
                ["X"] = element.X.ToString("0.###", CultureInfo.InvariantCulture),
                ["Y"] = element.Y.ToString("0.###", CultureInfo.InvariantCulture),
                ["Width"] = element.Width.ToString("0.###", CultureInfo.InvariantCulture),
                ["Height"] = element.Height.ToString("0.###", CultureInfo.InvariantCulture),
                ["Rotation"] = element.Rotation.ToString("0.###", CultureInfo.InvariantCulture),
                ["Layer"] = element.ZIndex.ToString(CultureInfo.InvariantCulture),
                ["Visible"] = element.Visible.ToString(),
                ["Locked"] = element.Locked.ToString()
            }
        })).ToArray();
    }
}
