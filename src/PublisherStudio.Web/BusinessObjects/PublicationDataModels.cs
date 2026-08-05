using System.Globalization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported publication data source kind values.
/// </summary>
public enum PublicationDataSourceKind
{
    Json,
    DelimitedText,
    Xml,
    DocumentObjects,
    PublicationPages,
    PublicationDocument,
    PublicationMedia,
    Web
}

/// <summary>
/// Lists supported publication data value kind values.
/// </summary>
public enum PublicationDataValueKind
{
    Text,
    Number,
    Boolean,
    DateTime
}

/// <summary>
/// Lists supported document object data scope values.
/// </summary>
public enum DocumentObjectDataScope
{
    CurrentPage,
    AllPages
}

/// <summary>
/// Represents a publication data object.
/// </summary>
public sealed class PublicationDataObject
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Data";
    /// <summary>
    /// Gets or sets source kind.
    /// </summary>
    public PublicationDataSourceKind SourceKind { get; set; } = PublicationDataSourceKind.DelimitedText;
    /// <summary>
    /// Gets or sets raw source.
    /// </summary>
    public string RawSource { get; set; } = "Category,Value\nA,42\nB,67\nC,53";
    /// <summary>
    /// Gets or sets source reference.
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets delimiter.
    /// </summary>
    public string Delimiter { get; set; } = ",";
    /// <summary>
    /// Gets or sets first row contains headers.
    /// </summary>
    public bool FirstRowContainsHeaders { get; set; } = true;
    /// <summary>
    /// Gets or sets document scope.
    /// </summary>
    public DocumentObjectDataScope DocumentScope { get; set; } = DocumentObjectDataScope.AllPages;
    /// <summary>
    /// Gets or sets web.
    /// </summary>
    public PublicationWebBinding Web { get; set; } = new();
    /// <summary>
    /// Gets or sets columns.
    /// </summary>
    public List<PublicationDataColumn> Columns { get; set; } = [];
    /// <summary>
    /// Gets or sets rows.
    /// </summary>
    public List<PublicationDataRow> Rows { get; set; } = [];
    /// <summary>
    /// Gets or sets the UTC modification time.
    /// </summary>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a publication data column.
/// </summary>
public sealed class PublicationDataColumn
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Column";
    /// <summary>
    /// Gets or sets value kind.
    /// </summary>
    public PublicationDataValueKind ValueKind { get; set; } = PublicationDataValueKind.Text;
}

/// <summary>
/// Represents a publication data row.
/// </summary>
public sealed class PublicationDataRow
{
    /// <summary>
    /// Gets or sets values.
    /// </summary>
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the get operation.
    /// </summary>
    public string Get(string field) => Values.TryGetValue(field, out var value) ? value : string.Empty;

    /// <summary>
    /// Gets number.
    /// </summary>
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
/// Lists supported data visual kind values.
/// </summary>
public enum DataVisualKind
{
    CartesianChart,
    PieChart,
    PolarChart,
    Sparkline,
    BarGauge,
    CircularGauge,
    LinearGauge,
    RangeSelector,
    Sankey,
    Funnel,
    Pyramid,
    TreeMap,
    DataTable,
    KpiProgress
}

/// <summary>
/// Lists supported cartesian chart style values.
/// </summary>
public enum CartesianChartStyle
{
    Bar,
    Line,
    Spline,
    Scatter,
    Area,
    SplineArea,
    StepLine,
    StepArea,
    StackedBar,
    FullStackedBar,
    StackedArea,
    FullStackedArea,
    StackedLine,
    FullStackedLine,
    StackedSpline,
    FullStackedSpline,
    StackedSplineArea,
    FullStackedSplineArea,
    RangeArea,
    RangeBar,
    Bubble,
    Candlestick,
    Stock
}

/// <summary>
/// Lists supported pie chart style values.
/// </summary>
public enum PieChartStyle
{
    Pie,
    Doughnut
}

/// <summary>
/// Lists supported polar chart style values.
/// </summary>
public enum PolarChartStyle
{
    Line,
    Area,
    Bar,
    StackedBar,
    Scatter
}

/// <summary>
/// Lists supported sparkline chart style values.
/// </summary>
public enum SparklineChartStyle
{
    Line,
    Spline,
    StepLine,
    Area,
    SplineArea,
    StepArea,
    Bar,
    WinLoss
}

/// <summary>
/// Lists supported data visual argument mode values.
/// </summary>
public enum DataVisualArgumentMode
{
    Auto,
    Discrete,
    Continuous,
    DateTime
}

/// <summary>
/// Lists supported data visual aggregation mode values.
/// </summary>
public enum DataVisualAggregationMode
{
    Auto,
    None,
    Sum,
    Average,
    Minimum,
    Maximum,
    Count
}

/// <summary>
/// Lists supported data visual sort mode values.
/// </summary>
public enum DataVisualSortMode
{
    DataOrder,
    ArgumentAscending,
    ArgumentDescending,
    ValueAscending,
    ValueDescending
}

/// <summary>
/// Represents a data visual element.
/// </summary>
public sealed class DataVisualElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.DataVisual;
    /// <summary>
    /// Gets or sets data object identifier.
    /// </summary>
    public Guid DataObjectId { get; set; }
    /// <summary>
    /// Gets or sets visual kind.
    /// </summary>
    public DataVisualKind VisualKind { get; set; } = DataVisualKind.CartesianChart;
    /// <summary>
    /// Gets or sets cartesian style.
    /// </summary>
    public CartesianChartStyle CartesianStyle { get; set; } = CartesianChartStyle.Bar;
    /// <summary>
    /// Gets or sets pie style.
    /// </summary>
    public PieChartStyle PieStyle { get; set; }
    /// <summary>
    /// Gets or sets polar style.
    /// </summary>
    public PolarChartStyle PolarStyle { get; set; }
    /// <summary>
    /// Gets or sets sparkline style.
    /// </summary>
    public SparklineChartStyle SparklineStyle { get; set; }
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = "Chart";
    /// <summary>
    /// Gets or sets argument field.
    /// </summary>
    public string ArgumentField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets series field.
    /// </summary>
    public string SeriesField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets argument mode.
    /// </summary>
    public DataVisualArgumentMode ArgumentMode { get; set; } = DataVisualArgumentMode.Auto;
    /// <summary>
    /// Gets or sets aggregation mode.
    /// </summary>
    public DataVisualAggregationMode AggregationMode { get; set; } = DataVisualAggregationMode.Auto;
    /// <summary>
    /// Gets or sets sort mode.
    /// </summary>
    public DataVisualSortMode SortMode { get; set; } = DataVisualSortMode.DataOrder;
    /// <summary>
    /// Gets or sets value fields.
    /// </summary>
    public List<string> ValueFields { get; set; } = [];
    /// <summary>
    /// Gets or sets low value field.
    /// </summary>
    public string LowValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets high value field.
    /// </summary>
    public string HighValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets open value field.
    /// </summary>
    public string OpenValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets close value field.
    /// </summary>
    public string CloseValueField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets size field.
    /// </summary>
    public string SizeField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target field.
    /// </summary>
    public string TargetField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parent field.
    /// </summary>
    public string ParentField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets show legend.
    /// </summary>
    public bool ShowLegend { get; set; } = true;
    /// <summary>
    /// Gets or sets show labels.
    /// </summary>
    public bool ShowLabels { get; set; }
    /// <summary>
    /// Gets or sets show title.
    /// </summary>
    public bool ShowTitle { get; set; } = true;
    /// <summary>
    /// Gets or sets table show header.
    /// </summary>
    public bool TableShowHeader { get; set; } = true;
    /// <summary>
    /// Gets or sets table show filter row.
    /// </summary>
    public bool TableShowFilterRow { get; set; }
    /// <summary>
    /// Gets or sets row limit.
    /// </summary>
    public int RowLimit { get; set; } = 12;
    /// <summary>
    /// Gets or sets minimum value.
    /// </summary>
    public double MinimumValue { get; set; }
    /// <summary>
    /// Gets or sets maximum value.
    /// </summary>
    public double MaximumValue { get; set; } = 100;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "#cbd5e1";
    /// <summary>
    /// Gets or sets border width millimetres.
    /// </summary>
    public double BorderWidthMm { get; set; } = .25;
}

/// <summary>
/// Represents a data chart point.
/// </summary>
public sealed record DataChartPoint(string Argument, string Series, double Value);
/// <summary>
/// Represents a data pie point.
/// </summary>
public sealed record DataPiePoint(string Argument, double Value);
/// <summary>
/// Represents a data spark point.
/// </summary>
public sealed record DataSparkPoint(string Argument, double Value);
/// <summary>
/// Represents a data range point.
/// </summary>
public sealed record DataRangePoint(string Argument, string Series, double Low, double High);
/// <summary>
/// Represents a data bubble point.
/// </summary>
public sealed record DataBubblePoint(string Argument, string Series, double Value, double Size);
/// <summary>
/// Represents a data financial point.
/// </summary>
public sealed record DataFinancialPoint(string Argument, double Open, double High, double Low, double Close);
/// <summary>
/// Represents a data sankey point.
/// </summary>
public sealed record DataSankeyPoint(string Source, string Target, double Weight);
/// <summary>
/// Represents a data tree map point.
/// </summary>
public sealed record DataTreeMapPoint(string Label, string Parent, double Value);

/// <summary>
/// Represents a publication grid row.
/// </summary>
public sealed class PublicationGridRow
{
    /// <summary>
    /// Gets or sets c1.
    /// </summary>
    public string C1 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c2.
    /// </summary>
    public string C2 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c3.
    /// </summary>
    public string C3 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c4.
    /// </summary>
    public string C4 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c5.
    /// </summary>
    public string C5 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c6.
    /// </summary>
    public string C6 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c7.
    /// </summary>
    public string C7 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets c8.
    /// </summary>
    public string C8 { get; set; } = string.Empty;


}
