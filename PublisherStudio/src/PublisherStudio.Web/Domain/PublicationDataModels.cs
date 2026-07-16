using System.Globalization;

namespace PublisherStudio.Domain;

public enum PublicationDataSourceKind
{
    Json,
    DelimitedText,
    Xml,
    DocumentObjects
}

public enum PublicationDataValueKind
{
    Text,
    Number,
    Boolean,
    DateTime
}

public enum DocumentObjectDataScope
{
    CurrentPage,
    AllPages
}

public sealed class PublicationDataObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Data";
    public PublicationDataSourceKind SourceKind { get; set; } = PublicationDataSourceKind.DelimitedText;
    public string RawSource { get; set; } = "Category,Value\nA,42\nB,67\nC,53";
    public string Delimiter { get; set; } = ",";
    public bool FirstRowContainsHeaders { get; set; } = true;
    public DocumentObjectDataScope DocumentScope { get; set; } = DocumentObjectDataScope.AllPages;
    public List<PublicationDataColumn> Columns { get; set; } = [];
    public List<PublicationDataRow> Rows { get; set; } = [];
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PublicationDataColumn
{
    public string Name { get; set; } = "Column";
    public PublicationDataValueKind ValueKind { get; set; } = PublicationDataValueKind.Text;
}

public sealed class PublicationDataRow
{
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string Get(string field) => Values.TryGetValue(field, out var value) ? value : string.Empty;

    public double GetNumber(string field)
    {
        var value = Get(field);
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var current)
                ? current
                : 0;
    }
}

public enum DataVisualKind
{
    CartesianChart,
    PieChart,
    PolarChart,
    Sparkline,
    BarGauge,
    DataTable,
    KpiProgress
}

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
    FullStackedSplineArea
}

public enum PieChartStyle
{
    Pie,
    Doughnut
}

public enum PolarChartStyle
{
    Line,
    Area,
    Bar,
    Scatter
}

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

public sealed class DataVisualElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.DataVisual;
    public Guid DataObjectId { get; set; }
    public DataVisualKind VisualKind { get; set; } = DataVisualKind.CartesianChart;
    public CartesianChartStyle CartesianStyle { get; set; } = CartesianChartStyle.Bar;
    public PieChartStyle PieStyle { get; set; }
    public PolarChartStyle PolarStyle { get; set; }
    public SparklineChartStyle SparklineStyle { get; set; }
    public string Title { get; set; } = "Chart";
    public string ArgumentField { get; set; } = string.Empty;
    public string SeriesField { get; set; } = string.Empty;
    public List<string> ValueFields { get; set; } = [];
    public bool ShowLegend { get; set; } = true;
    public bool ShowLabels { get; set; }
    public bool ShowTitle { get; set; } = true;
    public bool TableShowHeader { get; set; } = true;
    public bool TableShowFilterRow { get; set; }
    public int RowLimit { get; set; } = 12;
    public double MinimumValue { get; set; }
    public double MaximumValue { get; set; } = 100;
    public string Background { get; set; } = "#ffffff";
    public string BorderColor { get; set; } = "#cbd5e1";
    public double BorderWidthMm { get; set; } = .25;
}

public sealed record DataChartPoint(string Argument, string Series, double Value);
public sealed record DataPiePoint(string Argument, double Value);
public sealed record DataSparkPoint(string Argument, double Value);

public sealed class PublicationGridRow
{
    public string C1 { get; set; } = string.Empty;
    public string C2 { get; set; } = string.Empty;
    public string C3 { get; set; } = string.Empty;
    public string C4 { get; set; } = string.Empty;
    public string C5 { get; set; } = string.Empty;
    public string C6 { get; set; } = string.Empty;
    public string C7 { get; set; } = string.Empty;
    public string C8 { get; set; } = string.Empty;

    public static PublicationGridRow From(PublicationDataRow row, IReadOnlyList<string> columns)
    {
        var values = columns.Take(8).Select(column => row.Get(column)).Concat(Enumerable.Repeat(string.Empty, 8)).Take(8).ToArray();
        return new PublicationGridRow
        {
            C1 = values[0], C2 = values[1], C3 = values[2], C4 = values[3],
            C5 = values[4], C6 = values[5], C7 = values[6], C8 = values[7]
        };
    }
}
