using System.IO.Compression;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Creates configured rich text document instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RichTextDocumentFactory(ILogger<RichTextDocumentFactory> logger)
{
    /// <summary>
    /// Creates open XML using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="title">Title value supplied to the rich text document operation and used when producing its result.</param>
    /// <param name="subtitle">Subtitle value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    public byte[] CreateOpenXml(string title, string? subtitle = null)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXml)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXml)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates open XML from plain text.
    /// </summary>
    /// <param name="text">Text value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    public byte[] CreateOpenXmlFromPlainText(string text)
    {
    try
    {
            var paragraphs = NormalizeLines(text)
                .Split('\n')
                .Select(line => string.IsNullOrEmpty(line)
                    ? "<w:p/>"
                    : BuildParagraph(BuildRun(line)))
                .ToList();

            return CreateOpenXmlPackage(paragraphs);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlFromPlainText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlFromPlainText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates editable OpenXML from an existing text-frame preview when no stored rich-text document exists.
    /// </summary>
    /// <param name="previewHtml">Preview HTML that represents the currently visible text-frame content.</param>
    /// <returns>The byte array containing a valid OpenXML document package.</returns>
    public byte[] CreateOpenXmlFromPreviewHtml(string? previewHtml)
    {
    try
    {
            logger.LogTrace("Creating RichEdit OpenXML from text-frame preview HTML.");
            if (string.IsNullOrWhiteSpace(previewHtml))
                return CreateOpenXmlFromPlainText(string.Empty);

            var safeHtml = Regex.Replace(
                previewHtml,
                @"<(script|style)\b[^>]*>.*?</\1>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

            var heading = Regex.Match(
                safeHtml,
                @"<h[1-6]\b[^>]*>(?<content>.*?)</h[1-6]>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            if (heading.Success)
            {
                var title = HtmlFragmentToPlainText(heading.Groups["content"].Value).Trim();
                var paragraph = Regex.Match(
                    safeHtml[(heading.Index + heading.Length)..],
                    @"<p\b[^>]*>(?<content>.*?)</p>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
                var subtitle = paragraph.Success
                    ? HtmlFragmentToPlainText(paragraph.Groups["content"].Value).Trim()
                    : null;
                if (!string.IsNullOrWhiteSpace(title))
                    return CreateOpenXml(title, string.IsNullOrWhiteSpace(subtitle) ? null : subtitle);
            }

            return CreateOpenXmlFromPlainText(HtmlFragmentToPlainText(safeHtml));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlFromPreviewHtml)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlFromPreviewHtml)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates open XML from markdown.
    /// </summary>
    /// <param name="markdown">Markdown value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    public byte[] CreateOpenXmlFromMarkdown(string markdown)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlFromMarkdown)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlFromMarkdown)} failed.");
        throw;
    }
}

    /// <summary>
    /// Converts bounded preview HTML into plain text while retaining paragraph/list line boundaries.
    /// </summary>
    /// <param name="html">HTML fragment to convert.</param>
    /// <returns>Plain text suitable for a newly materialized RichEdit document.</returns>
    private string HtmlFragmentToPlainText(string html)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var text = Regex.Replace(
                html,
                @"<br\s*/?>",
                "\n",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            text = Regex.Replace(
                text,
                @"<(li)\b[^>]*>",
                "• ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            text = Regex.Replace(
                text,
                @"</(p|div|h[1-6]|li|tr|section|article|header|footer|blockquote)\s*>",
                "\n",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            text = Regex.Replace(
                text,
                @"<[^>]+>",
                string.Empty,
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            text = WebUtility.HtmlDecode(text).Replace('\u00a0', ' ');

            var normalized = NormalizeLines(text);
            var lines = normalized
                .Split('\n')
                .Select(line => line.Trim())
                .ToList();
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0])) lines.RemoveAt(0);
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);

            var result = new List<string>(lines.Count);
            var previousBlank = false;
            foreach (var line in lines)
            {
                var blank = string.IsNullOrWhiteSpace(line);
                if (blank && previousBlank) continue;
                result.Add(line);
                previousBlank = blank;
            }
            return string.Join("\n", result);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(HtmlFragmentToPlainText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(HtmlFragmentToPlainText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates open XML package using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="bodyElements">String dependency used by the rich text document workflow to provide the corresponding application capability.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] CreateOpenXmlPackage(IEnumerable<string> bodyElements)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlPackage)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(CreateOpenXmlPackage)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds paragraph using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="runs">Runs value supplied to the rich text document operation and used when producing its result.</param>
    /// <param name="paragraphProperties">Paragraph properties value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildParagraph(string runs, string? paragraphProperties = null) {
    try
    {
        return string.IsNullOrWhiteSpace(paragraphProperties)
            ? $"<w:p>{runs}</w:p>"
            : $"<w:p><w:pPr>{paragraphProperties}</w:pPr>{runs}</w:p>";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(BuildParagraph)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(BuildParagraph)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds markdown runs using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="value">Value value supplied to the rich text document operation and used when producing its result.</param>
    /// <param name="baseProperties">Base properties value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildMarkdownRuns(string value, string? baseProperties = null)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(BuildMarkdownRuns)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(BuildMarkdownRuns)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds run using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="value">Value value supplied to the rich text document operation and used when producing its result.</param>
    /// <param name="runProperties">Run properties value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildRun(string value, string? runProperties = null)
    {
    try
    {
            var escaped = SecurityElement.Escape(value) ?? string.Empty;
            var properties = string.IsNullOrWhiteSpace(runProperties)
                ? string.Empty
                : $"<w:rPr>{runProperties}</w:rPr>";
            return $"<w:r>{properties}<w:t xml:space=\"preserve\">{escaped}</w:t></w:r>";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(BuildRun)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(BuildRun)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes lines using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="value">Value value supplied to the rich text document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeLines(string? value) {
    try
    {
        return (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(NormalizeLines)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(NormalizeLines)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs write using the configuration and dependencies owned by <see cref="RichTextDocumentFactory"/>.
    /// </summary>
    /// <param name="archive">Archive value supplied to the rich text document operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the rich text document operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the rich text document operation and used when producing its result.</param>
    private void Write(ZipArchive archive, string name, string content)
    {
    try
    {
            var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content.Trim());
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(Write)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RichTextDocumentFactory)}.{nameof(Write)} failed.");
        throw;
    }
}
}
