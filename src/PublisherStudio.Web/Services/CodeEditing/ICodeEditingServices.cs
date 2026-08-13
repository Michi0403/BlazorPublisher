using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.CodeEditing;

/// <summary>
/// Defines the contract for code language behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICodeLanguageService
{
    /// <summary>
    /// Retrieves profiles as part of the code language service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CodeLanguageProfile> GetProfiles();
    /// <summary>
    /// Performs get as part of the code language service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="languageId">Identifier of the language to use for this operation.</param>
    /// <returns>The code language profile produced by the operation.</returns>
    CodeLanguageProfile Get(string languageId);
    /// <summary>
    /// Performs detect as part of the code language service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileNameOrExtension">File name or extension value supplied to the code language operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the code language operation and used when producing its result.</param>
    /// <returns>The code language profile produced by the operation.</returns>
    CodeLanguageProfile Detect(string fileNameOrExtension, string? content = null);
}

/// <summary>
/// Defines the contract for code formatting behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICodeFormattingService
{
    /// <summary>
    /// Performs format as part of the code formatting service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The code text result produced by the operation.</returns>
    CodeTextResult Format(CodeTextRequest request);
    /// <summary>
    /// Performs toggle comment as part of the code formatting service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The code text result produced by the operation.</returns>
    CodeTextResult ToggleComment(CodeCommentRequest request);
    /// <summary>
    /// Performs analyze as part of the code formatting service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The code text result produced by the operation.</returns>
    CodeTextResult Analyze(CodeTextRequest request);
}
