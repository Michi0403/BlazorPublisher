using System.Globalization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported publication data source kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationDataSourceKind
{
    /// <summary>
    /// Selects the JSON option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Json,
    /// <summary>
    /// Selects the delimited text option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DelimitedText,
    /// <summary>
    /// Selects the XML option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Xml,
    /// <summary>
    /// Selects the document objects option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DocumentObjects,
    /// <summary>
    /// Selects the publication pages option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PublicationPages,
    /// <summary>
    /// Selects the publication document option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PublicationDocument,
    /// <summary>
    /// Selects the publication media option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PublicationMedia,
    /// <summary>
    /// Selects the web option for <see cref="PublicationDataSourceKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Web
}

/// <summary>
/// Defines the supported publication data value kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationDataValueKind
{
    /// <summary>
    /// Selects the text option for <see cref="PublicationDataValueKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Text,
    /// <summary>
    /// Selects the number option for <see cref="PublicationDataValueKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Number,
    /// <summary>
    /// Selects the boolean option for <see cref="PublicationDataValueKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Boolean,
    /// <summary>
    /// Selects the date time option for <see cref="PublicationDataValueKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DateTime
}

/// <summary>
/// Defines the supported document object data scope values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum DocumentObjectDataScope
{
    /// <summary>
    /// Selects the current page option for <see cref="DocumentObjectDataScope"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CurrentPage,
    /// <summary>
    /// Selects the all pages option for <see cref="DocumentObjectDataScope"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AllPages
}

/// <summary>
/// Represents a publication data object application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationDataObject
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication data object instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationDataObject"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationDataObject"/>.</value>
    public string Name { get; set; } = "Data";
    /// <summary>
    /// Gets or sets the source kind value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source kind value exposed by <see cref="PublicationDataObject"/>.</value>
    public PublicationDataSourceKind SourceKind { get; set; } = PublicationDataSourceKind.DelimitedText;
    /// <summary>
    /// Gets or sets the raw source value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The raw source value exposed by <see cref="PublicationDataObject"/>.</value>
    public string RawSource { get; set; } = "Category,Value\nA,42\nB,67\nC,53";
    /// <summary>
    /// Gets or sets the source reference value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source reference value exposed by <see cref="PublicationDataObject"/>.</value>
    public string SourceReference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the delimiter value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delimiter value exposed by <see cref="PublicationDataObject"/>.</value>
    public string Delimiter { get; set; } = ",";
    /// <summary>
    /// Gets or sets a value indicating whether first row contains headers applies to the publication data object state.
    /// </summary>
    /// <value>The first row contains headers value exposed by <see cref="PublicationDataObject"/>.</value>
    public bool FirstRowContainsHeaders { get; set; } = true;
    /// <summary>
    /// Gets or sets the document scope value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document scope value exposed by <see cref="PublicationDataObject"/>.</value>
    public DocumentObjectDataScope DocumentScope { get; set; } = DocumentObjectDataScope.AllPages;
    /// <summary>
    /// Gets or sets the web value that forms part of the publication data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The web value exposed by <see cref="PublicationDataObject"/>.</value>
    public PublicationWebBinding Web { get; set; } = new();
    /// <summary>
    /// Gets or sets the columns collection maintained or exposed by this publication data object instance for downstream processing.
    /// </summary>
    /// <value>The columns value exposed by <see cref="PublicationDataObject"/>.</value>
    public List<PublicationDataColumn> Columns { get; set; } = [];
    /// <summary>
    /// Gets or sets the rows collection maintained or exposed by this publication data object instance for downstream processing.
    /// </summary>
    /// <value>The rows value exposed by <see cref="PublicationDataObject"/>.</value>
    public List<PublicationDataRow> Rows { get; set; } = [];
    /// <summary>
    /// Gets or sets the modified UTC associated with this publication data object state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The modified UTC value exposed by <see cref="PublicationDataObject"/>.</value>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a publication data column application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationDataColumn
{
    /// <summary>
    /// Gets or sets the name value that forms part of the publication data column state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationDataColumn"/>.</value>
    public string Name { get; set; } = "Column";
    /// <summary>
    /// Gets or sets the value kind value that forms part of the publication data column state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value kind value exposed by <see cref="PublicationDataColumn"/>.</value>
    public PublicationDataValueKind ValueKind { get; set; } = PublicationDataValueKind.Text;
    /// <summary>Gets or sets whether the value kind was explicitly selected by the user instead of inferred from source values.</summary>
    /// <value><see langword="true"/> when parsing must preserve <see cref="ValueKind"/>.</value>
    public bool ValueKindExplicit { get; set; }
}

/// <summary>Defines the severity of publication-data validation feedback shown to authors.</summary>
public enum PublicationDataValidationSeverity
{
    /// <summary>
    /// Selects the success option for <see cref="PublicationDataValidationSeverity"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Success,
    /// <summary>
    /// Selects the warning option for <see cref="PublicationDataValidationSeverity"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Warning,
    /// <summary>
    /// Selects the error option for <see cref="PublicationDataValidationSeverity"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Error
}

/// <summary>Describes one publication-data validation issue.</summary>
public sealed class PublicationDataValidationIssue
{
    /// <summary>
    /// Gets or sets the severity value that forms part of the publication data validation issue state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The severity value exposed by <see cref="PublicationDataValidationIssue"/>.</value>
    public PublicationDataValidationSeverity Severity { get; set; }
    /// <summary>
    /// Gets or sets the column name value that forms part of the publication data validation issue state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The column name value exposed by <see cref="PublicationDataValidationIssue"/>.</value>
    public string ColumnName { get; set; } = string.Empty;
    /// <summary>Gets or sets the one-based affected row number, or zero when the issue is schema-wide.</summary>
    /// <value>The row number value exposed by <see cref="PublicationDataValidationIssue"/>.</value>
    public int RowNumber { get; set; }
    /// <summary>
    /// Gets or sets the message value that forms part of the publication data validation issue state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="PublicationDataValidationIssue"/>.</value>
    public string Message { get; set; } = string.Empty;
}

/// <summary>Summarizes validation of a publication data object or spreadsheet selection.</summary>
public sealed class PublicationDataValidationResult
{
    /// <summary>
    /// Gets or sets the severity value that forms part of the publication data validation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The severity value exposed by <see cref="PublicationDataValidationResult"/>.</value>
    public PublicationDataValidationSeverity Severity { get; set; } = PublicationDataValidationSeverity.Success;
    /// <summary>
    /// Gets or sets the message value that forms part of the publication data validation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="PublicationDataValidationResult"/>.</value>
    public string Message { get; set; } = "Data is ready.";
    /// <summary>
    /// Gets or sets the column count that quantifies the associated publication data validation data.
    /// </summary>
    /// <value>The column count value exposed by <see cref="PublicationDataValidationResult"/>.</value>
    public int ColumnCount { get; set; }
    /// <summary>
    /// Gets or sets the row count that quantifies the associated publication data validation data.
    /// </summary>
    /// <value>The row count value exposed by <see cref="PublicationDataValidationResult"/>.</value>
    public int RowCount { get; set; }
    /// <summary>
    /// Gets or sets the issues collection maintained or exposed by this publication data validation instance for downstream processing.
    /// </summary>
    /// <value>The issues value exposed by <see cref="PublicationDataValidationResult"/>.</value>
    public List<PublicationDataValidationIssue> Issues { get; set; } = [];
}


/// <summary>
/// Represents a publication data row application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationDataRow
{
    /// <summary>
    /// Gets or sets the values collection maintained or exposed by this publication data row instance for downstream processing.
    /// </summary>
    /// <value>The values value exposed by <see cref="PublicationDataRow"/>.</value>
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Performs get for <see cref="PublicationDataRow"/>, keeping the operation consistent with the state and invariants of the surrounding publication data row workflow.
    /// </summary>
    /// <param name="field">Field value supplied to the publication data row operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Get(string field) => Values.TryGetValue(field, out var value) ? value : string.Empty;

    /// <summary>
    /// Retrieves number for <see cref="PublicationDataRow"/>, keeping the operation consistent with the state and invariants of the surrounding publication data row workflow.
    /// </summary>
    /// <param name="field">Field value supplied to the publication data row operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    public double GetNumber(string field)
    {
        var value = Get(field);
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariant)) return invariant;
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var current)) return current;
        if (bool.TryParse(value, out var boolean)) return boolean ? 1 : 0;

        // Text and date fields are valid measures too: a non-empty value counts as one.
        // This keeps every parsed field available to charts without pretending that text
        // has an arbitrary numeric magnitude.
        return string.IsNullOrWhiteSpace(value) ? 0 : 1;
    }
}

/// <summary>
/// Defines the supported data visual kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum DataVisualKind
{
    /// <summary>
    /// Selects the cartesian chart option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CartesianChart,
    /// <summary>
    /// Selects the pie chart option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PieChart,
    /// <summary>
    /// Selects the polar chart option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PolarChart,
    /// <summary>
    /// Selects the sparkline option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sparkline,
    /// <summary>
    /// Selects the bar gauge option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BarGauge,
    /// <summary>
    /// Selects the circular gauge option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CircularGauge,
    /// <summary>
    /// Selects the linear gauge option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LinearGauge,
    /// <summary>
    /// Selects the range selector option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RangeSelector,
    /// <summary>
    /// Selects the sankey option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sankey,
    /// <summary>
    /// Selects the funnel option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Funnel,
    /// <summary>
    /// Selects the pyramid option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Pyramid,
    /// <summary>
    /// Selects the tree map option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TreeMap,
    /// <summary>
    /// Selects the data table option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DataTable,
    /// <summary>
    /// Selects the kpi progress option for <see cref="DataVisualKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    KpiProgress
}

/// <summary>
/// Defines the supported cartesian chart style values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum CartesianChartStyle
{
    /// <summary>
    /// Selects the bar option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Bar,
    /// <summary>
    /// Selects the line option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Line,
    /// <summary>
    /// Selects the spline option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Spline,
    /// <summary>
    /// Selects the scatter option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Scatter,
    /// <summary>
    /// Selects the area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Area,
    /// <summary>
    /// Selects the spline area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SplineArea,
    /// <summary>
    /// Selects the step line option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StepLine,
    /// <summary>
    /// Selects the step area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StepArea,
    /// <summary>
    /// Selects the stacked bar option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StackedBar,
    /// <summary>
    /// Selects the full stacked bar option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FullStackedBar,
    /// <summary>
    /// Selects the stacked area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StackedArea,
    /// <summary>
    /// Selects the full stacked area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FullStackedArea,
    /// <summary>
    /// Selects the stacked line option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StackedLine,
    /// <summary>
    /// Selects the full stacked line option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FullStackedLine,
    /// <summary>
    /// Selects the stacked spline option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StackedSpline,
    /// <summary>
    /// Selects the full stacked spline option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FullStackedSpline,
    /// <summary>
    /// Selects the stacked spline area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StackedSplineArea,
    /// <summary>
    /// Selects the full stacked spline area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FullStackedSplineArea,
    /// <summary>
    /// Selects the range area option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RangeArea,
    /// <summary>
    /// Selects the range bar option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RangeBar,
    /// <summary>
    /// Selects the bubble option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Bubble,
    /// <summary>
    /// Selects the candlestick option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Candlestick,
    /// <summary>
    /// Selects the stock option for <see cref="CartesianChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Stock
}

/// <summary>
/// Defines the supported pie chart style values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PieChartStyle
{
    /// <summary>
    /// Selects the pie option for <see cref="PieChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Pie,
    /// <summary>
    /// Selects the doughnut option for <see cref="PieChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Doughnut
}

/// <summary>
/// Defines the supported polar chart style values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PolarChartStyle
{
    /// <summary>
    /// Selects the line option for <see cref="PolarChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Line,
    /// <summary>
    /// Selects the area option for <see cref="PolarChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Area,
    /// <summary>
    /// Selects the bar option for <see cref="PolarChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Bar,
    /// <summary>
    /// Selects the stacked bar option for <see cref="PolarChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StackedBar,
    /// <summary>
    /// Selects the scatter option for <see cref="PolarChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Scatter
}

/// <summary>
/// Defines the supported sparkline chart style values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum SparklineChartStyle
{
    /// <summary>
    /// Selects the line option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Line,
    /// <summary>
    /// Selects the spline option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Spline,
    /// <summary>
    /// Selects the step line option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StepLine,
    /// <summary>
    /// Selects the area option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Area,
    /// <summary>
    /// Selects the spline area option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SplineArea,
    /// <summary>
    /// Selects the step area option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StepArea,
    /// <summary>
    /// Selects the bar option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Bar,
    /// <summary>
    /// Selects the win loss option for <see cref="SparklineChartStyle"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WinLoss
}

/// <summary>
/// Defines the supported data visual argument mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum DataVisualArgumentMode
{
    /// <summary>
    /// Selects the auto option for <see cref="DataVisualArgumentMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Auto,
    /// <summary>
    /// Selects the discrete option for <see cref="DataVisualArgumentMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Discrete,
    /// <summary>
    /// Selects the continuous option for <see cref="DataVisualArgumentMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Continuous,
    /// <summary>
    /// Selects the date time option for <see cref="DataVisualArgumentMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DateTime
}

/// <summary>
/// Defines the supported data visual aggregation mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum DataVisualAggregationMode
{
    /// <summary>
    /// Selects the auto option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Auto,
    /// <summary>
    /// Selects the none option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None,
    /// <summary>
    /// Selects the sum option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sum,
    /// <summary>
    /// Selects the average option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Average,
    /// <summary>
    /// Selects the minimum option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Minimum,
    /// <summary>
    /// Selects the maximum option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Maximum,
    /// <summary>
    /// Selects the count option for <see cref="DataVisualAggregationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Count
}

/// <summary>
/// Defines the supported data visual sort mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum DataVisualSortMode
{
    /// <summary>
    /// Selects the data order option for <see cref="DataVisualSortMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DataOrder,
    /// <summary>
    /// Selects the argument ascending option for <see cref="DataVisualSortMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArgumentAscending,
    /// <summary>
    /// Selects the argument descending option for <see cref="DataVisualSortMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArgumentDescending,
    /// <summary>
    /// Selects the value ascending option for <see cref="DataVisualSortMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ValueAscending,
    /// <summary>
    /// Selects the value descending option for <see cref="DataVisualSortMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ValueDescending
}

/// <summary>
/// Represents a data visual element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class DataVisualElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="DataVisualElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.DataVisual;
    /// <summary>
    /// Gets or sets the stable data object identifier used to identify or correlate this data visual element instance with related application state.
    /// </summary>
    /// <value>The data object identifier value exposed by <see cref="DataVisualElement"/>.</value>
    public Guid DataObjectId { get; set; }
    /// <summary>
    /// Gets or sets the visual kind value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The visual kind value exposed by <see cref="DataVisualElement"/>.</value>
    public DataVisualKind VisualKind { get; set; } = DataVisualKind.CartesianChart;
    /// <summary>
    /// Gets or sets the cartesian style value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cartesian style value exposed by <see cref="DataVisualElement"/>.</value>
    public CartesianChartStyle CartesianStyle { get; set; } = CartesianChartStyle.Bar;
    /// <summary>
    /// Gets or sets the pie style value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pie style value exposed by <see cref="DataVisualElement"/>.</value>
    public PieChartStyle PieStyle { get; set; }
    /// <summary>
    /// Gets or sets the polar style value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The polar style value exposed by <see cref="DataVisualElement"/>.</value>
    public PolarChartStyle PolarStyle { get; set; }
    /// <summary>
    /// Gets or sets the sparkline style value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sparkline style value exposed by <see cref="DataVisualElement"/>.</value>
    public SparklineChartStyle SparklineStyle { get; set; }
    /// <summary>
    /// Gets or sets the title value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="DataVisualElement"/>.</value>
    public string Title { get; set; } = "Chart";
    /// <summary>
    /// Gets or sets the argument field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The argument field value exposed by <see cref="DataVisualElement"/>.</value>
    public string ArgumentField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the series field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The series field value exposed by <see cref="DataVisualElement"/>.</value>
    public string SeriesField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the argument mode value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The argument mode value exposed by <see cref="DataVisualElement"/>.</value>
    public DataVisualArgumentMode ArgumentMode { get; set; } = DataVisualArgumentMode.Auto;
    /// <summary>
    /// Gets or sets the aggregation mode value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The aggregation mode value exposed by <see cref="DataVisualElement"/>.</value>
    public DataVisualAggregationMode AggregationMode { get; set; } = DataVisualAggregationMode.Auto;
    /// <summary>
    /// Gets or sets the sort mode value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sort mode value exposed by <see cref="DataVisualElement"/>.</value>
    public DataVisualSortMode SortMode { get; set; } = DataVisualSortMode.DataOrder;
    /// <summary>
    /// Gets or sets the value fields collection maintained or exposed by this data visual element instance for downstream processing.
    /// </summary>
    /// <value>The value fields value exposed by <see cref="DataVisualElement"/>.</value>
    public List<string> ValueFields { get; set; } = [];
    /// <summary>
    /// Gets or sets the low value field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The low value field value exposed by <see cref="DataVisualElement"/>.</value>
    public string LowValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the high value field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The high value field value exposed by <see cref="DataVisualElement"/>.</value>
    public string HighValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the open value field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The open value field value exposed by <see cref="DataVisualElement"/>.</value>
    public string OpenValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the close value field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The close value field value exposed by <see cref="DataVisualElement"/>.</value>
    public string CloseValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The size field value exposed by <see cref="DataVisualElement"/>.</value>
    public string SizeField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target field value exposed by <see cref="DataVisualElement"/>.</value>
    public string TargetField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parent field value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parent field value exposed by <see cref="DataVisualElement"/>.</value>
    public string ParentField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether show legend applies to the data visual element state.
    /// </summary>
    /// <value>The show legend value exposed by <see cref="DataVisualElement"/>.</value>
    public bool ShowLegend { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show labels applies to the data visual element state.
    /// </summary>
    /// <value>The show labels value exposed by <see cref="DataVisualElement"/>.</value>
    public bool ShowLabels { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether show title applies to the data visual element state.
    /// </summary>
    /// <value>The show title value exposed by <see cref="DataVisualElement"/>.</value>
    public bool ShowTitle { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether table show header applies to the data visual element state.
    /// </summary>
    /// <value>The table show header value exposed by <see cref="DataVisualElement"/>.</value>
    public bool TableShowHeader { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether table show filter row applies to the data visual element state.
    /// </summary>
    /// <value>The table show filter row value exposed by <see cref="DataVisualElement"/>.</value>
    public bool TableShowFilterRow { get; set; }
    /// <summary>
    /// Gets or sets the row limit value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The row limit value exposed by <see cref="DataVisualElement"/>.</value>
    public int RowLimit { get; set; } = 12;
    /// <summary>
    /// Gets or sets the minimum value value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum value value exposed by <see cref="DataVisualElement"/>.</value>
    public double MinimumValue { get; set; }
    /// <summary>
    /// Gets or sets the maximum value value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum value value exposed by <see cref="DataVisualElement"/>.</value>
    public double MaximumValue { get; set; } = 100;
    /// <summary>
    /// Gets or sets the background value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="DataVisualElement"/>.</value>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the border color value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="DataVisualElement"/>.</value>
    public string BorderColor { get; set; } = "#cbd5e1";
    /// <summary>
    /// Gets or sets the border width mm value that forms part of the data visual element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border width mm value exposed by <see cref="DataVisualElement"/>.</value>
    public double BorderWidthMm { get; set; } = .25;
}

/// <summary>
/// Represents a data chart point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Argument">Argument value supplied to the data chart point operation and used when producing its result.</param>
/// <param name="Series">Series value supplied to the data chart point operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the data chart point operation and used when producing its result.</param>
public sealed record DataChartPoint(string Argument, string Series, double Value);
/// <summary>
/// Represents a data pie point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Argument">Argument value supplied to the data pie point operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the data pie point operation and used when producing its result.</param>
public sealed record DataPiePoint(string Argument, double Value);
/// <summary>
/// Represents a data spark point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Argument">Argument value supplied to the data spark point operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the data spark point operation and used when producing its result.</param>
public sealed record DataSparkPoint(string Argument, double Value);
/// <summary>
/// Represents a data range point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Argument">Argument value supplied to the data range point operation and used when producing its result.</param>
/// <param name="Series">Series value supplied to the data range point operation and used when producing its result.</param>
/// <param name="Low">Low value supplied to the data range point operation and used when producing its result.</param>
/// <param name="High">High value supplied to the data range point operation and used when producing its result.</param>
public sealed record DataRangePoint(string Argument, string Series, double Low, double High);
/// <summary>
/// Represents a data bubble point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Argument">Argument value supplied to the data bubble point operation and used when producing its result.</param>
/// <param name="Series">Series value supplied to the data bubble point operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the data bubble point operation and used when producing its result.</param>
/// <param name="Size">Size value supplied to the data bubble point operation and used when producing its result.</param>
public sealed record DataBubblePoint(string Argument, string Series, double Value, double Size);
/// <summary>
/// Represents a data financial point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Argument">Argument value supplied to the data financial point operation and used when producing its result.</param>
/// <param name="Open">Open value supplied to the data financial point operation and used when producing its result.</param>
/// <param name="High">High value supplied to the data financial point operation and used when producing its result.</param>
/// <param name="Low">Low value supplied to the data financial point operation and used when producing its result.</param>
/// <param name="Close">Close value supplied to the data financial point operation and used when producing its result.</param>
public sealed record DataFinancialPoint(string Argument, double Open, double High, double Low, double Close);
/// <summary>
/// Represents a data sankey point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Source">Source value supplied to the data sankey point operation and used when producing its result.</param>
/// <param name="Target">Target value supplied to the data sankey point operation and used when producing its result.</param>
/// <param name="Weight">Weight value supplied to the data sankey point operation and used when producing its result.</param>
public sealed record DataSankeyPoint(string Source, string Target, double Weight);
/// <summary>
/// Represents a data tree map point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Label">Label value supplied to the data tree map point operation and used when producing its result.</param>
/// <param name="Parent">Parent value supplied to the data tree map point operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the data tree map point operation and used when producing its result.</param>
public sealed record DataTreeMapPoint(string Label, string Parent, double Value);

/// <summary>
/// Represents a publication grid row application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationGridRow
{
    /// <summary>
    /// Gets or sets the c1 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c1 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C1 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c2 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c2 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C2 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c3 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c3 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C3 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c4 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c4 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C4 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c5 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c5 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C5 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c6 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c6 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C6 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c7 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c7 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C7 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the c8 value that forms part of the publication grid row state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The c8 value exposed by <see cref="PublicationGridRow"/>.</value>
    public string C8 { get; set; } = string.Empty;


}
