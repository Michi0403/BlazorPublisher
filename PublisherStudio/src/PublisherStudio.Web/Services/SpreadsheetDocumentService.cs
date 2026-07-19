using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

/// <summary>
/// Creates workbook packages and produces a safe, static canvas representation without
/// requiring the separately licensed Office File API HTML exporter.
/// </summary>
public sealed class SpreadsheetDocumentService
{
    private const int PreviewRowLimit = 80;
    private const int PreviewColumnLimit = 40;
    private const long PreviewXmlPartLimit = 32L * 1024 * 1024;
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public byte[] CreateBlankXlsx(string sheetName = "Sheet1")
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

    public SpreadsheetStorageFormat DetectFormat(string? fileName, string? contentType = null)
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

    public string DefaultExtension(SpreadsheetStorageFormat format) => format switch
    {
        SpreadsheetStorageFormat.Xlsm => ".xlsm",
        SpreadsheetStorageFormat.Xls => ".xls",
        SpreadsheetStorageFormat.Csv => ".csv",
        SpreadsheetStorageFormat.Text => ".txt",
        _ => ".xlsx"
    };

    public string NormalizeWorkbookFileName(string? fileName, SpreadsheetStorageFormat format)
    {
        var safe = PublicationFileService.SafeFileName(Path.GetFileNameWithoutExtension(fileName ?? "Spreadsheet"));
        return safe + DefaultExtension(format);
    }

    public string RenderPreviewHtml(byte[]? content, SpreadsheetStorageFormat format, out string activeSheetName)
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

    public void ValidateWorkbookContent(byte[]? content, SpreadsheetStorageFormat format)
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

    public bool IsOpenXmlWorkbook(byte[]? content)
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

    private string RenderOpenXml(byte[] content, out string activeSheetName)
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
            if (rowNumber > PreviewRowLimit) continue;
            foreach (var cell in row.Elements(Main + "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                var (cellRow, cellColumn) = ParseCellReference(reference, rowNumber);
                if (cellRow is < 1 or > PreviewRowLimit || cellColumn is < 1 or > PreviewColumnLimit) continue;
                var styleIndex = ParseInt(cell.Attribute("s")?.Value, 0);
                var value = ReadCellValue(cell, sharedStrings, styles.ElementAtOrDefault(styleIndex));
                cells[(cellRow, cellColumn)] = new CellPreview(value, styleIndex);
                maxRow = Math.Max(maxRow, cellRow);
                maxColumn = Math.Max(maxColumn, cellColumn);
            }
        }

        foreach (var merge in merged)
        {
            maxRow = Math.Max(maxRow, Math.Min(PreviewRowLimit, merge.EndRow));
            maxColumn = Math.Max(maxColumn, Math.Min(PreviewColumnLimit, merge.EndColumn));
        }
        maxRow = Math.Clamp(maxRow, 1, PreviewRowLimit);
        maxColumn = Math.Clamp(maxColumn, 1, PreviewColumnLimit);

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
                var style = styles.ElementAtOrDefault(cell.StyleIndex) ?? CellStyle.Default;
                html.Append("<td");
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
                var css = style.ToCss();
                if (!string.IsNullOrWhiteSpace(css)) html.Append(" style=\"").Append(WebUtility.HtmlEncode(css)).Append('"');
                html.Append('>').Append(WebUtility.HtmlEncode(cell.Value)).Append("</td>");
            }
            html.Append("</tr>");
        }
        html.Append("</tbody></table><span class=\"spreadsheet-preview-sheet\">")
            .Append(WebUtility.HtmlEncode(activeSheetName)).Append("</span></div>");
        return html.ToString();
    }

    private string RenderDelimitedText(byte[] content, char delimiter, string sheetName, out string activeSheetName)
    {
        activeSheetName = sheetName;
        var text = DecodeText(content);
        var rows = ParseDelimited(text, delimiter).Take(PreviewRowLimit).ToList();
        var columns = Math.Clamp(rows.Count == 0 ? 1 : rows.Max(row => row.Count), 1, PreviewColumnLimit);
        var html = new StringBuilder("<div class=\"spreadsheet-preview-document\"><table><tbody>");
        foreach (var row in rows)
        {
            html.Append("<tr>");
            for (var column = 0; column < columns; column++)
                html.Append("<td>").Append(WebUtility.HtmlEncode(column < row.Count ? row[column] : string.Empty)).Append("</td>");
            html.Append("</tr>");
        }
        if (rows.Count == 0) html.Append("<tr><td></td></tr>");
        html.Append("</tbody></table><span class=\"spreadsheet-preview-sheet\">")
            .Append(WebUtility.HtmlEncode(activeSheetName)).Append("</span></div>");
        return html.ToString();
    }

    private static char DetectDelimiter(byte[] content, char fallback)
    {
        var text = DecodeText(content);
        var line = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        if (string.IsNullOrWhiteSpace(line)) return fallback;
        var candidates = new[] { ',', ';', '\t', '|' };
        var best = candidates.Select(candidate => (Delimiter: candidate, Count: CountDelimiter(line, candidate)))
            .OrderByDescending(item => item.Count).First();
        return best.Count > 0 ? best.Delimiter : fallback;
    }

    private static int CountDelimiter(string line, char delimiter)
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

    private static List<List<string>> ParseDelimited(string text, char delimiter)
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
                if (rows.Count >= PreviewRowLimit) break;
                continue;
            }
            value.Append(current);
        }
        if (value.Length > 0 || row.Count > 0) { row.Add(value.ToString()); rows.Add(row); }
        return rows;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings, CellStyle? style)
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

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        if (entry.Length > PreviewXmlPartLimit) throw new InvalidDataException("Workbook shared strings are too large for a canvas preview.");
        using var input = entry.Open();
        var document = XDocument.Load(input, LoadOptions.None);
        return document.Root?.Elements(Main + "si")
            .Select(item => string.Concat(item.Descendants(Main + "t").Select(text => text.Value)))
            .ToList() ?? [];
    }

    private static IReadOnlyList<CellStyle> ReadStyles(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/styles.xml");
        if (entry is null) return [CellStyle.Default];
        if (entry.Length > PreviewXmlPartLimit) throw new InvalidDataException("Workbook styles are too large for a canvas preview.");
        using var input = entry.Open();
        var document = XDocument.Load(input, LoadOptions.None);
        var fonts = document.Root?.Element(Main + "fonts")?.Elements(Main + "font").Select(ReadFont).ToList() ?? [FontStyle.Default];
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
                fonts.ElementAtOrDefault(fontId) ?? FontStyle.Default,
                fills.ElementAtOrDefault(fillId) ?? string.Empty,
                borders.ElementAtOrDefault(borderId) ?? string.Empty,
                alignment?.Attribute("horizontal")?.Value,
                alignment?.Attribute("vertical")?.Value,
                ParseBool(alignment?.Attribute("wrapText")?.Value),
                IsDateFormat(numberFormatId, customNumberFormats.GetValueOrDefault(numberFormatId))));
        }
        return result.Count == 0 ? [CellStyle.Default] : result;
    }

    private static FontStyle ReadFont(XElement font)
    {
        return new FontStyle(
            NormalizeFontFamily(font.Element(Main + "name")?.Attribute("val")?.Value),
            Math.Clamp(ParseDouble(font.Element(Main + "sz")?.Attribute("val")?.Value, 11), 4, 96),
            font.Element(Main + "b") is not null,
            font.Element(Main + "i") is not null,
            font.Element(Main + "u") is not null,
            ReadColor(font.Element(Main + "color")));
    }

    private static string ReadFill(XElement fill)
    {
        var pattern = fill.Element(Main + "patternFill");
        if (pattern is null || string.Equals(pattern.Attribute("patternType")?.Value, "none", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return ReadColor(pattern.Element(Main + "fgColor"));
    }

    private static string ReadBorder(XElement border)
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

    private static string ReadColor(XElement? color)
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

    private static bool IsDateFormat(int numberFormatId, string? custom)
    {
        if (numberFormatId is >= 14 and <= 22 or 45 or 46 or 47) return true;
        if (string.IsNullOrWhiteSpace(custom)) return false;
        var cleaned = custom.Replace("\\", string.Empty, StringComparison.Ordinal).Replace("\"", string.Empty, StringComparison.Ordinal);
        return cleaned.Contains("d", StringComparison.OrdinalIgnoreCase) || cleaned.Contains("y", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("h", StringComparison.OrdinalIgnoreCase) || cleaned.Contains("s", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<MergedRange> ReadMergedRanges(XDocument worksheet)
    {
        return worksheet.Root?.Element(Main + "mergeCells")?.Elements(Main + "mergeCell")
            .Select(item => ParseRange(item.Attribute("ref")?.Value))
            .Where(item => item is not null)
            .Cast<MergedRange>()
            .ToList() ?? [];
    }

    private static Dictionary<int, double> ReadColumnWidths(XDocument worksheet)
    {
        var result = new Dictionary<int, double>();
        foreach (var column in worksheet.Root?.Element(Main + "cols")?.Elements(Main + "col") ?? [])
        {
            var start = ParseInt(column.Attribute("min")?.Value, 1);
            var end = ParseInt(column.Attribute("max")?.Value, start);
            var width = Math.Clamp(ParseDouble(column.Attribute("width")?.Value, 8.43) * 7 + 5, 24, 360);
            for (var index = start; index <= Math.Min(end, PreviewColumnLimit); index++) result[index] = width;
        }
        return result;
    }

    private static MergedRange? ParseRange(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var pieces = value.Split(':', 2);
        var start = ParseCellReference(pieces[0], 1);
        var end = ParseCellReference(pieces.Length > 1 ? pieces[1] : pieces[0], start.Row);
        return new MergedRange(start.Row, start.Column, end.Row, end.Column);
    }

    private static (int Row, int Column) ParseCellReference(string? reference, int fallbackRow)
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

    private static XDocument LoadEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"Workbook part '{path}' is missing.");
        if (entry.Length > PreviewXmlPartLimit) throw new InvalidDataException($"Workbook part '{path}' is too large for a canvas preview.");
        using var input = entry.Open();
        return XDocument.Load(input, LoadOptions.None);
    }

    private static string NormalizeArchivePath(string baseFolder, string target)
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

    private static string DecodeText(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xef && content[1] == 0xbb && content[2] == 0xbf)
            return Encoding.UTF8.GetString(content, 3, content.Length - 3);
        if (content.Length >= 2 && content[0] == 0xff && content[1] == 0xfe)
            return Encoding.Unicode.GetString(content, 2, content.Length - 2);
        return Encoding.UTF8.GetString(content);
    }

    private static string EmptyPreview(string sheetName, string message) =>
        $"<div class=\"spreadsheet-preview-document spreadsheet-preview-empty\"><div><b>{WebUtility.HtmlEncode(sheetName)}</b><span>{WebUtility.HtmlEncode(message)}</span></div><span class=\"spreadsheet-preview-sheet\">{WebUtility.HtmlEncode(sheetName)}</span></div>";

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }

    private static string NormalizeFontFamily(string? value)
    {
        var normalized = new string((value ?? string.Empty)
            .Where(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '_' or '.')
            .Take(64)
            .ToArray())
            .Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Calibri" : normalized;
    }

    private static string NormalizeSheetName(string value)
    {
        var invalid = new HashSet<char>(['\\', '/', '?', ':', '*', '[', ']']);
        var result = new string((value ?? string.Empty).Where(character => !invalid.Contains(character)).Take(31).ToArray()).Trim('\'');
        return string.IsNullOrWhiteSpace(result) ? "Sheet1" : result;
    }

    private static string SecurityElementEscape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
    private static int ParseInt(string? value, int fallback) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static double ParseDouble(string? value, double fallback) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ParseBool(string? value) => value is "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private sealed record CellPreview(string Value, int StyleIndex);
    private sealed record MergedRange(int StartRow, int StartColumn, int EndRow, int EndColumn);
    private sealed record FontStyle(string Family, double SizePt, bool Bold, bool Italic, bool Underline, string Color)
    {
        public static FontStyle Default { get; } = new("Calibri", 11, false, false, false, string.Empty);
    }
    private sealed record CellStyle(FontStyle Font, string Fill, string Border, string? Horizontal, string? Vertical, bool Wrap, bool IsDate)
    {
        public static CellStyle Default { get; } = new(FontStyle.Default, string.Empty, string.Empty, null, null, false, false);
        public string ToCss()
        {
            var css = new List<string>
            {
                $"font-family:{CssText(Font.Family)}",
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
        private static string CssText(string value) => "'" + value.Replace("'", "\\'", StringComparison.Ordinal) + "'";
    }
}
