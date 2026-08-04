using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.CodeEditing;

public interface ICodeLanguageService
{
    IReadOnlyList<CodeLanguageProfile> GetProfiles();
    CodeLanguageProfile Get(string languageId);
    CodeLanguageProfile Detect(string fileNameOrExtension, string? content = null);
}

public interface ICodeFormattingService
{
    CodeTextResult Format(CodeTextRequest request);
    CodeTextResult ToggleComment(CodeCommentRequest request);
    CodeTextResult Analyze(CodeTextRequest request);
}
