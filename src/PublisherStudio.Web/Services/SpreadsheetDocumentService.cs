using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Creates workbook packages and produces a safe, static canvas representation without
/// requiring the separately licensed Office File API HTML exporter.
/// </summary>
public sealed class SpreadsheetDocumentService
{
    /// <summary>
    /// Stores the publication markup service dependency used by <see cref="SpreadsheetDocumentService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPublicationMarkupService _markup;
    /// <summary>
    /// Stores the internal default font style state used by <see cref="SpreadsheetDocumentService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly FontStyle defaultFontStyle;
    /// <summary>
    /// Stores the internal default cell style state used by <see cref="SpreadsheetDocumentService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly CellStyle defaultCellStyle;
    /// <summary>
    /// Stores the internal main state used by <see cref="SpreadsheetDocumentService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    /// <summary>
    /// Stores the internal relationships state used by <see cref="SpreadsheetDocumentService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    /// <summary>
    /// Stores the internal package relationships state used by <see cref="SpreadsheetDocumentService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    /// <summary>
    /// Initializes a new <see cref="SpreadsheetDocumentService"/> instance and captures the dependencies or initial state required by its spreadsheet document workflow.
    /// </summary>
    /// <param name="markup">Publication markup service dependency used by the spreadsheet document workflow to provide the corresponding application capability.</param>
    public SpreadsheetDocumentService(IPublicationMarkupService markup)
    {
        _markup = markup ?? throw new ArgumentNullException(nameof(markup));
        defaultFontStyle = new FontStyle("Calibri", 11, false, false, false, string.Empty);
        defaultCellStyle = new CellStyle(defaultFontStyle, string.Empty, string.Empty, null, null, false, false);
    }

    /// <summary>
    /// Creates blank xlsx as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sheetName">Sheet name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    public byte[] CreateBlankXlsx(string sheetName = "Sheet1")
    {
    try
    {
            sheetName = NormalizeSheetName(sheetName);
            using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(archive, "[Content_Types].xml", """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                      <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                      <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                      <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
                      <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
                    </Types>
                    """);
                WriteEntry(archive, "_rels/.rels", """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                      <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
                      <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
                    </Relationships>
                    """);
                WriteEntry(archive, "xl/workbook.xml", $"""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                      <bookViews><workbookView xWindow="0" yWindow="0" windowWidth="24000" windowHeight="12000"/></bookViews>
                      <sheets><sheet name="{SecurityElementEscape(sheetName)}" sheetId="1" r:id="rId1"/></sheets>
                      <calcPr calcId="191029" fullCalcOnLoad="1"/>
                    </workbook>
                    """);
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                      <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                    </Relationships>
                    """);
                WriteEntry(archive, "xl/worksheets/sheet1.xml", """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                      <dimension ref="A1"/>
                      <sheetViews><sheetView workbookViewId="0" showGridLines="1"><selection activeCell="A1" sqref="A1"/></sheetView></sheetViews>
                      <sheetFormatPr defaultRowHeight="15"/>
                      <sheetData/>
                      <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
                    </worksheet>
                    """);
                WriteEntry(archive, "xl/styles.xml", """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                      <fonts count="1"><font><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/><scheme val="minor"/></font></fonts>
                      <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
                      <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
                      <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                      <cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/></cellXfs>
                      <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                      <dxfs count="0"/><tableStyles count="0" defaultTableStyle="TableStyleMedium2" defaultPivotStyle="PivotStyleLight16"/>
                    </styleSheet>
                    """);
                var created = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                WriteEntry(archive, "docProps/core.xml", $"""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                      <dc:title>PublisherStudio Spreadsheet</dc:title><dc:creator>PublisherStudio</dc:creator>
                      <dcterms:created xsi:type="dcterms:W3CDTF">{created}</dcterms:created><dcterms:modified xsi:type="dcterms:W3CDTF">{created}</dcterms:modified>
                    </cp:coreProperties>
                    """);
                WriteEntry(archive, "docProps/app.xml", $"""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
                      <Application>PublisherStudio</Application><DocSecurity>0</DocSecurity><ScaleCrop>false</ScaleCrop>
                      <HeadingPairs><vt:vector size="2" baseType="variant"><vt:variant><vt:lpstr>Worksheets</vt:lpstr></vt:variant><vt:variant><vt:i4>1</vt:i4></vt:variant></vt:vector></HeadingPairs>
                      <TitlesOfParts><vt:vector size="1" baseType="lpstr"><vt:lpstr>{SecurityElementEscape(sheetName)}</vt:lpstr></vt:vector></TitlesOfParts>
                    </Properties>
                    """);
            }
            return output.ToArray();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.CreateBlankXlsx failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs detect format as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileName">File name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="contentType">Content type value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The spreadsheet storage format produced by the operation.</returns>
    public SpreadsheetStorageFormat DetectFormat(string? fileName, string? contentType = null)
    {
    try
    {
            return Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
            {
                ".xlsx" => SpreadsheetStorageFormat.Xlsx,
                ".xlsm" => SpreadsheetStorageFormat.Xlsm,
                ".xls" => SpreadsheetStorageFormat.Xls,
                ".csv" => SpreadsheetStorageFormat.Csv,
                ".txt" or ".tsv" => SpreadsheetStorageFormat.Text,
                _ when string.Equals(contentType, "text/csv", StringComparison.OrdinalIgnoreCase) => SpreadsheetStorageFormat.Csv,
                _ when contentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true => SpreadsheetStorageFormat.Text,
                _ => throw new InvalidDataException("Supported spreadsheet formats are XLSX, XLSM, XLS, CSV, and tab-delimited TXT.")
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.DetectFormat failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs default extension as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="format">Format value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string DefaultExtension(SpreadsheetStorageFormat format) {
    try
    {
        return format switch
    {
        SpreadsheetStorageFormat.Xlsm => ".xlsm",
        SpreadsheetStorageFormat.Xls => ".xls",
        SpreadsheetStorageFormat.Csv => ".csv",
        SpreadsheetStorageFormat.Text => ".txt",
        _ => ".xlsx"
    };
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.DefaultExtension failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes workbook file name as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileName">File name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeWorkbookFileName(string? fileName, SpreadsheetStorageFormat format)
    {
    try
    {
            var safe = _markup.SafeFileName(Path.GetFileNameWithoutExtension(fileName ?? "Spreadsheet"));
            return safe + DefaultExtension(format);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.NormalizeWorkbookFileName failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render preview HTML as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="activeSheetName">Active sheet name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string RenderPreviewHtml(byte[]? content, SpreadsheetStorageFormat format, out string activeSheetName)
    {
    try
    {
            activeSheetName = "Sheet1";
            if (content is null || content.Length == 0)
                return EmptyPreview(activeSheetName, "The workbook is empty.");

            try
            {
                return format switch
                {
                    SpreadsheetStorageFormat.Xlsx or SpreadsheetStorageFormat.Xlsm => RenderOpenXml(content, out activeSheetName),
                    SpreadsheetStorageFormat.Csv => RenderDelimitedText(content, DetectDelimiter(content, ','), "CSV", out activeSheetName),
                    SpreadsheetStorageFormat.Text => RenderDelimitedText(content, '\t', "Text", out activeSheetName),
                    SpreadsheetStorageFormat.Xls => EmptyPreview("Workbook", "Open this legacy XLS workbook in Spreadsheet Studio to generate its live canvas preview."),
                    _ => EmptyPreview("Workbook", "Open this workbook in Spreadsheet Studio to generate its canvas preview.")
                };
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or System.Xml.XmlException or ArgumentException)
            {
                activeSheetName = "Workbook";
                return EmptyPreview(activeSheetName, "The workbook is stored intact. Open Spreadsheet Studio to edit or repair its preview.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.RenderPreviewHtml failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Validates workbook content as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="format">Format value supplied to the spreadsheet document operation and used when producing its result.</param>
    public void ValidateWorkbookContent(byte[]? content, SpreadsheetStorageFormat format)
    {
    try
    {
            if (content is null || content.Length == 0)
                throw new InvalidDataException("The selected spreadsheet is empty.");

            switch (format)
            {
                case SpreadsheetStorageFormat.Xlsx:
                case SpreadsheetStorageFormat.Xlsm:
                    if (!IsOpenXmlWorkbook(content))
                        throw new InvalidDataException("The selected file is not a valid XLSX or XLSM workbook.");
                    using (var stream = new MemoryStream(content, writable: false))
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
                    {
                        _ = LoadEntry(archive, "xl/workbook.xml");
                        _ = LoadEntry(archive, "xl/_rels/workbook.xml.rels");
                    }
                    break;
                case SpreadsheetStorageFormat.Xls:
                    ReadOnlySpan<byte> compoundFileSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
                    if (content.Length < compoundFileSignature.Length || !content.AsSpan(0, compoundFileSignature.Length).SequenceEqual(compoundFileSignature))
                        throw new InvalidDataException("The selected file is not a valid legacy XLS workbook.");
                    break;
                case SpreadsheetStorageFormat.Csv:
                case SpreadsheetStorageFormat.Text:
                    _ = DecodeText(content);
                    break;
                default:
                    throw new InvalidDataException("Unsupported spreadsheet format.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ValidateWorkbookContent failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Determines whether open XML workbook as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsOpenXmlWorkbook(byte[]? content)
    {
    try
    {
            if (content is null || content.Length < 4 || content[0] != (byte)'P' || content[1] != (byte)'K') return false;
            try
            {
                using var stream = new MemoryStream(content, writable: false);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
                return archive.GetEntry("xl/workbook.xml") is not null && archive.GetEntry("[Content_Types].xml") is not null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.IsOpenXmlWorkbook failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render open XML as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="activeSheetName">Active sheet name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderOpenXml(byte[] content, out string activeSheetName)
    {
    try
    {
            if (!IsOpenXmlWorkbook(content)) throw new InvalidDataException("Invalid Office Open XML workbook.");
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var workbook = LoadEntry(archive, "xl/workbook.xml");
            var relationships = LoadEntry(archive, "xl/_rels/workbook.xml.rels");
            var sheets = workbook.Root?.Element(Main + "sheets")?.Elements(Main + "sheet").ToList() ?? [];
            if (sheets.Count == 0) throw new InvalidDataException("The workbook has no worksheet.");
            var activeTab = ParseInt(workbook.Root?.Element(Main + "bookViews")?.Element(Main + "workbookView")?.Attribute("activeTab")?.Value, 0);
            var candidates = new List<XElement>();
            if (activeTab >= 0 && activeTab < sheets.Count) candidates.Add(sheets[activeTab]);
            candidates.AddRange(sheets.Where(item => !string.Equals(item.Attribute("state")?.Value, "hidden", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.Attribute("state")?.Value, "veryHidden", StringComparison.OrdinalIgnoreCase)));
            candidates.AddRange(sheets);

            XElement? sheet = null;
            XElement? relationship = null;
            foreach (var candidate in candidates.Distinct())
            {
                var candidateRelationshipId = candidate.Attribute(Relationships + "id")?.Value;
                var candidateRelationship = relationships.Root?.Elements(PackageRelationships + "Relationship")
                    .FirstOrDefault(item => string.Equals(item.Attribute("Id")?.Value, candidateRelationshipId, StringComparison.Ordinal));
                if (candidateRelationship?.Attribute("Type")?.Value.EndsWith("/worksheet", StringComparison.OrdinalIgnoreCase) != true) continue;
                sheet = candidate;
                relationship = candidateRelationship;
                break;
            }
            if (sheet is null || relationship is null) throw new InvalidDataException("The workbook has no worksheet.");
            activeSheetName = sheet.Attribute("name")?.Value ?? "Sheet1";
            var target = relationship.Attribute("Target")?.Value ?? throw new InvalidDataException("Worksheet target missing.");
            var worksheetPath = NormalizeArchivePath("xl", target);
            var worksheet = LoadEntry(archive, worksheetPath);
            var sharedStrings = ReadSharedStrings(archive);
            var styles = ReadStyles(archive);
            var merged = ReadMergedRanges(worksheet);
            var rows = worksheet.Root?.Element(Main + "sheetData")?.Elements(Main + "row").ToList() ?? [];
            var columnWidths = ReadColumnWidths(worksheet);
            var cells = new Dictionary<(int Row, int Column), CellPreview>();
            var maxRow = 1;
            var maxColumn = 1;

            foreach (var row in rows)
            {
                var rowNumber = ParseInt(row.Attribute("r")?.Value, 1);
                foreach (var cell in row.Elements(Main + "c"))
                {
                    var reference = cell.Attribute("r")?.Value;
                    var (cellRow, cellColumn) = ParseCellReference(reference, rowNumber);
                    if (cellRow < 1 || cellColumn < 1) continue;
                    var styleIndex = ParseInt(cell.Attribute("s")?.Value, 0);
                    var value = ReadCellValue(cell, sharedStrings, styles.ElementAtOrDefault(styleIndex));
                    cells[(cellRow, cellColumn)] = new CellPreview(value, styleIndex);
                    maxRow = Math.Max(maxRow, cellRow);
                    maxColumn = Math.Max(maxColumn, cellColumn);
                }
            }

            foreach (var merge in merged)
            {
                maxRow = Math.Max(maxRow, merge.EndRow);
                maxColumn = Math.Max(maxColumn, merge.EndColumn);
            }
            maxRow = Math.Max(maxRow, 1);
            maxColumn = Math.Max(maxColumn, 1);

            var html = new StringBuilder(32_768);
            html.Append("<div class=\"spreadsheet-preview-document\" data-sheet=\"")
                .Append(WebUtility.HtmlEncode(activeSheetName)).Append("\"><table><colgroup>");
            for (var column = 1; column <= maxColumn; column++)
            {
                var width = columnWidths.TryGetValue(column, out var customWidth) ? customWidth : 64;
                html.Append("<col style=\"width:").Append(width.ToString("0.#", CultureInfo.InvariantCulture)).Append("px\">");
            }
            html.Append("</colgroup><tbody>");

            var covered = new HashSet<(int Row, int Column)>();
            for (var row = 1; row <= maxRow; row++)
            {
                var rowElement = rows.FirstOrDefault(item => ParseInt(item.Attribute("r")?.Value, 0) == row);
                var rowHeight = Math.Clamp(ParseDouble(rowElement?.Attribute("ht")?.Value, 0) * 1.333333, 0, 800);
                html.Append("<tr");
                if (rowHeight > 0) html.Append(" style=\"height:").Append(rowHeight.ToString("0.#", CultureInfo.InvariantCulture)).Append("px\"");
                html.Append('>');
                for (var column = 1; column <= maxColumn; column++)
                {
                    if (covered.Contains((row, column))) continue;
                    var merge = merged.FirstOrDefault(item => item.StartRow == row && item.StartColumn == column);
                    var cell = cells.TryGetValue((row, column), out var existingCell) ? existingCell : new CellPreview(string.Empty, 0);
                    var style = styles.ElementAtOrDefault(cell.StyleIndex) ?? defaultCellStyle;
                    var address = ColumnName(column) + row.ToString(CultureInfo.InvariantCulture);
                    html.Append("<td class=\"publisher-sheet-cell\" data-cell=\"")
                        .Append(address)
                        .Append("\" data-row=\"").Append(row.ToString(CultureInfo.InvariantCulture))
                        .Append("\" data-column=\"").Append(column.ToString(CultureInfo.InvariantCulture)).Append('"');
                    if (merge is not null)
                    {
                        var rowSpan = Math.Min(maxRow, merge.EndRow) - row + 1;
                        var columnSpan = Math.Min(maxColumn, merge.EndColumn) - column + 1;
                        if (rowSpan > 1) html.Append(" rowspan=\"").Append(rowSpan).Append('"');
                        if (columnSpan > 1) html.Append(" colspan=\"").Append(columnSpan).Append('"');
                        for (var coveredRow = row; coveredRow <= Math.Min(maxRow, merge.EndRow); coveredRow++)
                        for (var coveredColumn = column; coveredColumn <= Math.Min(maxColumn, merge.EndColumn); coveredColumn++)
                            if (coveredRow != row || coveredColumn != column) covered.Add((coveredRow, coveredColumn));
                    }
                    var css = style.ToCss(CssText);
                    if (!string.IsNullOrWhiteSpace(css)) html.Append(" style=\"").Append(WebUtility.HtmlEncode(css)).Append('"');
                    html.Append('>').Append(WebUtility.HtmlEncode(cell.Value)).Append("</td>");
                }
                html.Append("</tr>");
            }
            html.Append("</tbody></table><span class=\"spreadsheet-preview-sheet\">")
                .Append(WebUtility.HtmlEncode(activeSheetName)).Append("</span></div>");
            return html.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.RenderOpenXml failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render delimited text as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="delimiter">Delimiter value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="sheetName">Sheet name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="activeSheetName">Active sheet name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderDelimitedText(byte[] content, char delimiter, string sheetName, out string activeSheetName)
    {
    try
    {
            activeSheetName = sheetName;
            var text = DecodeText(content);
            var rows = ParseDelimited(text, delimiter).ToList();
            var columns = Math.Max(rows.Count == 0 ? 1 : rows.Max(row => row.Count), 1);
            var html = new StringBuilder("<div class=\"spreadsheet-preview-document\"><table><tbody>");
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                html.Append("<tr>");
                for (var column = 0; column < columns; column++)
                {
                    var address = ColumnName(column + 1) + (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
                    html.Append("<td class=\"publisher-sheet-cell\" data-cell=\"").Append(address)
                        .Append("\" data-row=\"").Append((rowIndex + 1).ToString(CultureInfo.InvariantCulture))
                        .Append("\" data-column=\"").Append((column + 1).ToString(CultureInfo.InvariantCulture)).Append("\">")
                        .Append(WebUtility.HtmlEncode(column < row.Count ? row[column] : string.Empty)).Append("</td>");
                }
                html.Append("</tr>");
            }
            if (rows.Count == 0) html.Append("<tr><td class=\"publisher-sheet-cell\" data-cell=\"A1\" data-row=\"1\" data-column=\"1\"></td></tr>");
            html.Append("</tbody></table><span class=\"spreadsheet-preview-sheet\">")
                .Append(WebUtility.HtmlEncode(activeSheetName)).Append("</span></div>");
            return html.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.RenderDelimitedText failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs detect delimiter as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The char produced by the operation.</returns>
    private char DetectDelimiter(byte[] content, char fallback)
    {
    try
    {
            var text = DecodeText(content);
            var line = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
            if (string.IsNullOrWhiteSpace(line)) return fallback;
            var candidates = new[] { ',', ';', '\t', '|' };
            var best = candidates.Select(candidate => (Delimiter: candidate, Count: CountDelimiter(line, candidate)))
                .OrderByDescending(item => item.Count).First();
            return best.Count > 0 ? best.Delimiter : fallback;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.DetectDelimiter failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs count delimiter as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="line">Line value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="delimiter">Delimiter value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int CountDelimiter(string line, char delimiter)
    {
    try
    {
            var count = 0;
            var quoted = false;
            for (var index = 0; index < line.Length; index++)
            {
                if (line[index] == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"') index++;
                    else quoted = !quoted;
                }
                else if (!quoted && line[index] == delimiter) count++;
            }
            return count;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.CountDelimiter failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses delimited as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="delimiter">Delimiter value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<List<string>> ParseDelimited(string text, char delimiter)
    {
    try
    {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var value = new StringBuilder();
            var quoted = false;
            for (var index = 0; index < text.Length; index++)
            {
                var current = text[index];
                if (current == '"')
                {
                    if (quoted && index + 1 < text.Length && text[index + 1] == '"') { value.Append('"'); index++; }
                    else quoted = !quoted;
                    continue;
                }
                if (!quoted && current == delimiter) { row.Add(value.ToString()); value.Clear(); continue; }
                if (!quoted && (current == '\r' || current == '\n'))
                {
                    if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                    row.Add(value.ToString()); value.Clear(); rows.Add(row); row = [];
                    continue;
                }
                value.Append(current);
            }
            if (value.Length > 0 || row.Count > 0) { row.Add(value.ToString()); rows.Add(row); }
            return rows;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ParseDelimited failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads cell value as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cell">Cell value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="sharedStrings">String dependency used by the spreadsheet document workflow to provide the corresponding application capability.</param>
    /// <param name="style">Style value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings, CellStyle? style)
    {
    try
    {
            var type = cell.Attribute("t")?.Value;
            if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
                return string.Concat(cell.Descendants(Main + "t").Select(item => item.Value));
            var raw = cell.Element(Main + "v")?.Value ?? string.Empty;
            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                return sharedStrings[sharedIndex];
            if (string.Equals(type, "b", StringComparison.OrdinalIgnoreCase)) return raw == "1" ? "TRUE" : "FALSE";
            if (string.Equals(type, "str", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "e", StringComparison.OrdinalIgnoreCase)) return raw;
            if (cell.Element(Main + "f") is { } formula && string.IsNullOrWhiteSpace(raw)) return "=" + formula.Value;
            if (style?.IsDate == true && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial is > -657435 and < 2958466)
            {
                try { return DateTime.FromOADate(serial).ToString(serial % 1 == 0 ? "d" : "g", CultureInfo.CurrentCulture); }
                catch (ArgumentException) { }
            }
            return raw;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadCellValue failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads shared strings as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="archive">Archive value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
    try
    {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry is null) return [];
            using var input = entry.Open();
            var document = XDocument.Load(input, LoadOptions.None);
            return document.Root?.Elements(Main + "si")
                .Select(item => string.Concat(item.Descendants(Main + "t").Select(text => text.Value)))
                .ToList() ?? [];
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadSharedStrings failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads styles as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="archive">Archive value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<CellStyle> ReadStyles(ZipArchive archive)
    {
    try
    {
            var entry = archive.GetEntry("xl/styles.xml");
            if (entry is null) return [defaultCellStyle];
            using var input = entry.Open();
            var document = XDocument.Load(input, LoadOptions.None);
            var fonts = document.Root?.Element(Main + "fonts")?.Elements(Main + "font").Select(ReadFont).ToList() ?? [defaultFontStyle];
            var fills = document.Root?.Element(Main + "fills")?.Elements(Main + "fill").Select(ReadFill).ToList() ?? [string.Empty];
            var borders = document.Root?.Element(Main + "borders")?.Elements(Main + "border").Select(ReadBorder).ToList() ?? [string.Empty];
            var customNumberFormats = document.Root?.Element(Main + "numFmts")?.Elements(Main + "numFmt")
                .Where(item => item.Attribute("numFmtId") is not null)
                .ToDictionary(item => ParseInt(item.Attribute("numFmtId")?.Value, 0), item => item.Attribute("formatCode")?.Value ?? string.Empty) ?? [];
            var result = new List<CellStyle>();
            foreach (var xf in document.Root?.Element(Main + "cellXfs")?.Elements(Main + "xf") ?? [])
            {
                var fontId = ParseInt(xf.Attribute("fontId")?.Value, 0);
                var fillId = ParseInt(xf.Attribute("fillId")?.Value, 0);
                var borderId = ParseInt(xf.Attribute("borderId")?.Value, 0);
                var numberFormatId = ParseInt(xf.Attribute("numFmtId")?.Value, 0);
                var alignment = xf.Element(Main + "alignment");
                result.Add(new CellStyle(
                    fonts.ElementAtOrDefault(fontId) ?? defaultFontStyle,
                    fills.ElementAtOrDefault(fillId) ?? string.Empty,
                    borders.ElementAtOrDefault(borderId) ?? string.Empty,
                    alignment?.Attribute("horizontal")?.Value,
                    alignment?.Attribute("vertical")?.Value,
                    ParseBool(alignment?.Attribute("wrapText")?.Value),
                    IsDateFormat(numberFormatId, customNumberFormats.GetValueOrDefault(numberFormatId))));
            }
            return result.Count == 0 ? [defaultCellStyle] : result;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadStyles failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads font as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="font">Font value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The font style produced by the operation.</returns>
    private FontStyle ReadFont(XElement font)
    {
    try
    {
            return new FontStyle(
                NormalizeFontFamily(font.Element(Main + "name")?.Attribute("val")?.Value),
                Math.Clamp(ParseDouble(font.Element(Main + "sz")?.Attribute("val")?.Value, 11), 4, 96),
                font.Element(Main + "b") is not null,
                font.Element(Main + "i") is not null,
                font.Element(Main + "u") is not null,
                ReadColor(font.Element(Main + "color")));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadFont failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads fill as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fill">Fill value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadFill(XElement fill)
    {
    try
    {
            var pattern = fill.Element(Main + "patternFill");
            if (pattern is null || string.Equals(pattern.Attribute("patternType")?.Value, "none", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return ReadColor(pattern.Element(Main + "fgColor"));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadFill failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads border as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="border">Border value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadBorder(XElement border)
    {
    try
    {
            var parts = new List<string>();
            foreach (var name in new[] { "top", "right", "bottom", "left" })
            {
                var edge = border.Element(Main + name);
                if (edge is null || string.IsNullOrWhiteSpace(edge.Attribute("style")?.Value)) continue;
                var color = ReadColor(edge.Element(Main + "color"));
                parts.Add($"border-{name}:1px solid {(string.IsNullOrWhiteSpace(color) ? "#94a3b8" : color)}");
            }
            return string.Join(';', parts);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadBorder failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads color as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="color">Color value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadColor(XElement? color)
    {
    try
    {
            var rgb = color?.Attribute("rgb")?.Value;
            if (!string.IsNullOrWhiteSpace(rgb))
            {
                rgb = rgb.Trim();
                if (rgb.Length == 8) rgb = rgb[2..];
                if (rgb.Length == 6 && rgb.All(Uri.IsHexDigit)) return "#" + rgb;
            }
            return string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadColor failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Determines whether date format as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="numberFormatId">Identifier of the number format to use for this operation.</param>
    /// <param name="custom">Custom value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsDateFormat(int numberFormatId, string? custom)
    {
    try
    {
            if (numberFormatId is >= 14 and <= 22 or 45 or 46 or 47) return true;
            if (string.IsNullOrWhiteSpace(custom)) return false;
            var cleaned = custom.Replace("\\", string.Empty, StringComparison.Ordinal).Replace("\"", string.Empty, StringComparison.Ordinal);
            return cleaned.Contains("d", StringComparison.OrdinalIgnoreCase) || cleaned.Contains("y", StringComparison.OrdinalIgnoreCase)
                || cleaned.Contains("h", StringComparison.OrdinalIgnoreCase) || cleaned.Contains("s", StringComparison.OrdinalIgnoreCase);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.IsDateFormat failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads merged ranges as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="worksheet">Worksheet value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<MergedRange> ReadMergedRanges(XDocument worksheet)
    {
    try
    {
            return worksheet.Root?.Element(Main + "mergeCells")?.Elements(Main + "mergeCell")
                .Select(item => ParseRange(item.Attribute("ref")?.Value))
                .Where(item => item is not null)
                .Cast<MergedRange>()
                .ToList() ?? [];
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadMergedRanges failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Reads column widths as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="worksheet">Worksheet value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The dictionary int double produced by the operation.</returns>
    private Dictionary<int, double> ReadColumnWidths(XDocument worksheet)
    {
    try
    {
            var result = new Dictionary<int, double>();
            foreach (var column in worksheet.Root?.Element(Main + "cols")?.Elements(Main + "col") ?? [])
            {
                var start = ParseInt(column.Attribute("min")?.Value, 1);
                var end = ParseInt(column.Attribute("max")?.Value, start);
                var width = Math.Clamp(ParseDouble(column.Attribute("width")?.Value, 8.43) * 7 + 5, 24, 360);
                for (var index = start; index <= end; index++) result[index] = width;
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ReadColumnWidths failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses range as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The merged range produced by the operation.</returns>
    private MergedRange? ParseRange(string? value)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var pieces = value.Split(':', 2);
            var start = ParseCellReference(pieces[0], 1);
            var end = ParseCellReference(pieces.Length > 1 ? pieces[1] : pieces[0], start.Row);
            return new MergedRange(start.Row, start.Column, end.Row, end.Column);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ParseRange failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs column name as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="column">Column value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ColumnName(int column)
    {
    try
    {
            column = Math.Max(1, column);
            var name = new StringBuilder(4);
            while (column > 0)
            {
                column--;
                name.Insert(0, (char)('A' + column % 26));
                column /= 26;
            }
            return name.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ColumnName failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Parses cell reference as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="reference">Reference value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="fallbackRow">Fallback row value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The int row int column produced by the operation.</returns>
    private (int Row, int Column) ParseCellReference(string? reference, int fallbackRow)
    {
        if (string.IsNullOrWhiteSpace(reference)) return (fallbackRow, 1);
        var column = 0;
        var index = 0;
        while (index < reference.Length && char.IsLetter(reference[index]))
        {
            column = column * 26 + char.ToUpperInvariant(reference[index]) - 'A' + 1;
            index++;
        }
        var row = int.TryParse(reference[index..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRow) ? parsedRow : fallbackRow;
        return (Math.Max(1, row), Math.Max(1, column));
    }

    /// <summary>
    /// Loads entry as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="archive">Archive value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The x document produced by the operation.</returns>
    private XDocument LoadEntry(ZipArchive archive, string path)
    {
    try
    {
            var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"Workbook part '{path}' is missing.");
            using var input = entry.Open();
            return XDocument.Load(input, LoadOptions.None);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.LoadEntry failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes archive path as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="baseFolder">Base folder value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeArchivePath(string baseFolder, string target)
    {
    try
    {
            var combined = target.StartsWith('/') ? target.TrimStart('/') : baseFolder.TrimEnd('/') + "/" + target;
            var parts = new Stack<string>();
            foreach (var part in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part == ".") continue;
                if (part == "..") { if (parts.Count > 0) parts.Pop(); continue; }
                parts.Push(part);
            }
            return string.Join('/', parts.Reverse());
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.NormalizeArchivePath failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs decode text as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DecodeText(byte[] content)
    {
    try
    {
            if (content.Length >= 3 && content[0] == 0xef && content[1] == 0xbb && content[2] == 0xbf)
                return Encoding.UTF8.GetString(content, 3, content.Length - 3);
            if (content.Length >= 2 && content[0] == 0xff && content[1] == 0xfe)
                return Encoding.Unicode.GetString(content, 2, content.Length - 2);
            return Encoding.UTF8.GetString(content);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.DecodeText failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs empty preview as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sheetName">Sheet name value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="message">Message value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string EmptyPreview(string sheetName, string message) {
    try
    {
        return $"<div class=\"spreadsheet-preview-document spreadsheet-preview-empty\"><div><b>{WebUtility.HtmlEncode(sheetName)}</b><span>{WebUtility.HtmlEncode(message)}</span></div><span class=\"spreadsheet-preview-sheet\">{WebUtility.HtmlEncode(sheetName)}</span></div>";
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.EmptyPreview failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Writes entry as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="archive">Archive value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the spreadsheet document operation and used when producing its result.</param>
    private void WriteEntry(ZipArchive archive, string path, string content)
    {
    try
    {
            var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content.Trim());
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.WriteEntry failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes font family as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeFontFamily(string? value)
    {
    try
    {
            var normalized = new string((value ?? string.Empty)
                .Where(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '_' or '.')
                .Take(64)
                .ToArray())
                .Trim();
            return string.IsNullOrWhiteSpace(normalized) ? "Calibri" : normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.NormalizeFontFamily failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes sheet name as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeSheetName(string value)
    {
    try
    {
            var invalid = new HashSet<char>(['\\', '/', '?', ':', '*', '[', ']']);
            var result = new string((value ?? string.Empty).Where(character => !invalid.Contains(character)).Take(31).ToArray()).Trim('\'');
            return string.IsNullOrWhiteSpace(result) ? "Sheet1" : result;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.NormalizeSheetName failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs security element escape as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SecurityElementEscape(string value) {
    try
    {
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.SecurityElementEscape failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Parses int as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ParseInt(string? value, int fallback) {
    try
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ParseInt failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Parses double as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double ParseDouble(string? value, double fallback) {
    try
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ParseDouble failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Parses bool as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ParseBool(string? value) {
    try
    {
        return value is "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.ParseBool failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs CSS text as part of the spreadsheet document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CssText(string value) {
    try
    {
        return "'" + value.Replace("'", "\\'", StringComparison.Ordinal) + "'";
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method SpreadsheetDocumentService.CssText failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Represents a cell preview helper type nested within <see cref="SpreadsheetDocumentService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Value">Value value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="StyleIndex">Style index value supplied to the spreadsheet document operation and used when producing its result.</param>
    private sealed record CellPreview(string Value, int StyleIndex);
    /// <summary>
    /// Represents a merged range helper type nested within <see cref="SpreadsheetDocumentService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="StartRow">Start row value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="StartColumn">Start column value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="EndRow">End row value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="EndColumn">End column value supplied to the spreadsheet document operation and used when producing its result.</param>
    private sealed record MergedRange(int StartRow, int StartColumn, int EndRow, int EndColumn);
    /// <summary>
    /// Represents a font style helper type nested within <see cref="SpreadsheetDocumentService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Family">Family value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="SizePt">Size pt value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="Bold">Value indicating whether bold should apply to this operation.</param>
    /// <param name="Italic">Value indicating whether italic should apply to this operation.</param>
    /// <param name="Underline">Value indicating whether underline should apply to this operation.</param>
    /// <param name="Color">Color value supplied to the spreadsheet document operation and used when producing its result.</param>
    private sealed record FontStyle(string Family, double SizePt, bool Bold, bool Italic, bool Underline, string Color);
    /// <summary>
    /// Represents a cell style helper type nested within <see cref="SpreadsheetDocumentService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Font">Font value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="Fill">Fill value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="Border">Border value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="Horizontal">Horizontal value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="Vertical">Vertical value supplied to the spreadsheet document operation and used when producing its result.</param>
    /// <param name="Wrap">Value indicating whether wrap should apply to this operation.</param>
    /// <param name="IsDate">Value indicating whether date should apply to this operation.</param>
    private sealed record CellStyle(FontStyle Font, string Fill, string Border, string? Horizontal, string? Vertical, bool Wrap, bool IsDate)
    {
        /// <summary>
        /// Performs to CSS for <see cref="CellStyle"/>, keeping the operation consistent with the state and invariants of the surrounding cell style workflow.
        /// </summary>
        /// <param name="cssText">Css text value supplied to the cell style operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToCss(Func<string, string> cssText)
        {
    try
    {
                var css = new List<string>
                {
                    $"font-family:{cssText(Font.Family)}",
                    $"font-size:{Font.SizePt.ToString("0.#", CultureInfo.InvariantCulture)}pt"
                };
                if (Font.Bold) css.Add("font-weight:700");
                if (Font.Italic) css.Add("font-style:italic");
                if (Font.Underline) css.Add("text-decoration:underline");
                if (!string.IsNullOrWhiteSpace(Font.Color)) css.Add("color:" + Font.Color);
                if (!string.IsNullOrWhiteSpace(Fill)) css.Add("background:" + Fill);
                if (!string.IsNullOrWhiteSpace(Border)) css.Add(Border);
                var horizontal = Horizontal switch
                {
                    "left" => "left",
                    "center" or "centerContinuous" => "center",
                    "right" => "right",
                    "fill" => "left",
                    "justify" or "distributed" => "justify",
                    _ => string.Empty
                };
                var vertical = Vertical switch
                {
                    "top" => "top",
                    "center" => "middle",
                    "bottom" => "bottom",
                    "justify" or "distributed" => "middle",
                    _ => string.Empty
                };
                if (!string.IsNullOrWhiteSpace(horizontal)) css.Add("text-align:" + horizontal);
                if (!string.IsNullOrWhiteSpace(vertical)) css.Add("vertical-align:" + vertical);
                if (Wrap) css.Add("white-space:normal");
                return string.Join(';', css);
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method CellStyle.ToCss failed: {__serviceMethodException}");
        throw;
    }
}
    }
}
