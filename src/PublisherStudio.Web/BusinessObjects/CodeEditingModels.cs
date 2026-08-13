namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a code language profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeLanguageProfile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this code language profile instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public string Id { get; set; } = "text";
    /// <summary>
    /// Gets or sets the display name value that forms part of the code language profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public string DisplayName { get; set; } = "Plain text";
    /// <summary>
    /// Gets or sets the extensions collection maintained or exposed by this code language profile instance for downstream processing.
    /// </summary>
    /// <value>The extensions value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public List<string> Extensions { get; set; } = [];
    /// <summary>
    /// Gets or sets the line comment value that forms part of the code language profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The line comment value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public string LineComment { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the block comment start value that forms part of the code language profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The block comment start value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public string BlockCommentStart { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the block comment end value that forms part of the code language profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The block comment end value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public string BlockCommentEnd { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the keywords collection maintained or exposed by this code language profile instance for downstream processing.
    /// </summary>
    /// <value>The keywords value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public List<string> Keywords { get; set; } = [];
    /// <summary>
    /// Gets or sets the string delimiters value that forms part of the code language profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The string delimiters value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public string StringDelimiters { get; set; } = "\"'";
    /// <summary>
    /// Gets or sets a value indicating whether uses braces applies to the code language profile state.
    /// </summary>
    /// <value>The uses braces value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public bool UsesBraces { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether uses indentation applies to the code language profile state.
    /// </summary>
    /// <value>The uses indentation value exposed by <see cref="CodeLanguageProfile"/>.</value>
    public bool UsesIndentation { get; set; }
}

/// <summary>
/// Represents the input contract for code text, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="LanguageId">Identifier of the language to use for this operation.</param>
/// <param name="Text">Text value supplied to the code text operation and used when producing its result.</param>
/// <param name="IndentSize">Indent size value supplied to the code text operation and used when producing its result.</param>
public sealed record CodeTextRequest(string LanguageId, string Text, int IndentSize = 4);
/// <summary>
/// Represents the input contract for code comment, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="LanguageId">Identifier of the language to use for this operation.</param>
/// <param name="Text">Text value supplied to the code comment operation and used when producing its result.</param>
/// <param name="Uncomment">Value indicating whether uncomment should apply to this operation.</param>
public sealed record CodeCommentRequest(string LanguageId, string Text, bool Uncomment = false);
/// <summary>
/// Represents the outcome of code text, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="LanguageId">Identifier of the language to use for this operation.</param>
/// <param name="Text">Text value supplied to the code text operation and used when producing its result.</param>
/// <param name="Tokens">Code token span dependency used by the code text workflow to provide the corresponding application capability.</param>
public sealed record CodeTextResult(string LanguageId, string Text, IReadOnlyList<CodeTokenSpan> Tokens);
/// <summary>
/// Represents a code token span application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Start">Start value supplied to the code token span operation and used when producing its result.</param>
/// <param name="Length">Length value supplied to the code token span operation and used when producing its result.</param>
/// <param name="Kind">Kind value supplied to the code token span operation and used when producing its result.</param>
public sealed record CodeTokenSpan(int Start, int Length, string Kind);
