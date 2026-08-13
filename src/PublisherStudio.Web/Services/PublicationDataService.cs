using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Coordinates publication behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="gridRows">Publication grid row factory dependency used by the publication workflow to provide the corresponding application capability.</param>
/// <param name="mediaData">Media data value supplied to the publication operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationDataService(IPublicationGridRowFactory gridRows, PublicationMediaData mediaData, ILogger<PublicationDataService> logger)
{
    /// <summary>
    /// Creates sample as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The publication data object produced by the operation.</returns>
    public PublicationDataObject CreateSample(string name = "Quarterly Sales")
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.CreateSample.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.CreateSample failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs clone as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The publication data object produced by the operation.</returns>
    public PublicationDataObject Clone(PublicationDataObject source)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.Clone.");
                    var json = JsonSerializer.Serialize(source);
                    return JsonSerializer.Deserialize<PublicationDataObject>(json) ?? CreateSample();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.Clone failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs normalize as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    public void Normalize(PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.Normalize.");
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
                        // ValueKindExplicit did not exist in older publication files. Re-infer those legacy
                        // columns from their canonical rows so spreadsheet snapshots that were previously
                        // frozen as Text immediately become chart-safe without forcing a destructive re-import.
                        if (data.Rows.Count > 0 && !IsLiveSource(data.SourceKind))
                        {
                            foreach (var column in data.Columns.Where(column => !column.ValueKindExplicit && !string.IsNullOrWhiteSpace(column.Name)))
                                column.ValueKind = InferKind(data.Rows.Select(row => row.Get(column.Name)));
                        }
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.Normalize failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses into as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    public void ParseInto(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ParseInto.");
                    var explicitKinds = data.Columns
                        .Where(column => column.ValueKindExplicit && !string.IsNullOrWhiteSpace(column.Name))
                        .ToDictionary(column => column.Name, column => column.ValueKind, StringComparer.OrdinalIgnoreCase);
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
                    foreach (var column in data.Columns)
                    {
                        if (!explicitKinds.TryGetValue(column.Name, out var explicitKind)) continue;
                        column.ValueKind = explicitKind;
                        column.ValueKindExplicit = true;
                    }
                    data.ModifiedUtc = DateTimeOffset.UtcNow;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ParseInto failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Resolves rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<PublicationDataRow> ResolveRows(PublicationDocument document, PublicationDataObject? data, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ResolveRows.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ResolveRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Resolves columns as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<PublicationDataColumn> ResolveColumns(PublicationDataObject? data)
        {
            try
            {
                logger.LogTrace($"Entering PublicationDataService.ResolveColumns.");
                return data?.SourceKind switch
        {
            PublicationDataSourceKind.DocumentObjects => DocumentObjectColumns(),
            PublicationDataSourceKind.PublicationPages => PublicationPageColumns(),
            PublicationDataSourceKind.PublicationDocument => PublicationDocumentColumns(),
            PublicationDataSourceKind.PublicationMedia => PublicationMediaColumns(),
            _ => data?.Columns ?? []
        };
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublicationDataService.ResolveColumns failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Ensures built in objects as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    public void EnsureBuiltInObjects(PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.EnsureBuiltInObjects.");
                    document.DataObjects ??= [];
                    EnsureBuiltIn(document, PublicationDataSourceKind.PublicationPages, "Publication pages");
                    EnsureBuiltIn(document, PublicationDataSourceKind.PublicationDocument, "Publication document");
                    EnsureBuiltIn(document, PublicationDataSourceKind.DocumentObjects, "Publication objects");
                    EnsureBuiltIn(document, PublicationDataSourceKind.PublicationMedia, "Publication media");
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.EnsureBuiltInObjects failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures built in as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="kind">Kind value supplied to the publication operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the publication operation and used when producing its result.</param>
    private void EnsureBuiltIn(PublicationDocument document, PublicationDataSourceKind kind, string name)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.EnsureBuiltIn.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.EnsureBuiltIn failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds chart points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataChartPoint> BuildChartPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildChartPoints.");
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
                                GetNumber(row, valueField)));
                        }
                    }
                    return points;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildChartPoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds pie points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataPiePoint> BuildPiePoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildPiePoints.");
                    var field = item.ValueFields.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(field)) return [];
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    return ResolveRows(document, data, currentPageId)
                        .Select((row, index) => new DataPiePoint(
                            string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
                            GetNumber(row, field)))
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildPiePoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds spark points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataSparkPoint> BuildSparkPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildSparkPoints.");
                    var field = item.ValueFields.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(field)) return [];
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    return ResolveRows(document, data, currentPageId)
                        .Select((row, index) => new DataSparkPoint(
                            string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
                            GetNumber(row, field)))
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildSparkPoints failed: {exception.Message}");
            throw;
        }
    }


    /// <summary>
    /// Builds range points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataRangePoint> BuildRangePoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildRangePoints.");
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    var lowField = !string.IsNullOrWhiteSpace(item.LowValueField) ? item.LowValueField : item.ValueFields.ElementAtOrDefault(0) ?? string.Empty;
                    var highField = !string.IsNullOrWhiteSpace(item.HighValueField) ? item.HighValueField : item.ValueFields.ElementAtOrDefault(1) ?? lowField;
                    if (string.IsNullOrWhiteSpace(lowField) || string.IsNullOrWhiteSpace(highField)) return [];
                    return ResolveRows(document, data, currentPageId).Select((row, index) =>
                    {
                        var argument = string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField);
                        var series = string.IsNullOrWhiteSpace(item.SeriesField) ? item.Title : row.Get(item.SeriesField);
                        return new DataRangePoint(string.IsNullOrWhiteSpace(argument) ? "(blank)" : argument, series, GetNumber(row, lowField), GetNumber(row, highField));
                    }).ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildRangePoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds bubble points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataBubblePoint> BuildBubblePoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildBubblePoints.");
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    var valueField = item.ValueFields.FirstOrDefault() ?? string.Empty;
                    var sizeField = !string.IsNullOrWhiteSpace(item.SizeField) ? item.SizeField : item.ValueFields.ElementAtOrDefault(1) ?? valueField;
                    if (string.IsNullOrWhiteSpace(valueField)) return [];
                    return ResolveRows(document, data, currentPageId).Select((row, index) =>
                    {
                        var argument = string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField);
                        var series = string.IsNullOrWhiteSpace(item.SeriesField) ? item.Title : row.Get(item.SeriesField);
                        return new DataBubblePoint(string.IsNullOrWhiteSpace(argument) ? "(blank)" : argument, series, GetNumber(row, valueField), Math.Abs(GetNumber(row, sizeField)));
                    }).ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildBubblePoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds financial points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataFinancialPoint> BuildFinancialPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildFinancialPoints.");
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    var open = !string.IsNullOrWhiteSpace(item.OpenValueField) ? item.OpenValueField : item.ValueFields.ElementAtOrDefault(0) ?? string.Empty;
                    var high = !string.IsNullOrWhiteSpace(item.HighValueField) ? item.HighValueField : item.ValueFields.ElementAtOrDefault(1) ?? open;
                    var low = !string.IsNullOrWhiteSpace(item.LowValueField) ? item.LowValueField : item.ValueFields.ElementAtOrDefault(2) ?? open;
                    var close = !string.IsNullOrWhiteSpace(item.CloseValueField) ? item.CloseValueField : item.ValueFields.ElementAtOrDefault(3) ?? open;
                    if (string.IsNullOrWhiteSpace(open)) return [];
                    return ResolveRows(document, data, currentPageId).Select((row, index) => new DataFinancialPoint(
                        string.IsNullOrWhiteSpace(item.ArgumentField) ? (index + 1).ToString(CultureInfo.InvariantCulture) : row.Get(item.ArgumentField),
                        GetNumber(row, open), GetNumber(row, high), GetNumber(row, low), GetNumber(row, close))).ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildFinancialPoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds sankey points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataSankeyPoint> BuildSankeyPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildSankeyPoints.");
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    var weightField = item.ValueFields.FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(item.ArgumentField) || string.IsNullOrWhiteSpace(item.TargetField) || string.IsNullOrWhiteSpace(weightField)) return [];
                    return ResolveRows(document, data, currentPageId)
                        .Select(row => new DataSankeyPoint(row.Get(item.ArgumentField), row.Get(item.TargetField), GetNumber(row, weightField)))
                        .Where(point => !string.IsNullOrWhiteSpace(point.Source) && !string.IsNullOrWhiteSpace(point.Target))
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildSankeyPoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds tree map points as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DataTreeMapPoint> BuildTreeMapPoints(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildTreeMapPoints.");
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    var valueField = item.ValueFields.FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(item.ArgumentField) || string.IsNullOrWhiteSpace(valueField)) return [];
                    return ResolveRows(document, data, currentPageId)
                        .Select(row => new DataTreeMapPoint(row.Get(item.ArgumentField), string.IsNullOrWhiteSpace(item.ParentField) ? string.Empty : row.Get(item.ParentField), GetNumber(row, valueField)))
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildTreeMapPoints failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds client visual configuration as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The object produced by the operation.</returns>
    public object BuildClientVisualConfiguration(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildClientVisualConfiguration.");
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
                        argumentMode = item.ArgumentMode.ToString(),
                        aggregationMode = item.AggregationMode.ToString(),
                        sortMode = item.SortMode.ToString(),
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildClientVisualConfiguration failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs grid columns as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<string> GridColumns(PublicationDataObject? data)
        {
            try
            {
                logger.LogTrace($"Entering PublicationDataService.GridColumns.");
                return ResolveColumns(data).Select(column => column.Name).Take(8).ToArray();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublicationDataService.GridColumns failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Builds grid rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="item">Item value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<PublicationGridRow> BuildGridRows(PublicationDocument document, DataVisualElement item, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildGridRows.");
                    var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.DataObjectId);
                    var columns = GridColumns(data);
                    return ResolveRows(document, data, currentPageId)
                        .Take(Math.Clamp(item.RowLimit, 1, 100))
                        .Select(row => gridRows.Create(row, columns))
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildGridRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses web snapshot as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    private void ParseWebSnapshot(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ParseWebSnapshot.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ParseWebSnapshot failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs detect web response format as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The publication web response format produced by the operation.</returns>
    private PublicationWebResponseFormat DetectWebResponseFormat(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.DetectWebResponseFormat.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.DetectWebResponseFormat failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs select JSON path as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the publication operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SelectJsonPath(string source, string path)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.SelectJsonPath.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.SelectJsonPath failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses JSON as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    private void ParseJson(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ParseJson.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ParseJson failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs select JSON rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
    private JsonElement[] SelectJsonRows(JsonElement root)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.SelectJsonRows.");
                    if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray().ToArray();
                    if (root.ValueKind != JsonValueKind.Object)
                        throw new InvalidDataException("JSON must contain an object, an array of objects, or a wrapped object array.");
                    return TryFindJsonRowArray(root, 0, out var rows) ? rows : [root];
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.SelectJsonRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Attempts to find JSON row array as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the publication operation and used when producing its result.</param>
    /// <param name="depth">Depth value supplied to the publication operation and used when producing its result.</param>
    /// <param name="rows">Rows value supplied to the publication operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryFindJsonRowArray(JsonElement root, int depth, out JsonElement[] rows)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.TryFindJsonRowArray.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.TryFindJsonRowArray failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs flatten JSON object as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the publication operation and used when producing its result.</param>
    /// <param name="prefix">Prefix value supplied to the publication operation and used when producing its result.</param>
    /// <param name="values">Values value supplied to the publication operation and used when producing its result.</param>
    /// <param name="names">Names value supplied to the publication operation and used when producing its result.</param>
    private void FlattenJsonObject(JsonElement source, string prefix, Dictionary<string, string> values, List<string> names)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.FlattenJsonObject.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.FlattenJsonObject failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs JSON text as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string JsonText(JsonElement value) {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.JsonText.");
            return value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => value.GetRawText()
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.JsonText failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses XML as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    private void ParseXml(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ParseXml.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ParseXml failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds XML value as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">Values value supplied to the publication operation and used when producing its result.</param>
    /// <param name="names">Names value supplied to the publication operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the publication operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the publication operation and used when producing its result.</param>
    /// <param name="attribute">Value indicating whether attribute should apply to this operation.</param>
    private void AddXmlValue(Dictionary<string, string> values, List<string> names, string name, string value, bool attribute)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.AddXmlValue.");
                    var basis = string.IsNullOrWhiteSpace(name) ? (attribute ? "Attribute" : "Value") : name.Trim();
                    var candidate = basis;
                    if (values.ContainsKey(candidate) && attribute) candidate = $"@{basis}";
                    var suffix = 2;
                    while (values.ContainsKey(candidate)) candidate = $"{basis} {suffix++}";
                    values[candidate] = value?.Trim() ?? string.Empty;
                    if (!names.Contains(candidate, StringComparer.OrdinalIgnoreCase)) names.Add(candidate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.AddXmlValue failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses delimited as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    private void ParseDelimited(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ParseDelimited.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ParseDelimited failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses delimited rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the publication operation and used when producing its result.</param>
    /// <param name="delimiter">Delimiter value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<List<string>> ParseDelimitedRows(string source, char delimiter)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.ParseDelimitedRows.");
                    source ??= string.Empty;
                    var result = new List<List<string>>();
                    var row = new List<string>();
                    var value = new StringBuilder();
                    var quoted = false;
                    for (var index = 0; index < source.Length; index++)
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.ParseDelimitedRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs make unique as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="names">Names value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> MakeUnique(List<string> names)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.MakeUnique.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.MakeUnique failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs infer kind as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">String dependency used by the publication workflow to provide the corresponding application capability.</param>
    /// <returns>The publication data value kind produced by the operation.</returns>
    public PublicationDataValueKind InferKind(IEnumerable<string> values)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.InferKind.");
            var materialized = values.Where(value => !string.IsNullOrWhiteSpace(value)).Take(100).ToArray();
            if (materialized.Length == 0) return PublicationDataValueKind.Text;
            if (materialized.All(value => TryParseNumber(value, out _))) return PublicationDataValueKind.Number;
            if (materialized.All(value => bool.TryParse(value, out _))) return PublicationDataValueKind.Boolean;
            if (materialized.All(value => DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out _)
                || DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _))) return PublicationDataValueKind.DateTime;
            return PublicationDataValueKind.Text;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.InferKind failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>Parses numeric publication values including localized thousands separators, currency symbols, percentages, and accounting negatives.</summary>
    /// <param name="value">Value value supplied to the publication operation and used when producing its result.</param>
    /// <param name="number">Number value supplied to the publication operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryParseNumber(string? value, out double number)
    {
        number = 0;
        try
        {
            logger.LogTrace("Entering PublicationDataService.TryParseNumber.");
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim().Replace("\u00a0", string.Empty).Replace("\u202f", string.Empty);
            var percent = text.EndsWith('%');
            if (percent) text = text[..^1].Trim();
            var styles = NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowParentheses;

            // Spreadsheet display text often arrives as already-formatted currency. When there is a single
            // separator followed by exactly three digits (for example "€858,882"), interpreting that
            // separator as a decimal merely because the machine culture is de-DE would silently turn
            // 858,882 into 858.882. Preserve the spreadsheet's visible grouping intent before trying cultures.
            var hasCurrencySymbol = text.Any(character => CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.CurrencySymbol);
            if (hasCurrencySymbol)
            {
                var compact = new string(text.Where(character => char.IsDigit(character) || character is '-' or '+' or '.' or ',' or '(' or ')').ToArray());
                var commaCount = compact.Count(character => character == ',');
                var dotCount = compact.Count(character => character == '.');
                var groupingSeparator = commaCount == 1 && dotCount == 0 ? ',' : dotCount == 1 && commaCount == 0 ? '.' : '\0';
                if (groupingSeparator != '\0')
                {
                    var separatorIndex = compact.IndexOf(groupingSeparator);
                    var digitsAfter = compact[(separatorIndex + 1)..].Count(char.IsDigit);
                    var digitsBefore = compact[..separatorIndex].Count(char.IsDigit);
                    if (digitsBefore > 0 && digitsAfter == 3)
                    {
                        var accountingNegative = compact.StartsWith('(') && compact.EndsWith(')');
                        var grouped = compact.Replace(groupingSeparator.ToString(), string.Empty).Trim('(', ')');
                        if (double.TryParse(grouped, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                        {
                            if (accountingNegative) number = -Math.Abs(number);
                            if (percent) number /= 100d;
                            return true;
                        }
                    }
                }
            }

            var cultures = new[]
            {
                CultureInfo.CurrentCulture, CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE"),
                CultureInfo.GetCultureInfo("fr-FR"), CultureInfo.GetCultureInfo("en-GB")
            };
            foreach (var culture in cultures.DistinctBy(culture => culture.Name))
            {
                if (!double.TryParse(text, styles, culture, out number)) continue;
                if (percent) number /= 100d;
                return true;
            }

            // Spreadsheet display text can combine a currency symbol with separators from a different locale.
            // Strip only currency/spacing characters, then infer the final decimal/group separator conservatively.
            var cleaned = new string(text.Where(character => char.IsDigit(character) || character is '-' or '+' or '.' or ',' or '(' or ')').ToArray());
            var negative = cleaned.StartsWith('(') && cleaned.EndsWith(')');
            cleaned = cleaned.Trim('(', ')');
            var comma = cleaned.LastIndexOf(',');
            var dot = cleaned.LastIndexOf('.');
            if (comma >= 0 || dot >= 0)
            {
                var separator = comma > dot ? ',' : '.';
                var separatorIndex = Math.Max(comma, dot);
                var digitsAfter = cleaned.Length - separatorIndex - 1;
                var sameSeparatorCount = cleaned.Count(character => character == separator);
                var treatAsGrouping = digitsAfter == 3 && (sameSeparatorCount > 1 || cleaned[..separatorIndex].Count(char.IsDigit) >= 1);
                cleaned = treatAsGrouping
                    ? cleaned.Replace(",", string.Empty).Replace(".", string.Empty)
                    : separator == ','
                        ? cleaned.Replace(".", string.Empty).Replace(',', '.')
                        : cleaned.Replace(",", string.Empty);
            }
            if (!double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
            if (negative) number = -Math.Abs(number);
            if (percent) number /= 100d;
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.TryParseNumber failed for a publication value.");
            number = 0;
            return false;
        }
    }

    /// <summary>Returns the numeric interpretation used by charts and components while retaining count semantics for nonnumeric values.</summary>
    /// <param name="row">Row value supplied to the publication operation and used when producing its result.</param>
    /// <param name="field">Field value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    public double GetNumber(PublicationDataRow row, string field)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.GetNumber.");
            var value = row.Get(field);
            if (TryParseNumber(value, out var number)) return number;
            if (bool.TryParse(value, out var boolean)) return boolean ? 1d : 0d;
            return string.IsNullOrWhiteSpace(value) ? 0d : 1d;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.GetNumber failed for field {Field}.", field);
            throw;
        }
    }

    /// <summary>Validates column names, declared data types, and row values for authoring feedback.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The publication data validation result produced by the operation.</returns>
    public PublicationDataValidationResult Validate(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.Validate.");
            var result = new PublicationDataValidationResult { ColumnCount = data.Columns.Count, RowCount = data.Rows.Count };
            void Add(PublicationDataValidationSeverity severity, string message, string column = "", int row = 0)
                => result.Issues.Add(new PublicationDataValidationIssue { Severity = severity, Message = message, ColumnName = column, RowNumber = row });

            var names = data.Columns.Select(column => column.Name?.Trim() ?? string.Empty).ToArray();
            if (names.Length == 0) Add(PublicationDataValidationSeverity.Error, "No columns were parsed.");
            if (names.Any(string.IsNullOrWhiteSpace)) Add(PublicationDataValidationSeverity.Error, "Every column needs a name.");
            if (names.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count(name => !string.IsNullOrWhiteSpace(name)))
                Add(PublicationDataValidationSeverity.Error, "Column names must be unique.");
            if (data.Rows.Count == 0) Add(PublicationDataValidationSeverity.Warning, "The dataset contains no data rows yet.");

            foreach (var column in data.Columns)
            {
                var values = data.Rows.Select((row, index) => (Value: row.Get(column.Name), Row: index + 1)).Where(pair => !string.IsNullOrWhiteSpace(pair.Value)).ToArray();
                foreach (var pair in values)
                {
                    var valid = column.ValueKind switch
                    {
                        PublicationDataValueKind.Number => TryParseNumber(pair.Value, out _),
                        PublicationDataValueKind.Boolean => bool.TryParse(pair.Value, out _),
                        PublicationDataValueKind.DateTime => DateTimeOffset.TryParse(pair.Value, out _),
                        _ => true
                    };
                    if (!valid) Add(PublicationDataValidationSeverity.Error, $"'{pair.Value}' is not a valid {column.ValueKind} value.", column.Name, pair.Row);
                    if (result.Issues.Count(issue => issue.Severity == PublicationDataValidationSeverity.Error) >= 12) break;
                }
                if (column.ValueKind == PublicationDataValueKind.Text && values.Length >= 3)
                {
                    var numeric = values.Count(pair => TryParseNumber(pair.Value, out _));
                    if (numeric > 0 && numeric >= Math.Ceiling(values.Length * .8))
                        Add(PublicationDataValidationSeverity.Warning, "Most values look numeric; consider Number or Auto for charting.", column.Name);
                }
            }

            result.Severity = result.Issues.Any(issue => issue.Severity == PublicationDataValidationSeverity.Error)
                ? PublicationDataValidationSeverity.Error
                : result.Issues.Any(issue => issue.Severity == PublicationDataValidationSeverity.Warning)
                    ? PublicationDataValidationSeverity.Warning
                    : PublicationDataValidationSeverity.Success;
            result.Message = result.Severity switch
            {
                PublicationDataValidationSeverity.Success => $"Ready · {result.RowCount} rows × {result.ColumnCount} columns validated.",
                PublicationDataValidationSeverity.Warning => $"Usable with warnings · {result.Issues.Count} item(s) need review.",
                _ => $"Cannot save cleanly · {result.Issues.Count(issue => issue.Severity == PublicationDataValidationSeverity.Error)} error(s)."
            };
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.Validate failed for data object {DataObjectId}.", data.Id);
            throw;
        }
    }

    /// <summary>Updates a column type, using inferred typing when <paramref name="kind"/> is null.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="columnName">Column name value supplied to the publication operation and used when producing its result.</param>
    /// <param name="kind">Kind value supplied to the publication operation and used when producing its result.</param>
    public void SetColumnKind(PublicationDataObject data, string columnName, PublicationDataValueKind? kind)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.SetColumnKind.");
            var column = data.Columns.FirstOrDefault(candidate => string.Equals(candidate.Name, columnName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"Column '{columnName}' was not found.");
            column.ValueKindExplicit = kind.HasValue;
            column.ValueKind = kind ?? InferKind(data.Rows.Select(row => row.Get(column.Name)));
            data.ModifiedUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.SetColumnKind failed for column {ColumnName}.", columnName);
            throw;
        }
    }

    /// <summary>Renames a managed snapshot column and moves all row values with it.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="oldName">Old name value supplied to the publication operation and used when producing its result.</param>
    /// <param name="newName">New name value supplied to the publication operation and used when producing its result.</param>
    public void RenameColumn(PublicationDataObject data, string oldName, string newName)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.RenameColumn.");
            newName = newName.Trim();
            if (string.IsNullOrWhiteSpace(newName)) throw new InvalidDataException("Column names cannot be blank.");
            if (data.Columns.Any(column => !string.Equals(column.Name, oldName, StringComparison.OrdinalIgnoreCase) && string.Equals(column.Name, newName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"Column '{newName}' already exists.");
            var column = data.Columns.First(candidate => string.Equals(candidate.Name, oldName, StringComparison.OrdinalIgnoreCase));
            foreach (var row in data.Rows)
            {
                var value = row.Get(column.Name);
                row.Values.Remove(column.Name);
                row.Values[newName] = value;
            }
            column.Name = newName;
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.RenameColumn failed from {OldName} to {NewName}.", oldName, newName);
            throw;
        }
    }

    /// <summary>Edits one cell in an embedded JSON snapshot.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="rowIndex">Row index value supplied to the publication operation and used when producing its result.</param>
    /// <param name="columnName">Column name value supplied to the publication operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the publication operation and used when producing its result.</param>
    public void SetCellValue(PublicationDataObject data, int rowIndex, string columnName, string value)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.SetCellValue.");
            if (rowIndex < 0 || rowIndex >= data.Rows.Count) throw new ArgumentOutOfRangeException(nameof(rowIndex));
            data.Rows[rowIndex].Values[columnName] = value ?? string.Empty;
            var column = data.Columns.FirstOrDefault(candidate => string.Equals(candidate.Name, columnName, StringComparison.OrdinalIgnoreCase));
            if (column is not null && !column.ValueKindExplicit) column.ValueKind = InferKind(data.Rows.Select(row => row.Get(column.Name)));
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.SetCellValue failed for row {RowIndex}, column {ColumnName}.", rowIndex, columnName);
            throw;
        }
    }

    /// <summary>
    /// Adds row as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    public void AddRow(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.AddRow.");
            EnsureManagedSnapshot(data);
            data.Rows.Add(new PublicationDataRow { Values = data.Columns.ToDictionary(column => column.Name, _ => string.Empty, StringComparer.OrdinalIgnoreCase) });
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.AddRow failed for data object {DataObjectId}.", data.Id);
            throw;
        }
    }

    /// <summary>Removes a row from an embedded JSON snapshot.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="rowIndex">Row index value supplied to the publication operation and used when producing its result.</param>
    public void RemoveRow(PublicationDataObject data, int rowIndex)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.RemoveRow.");
            EnsureManagedSnapshot(data);
            if (rowIndex >= 0 && rowIndex < data.Rows.Count) data.Rows.RemoveAt(rowIndex);
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.RemoveRow failed for row {RowIndex}.", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// Adds column as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="requestedName">Requested name value supplied to the publication operation and used when producing its result.</param>
    public void AddColumn(PublicationDataObject data, string? requestedName = null)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.AddColumn.");
            EnsureManagedSnapshot(data);
            var basis = string.IsNullOrWhiteSpace(requestedName) ? "Column" : requestedName.Trim();
            var name = basis;
            var suffix = 2;
            while (data.Columns.Any(column => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase))) name = $"{basis} {suffix++}";
            data.Columns.Add(new PublicationDataColumn { Name = name, ValueKind = PublicationDataValueKind.Text });
            foreach (var row in data.Rows) row.Values[name] = string.Empty;
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.AddColumn failed for data object {DataObjectId}.", data.Id);
            throw;
        }
    }

    /// <summary>Removes a column and its row values from an embedded JSON snapshot.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    /// <param name="columnName">Column name value supplied to the publication operation and used when producing its result.</param>
    public void RemoveColumn(PublicationDataObject data, string columnName)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.RemoveColumn.");
            EnsureManagedSnapshot(data);
            data.Columns.RemoveAll(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
            foreach (var row in data.Rows) row.Values.Remove(columnName);
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.RemoveColumn failed for column {ColumnName}.", columnName);
            throw;
        }
    }

    /// <summary>Converts a parsed file/API source to a stable editable JSON snapshot after explicit author action.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    public void EnsureManagedSnapshot(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.EnsureManagedSnapshot.");
            if (IsLiveSource(data.SourceKind)) throw new InvalidOperationException("Built-in live publication datasets cannot be detached or edited.");
            if (data.SourceKind == PublicationDataSourceKind.Json) return;
            if (data.Rows.Count == 0 && !string.IsNullOrWhiteSpace(data.RawSource)) ParseInto(data);
            var prior = data.SourceKind;
            data.SourceKind = PublicationDataSourceKind.Json;
            if (string.IsNullOrWhiteSpace(data.SourceReference)) data.SourceReference = $"Editable snapshot detached from {prior}";
            WriteManagedSnapshot(data);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.EnsureManagedSnapshot failed for data object {DataObjectId}.", data.Id);
            throw;
        }
    }

    /// <summary>Serializes canonical rows back into the editable JSON snapshot while preserving explicit schema types.</summary>
    /// <param name="data">Data value supplied to the publication operation and used when producing its result.</param>
    public void WriteManagedSnapshot(PublicationDataObject data)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.WriteManagedSnapshot.");
            if (data.SourceKind != PublicationDataSourceKind.Json) throw new InvalidOperationException("Table editing requires an embedded JSON snapshot.");
            var rows = data.Rows.Select(row => data.Columns.ToDictionary(column => column.Name, column => row.Get(column.Name), StringComparer.OrdinalIgnoreCase)).ToList();
            data.RawSource = JsonSerializer.Serialize(rows);
            data.ModifiedUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.WriteManagedSnapshot failed for data object {DataObjectId}.", data.Id);
            throw;
        }
    }

    /// <summary>
    /// Determines whether live source as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the publication operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsLiveSource(PublicationDataSourceKind kind)
    {
        try
        {
            logger.LogTrace("Entering PublicationDataService.IsLiveSource.");
            return kind is PublicationDataSourceKind.DocumentObjects or PublicationDataSourceKind.PublicationPages or PublicationDataSourceKind.PublicationDocument or PublicationDataSourceKind.PublicationMedia;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublicationDataService.IsLiveSource failed for {SourceKind}.", kind);
            throw;
        }
    }

    /// <summary>
    /// Performs document object columns as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationDataColumn> DocumentObjectColumns() {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.DocumentObjectColumns.");
            return [
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
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.DocumentObjectColumns failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs publication page columns as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationDataColumn> PublicationPageColumns() {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.PublicationPageColumns.");
            return [
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
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.PublicationPageColumns failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs publication document columns as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationDataColumn> PublicationDocumentColumns() {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.PublicationDocumentColumns.");
            return [
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
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.PublicationDocumentColumns failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs publication media columns as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PublicationDataColumn> PublicationMediaColumns() {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.PublicationMediaColumns.");
            return [
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
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.PublicationMediaColumns failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs MIME from data URL as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the publication operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string MimeFromDataUrl(string dataUrl, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.MimeFromDataUrl.");
                    if (!dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return fallback;
                    var start = 5;
                    var separator = dataUrl.IndexOf(';', start);
                    var end = separator >= start ? separator : dataUrl.IndexOf(',', start);
                    var value = end > start ? dataUrl[start..end] : string.Empty;
                    return mediaData.NormalizeMimeType(value, fallback);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.MimeFromDataUrl failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds publication media rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="scope">Scope value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<PublicationDataRow> BuildPublicationMediaRows(PublicationDocument document, DocumentObjectDataScope scope, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildPublicationMediaRows.");
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
                                PublicationMediaElement media => mediaData.NormalizeMimeType(media.MimeType, mediaType == "video" ? "video/mp4" : "audio/mpeg"),
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildPublicationMediaRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds document rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="scope">Scope value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<PublicationDataRow> BuildDocumentRows(PublicationDocument document, DocumentObjectDataScope scope, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildDocumentRows.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildDocumentRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds publication page rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<PublicationDataRow> BuildPublicationPageRows(PublicationDocument document)
        {
            try
            {
                logger.LogTrace($"Entering PublicationDataService.BuildPublicationPageRows.");
                return document.Pages.Select((page, index) => new PublicationDataRow
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
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublicationDataService.BuildPublicationPageRows failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Builds publication document rows as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication operation and used when producing its result.</param>
    /// <param name="currentPageId">Identifier of the current page to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<PublicationDataRow> BuildPublicationDocumentRows(PublicationDocument document, Guid currentPageId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.BuildPublicationDocumentRows.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.BuildPublicationDocumentRows failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs slug as part of the publication service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the publication operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Slug(string value, int fallback)
    {
        try
        {
            logger.LogTrace($"Entering PublicationDataService.Slug.");
                    var text = new string((value ?? string.Empty).Trim().ToLowerInvariant()
                        .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                        .ToArray());
                    while (text.Contains("--", StringComparison.Ordinal)) text = text.Replace("--", "-", StringComparison.Ordinal);
                    text = text.Trim('-');
                    return string.IsNullOrWhiteSpace(text) ? $"page-{fallback}" : text;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationDataService.Slug failed: {exception.Message}");
            throw;
        }
    }

}
