using Microsoft.Extensions.Options;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public sealed class ApplicationPathService(IOptions<PublisherStudioPathOptions> options) : IApplicationPathService
{
    public PublisherStudioPathOptions GetDefaults() => Resolve();

    public PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null)
    {
        var configured = options.Value;
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        var studioRoot = Path.Combine(documents, "PublisherStudio");
        return new PublisherStudioPathOptions
        {
            Images = Choose(projectOverrides?.Images, configured.Images, pictures, Path.Combine(studioRoot, "Images")),
            Video = Choose(projectOverrides?.Video, configured.Video, videos, Path.Combine(studioRoot, "Video")),
            Audio = Choose(projectOverrides?.Audio, configured.Audio, music, Path.Combine(studioRoot, "Audio")),
            Documents = Choose(projectOverrides?.Documents, configured.Documents, documents, Path.Combine(studioRoot, "Documents")),
            Exports = Choose(projectOverrides?.Exports, configured.Exports, Path.Combine(studioRoot, "Exports")),
            OpenScad = Choose(projectOverrides?.OpenScad, configured.OpenScad, Path.Combine(studioRoot, "OpenSCAD")),
            Projects = Choose(projectOverrides?.Projects, configured.Projects, Path.Combine(studioRoot, "Projects"))
        };
    }

    public string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null)
    {
        var paths = Resolve(projectOverrides);
        return (mediaKind ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "image" or "images" or "picture" => paths.Images,
            "video" or "videos" => paths.Video,
            "audio" or "music" => paths.Audio,
            "openscad" or "3d" => paths.OpenScad,
            "export" or "exports" => paths.Exports,
            "project" or "projects" => paths.Projects,
            _ => paths.Documents
        };
    }

    public void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null)
    {
        var paths = Resolve(projectOverrides);
        foreach (var path in new[] { paths.Images, paths.Video, paths.Audio, paths.Documents, paths.Exports, paths.OpenScad, paths.Projects }.Where(path => !string.IsNullOrWhiteSpace(path)))
            Directory.CreateDirectory(path);
    }

    private string Choose(params string?[] candidates)
    {
        var selected = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate)) ?? AppContext.BaseDirectory;
        return Environment.ExpandEnvironmentVariables(Path.GetFullPath(selected));
    }
}
