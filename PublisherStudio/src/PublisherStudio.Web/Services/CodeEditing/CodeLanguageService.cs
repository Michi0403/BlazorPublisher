using PublisherStudio.Domain;

namespace PublisherStudio.Services.CodeEditing;

public sealed class CodeLanguageService : ICodeLanguageService
{
    private readonly IReadOnlyList<CodeLanguageProfile> _profiles;
    private readonly IReadOnlyDictionary<string, CodeLanguageProfile> _byId;

    public CodeLanguageService()
    {
        _profiles = BuildProfiles();
        _byId = _profiles.ToDictionary(profile => profile.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CodeLanguageProfile> GetProfiles() => _profiles;
    public CodeLanguageProfile Get(string languageId) => _byId.GetValueOrDefault(languageId ?? string.Empty) ?? _byId["text"];

    public CodeLanguageProfile Detect(string fileNameOrExtension, string? content = null)
    {
        var extension = Path.GetExtension(fileNameOrExtension ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension) && (fileNameOrExtension ?? string.Empty).StartsWith('.')) extension = fileNameOrExtension;
        var profile = _profiles.FirstOrDefault(candidate => candidate.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        if (profile is not null) return profile;
        var sample = content ?? string.Empty;
        if (sample.Contains("using System;", StringComparison.Ordinal) || sample.Contains("namespace ", StringComparison.Ordinal)) return Get("csharp");
        if (sample.Contains("function ", StringComparison.Ordinal) || sample.Contains("const ", StringComparison.Ordinal)) return Get("javascript");
        if (sample.Contains("$fn", StringComparison.Ordinal) || sample.Contains("linear_extrude", StringComparison.Ordinal)) return Get("openscad");
        return Get("text");
    }

    private IReadOnlyList<CodeLanguageProfile> BuildProfiles() => new List<CodeLanguageProfile>
    {
        Profile("text", "Plain text", [], "", "", "", [], false, false),
        Profile("csharp", "C#", [".cs", ".razor", ".cshtml"], "//", "/*", "*/", ["class","interface","record","public","private","protected","internal","static","sealed","abstract","async","await","using","namespace","new","return","if","else","switch","case","for","foreach","while","try","catch","finally","throw","var","string","int","double","bool","null","true","false"], true, false),
        Profile("javascript", "JavaScript", [".js", ".mjs", ".cjs"], "//", "/*", "*/", ["const","let","var","function","class","extends","async","await","return","if","else","switch","case","for","while","try","catch","finally","throw","new","import","export","from","true","false","null","undefined"], true, false),
        Profile("typescript", "TypeScript", [".ts", ".tsx"], "//", "/*", "*/", ["interface","type","enum","namespace","implements","public","private","protected","readonly","const","let","class","function","async","await","return","import","export","from"], true, false),
        Profile("html", "HTML / Razor markup", [".html", ".htm", ".razor"], "", "<!--", "-->", ["html","head","body","script","style","div","section","article","template","component"], false, false),
        Profile("xml", "XML", [".xml", ".xaml", ".svg", ".csproj", ".props", ".targets"], "", "<!--", "-->", [], false, false),
        Profile("css", "CSS / SCSS", [".css", ".scss", ".sass", ".less"], "//", "/*", "*/", ["@media","@supports","@keyframes","display","position","transform","animation","color","background","grid","flex"], true, false),
        Profile("json", "JSON", [".json", ".jsonc"], "//", "/*", "*/", ["true","false","null"], true, false),
        Profile("yaml", "YAML", [".yaml", ".yml"], "#", "", "", ["true","false","null"], false, true),
        Profile("markdown", "Markdown", [".md", ".markdown"], "", "<!--", "-->", [], false, false),
        Profile("sql", "SQL", [".sql"], "--", "/*", "*/", ["select","from","where","join","inner","left","right","group","order","by","insert","update","delete","create","alter","drop","table","view","procedure","function","as","and","or","not","null"], false, false),
        Profile("python", "Python", [".py", ".pyw"], "#", "\"\"\"", "\"\"\"", ["def","class","async","await","return","yield","if","elif","else","for","while","try","except","finally","raise","import","from","as","with","lambda","True","False","None"], false, true),
        Profile("powershell", "PowerShell", [".ps1", ".psm1", ".psd1"], "#", "<#", "#>", ["function","param","begin","process","end","if","elseif","else","foreach","while","switch","try","catch","finally","throw","return","class","enum"], true, false),
        Profile("bash", "Shell / Bash", [".sh", ".bash", ".zsh"], "#", "", "", ["if","then","else","elif","fi","for","while","do","done","case","esac","function","return","local","export"], false, false),
        Profile("cpp", "C / C++", [".c", ".h", ".cpp", ".hpp", ".cc"], "//", "/*", "*/", ["class","struct","namespace","template","typename","public","private","protected","virtual","override","const","constexpr","auto","return","if","else","switch","for","while","try","catch","throw","new","delete","nullptr"], true, false),
        Profile("java", "Java / Kotlin", [".java", ".kt", ".kts"], "//", "/*", "*/", ["class","interface","enum","object","fun","val","var","public","private","protected","internal","static","final","abstract","return","if","else","when","switch","for","while","try","catch","finally","throw","new","null","true","false"], true, false),
        Profile("rust", "Rust", [".rs"], "//", "/*", "*/", ["fn","struct","enum","trait","impl","pub","crate","mod","use","let","mut","const","static","async","await","move","return","if","else","match","for","while","loop","true","false"], true, false),
        Profile("go", "Go", [".go"], "//", "/*", "*/", ["package","import","func","type","struct","interface","var","const","go","defer","return","if","else","switch","select","case","for","range","map","chan","true","false","nil"], true, false),
        Profile("php", "PHP", [".php", ".phtml"], "//", "/*", "*/", ["class","interface","trait","function","public","private","protected","static","final","abstract","namespace","use","return","if","else","switch","foreach","while","try","catch","finally","throw","new","true","false","null"], true, false),
        Profile("ruby", "Ruby", [".rb", ".rake"], "#", "=begin", "=end", ["class","module","def","end","do","if","elsif","else","unless","case","when","while","until","for","begin","rescue","ensure","raise","return","yield","true","false","nil"], false, true),
        Profile("swift", "Swift", [".swift"], "//", "/*", "*/", ["class","struct","enum","protocol","extension","func","var","let","public","private","internal","open","static","override","async","await","throws","return","if","else","switch","case","for","while","guard","defer","true","false","nil"], true, false),
        Profile("openscad", "OpenSCAD", [".scad"], "//", "/*", "*/", ["module","function","let","for","intersection_for","if","else","each","include","use","cube","sphere","cylinder","polyhedron","square","circle","polygon","text","translate","rotate","scale","resize","mirror","multmatrix","color","offset","minkowski","hull","union","difference","intersection","render","linear_extrude","rotate_extrude","projection","import","surface","children","true","false","undef"], true, false)
    }.AsReadOnly();

    private CodeLanguageProfile Profile(string id, string name, List<string> extensions, string line, string blockStart, string blockEnd, List<string> keywords, bool braces, bool indentation) =>
        new() { Id = id, DisplayName = name, Extensions = extensions, LineComment = line, BlockCommentStart = blockStart, BlockCommentEnd = blockEnd, Keywords = keywords, UsesBraces = braces, UsesIndentation = indentation };
}
