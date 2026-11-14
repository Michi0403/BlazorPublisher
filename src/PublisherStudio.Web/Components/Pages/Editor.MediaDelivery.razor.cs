using Microsoft.JSInterop;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Components.Pages;

/// <summary>Handles retrieval and insertion of generated media for the current editor.</summary>
public partial class Editor
{
    private Guid _generatedMediaRequest;
    private string _generatedMediaPeer = string.Empty;
    private Guid _generatedMediaDocument;
    private Guid _generatedMediaPage;
    private string _generatedMediaStatus = string.Empty;

    private async Task InsertGeneratedMediaAsync()
    {
        if (_mediaAiBusy || !_mediaAiVisible || _generatedMediaRequest == Guid.Empty) return;
        var generation = _mediaAiGeneration;
        if (!IsMediaInsertionTarget(generation))
        {
            _generatedMediaStatus = LT("Return to the page that requested this media before inserting it.");
            return;
        }

        var kind = _mediaAiKind;
        var cancellation = new CancellationTokenSource();
        _mediaAiCancellation = cancellation;
        _mediaAiBusy = true;
        _generatedMediaStatus = LT("Checking LocalGPT for generated media…");
        try
        {
            var asset = await GeneratedMedia.DownloadAsync(_generatedMediaPeer, _generatedMediaRequest, kind, cancellation.Token).ConfigureAwait(true);
            if (!IsMediaInsertionTarget(generation)) return;
            if (asset is null)
            {
                _generatedMediaStatus = LT("No file is ready yet. Check the LocalGPT Council and its approval inbox, then try again.");
                return;
            }

            var dataUrl = $"data:{asset.ContentType};base64,{Convert.ToBase64String(asset.Bytes)}";
            if (kind == "picture")
            {
                State.AddImage(dataUrl, asset.FileName);
            }
            else
            {
                var module = await JS.InvokeAsync<IJSObjectReference>("import", cancellation.Token, "./js/mediaStudioInterop.js?v=4.3.0").ConfigureAwait(true);
                await using (module.ConfigureAwait(true))
                {
                    var temporaryAssetId = Guid.NewGuid();
                    var inspectionUrl = MediaAssets.RegisterBytes(temporaryAssetId, asset.Bytes, asset.ContentType);
                    try
                    {
                        var info = await module.InvokeAsync<MediaSourceInfo>("inspectMediaDataUrl", cancellation.Token, inspectionUrl, "video").ConfigureAwait(true);
                        if (!IsMediaInsertionTarget(generation)) return;
                        var media = State.AddVideo(dataUrl, asset.ContentType, asset.FileName, info.DurationSeconds, info.PosterDataUrl);
                        State.UpdateMedia(media.Id, item => item.WaveformSamples = info.WaveformSamples ?? [], capture: false);
                        MediaAssets.Copy(temporaryAssetId, media.Id);
                        State.SelectElement(media.Id);
                        _timelineVisible = true;
                    }
                    finally
                    {
                        MediaAssets.Remove(temporaryAssetId);
                    }
                }
            }

            _generatedMediaRequest = Guid.Empty;
            await CloseMediaAiAsync().ConfigureAwait(true);
            ShowNotice($"{LT("Inserted generated media")} {asset.FileName}");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Logger.LogDebug("Canceled generated media insertion.");
        }
        catch (Exception error)
        {
            if (generation == _mediaAiGeneration && _mediaAiVisible)
            {
                _generatedMediaStatus = error.Message;
                Notifications.Error(error.Message, source: nameof(Editor));
            }
            Logger.LogWarning(error, "Could not insert LocalGPT-generated media of kind {Kind}.", kind);
        }
        finally
        {
            if (ReferenceEquals(_mediaAiCancellation, cancellation))
            {
                _mediaAiCancellation = null;
                _mediaAiBusy = false;
            }
            cancellation.Dispose();
        }
    }

    private bool IsMediaInsertionTarget(int generation) => _mediaAiVisible && generation == _mediaAiGeneration &&
        State.Document.Id == _generatedMediaDocument && State.CurrentPage.Id == _generatedMediaPage;
}
