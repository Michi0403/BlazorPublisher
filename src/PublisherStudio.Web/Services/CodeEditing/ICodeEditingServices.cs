using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.CodeEditing;

/// <summary>
/// Defines the code language service contract.
/// </summary>
public interface ICodeLanguageService
{
    /// <summary>
    /// Gets profiles.
    /// </summary>
    IReadOnlyList<CodeLanguageProfile> GetProfiles();
    /// <summary>
    /// Runs the get operation.
    /// </summary>
    CodeLanguageProfile Get(string languageId);
    /// <summary>
    /// Runs the detect operation.
    /// </summary>
    CodeLanguageProfile Detect(string fileNameOrExtension, string? content = null);
}

/// <summary>
/// Defines the code formatting service contract.
/// </summary>
public interface ICodeFormattingService
{
    /// <summary>
    /// Runs the format operation.
    /// </summary>
    CodeTextResult Format(CodeTextRequest request);
    /// <summary>
    /// Runs the toggle comment operation.
    /// </summary>
    CodeTextResult ToggleComment(CodeCommentRequest request);
    /// <summary>
    /// Runs the analyze operation.
    /// </summary>
    CodeTextResult Analyze(CodeTextRequest request);
}
