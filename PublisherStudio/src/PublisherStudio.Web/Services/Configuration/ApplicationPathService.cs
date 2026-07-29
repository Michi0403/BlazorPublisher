using Microsoft.Extensions.Options;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public sealed class ApplicationPathService(IOptions<PublisherStudioPathOptions> options, ILogger<ApplicationPathService> logger) : IApplicationPathService
{
    public PublisherStudioPathOptions GetDefaults() {
        try
        {
            logger.LogTrace($"Entering ApplicationPathService.GetDefaults.");
            return Resolve();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"ApplicationPathService.GetDefaults failed: {exception.Message}");
            throw;
        }
    }

    public PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null)
    {
        try
        {
            logger.LogTrace($"Entering ApplicationPathService.Resolve.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"ApplicationPathService.Resolve failed: {exception.Message}");
            throw;
        }
    }

    public string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null)
    {
        try
        {
            logger.LogTrace($"Entering ApplicationPathService.ResolveMediaPath.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"ApplicationPathService.ResolveMediaPath failed: {exception.Message}");
            throw;
        }
    }

    public void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null)
    {
        try
        {
            logger.LogTrace($"Entering ApplicationPathService.EnsureDirectories.");
                    var paths = Resolve(projectOverrides);
                    foreach (var path in new[] { paths.Images, paths.Video, paths.Audio, paths.Documents, paths.Exports, paths.OpenScad, paths.Projects }.Where(path => !string.IsNullOrWhiteSpace(path)))
                        Directory.CreateDirectory(path);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"ApplicationPathService.EnsureDirectories failed: {exception.Message}");
            throw;
        }
    }

    private string Choose(params string?[] candidates)
    {
        try
        {
            logger.LogTrace($"Entering ApplicationPathService.Choose.");
                    var selected = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate)) ?? AppContext.BaseDirectory;
                    return Environment.ExpandEnvironmentVariables(Path.GetFullPath(selected));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"ApplicationPathService.Choose failed: {exception.Message}");
            throw;
        }
    }
}
