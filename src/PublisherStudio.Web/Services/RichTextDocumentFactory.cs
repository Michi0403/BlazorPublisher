using System.IO.Compression;
using System.Security;
using System.Text;
// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Provides rich text document factory operations.
/// </summary>
public sealed class RichTextDocumentFactory(ILogger<RichTextDocumentFactory> logger)
{
    /// <summary>
    /// Creates open XML.
    /// </summary>
    public byte[] CreateOpenXml(string title, string? subtitle = null)
    {
        logger.LogTrace("Creating a RichEdit OpenXML document.");
        var paragraphs = new List<string>
        {
            BuildParagraph(
                BuildRun(title, "<w:b/><w:sz w:val=\"56\"/><w:color w:val=\"17365D\"/>"))
        };

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            paragraphs.Add(BuildParagraph(
                BuildRun(subtitle, "<w:sz w:val=\"24\"/><w:color w:val=\"475569\"/>"),
                "<w:spacing w:before=\"120\"/>"));
        }

        return CreateOpenXmlPackage(paragraphs);
    }

    /// <summary>
    /// Creates open XML from plain text.
    /// </summary>
    public byte[] CreateOpenXmlFromPlainText(string text)
    {
        var paragraphs = NormalizeLines(text)
            .Split('\n')
            .Select(line => string.IsNullOrEmpty(line)
                ? "<w:p/>"
                : BuildParagraph(BuildRun(line)))
            .ToList();

        return CreateOpenXmlPackage(paragraphs);
    }

    /// <summary>
    /// Creates open XML from markdown.
    /// </summary>
    public byte[] CreateOpenXmlFromMarkdown(string markdown)
    {
        var paragraphs = new List<string>();
        foreach (var sourceLine in NormalizeLines(markdown).Split('\n'))
        {
            var line = sourceLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                paragraphs.Add("<w:p/>");
                continue;
            }

            var trimmed = line.TrimStart();
            var headingLevel = 0;
            while (headingLevel < trimmed.Length && headingLevel < 6 && trimmed[headingLevel] == '#')
                headingLevel++;

            if (headingLevel > 0
                && headingLevel < trimmed.Length
                && char.IsWhiteSpace(trimmed[headingLevel]))
            {
                var headingText = trimmed[(headingLevel + 1)..].Trim();
                var halfPointSize = Math.Max(24, 48 - (headingLevel - 1) * 4);
                paragraphs.Add(BuildParagraph(
                    BuildMarkdownRuns(headingText, $"<w:b/><w:sz w:val=\"{halfPointSize}\"/>"),
                    "<w:spacing w:before=\"160\" w:after=\"80\"/>"));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal)
                || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                paragraphs.Add(BuildParagraph(
                    BuildRun("• ", "<w:b/>") + BuildMarkdownRuns(trimmed[2..]),
                    "<w:ind w:left=\"360\" w:hanging=\"180\"/><w:spacing w:after=\"40\"/>"));
                continue;
            }

            paragraphs.Add(BuildParagraph(
                BuildMarkdownRuns(trimmed),
                "<w:spacing w:after=\"80\"/>"));
        }

        return CreateOpenXmlPackage(paragraphs);
    }

    private byte[] CreateOpenXmlPackage(IEnumerable<string> bodyElements)
    {
        var body = string.Join(Environment.NewLine, bodyElements);
        if (string.IsNullOrWhiteSpace(body))
            body = "<w:p/>";

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
                </Types>
                """);
            Write(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);
            Write(archive, "word/_rels/document.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            Write(archive, "word/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/></w:style>
                  <w:style w:type="character" w:default="1" w:styleId="DefaultParagraphFont"><w:name w:val="Default Paragraph Font"/></w:style>
                </w:styles>
                """);
            Write(archive, "word/document.xml", $$"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    {{body}}
                    <w:sectPr>
                      <w:pgSz w:w="11906" w:h="16838"/>
                      <w:pgMar w:top="720" w:right="720" w:bottom="720" w:left="720" w:header="360" w:footer="360" w:gutter="0"/>
                    </w:sectPr>
                  </w:body>
                </w:document>
                """);
        }

        return stream.ToArray();
    }

    private string BuildParagraph(string runs, string? paragraphProperties = null) =>
        string.IsNullOrWhiteSpace(paragraphProperties)
            ? $"<w:p>{runs}</w:p>"
            : $"<w:p><w:pPr>{paragraphProperties}</w:pPr>{runs}</w:p>";

    private string BuildMarkdownRuns(string value, string? baseProperties = null)
    {
        var runs = new StringBuilder();
        var buffer = new StringBuilder();
        var bold = false;
        var italic = false;
        var code = false;

        void Flush()
        {
            if (buffer.Length == 0) return;
            var properties = new StringBuilder(baseProperties ?? string.Empty);
            if (bold) properties.Append("<w:b/>");
            if (italic) properties.Append("<w:i/>");
            if (code)
                properties.Append("<w:rFonts w:ascii=\"Consolas\" w:hAnsi=\"Consolas\"/><w:shd w:val=\"clear\" w:fill=\"E5E7EB\"/>");
            runs.Append(BuildRun(buffer.ToString(), properties.ToString()));
            buffer.Clear();
        }

        for (var index = 0; index < value.Length;)
        {
            if (!code && index + 1 < value.Length && value[index] == '*' && value[index + 1] == '*')
            {
                Flush();
                bold = !bold;
                index += 2;
                continue;
            }

            if (value[index] == '`')
            {
                Flush();
                code = !code;
                index++;
                continue;
            }

            if (!code && value[index] is '*' or '_')
            {
                Flush();
                italic = !italic;
                index++;
                continue;
            }

            buffer.Append(value[index]);
            index++;
        }

        Flush();
        return runs.Length == 0 ? BuildRun(string.Empty, baseProperties) : runs.ToString();
    }

    private string BuildRun(string value, string? runProperties = null)
    {
        var escaped = SecurityElement.Escape(value) ?? string.Empty;
        var properties = string.IsNullOrWhiteSpace(runProperties)
            ? string.Empty
            : $"<w:rPr>{runProperties}</w:rPr>";
        return $"<w:r>{properties}<w:t xml:space=\"preserve\">{escaped}</w:t></w:r>";
    }

    private string NormalizeLines(string? value) =>
        (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private void Write(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }
}
