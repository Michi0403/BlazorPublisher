using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.CodeEditing;

/// <summary>
/// Defines the code language service contract.
/// </summary>
public interface ICodeLanguageService
{
    IReadOnlyList<CodeLanguageProfile> GetProfiles();
    CodeLanguageProfile Get(string languageId);
    CodeLanguageProfile Detect(string fileNameOrExtension, string? content = null);
}

/// <summary>
/// Defines the code formatting service contract.
/// </summary>
public interface ICodeFormattingService
{
    CodeTextResult Format(CodeTextRequest request);
    CodeTextResult ToggleComment(CodeCommentRequest request);
    CodeTextResult Analyze(CodeTextRequest request);
}
