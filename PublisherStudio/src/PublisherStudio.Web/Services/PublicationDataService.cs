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
        EnsureBuiltInObjects(document);
        foreach (var data in document.DataObjects)
        {
            data.Name = string.IsNullOrWhiteSpace(data.Name) ? "Data" : data.Name.Trim();
            data.RawSource ??= string.Empty;
            data.SourceReference ??= string.Empty;
            data.Delimiter = string.IsNullOrEmpty(data.Delimiter) ? "," : data.Delimiter[..1];
            data.Columns ??= [];
            data.Rows ??= [];
            data.Web ??= new PublicationWebBinding();
            data.Web.Headers ??= [];
            if (data.Web.Id == Guid.Empty) data.Web.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(data.Web.WebhookToken)) data.Web.WebhookToken = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(data.Web.ExportAccessToken)) data.Web.ExportAccessToken = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
            data.Web.Url ??= string.Empty;
            data.Web.RequestBody ??= string.Empty;
            data.Web.JsonPath ??= string.Empty;
            data.Web.Delimiter = string.IsNullOrEmpty(data.Web.Delimiter) ? "," : data.Web.Delimiter[..1];
            data.Web.RefreshIntervalSeconds = Math.Max(0, data.Web.RefreshIntervalSeconds);
            data.Web.TimeoutSeconds = Math.Max(0, data.Web.TimeoutSeconds);
            foreach (var header in data.Web.Headers)
            {
                header.Name ??= string.Empty;
                header.Value ??= string.Empty;
            }
            foreach (var row in data.Rows)
                row.Values = new Dictionary<string, string>(row.Values ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            if (data.SourceKind is not PublicationDataSourceKind.DocumentObjects
                and not PublicationDataSourceKind.PublicationPages
                and not PublicationDataSourceKind.PublicationDocument
                and not PublicationDataSourceKind.PublicationMedia
                && (data.Columns.Count == 0 || data.Rows.Count == 0)
                && !string.IsNullOrWhiteSpace(data.RawSource))
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
            case PublicationDataSourceKind.PublicationPages:
                data.Columns = PublicationPageColumns();
                data.Rows = [];
                break;
            case PublicationDataSourceKind.PublicationDocument:
                data.Columns = PublicationDocumentColumns();
                data.Rows = [];
                break;
            case PublicationDataSourceKind.PublicationMedia:
                data.Columns = PublicationMediaColumns();
                data.Rows = [];
                break;
            case PublicationDataSourceKind.Web:
                ParseWebSnapshot(data);
                break;
            default:
                throw new InvalidDataException("Unsupported data source type.");
        }
        data.ModifiedUtc = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<PublicationDataRow> ResolveRows(PublicationDocument document, PublicationDataObject? data, Guid currentPageId)
    {
        if (data is null) return [];
        return data.SourceKind switch
        {
            PublicationDataSourceKind.DocumentObjects => BuildDocumentRows(document, data.DocumentScope, currentPageId),
            PublicationDataSourceKind.PublicationPages => BuildPublicationPageRows(document),
            PublicationDataSourceKind.PublicationDocument => BuildPublicationDocumentRows(document, currentPageId),
            PublicationDataSourceKind.PublicationMedia => BuildPublicationMediaRows(document, data.DocumentScope, currentPageId),
            _ => data.Rows
        };
    }

    public IReadOnlyList<PublicationDataColumn> ResolveColumns(PublicationDataObject? data)
        => data?.SourceKind switch
        {
            PublicationDataSourceKind.DocumentObjects => DocumentObjectColumns(),
            PublicationDataSourceKind.PublicationPages => PublicationPageColumns(),
            PublicationDataSourceKind.PublicationDocument => PublicationDocumentColumns(),
            PublicationDataSourceKind.PublicationMedia => PublicationMediaColumns(),
            _ => data?.Columns ?? []
        };

    public void EnsureBuiltInObjects(PublicationDocument document)
    {
        document.DataObjects ??= [];
        EnsureBuiltIn(document, PublicationDataSourceKind.PublicationPages, "Publication pages");
        EnsureBuiltIn(document, PublicationDataSourceKind.PublicationDocument, "Publication document");
        EnsureBuiltIn(document, PublicationDataSourceKind.DocumentObjects, "Publication objects");
        EnsureBuiltIn(document, PublicationDataSourceKind.PublicationMedia, "Publication media");
    }

    private static void EnsureBuiltIn(PublicationDocument document, PublicationDataSourceKind kind, string name)
    {
        if (document.DataObjects.Any(data => data.SourceKind == kind)) return;
        document.DataObjects.Add(new PublicationDataObject
        {
            Name = name,
            SourceKind = kind,
            RawSource = string.Empty,
            Columns = kind switch
            {
                PublicationDataSourceKind.PublicationPages => PublicationPageColumns(),
                PublicationDataSourceKind.PublicationDocument => PublicationDocumentColumns(),
                PublicationDataSourceKind.PublicationMedia => PublicationMediaColumns(),
                _ => DocumentObjectColumns()
            },
            Rows = []
        });
    }

    public IReadOnlyList<DataChartPoint> BuildChartPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var rows = ResolveRows(document, data, currentPageId);
        var valueFields = item.ValueFields.Where(field => !string.IsNullOrWhiteSpace(field)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (valueFields.Length == 0) return [];
        var points = new List<DataChartPoint>();
        foreach (var row in rows)
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
            .Select((row, index) => new DataSparkPoint(
                string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
                row.GetNumber(field)))
            .ToArray();
    }


    public IReadOnlyList<DataRangePoint> BuildRangePoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var lowField = !string.IsNullOrWhiteSpace(item.LowValueField) ? item.LowValueField : item.ValueFields.ElementAtOrDefault(0) ?? string.Empty;
        var highField = !string.IsNullOrWhiteSpace(item.HighValueField) ? item.HighValueField : item.ValueFields.ElementAtOrDefault(1) ?? lowField;
        if (string.IsNullOrWhiteSpace(lowField) || string.IsNullOrWhiteSpace(highField)) return [];
        return ResolveRows(document, data, currentPageId).Select((row, index) =>
        {
            var argument = string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField);
            var series = string.IsNullOrWhiteSpace(item.SeriesField) ? item.Title : row.Get(item.SeriesField);
            return new DataRangePoint(string.IsNullOrWhiteSpace(argument) ? "(blank)" : argument, series, row.GetNumber(lowField), row.GetNumber(highField));
        }).ToArray();
    }

    public IReadOnlyList<DataBubblePoint> BuildBubblePoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var valueField = item.ValueFields.FirstOrDefault() ?? string.Empty;
        var sizeField = !string.IsNullOrWhiteSpace(item.SizeField) ? item.SizeField : item.ValueFields.ElementAtOrDefault(1) ?? valueField;
        if (string.IsNullOrWhiteSpace(valueField)) return [];
        return ResolveRows(document, data, currentPageId).Select((row, index) =>
        {
            var argument = string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField);
            var series = string.IsNullOrWhiteSpace(item.SeriesField) ? item.Title : row.Get(item.SeriesField);
            return new DataBubblePoint(string.IsNullOrWhiteSpace(argument) ? "(blank)" : argument, series, row.GetNumber(valueField), Math.Abs(row.GetNumber(sizeField)));
        }).ToArray();
    }

    public IReadOnlyList<DataFinancialPoint> BuildFinancialPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var open = !string.IsNullOrWhiteSpace(item.OpenValueField) ? item.OpenValueField : item.ValueFields.ElementAtOrDefault(0) ?? string.Empty;
        var high = !string.IsNullOrWhiteSpace(item.HighValueField) ? item.HighValueField : item.ValueFields.ElementAtOrDefault(1) ?? open;
        var low = !string.IsNullOrWhiteSpace(item.LowValueField) ? item.LowValueField : item.ValueFields.ElementAtOrDefault(2) ?? open;
        var close = !string.IsNullOrWhiteSpace(item.CloseValueField) ? item.CloseValueField : item.ValueFields.ElementAtOrDefault(3) ?? open;
        if (string.IsNullOrWhiteSpace(open)) return [];
        return ResolveRows(document, data, currentPageId).Select((row, index) => new DataFinancialPoint(
            string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
            row.GetNumber(open), row.GetNumber(high), row.GetNumber(low), row.GetNumber(close))).ToArray();
    }

    public IReadOnlyList<DataSankeyPoint> BuildSankeyPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var weightField = item.ValueFields.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(item.ArgumentField) || string.IsNullOrWhiteSpace(item.TargetField) || string.IsNullOrWhiteSpace(weightField)) return [];
        return ResolveRows(document, data, currentPageId)
            .Select(row => new DataSankeyPoint(row.Get(item.ArgumentField), row.Get(item.TargetField), row.GetNumber(weightField)))
            .Where(point => !string.IsNullOrWhiteSpace(point.Source) && !string.IsNullOrWhiteSpace(point.Target))
            .ToArray();
    }

    public IReadOnlyList<DataTreeMapPoint> BuildTreeMapPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var valueField = item.ValueFields.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(item.ArgumentField) || string.IsNullOrWhiteSpace(valueField)) return [];
        return ResolveRows(document, data, currentPageId)
            .Select(row => new DataTreeMapPoint(row.Get(item.ArgumentField), string.IsNullOrWhiteSpace(item.ParentField) ? string.Empty : row.Get(item.ParentField), row.GetNumber(valueField)))
            .ToArray();
    }

    public object BuildClientVisualConfiguration(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
        var rows = ResolveRows(document, data, currentPageId)
            .Select(row => row.Values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)).ToArray();
        return new
        {
            id = item.Id,
            kind = item.VisualKind.ToString(),
            cartesianStyle = item.CartesianStyle.ToString(),
            pieStyle = item.PieStyle.ToString(),
            polarStyle = item.PolarStyle.ToString(),
            sparklineStyle = item.SparklineStyle.ToString(),
            item.Title,
            item.ArgumentField,
            item.SeriesField,
            valueFields = item.ValueFields,
            item.LowValueField,
            item.HighValueField,
            item.OpenValueField,
            item.CloseValueField,
            item.SizeField,
            item.TargetField,
            item.ParentField,
            item.ShowLegend,
            item.ShowLabels,
            item.ShowTitle,
            item.TableShowHeader,
            item.TableShowFilterRow,
            item.RowLimit,
            item.MinimumValue,
            item.MaximumValue,
            item.Background,
            rows,
            columnKinds = ResolveColumns(data).ToDictionary(column => column.Name, column => column.ValueKind.ToString(), StringComparer.OrdinalIgnoreCase),
            live = data?.SourceKind == PublicationDataSourceKind.Web ? new
            {
                enabled = data.Web.Enabled,
                transport = data.Web.Transport.ToString(),
                method = data.Web.Method.ToString(),
                url = data.Web.AllowExportedHtmlFetch ? data.Web.Url : string.Empty,
                headers = data.Web.AllowExportedHtmlFetch ? data.Web.Headers : new List<PublicationWebHeader>(),
                body = data.Web.AllowExportedHtmlFetch ? data.Web.RequestBody : string.Empty,
                responseFormat = data.Web.ResponseFormat.ToString(),
                jsonPath = data.Web.JsonPath,
                delimiter = data.Web.Delimiter,
                firstRowContainsHeaders = data.Web.FirstRowContainsHeaders,
                refreshIntervalSeconds = data.Web.RefreshIntervalSeconds,
                allowExportedHtmlFetch = data.Web.AllowExportedHtmlFetch,
                useSnapshotOnFailure = data.Web.UseSnapshotOnFailure,
                monolithRowsUrl = data.Web.AllowExportedHtmlFetch
                    ? $"/api/publisher/exports/{document.Id}/data/{data.Id}/{data.Web.ExportAccessToken}/rows"
                    : string.Empty
            } : null
        };
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

    private static void ParseWebSnapshot(PublicationDataObject data)
    {
        if (string.IsNullOrWhiteSpace(data.RawSource))
        {
            data.Columns = [];
            data.Rows = [];
            return;
        }
        var format = data.Web.ResponseFormat;
        if (format == PublicationWebResponseFormat.Auto)
            format = DetectWebResponseFormat(data);
        switch (format)
        {
            case PublicationWebResponseFormat.Json:
                var originalJson = data.RawSource;
                data.RawSource = SelectJsonPath(originalJson, data.Web.JsonPath);
                try { ParseJson(data); }
                finally { data.RawSource = originalJson; }
                break;
            case PublicationWebResponseFormat.Xml:
                ParseXml(data);
                break;
            case PublicationWebResponseFormat.DelimitedText:
                var originalDelimiter = data.Delimiter;
                var originalHeaders = data.FirstRowContainsHeaders;
                data.Delimiter = string.IsNullOrEmpty(data.Web.Delimiter) ? "," : data.Web.Delimiter[..1];
                data.FirstRowContainsHeaders = data.Web.FirstRowContainsHeaders;
                try { ParseDelimited(data); }
                finally { data.Delimiter = originalDelimiter; data.FirstRowContainsHeaders = originalHeaders; }
                break;
            case PublicationWebResponseFormat.Text:
                data.Columns = [new PublicationDataColumn { Name = "Value", ValueKind = PublicationDataValueKind.Text }];
                data.Rows = [new PublicationDataRow
                {
                    Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Value"] = data.RawSource }
                }];
                break;
            default:
                throw new InvalidDataException("Unsupported web response format.");
        }
    }

    private static PublicationWebResponseFormat DetectWebResponseFormat(PublicationDataObject data)
    {
        var mediaType = data.Web.LastContentType?.Trim() ?? string.Empty;
        if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)) return PublicationWebResponseFormat.Json;
        if (mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)) return PublicationWebResponseFormat.Xml;
        if (mediaType.Contains("csv", StringComparison.OrdinalIgnoreCase) || mediaType.Contains("tab-separated", StringComparison.OrdinalIgnoreCase))
            return PublicationWebResponseFormat.DelimitedText;

        var trimmed = data.RawSource.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("[")) return PublicationWebResponseFormat.Json;
        if (trimmed.StartsWith("<")) return PublicationWebResponseFormat.Xml;

        // A few webhook gateways encode the complete JSON payload as a JSON string.
        // Recognize that shape while Auto is selected so ParseJson can unwrap it.
        if (trimmed.StartsWith('"'))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                var encoded = document.RootElement.ValueKind == JsonValueKind.String ? document.RootElement.GetString()?.TrimStart() : null;
                if (!string.IsNullOrWhiteSpace(encoded) && (encoded.StartsWith('{') || encoded.StartsWith('[')))
                    return PublicationWebResponseFormat.Json;
            }
            catch (JsonException) { }
        }
        return PublicationWebResponseFormat.DelimitedText;
    }

    private static string SelectJsonPath(string source, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return source;
        using var document = JsonDocument.Parse(source, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        JsonDocument? encodedDocument = null;
        var current = document.RootElement;
        if (current.ValueKind == JsonValueKind.String)
        {
            var encoded = current.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(encoded) && (encoded.StartsWith('{') || encoded.StartsWith('[')))
            {
                encodedDocument = JsonDocument.Parse(encoded, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                current = encodedDocument.RootElement;
            }
        }
        try
        {
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var property)) current = property;
                else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index) && index >= 0 && index < current.GetArrayLength()) current = current[index];
                else throw new InvalidDataException($"JSON path segment '{segment}' was not found.");
            }
            return current.GetRawText();
        }
        finally
        {
            encodedDocument?.Dispose();
        }
    }

    private static void ParseJson(PublicationDataObject data)
    {
        using var document = JsonDocument.Parse(data.RawSource, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        JsonDocument? encodedDocument = null;
        var root = document.RootElement;

        // Some webhook relays return JSON as an encoded JSON string. Unwrap one level
        // so an array of objects is not treated as one opaque value column.
        if (root.ValueKind == JsonValueKind.String)
        {
            var encoded = root.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(encoded) && (encoded.StartsWith('{') || encoded.StartsWith('[')))
            {
                encodedDocument = JsonDocument.Parse(encoded, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                root = encodedDocument.RootElement;
            }
        }

        try
        {
            var sourceRows = SelectJsonRows(root);
            if (sourceRows.Length == 0)
            {
                data.Columns = [];
                data.Rows = [];
                return;
            }
            if (sourceRows.Any(row => row.ValueKind != JsonValueKind.Object))
                throw new InvalidDataException("Every JSON row must be an object with named properties.");

            var names = new List<string>();
            var rows = new List<PublicationDataRow>(sourceRows.Length);
            foreach (var sourceRow in sourceRows)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                FlattenJsonObject(sourceRow, string.Empty, values, names);
                rows.Add(new PublicationDataRow { Values = values });
            }

            data.Rows = rows;
            data.Columns = names.Select(name => new PublicationDataColumn
            {
                Name = name,
                ValueKind = InferKind(data.Rows.Select(row => row.Get(name)))
            }).ToList();
        }
        finally
        {
            encodedDocument?.Dispose();
        }
    }

    private static JsonElement[] SelectJsonRows(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray().ToArray();
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("JSON must contain an object, an array of objects, or a wrapped object array.");
        return TryFindJsonRowArray(root, 0, out var rows) ? rows : [root];
    }

    private static bool TryFindJsonRowArray(JsonElement root, int depth, out JsonElement[] rows)
    {
        rows = [];
        if (depth > 4 || root.ValueKind != JsonValueKind.Object) return false;

        var properties = root.EnumerateObject().ToArray();
        var commonWrapperNames = new HashSet<string>(new[] { "data", "items", "results", "records", "rows" }, StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties.Where(property => commonWrapperNames.Contains(property.Name)))
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                rows = property.Value.EnumerateArray().ToArray();
                return true;
            }
            if (property.Value.ValueKind == JsonValueKind.Object && TryFindJsonRowArray(property.Value, depth + 1, out rows))
                return true;
        }

        var arrays = properties.Where(property => property.Value.ValueKind == JsonValueKind.Array).Select(property => property.Value).ToArray();
        if (arrays.Length == 1)
        {
            rows = arrays[0].EnumerateArray().ToArray();
            return true;
        }

        var objects = properties.Where(property => property.Value.ValueKind == JsonValueKind.Object).Select(property => property.Value).ToArray();
        return objects.Length == 1 && TryFindJsonRowArray(objects[0], depth + 1, out rows);
    }

    private static void FlattenJsonObject(JsonElement source, string prefix, Dictionary<string, string> values, List<string> names)
    {
        foreach (var property in source.EnumerateObject())
        {
            var name = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                FlattenJsonObject(property.Value, name, values, names);
                continue;
            }

            values[name] = JsonText(property.Value);
            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase)) names.Add(name);
        }
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
        new() { Name = "pageId", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageName", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageNumber", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "objectId", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "objectName", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "kind", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "x", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "y", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "width", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "height", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "rotation", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "layer", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "visible", ValueKind = PublicationDataValueKind.Boolean },
        new() { Name = "locked", ValueKind = PublicationDataValueKind.Boolean },
        // Legacy aliases retained so existing publications bound before v1.0.43 do not break.
        // Other legacy names differ only by casing and are already resolved by case-insensitive rows.
        new() { Name = "Page", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "Object", ValueKind = PublicationDataValueKind.Text }
    ];

    private static List<PublicationDataColumn> PublicationPageColumns() =>
    [
        new() { Name = "id", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "text", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageName", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "targetPageId", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageNumber", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "slug", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "width", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "height", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "orientation", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "background", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "elementCount", ValueKind = PublicationDataValueKind.Number }
    ];

    private static List<PublicationDataColumn> PublicationDocumentColumns() =>
    [
        new() { Name = "id", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "name", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "formatVersion", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "modifiedUtc", ValueKind = PublicationDataValueKind.DateTime },
        new() { Name = "pageCount", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "currentPageId", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "currentPageName", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "currentPageNumber", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "currentPageWidth", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "currentPageHeight", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "currentPageOrientation", ValueKind = PublicationDataValueKind.Text }
    ];

    private static List<PublicationDataColumn> PublicationMediaColumns() =>
    [
        new() { Name = "id", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageId", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageName", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "pageNumber", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "name", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "title", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "mediaType", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "mimeType", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "source", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "image", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "poster", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "altText", ValueKind = PublicationDataValueKind.Text },
        new() { Name = "duration", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "width", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "height", ValueKind = PublicationDataValueKind.Number },
        new() { Name = "visible", ValueKind = PublicationDataValueKind.Boolean }
    ];

    private static string MimeFromDataUrl(string dataUrl, string fallback)
    {
        if (!dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return fallback;
        var start = 5;
        var separator = dataUrl.IndexOf(';', start);
        var end = separator >= start ? separator : dataUrl.IndexOf(',', start);
        var value = end > start ? dataUrl[start..end] : string.Empty;
        return PublicationMediaData.NormalizeMimeType(value, fallback);
    }

    private static IReadOnlyList<PublicationDataRow> BuildPublicationMediaRows(PublicationDocument document, DocumentObjectDataScope scope, Guid currentPageId)
    {
        var pages = scope == DocumentObjectDataScope.CurrentPage
            ? document.Pages.Where(page => page.Id == currentPageId)
            : document.Pages;
        return pages.SelectMany(page => page.Elements.Select(element => (page, element)))
            .Where(pair => pair.element is ImageFrameElement or VideoElement or AudioElement)
            .Select(pair =>
            {
                var pageNumber = document.Pages.IndexOf(pair.page) + 1;
                var mediaType = pair.element switch
                {
                    ImageFrameElement => "image",
                    VideoElement => "video",
                    AudioElement => "audio",
                    _ => "media"
                };
                var source = pair.element switch
                {
                    ImageFrameElement image => image.DataUrl,
                    PublicationMediaElement media => media.DataUrl,
                    _ => string.Empty
                };
                var mimeType = pair.element switch
                {
                    ImageFrameElement image when image.DataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                        => MimeFromDataUrl(image.DataUrl, "image/png"),
                    ImageFrameElement => "image/*",
                    PublicationMediaElement media => PublicationMediaData.NormalizeMimeType(media.MimeType, mediaType == "video" ? "video/mp4" : "audio/mpeg"),
                    _ => "application/octet-stream"
                };
                var poster = pair.element is VideoElement videoElement
                    ? videoElement.PosterDataUrl
                    : string.Empty;
                var altText = pair.element switch
                {
                    ImageFrameElement image => image.AltText,
                    VideoElement videoWithAltText => videoWithAltText.AltText,
                    _ => pair.element.Name
                };
                var duration = pair.element is PublicationMediaElement timed ? timed.DurationSeconds : 0;
                var thumbnail = mediaType == "video" && !string.IsNullOrWhiteSpace(poster) ? poster : source;
                return new PublicationDataRow
                {
                    Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["id"] = pair.element.Id.ToString("D"),
                        ["pageId"] = pair.page.Id.ToString("D"),
                        ["pageName"] = pair.page.Name,
                        ["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture),
                        ["name"] = pair.element.Name,
                        ["title"] = pair.element.Name,
                        ["mediaType"] = mediaType,
                        ["mimeType"] = mimeType,
                        ["source"] = source,
                        ["image"] = thumbnail,
                        ["poster"] = poster,
                        ["altText"] = altText,
                        ["duration"] = duration.ToString("0.###", CultureInfo.InvariantCulture),
                        ["width"] = pair.element.Width.ToString("0.###", CultureInfo.InvariantCulture),
                        ["height"] = pair.element.Height.ToString("0.###", CultureInfo.InvariantCulture),
                        ["visible"] = pair.element.Visible.ToString()
                    }
                };
            }).ToArray();
    }

    private static IReadOnlyList<PublicationDataRow> BuildDocumentRows(PublicationDocument document, DocumentObjectDataScope scope, Guid currentPageId)
    {
        var pages = scope == DocumentObjectDataScope.CurrentPage
            ? document.Pages.Where(page => page.Id == currentPageId)
            : document.Pages;
        return pages.SelectMany(page => page.Elements.Select(element => new PublicationDataRow
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pageId"] = page.Id.ToString("D"),
                ["pageName"] = page.Name,
                ["pageNumber"] = (document.Pages.IndexOf(page) + 1).ToString(CultureInfo.InvariantCulture),
                ["objectId"] = element.Id.ToString("D"),
                ["objectName"] = element.Name,
                ["kind"] = element.Kind.ToString(),
                ["x"] = element.X.ToString("0.###", CultureInfo.InvariantCulture),
                ["y"] = element.Y.ToString("0.###", CultureInfo.InvariantCulture),
                ["width"] = element.Width.ToString("0.###", CultureInfo.InvariantCulture),
                ["height"] = element.Height.ToString("0.###", CultureInfo.InvariantCulture),
                ["rotation"] = element.Rotation.ToString("0.###", CultureInfo.InvariantCulture),
                ["layer"] = element.ZIndex.ToString(CultureInfo.InvariantCulture),
                ["visible"] = element.Visible.ToString(),
                ["locked"] = element.Locked.ToString(),
                ["Page"] = page.Name,
                ["Object"] = element.Name
            }
        })).ToArray();
    }

    private static IReadOnlyList<PublicationDataRow> BuildPublicationPageRows(PublicationDocument document)
        => document.Pages.Select((page, index) => new PublicationDataRow
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = page.Id.ToString("D"),
                ["text"] = page.Name,
                ["pageName"] = page.Name,
                ["targetPageId"] = page.Id.ToString("D"),
                ["pageNumber"] = (index + 1).ToString(CultureInfo.InvariantCulture),
                ["slug"] = Slug(page.Name, index + 1),
                ["width"] = page.WidthMm.ToString("0.###", CultureInfo.InvariantCulture),
                ["height"] = page.HeightMm.ToString("0.###", CultureInfo.InvariantCulture),
                ["orientation"] = page.WidthMm > page.HeightMm ? "landscape" : "portrait",
                ["background"] = page.Background,
                ["elementCount"] = page.Elements.Count.ToString(CultureInfo.InvariantCulture)
            }
        }).ToArray();

    private static IReadOnlyList<PublicationDataRow> BuildPublicationDocumentRows(PublicationDocument document, Guid currentPageId)
    {
        var page = document.Pages.FirstOrDefault(candidate => candidate.Id == currentPageId) ?? document.Pages.FirstOrDefault();
        var index = page is null ? -1 : document.Pages.IndexOf(page);
        return
        [
            new PublicationDataRow
            {
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = document.Id.ToString("D"),
                    ["name"] = document.Name,
                    ["formatVersion"] = document.FormatVersion,
                    ["modifiedUtc"] = document.ModifiedUtc.ToString("O", CultureInfo.InvariantCulture),
                    ["pageCount"] = document.Pages.Count.ToString(CultureInfo.InvariantCulture),
                    ["currentPageId"] = page?.Id.ToString("D") ?? string.Empty,
                    ["currentPageName"] = page?.Name ?? string.Empty,
                    ["currentPageNumber"] = (index + 1).ToString(CultureInfo.InvariantCulture),
                    ["currentPageWidth"] = page?.WidthMm.ToString("0.###", CultureInfo.InvariantCulture) ?? "0",
                    ["currentPageHeight"] = page?.HeightMm.ToString("0.###", CultureInfo.InvariantCulture) ?? "0",
                    ["currentPageOrientation"] = page is null ? string.Empty : page.WidthMm > page.HeightMm ? "landscape" : "portrait"
                }
            }
        ];
    }

    private static string Slug(string value, int fallback)
    {
        var text = new string((value ?? string.Empty).Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (text.Contains("--", StringComparison.Ordinal)) text = text.Replace("--", "-", StringComparison.Ordinal);
        text = text.Trim('-');
        return string.IsNullOrWhiteSpace(text) ? $"page-{fallback}" : text;
    }

}
