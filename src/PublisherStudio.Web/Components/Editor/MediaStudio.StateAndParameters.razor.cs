using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using Microsoft.JSInterop;
using Microsoft.Extensions.Options;
using PublisherStudio;
using PublisherStudio.Components;
using PublisherStudio.Components.Editor;
using PublisherStudio.Components.Layout;
using PublisherStudio.Components.OrganicPlugins;
using PublisherStudio.Components.Shared;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.Documentation;
using PublisherStudio.Services.MediaConversion;
using PublisherStudio.Services.MediaStudio.UseCases;
using PublisherStudio.Services.OrganicPlugins;
using PublisherStudio.Services.Panels;
using PublisherStudio.Services.PictureStudio.Import;
using PublisherStudio.Services.Publication;
using PublisherStudio.Services.Streaming.MediaHost;
using PublisherStudio.Services.Streaming.Profiles;
using PublisherStudio.Services.Streaming.UseCases.Editor;
using PublisherStudio.Services.UserExperience;
using PublisherStudio.Services.VideoStudio.Export;
using PublisherStudio.Services.VideoStudio.Import;
using System.Text.Json;
using System.Net;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PublisherStudio.Components.Editor;

/// <summary>Maintains the existing MediaStudio component behavior as a cohesive, independently reviewable partial declaration without changing its state or public surface.</summary>
public partial class MediaStudio
{

    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the media studio state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public bool Visible { get; set; }
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this media studio instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the kind value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public PublicationElementKind Kind { get; set; } = PublicationElementKind.Audio;
    /// <summary>
    /// Gets or sets the initial media value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial media value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public PublicationMediaElement? InitialMedia { get; set; }
    /// <summary>
    /// Gets or sets the initial inserted segment value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial inserted segment value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public PublicationMediaSegment? InitialInsertedSegment { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether editing existing applies to the media studio state.
    /// </summary>
    /// <value>The editing existing value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public bool EditingExisting { get; set; }
    /// <summary>
    /// Gets or sets the saved value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The saved value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public EventCallback<MediaEditorResult> Saved { get; set; }
    /// <summary>
    /// Gets or sets the send to media converter value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The send to media converter value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public EventCallback<MediaConversionInsertRequest> SendToMediaConverter { get; set; }
    /// <summary>
    /// Gets or sets the insert layer into mainframe value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The insert layer into mainframe value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public EventCallback<VideoLayerMainframeInsertRequest> InsertLayerIntoMainframe { get; set; }
    /// <summary>
    /// Gets or sets the cancelled value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The cancelled value exposed by <see cref="MediaStudio"/>.</value>
    [Parameter] public EventCallback Cancelled { get; set; }

    /// <summary>
    /// Stores the internal self state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private DotNetObjectReference<MediaStudio>? _self;
    /// <summary>
    /// Stores the internal media context menu state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private DxContextMenu _mediaContextMenu = default!;
    /// <summary>
    /// Stores the JavaScript object reference dependency used by <see cref="MediaStudio"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IJSObjectReference? _module;
    /// <summary>
    /// Stores the internal loaded session state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid _loadedSession;
    /// <summary>
    /// Stores the internal initialized session state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid _initializedSession;
    /// <summary>
    /// Stores the internal recording recovery attempted session state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid _recordingRecoveryAttemptedSession;
    /// <summary>
    /// Stores the internal name state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _name = "Audio";
    /// <summary>
    /// Stores the internal data URL state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _dataUrl = string.Empty;
    /// <summary>
    /// Stores the internal MIME type state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _mimeType = string.Empty;
    /// <summary>
    /// Stores the internal poster data URL state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _posterDataUrl = string.Empty;
    /// <summary>
    /// Stores the internal preview asset identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid _previewAssetId = Guid.NewGuid();
    /// <summary>
    /// Stores the internal preview source state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _previewSource = string.Empty;
    /// <summary>
    /// Stores the internal duration state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _duration;
    /// <summary>
    /// Stores the internal trim start state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _trimStart;
    /// <summary>
    /// Stores the internal trim end state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _trimEnd;
    /// <summary>
    /// Stores the internal volume state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _volume = 1;
    /// <summary>
    /// Stores the internal playback rate state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _playbackRate = 1;
    /// <summary>
    /// Stores the internal fade in state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _fadeIn;
    /// <summary>
    /// Stores the internal fade out state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _fadeOut;
    /// <summary>
    /// Stores the internal video fit mode state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private PublicationVideoFitMode _videoFitMode = PublicationVideoFitMode.Stretch;
    /// <summary>
    /// Stores the internal muted state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _muted;
    /// <summary>
    /// Stores the internal loop state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _loop;
    /// <summary>
    /// Stores the internal auto play state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _autoPlay = true;
    /// <summary>
    /// Stores the internal recording state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recording;
    /// <summary>
    /// Stores the internal recording stopping state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingStopping;
    /// <summary>
    /// Stores the internal rendering video export state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _renderingVideoExport;
    /// <summary>
    /// Stores the internal recording adaptive quality state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingAdaptiveQuality = true;
    /// <summary>
    /// Stores the internal recording adaptive profile state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _recordingAdaptiveProfile = "quality";
    /// <summary>
    /// Stores the internal recording adapt video state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingAdaptVideo = true;
    /// <summary>
    /// Stores the internal recording adapt audio state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingAdaptAudio = true;
    /// <summary>
    /// Stores the internal recording use browser capability probe state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingUseBrowserCapabilityProbe = true;
    /// <summary>
    /// Stores the internal recording preserve native resolution state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingPreserveNativeResolution = true;
    /// <summary>
    /// Stores the internal recording allow frame rate reduction state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingAllowFrameRateReduction = true;
    /// <summary>
    /// Stores the internal recording allow resolution reduction state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _recordingAllowResolutionReduction;
    /// <summary>
    /// Stores the internal recording size mode state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _recordingSizeMode = "source";
    /// <summary>Stores Panel Studio viewport presets that are valid capture-size choices for the current Media Studio policy.</summary>
    private List<PanelStudioPreviewPreset> _recordingViewportPresets = [];
    /// <summary>
    /// Stores the internal recording custom width state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int _recordingCustomWidth;
    /// <summary>
    /// Stores the internal recording custom height state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int _recordingCustomHeight;
    /// <summary>
    /// Stores the internal recording frame rate state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _recordingFrameRate;
    /// <summary>
    /// Stores the internal recording video bitrate kbps state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int _recordingVideoBitrateKbps;
    /// <summary>
    /// Stores the internal recording audio bitrate kbps state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int _recordingAudioBitrateKbps;
    /// <summary>
    /// Stores the internal recording codec preference state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _recordingCodecPreference = "auto";
    /// <summary>
    /// Stores the internal saving state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _saving;
    /// <summary>
    /// Stores the internal embedding state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _embedding;
    /// <summary>
    /// Stores the internal embedding progress state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _embeddingProgress;
    /// <summary>
    /// Stores the internal has retained recording state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _hasRetainedRecording;
    /// <summary>
    /// Stores the internal retained recording size bytes state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private long _retainedRecordingSizeBytes;
    /// <summary>
    /// Stores the internal retained recording file name state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _retainedRecordingFileName = string.Empty;
    /// <summary>
    /// Stores the internal retained recording object URL state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string _retainedRecordingObjectUrl = string.Empty;
    /// <summary>
    /// Stores the internal playback trigger state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private PublicationMediaPlaybackTrigger _playbackTrigger = PublicationMediaPlaybackTrigger.OnPageEnter;
    /// <summary>
    /// Stores the in-memory waveform collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private List<double> _waveform = [];
    /// <summary>
    /// Stores the internal error state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string? _error;
    /// <summary>
    /// Stores the internal warning state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string? _warning;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _disposed;
    /// <summary>
    /// Stores the internal recording buffer state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private StringBuilder? _recordingBuffer;
    /// <summary>
    /// Stores the internal recording identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private string? _recordingId;
    /// <summary>
    /// Stores the internal recording expected chunks state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int _recordingExpectedChunks;
    /// <summary>
    /// Stores the internal recording next chunk state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int _recordingNextChunk;
    /// <summary>
    /// Stores the internal recording expected bytes state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private long _recordingExpectedBytes;
    /// <summary>
    /// Stores the in-memory segments collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private readonly List<PublicationMediaSegment> _segments = [];
    /// <summary>Stores the timeline clip identifiers selected by plain, Ctrl/Command, or Shift click.</summary>
    private readonly HashSet<Guid> _selectedSegmentIds = [];
    /// <summary>Stores the anchor clip used when Shift extends a timeline selection.</summary>
    private Guid? _timelineSelectionAnchorId;
    /// <summary>Stores transient preview asset identifiers that may still be referenced by the currently rendered browser DOM.</summary>
    private readonly HashSet<Guid> _previewAssetIds = [];
    /// <summary>Stores preview asset identifiers whose DOM references were replaced and can be released after the next render completes.</summary>
    private readonly HashSet<Guid> _retiredPreviewAssetIds = [];
    /// <summary>Tracks the multi-clip selected-range export that is currently running in the browser.</summary>
    private bool _renderingTimelineSelectionExport;
    /// <summary>
    /// Stores the internal selected segment identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid? _selectedSegmentId;
    /// <summary>
    /// Stores the internal clip clipboard state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private PublicationMediaSegment? _clipClipboard;
    /// <summary>
    /// Stores the internal mouse mode state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private MediaStudioMouseMode _mouseMode = MediaStudioMouseMode.SelectSection;
    /// <summary>
    /// Stores the internal playhead state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _playhead;
    /// <summary>
    /// Stores the in-memory frame clip polygon collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private readonly List<MediaFramePoint> _frameClipPolygon = [];
    /// <summary>
    /// Stores the in-memory morph clip polygon collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private readonly List<MediaFramePoint> _morphClipPolygon = [];
    /// <summary>
    /// Stores the in-memory frame draft collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private readonly List<MediaFramePoint> _frameDraft = [];
    /// <summary>
    /// Stores the in-memory frame clipboard collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private readonly List<MediaFramePoint> _frameClipboard = [];
    /// <summary>
    /// Stores the internal frame region editing state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _frameRegionEditing;
    /// <summary>
    /// Stores the internal editing morph region state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _editingMorphRegion;
    /// <summary>
    /// Stores the internal selected frame point index state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private int? _selectedFramePointIndex;
    /// <summary>
    /// Stores the internal auto create selection layers state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _autoCreateSelectionLayers = true;
    /// <summary>
    /// Stores the internal selected video layer identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid? _selectedVideoLayerId;
    /// <summary>
    /// Stores the internal selected video filter identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid? _selectedVideoFilterId;
    /// <summary>
    /// Stores the internal selected cut section identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid? _selectedCutSectionId;
    /// <summary>
    /// Stores the internal new video filter kind state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private VideoEffectFilterKind _newVideoFilterKind = VideoEffectFilterKind.ChromaKey;
    /// <summary>
    /// Stores the internal video selection is point state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _videoSelectionIsPoint;
    /// <summary>
    /// Stores the internal video selection point seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _videoSelectionPointSeconds;
    /// <summary>
    /// Stores the internal video selection start seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _videoSelectionStartSeconds;
    /// <summary>
    /// Stores the internal video selection end seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _videoSelectionEndSeconds;
    /// <summary>
    /// Stores the internal video selection user committed state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _videoSelectionUserCommitted;
    /// <summary>
    /// Stores the internal pending insert segment state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private PublicationMediaSegment? _pendingInsertSegment;
    /// <summary>
    /// Stores the internal pending insert source seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _pendingInsertSourceSeconds;
    /// <summary>
    /// Stores the internal pending insert minimum source seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _pendingInsertMinimumSourceSeconds;
    /// <summary>
    /// Stores the internal pending insert maximum source seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double _pendingInsertMaximumSourceSeconds;
    /// <summary>
    /// Stores the internal pending insert timeline seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double? _pendingInsertTimelineSeconds;
    /// <summary>
    /// Stores the internal pending drop source seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double? _pendingDropSourceSeconds;
    /// <summary>
    /// Stores the internal pending drop timeline seconds state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private double? _pendingDropTimelineSeconds;
    /// <summary>
    /// Stores the internal pending drop is point state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private bool _pendingDropIsPoint;
    /// <summary>Stores whether the next video recording is waiting for a sequence boundary choice before capture starts.</summary>
    private bool _recordingStartPlacementRequired;
    /// <summary>Stores the zero-based sequence boundary selected for the next recording before capture starts.</summary>
    private int _recordingStartPlacementBoundary;
    /// <summary>Stores the browser recording source that will start after pre-recording placement is confirmed.</summary>
    private string _pendingRecordingSource = string.Empty;
    /// <summary>Stores the preselected sequence boundary that should remain selected when the new recording finishes.</summary>
    private int? _plannedRecordingPlacementBoundary;
    /// <summary>Stores whether a newly completed video recording is waiting for an explicit sequence boundary choice.</summary>
    private bool _recordingPlacementRequired;
    /// <summary>Stores the zero-based sequence boundary chosen for a newly completed video recording.</summary>
    private int _recordingPlacementBoundary;
    /// <summary>Stores whether the browser-retained recording has already been embedded into the canonical sequence.</summary>
    private bool _retainedRecordingCommittedToSequence;
    /// <summary>Stores the canonical sequence time at which the next embedded recording must be inserted.</summary>
    private double? _pendingRecordingInsertTimelineSeconds;
    /// <summary>
    /// Stores the internal video project state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private VideoProjectDocument? _videoProject;
    /// <summary>
    /// Stores the internal active project track identifier state used by <see cref="MediaStudio"/> while executing its surrounding workflow.
    /// </summary>
    private Guid _activeProjectTrackId;
    /// <summary>
    /// Stores the in-memory video project issues collection maintained internally by <see cref="MediaStudio"/> for its current workflow state.
    /// </summary>
    private List<InterchangeIssue> _videoProjectIssues = [];

    /// <summary>
    /// Gets a value indicating whether video applies to the media studio state.
    /// </summary>
    /// <value>The is video value exposed by <see cref="MediaStudio"/>.</value>
    private bool IsVideo => GetIsVideoForRender();
    /// <summary>
    /// Gets a value indicating whether retained recording applies to the media studio state.
    /// </summary>
    /// <value>The has retained recording value exposed by <see cref="MediaStudio"/>.</value>
    private bool HasRetainedRecording => GetHasRetainedRecordingForRender();
    /// <summary>Gets whether the browser-retained recording or the currently selected embedded media can be downloaded.</summary>
    /// <value><see langword="true"/> when a downloadable current media source exists; otherwise <see langword="false"/>.</value>
    private bool CanDownloadCurrentMedia => GetCanDownloadCurrentMediaForRender();
    /// <summary>Gets the output command label that distinguishes a fresh recording from an embedded selected source.</summary>
    /// <value>The localized download command text for the active source.</value>
    private string DownloadMediaText => GetDownloadMediaTextForRender();
    /// <summary>
    /// Gets the recording policy value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording policy value exposed by <see cref="MediaStudio"/>.</value>
    private PublisherMediaSessionDefaultsPolicy RecordingPolicy => GetRecordingPolicyForRender();
    /// <summary>
    /// Gets the recording target width value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording target width value exposed by <see cref="MediaStudio"/>.</value>
    private int RecordingTargetWidth => GetRecordingTargetWidthForRender();
    /// <summary>
    /// Gets the recording target height value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording target height value exposed by <see cref="MediaStudio"/>.</value>
    private int RecordingTargetHeight => GetRecordingTargetHeightForRender();
    /// <summary>
    /// Gets the recording size summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recording size summary value exposed by <see cref="MediaStudio"/>.</value>
    private string RecordingSizeSummary
    {
        get
        {
            var selection = ResolveRecordingSizeSelection(_recordingSizeMode);
            return selection.Width > 0 && selection.Height > 0
                ? $"{selection.Name} · {selection.Width} × {selection.Height} · {AspectRatioLabel(selection.Width, selection.Height)}"
                : selection.Name;
        }
    }
    /// <summary>Gets the frame-rate preset key that best represents the current capture frame-rate value.</summary>
    /// <value>The source, standard-rate, or custom frame-rate preset key.</value>
    private string RecordingFrameRatePresetKey => GetRecordingFrameRatePresetKey();
    /// <summary>
    /// Gets a value indicating whether video project applies to the media studio state.
    /// </summary>
    /// <value>The has video project value exposed by <see cref="MediaStudio"/>.</value>
    private bool HasVideoProject => GetHasVideoProjectForRender();
    /// <summary>
    /// Gets the project video tracks collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The project video tracks value exposed by <see cref="MediaStudio"/>.</value>
    private IReadOnlyList<MediaTimelineTrack> ProjectVideoTracks => GetProjectVideoTracksForRender();
    /// <summary>
    /// Gets the video project missing media count that quantifies the associated media studio data.
    /// </summary>
    /// <value>The video project missing media count value exposed by <see cref="MediaStudio"/>.</value>
    private int VideoProjectMissingMediaCount => GetVideoProjectMissingMediaCountForRender();
    /// <summary>
    /// Gets the video project title value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project title value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoProjectTitle => GetVideoProjectTitleForRender();
    /// <summary>
    /// Gets the video project format summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project format summary value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoProjectFormatSummary => GetVideoProjectFormatSummaryForRender();
    /// <summary>
    /// Gets the video project canvas summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project canvas summary value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoProjectCanvasSummary => GetVideoProjectCanvasSummaryForRender();
    /// <summary>
    /// Gets the video project track summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project track summary value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoProjectTrackSummary => GetVideoProjectTrackSummaryForRender();
    /// <summary>
    /// Gets the video project missing summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video project missing summary value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoProjectMissingSummary => GetVideoProjectMissingSummaryForRender();
    /// <summary>
    /// Gets the selected segment value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected segment value exposed by <see cref="MediaStudio"/>.</value>
    private PublicationMediaSegment? SelectedSegment => GetSelectedSegmentForRender();
    /// <summary>
    /// Gets the selected timeline segments collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The selected timeline segments value exposed by <see cref="MediaStudio"/>.</value>
    private IReadOnlyList<PublicationMediaSegment> SelectedTimelineSegments => GetSelectedTimelineSegmentsForRender();
    /// <summary>Gets whether one or more playable selected clips can be exported as selected ranges.</summary>
    /// <value>The can export selected timeline ranges value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanExportSelectedTimelineRanges => GetCanExportSelectedTimelineRangesForRender();
    /// <summary>
    /// Gets the selected video layers collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The selected video layers value exposed by <see cref="MediaStudio"/>.</value>
    private IReadOnlyList<VideoEffectLayer> SelectedVideoLayers => GetSelectedVideoLayersForRender();
    /// <summary>
    /// Gets the selected video layer value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected video layer value exposed by <see cref="MediaStudio"/>.</value>
    private VideoEffectLayer? SelectedVideoLayer => GetSelectedVideoLayerForRender();
    /// <summary>
    /// Gets the selected video filters collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The selected video filters value exposed by <see cref="MediaStudio"/>.</value>
    private IReadOnlyList<VideoEffectFilter> SelectedVideoFilters => GetSelectedVideoFiltersForRender();
    /// <summary>
    /// Gets the selected video filter value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected video filter value exposed by <see cref="MediaStudio"/>.</value>
    private VideoEffectFilter? SelectedVideoFilter => GetSelectedVideoFilterForRender();
    /// <summary>
    /// Gets the selected cut sections collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The selected cut sections value exposed by <see cref="MediaStudio"/>.</value>
    private IReadOnlyList<MediaTemporalSection> SelectedCutSections => GetSelectedCutSectionsForRender();
    /// <summary>
    /// Gets the selected cut section value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected cut section value exposed by <see cref="MediaStudio"/>.</value>
    private MediaTemporalSection? SelectedCutSection => GetSelectedCutSectionForRender();
    /// <summary>
    /// Gets the selected segment index value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected segment index value exposed by <see cref="MediaStudio"/>.</value>
    private int SelectedSegmentIndex => GetSelectedSegmentIndexForRender();
    /// <summary>
    /// Gets a value indicating whether selected segment applies to the media studio state.
    /// </summary>
    /// <value>The has selected segment value exposed by <see cref="MediaStudio"/>.</value>
    private bool HasSelectedSegment => GetHasSelectedSegmentForRender();
    /// <summary>
    /// Gets a value indicating whether source applies to the media studio state.
    /// </summary>
    /// <value>The has source value exposed by <see cref="MediaStudio"/>.</value>
    private bool HasSource => GetHasSourceForRender();
    /// <summary>
    /// Gets a value indicating whether paste section applies to the media studio state.
    /// </summary>
    /// <value>The can paste section value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanPasteSection => GetCanPasteSectionForRender();
    /// <summary>
    /// Gets a value indicating whether delete section applies to the media studio state.
    /// </summary>
    /// <value>The can delete section value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanDeleteSection => GetCanDeleteSectionForRender();
    /// <summary>
    /// Gets a value indicating whether add cut line applies to the media studio state.
    /// </summary>
    /// <value>The can add cut line value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanAddCutLine => GetCanAddCutLineForRender();
    /// <summary>
    /// Gets a value indicating whether remove cut line applies to the media studio state.
    /// </summary>
    /// <value>The can remove cut line value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanRemoveCutLine => GetCanRemoveCutLineForRender();
    /// <summary>
    /// Gets a value indicating whether cut video selection applies to the media studio state.
    /// </summary>
    /// <value>The can cut video selection value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanCutVideoSelection => GetCanCutVideoSelectionForRender();
    /// <summary>
    /// Gets a value indicating whether use video selection as trim applies to the media studio state.
    /// </summary>
    /// <value>The can use video selection as trim value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanUseVideoSelectionAsTrim => GetCanUseVideoSelectionAsTrimForRender();
    /// <summary>
    /// Gets a value indicating whether copy video selection applies to the media studio state.
    /// </summary>
    /// <value>The can copy video selection value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanCopyVideoSelection => GetCanCopyVideoSelectionForRender();
    /// <summary>
    /// Gets a value indicating whether save cut section applies to the media studio state.
    /// </summary>
    /// <value>The can save cut section value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanSaveCutSection => GetCanSaveCutSectionForRender();
    /// <summary>
    /// Gets a value indicating whether apply selection to layer applies to the media studio state.
    /// </summary>
    /// <value>The can apply selection to layer value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanApplySelectionToLayer => GetCanApplySelectionToLayerForRender();
    /// <summary>
    /// Gets the selected video layer index value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected video layer index value exposed by <see cref="MediaStudio"/>.</value>
    private int SelectedVideoLayerIndex => GetSelectedVideoLayerIndexForRender();
    /// <summary>
    /// Gets a value indicating whether move video layer up applies to the media studio state.
    /// </summary>
    /// <value>The can move video layer up value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanMoveVideoLayerUp => GetCanMoveVideoLayerUpForRender();
    /// <summary>
    /// Gets a value indicating whether move video layer down applies to the media studio state.
    /// </summary>
    /// <value>The can move video layer down value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanMoveVideoLayerDown => GetCanMoveVideoLayerDownForRender();
    /// <summary>
    /// Gets a value indicating whether delete video layer applies to the media studio state.
    /// </summary>
    /// <value>The can delete video layer value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanDeleteVideoLayer => GetCanDeleteVideoLayerForRender();
    /// <summary>
    /// Gets the selected video filter index value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected video filter index value exposed by <see cref="MediaStudio"/>.</value>
    private int SelectedVideoFilterIndex => GetSelectedVideoFilterIndexForRender();
    /// <summary>
    /// Gets a value indicating whether move video filter up applies to the media studio state.
    /// </summary>
    /// <value>The can move video filter up value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanMoveVideoFilterUp => GetCanMoveVideoFilterUpForRender();
    /// <summary>
    /// Gets a value indicating whether move video filter down applies to the media studio state.
    /// </summary>
    /// <value>The can move video filter down value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanMoveVideoFilterDown => GetCanMoveVideoFilterDownForRender();
    /// <summary>
    /// Gets the sequence duration value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence duration value exposed by <see cref="MediaStudio"/>.</value>
    private double SequenceDuration => GetSequenceDurationForRender();
    /// <summary>
    /// Gets the cut boundary positions collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The cut boundary positions value exposed by <see cref="MediaStudio"/>.</value>
    private IEnumerable<double> CutBoundaryPositions
    {
        get
        {
            for (var index = 1; index < _segments.Count; index++)
                yield return TimelineEdits.SegmentTimelineStart(_segments, index, _playbackRate);
        }
    }
    /// <summary>
    /// Gets the stored active frame points collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The stored active frame points value exposed by <see cref="MediaStudio"/>.</value>
    private List<MediaFramePoint> StoredActiveFramePoints => GetStoredActiveFramePointsForRender();
    /// <summary>
    /// Gets the active frame points collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The active frame points value exposed by <see cref="MediaStudio"/>.</value>
    private IReadOnlyList<MediaFramePoint> ActiveFramePoints => GetActiveFramePointsForRender();
    /// <summary>
    /// Gets the selected frame point value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected frame point value exposed by <see cref="MediaStudio"/>.</value>
    private MediaFramePoint? SelectedFramePoint => GetSelectedFramePointForRender();
    /// <summary>
    /// Gets a value indicating whether frame region applies to the media studio state.
    /// </summary>
    /// <value>The has frame region value exposed by <see cref="MediaStudio"/>.</value>
    private bool HasFrameRegion => GetHasFrameRegionForRender();
    /// <summary>
    /// Gets a value indicating whether morph region applies to the media studio state.
    /// </summary>
    /// <value>The has morph region value exposed by <see cref="MediaStudio"/>.</value>
    private bool HasMorphRegion => GetHasMorphRegionForRender();
    /// <summary>
    /// Gets a value indicating whether active frame region inverted applies to the media studio state.
    /// </summary>
    /// <value>The active frame region inverted value exposed by <see cref="MediaStudio"/>.</value>
    private bool ActiveFrameRegionInverted => GetActiveFrameRegionInvertedForRender();
    /// <summary>
    /// Gets the active frame region name value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active frame region name value exposed by <see cref="MediaStudio"/>.</value>
    private string ActiveFrameRegionName => GetActiveFrameRegionNameForRender();
    /// <summary>
    /// Gets a value indicating whether finish frame region applies to the media studio state.
    /// </summary>
    /// <value>The can finish frame region value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanFinishFrameRegion => GetCanFinishFrameRegionForRender();
    /// <summary>
    /// Gets a value indicating whether paste frame region applies to the media studio state.
    /// </summary>
    /// <value>The can paste frame region value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanPasteFrameRegion => GetCanPasteFrameRegionForRender();
    /// <summary>
    /// Gets a value indicating whether create selection layer applies to the media studio state.
    /// </summary>
    /// <value>The can create selection layer value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanCreateSelectionLayer => GetCanCreateSelectionLayerForRender();
    /// <summary>
    /// Gets a value indicating whether insert layer into mainframe applies to the media studio state.
    /// </summary>
    /// <value>The can insert layer into mainframe value exposed by <see cref="MediaStudio"/>.</value>
    private bool CanInsertLayerIntoMainframe => GetCanInsertLayerIntoMainframeForRender();
    /// <summary>
    /// Gets the video selection start value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection start value exposed by <see cref="MediaStudio"/>.</value>
    private double VideoSelectionStart => GetVideoSelectionStartForRender();
    /// <summary>
    /// Gets the video selection end value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection end value exposed by <see cref="MediaStudio"/>.</value>
    private double VideoSelectionEnd => GetVideoSelectionEndForRender();
    /// <summary>
    /// Gets the video selection label value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection label value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSelectionLabel => GetVideoSelectionLabelForRender();
    /// <summary>
    /// Gets the video selection readout value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection readout value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSelectionReadout => GetVideoSelectionReadoutForRender();
    /// <summary>
    /// Gets the video selection timeline start value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection timeline start value exposed by <see cref="MediaStudio"/>.</value>
    private double VideoSelectionTimelineStart => SourcePositionToTimeline(VideoSelectionStart);
    /// <summary>
    /// Gets the video selection timeline end value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection timeline end value exposed by <see cref="MediaStudio"/>.</value>
    private double VideoSelectionTimelineEnd => SourcePositionToTimeline(VideoSelectionEnd);
    /// <summary>
    /// Gets the video selection detailed summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video selection detailed summary value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSelectionDetailedSummary => GetVideoSelectionDetailedSummaryForRender();
    /// <summary>
    /// Gets the video or audio selection title value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video or audio selection title value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoOrAudioSelectionTitle => GetVideoOrAudioSelectionTitleForRender();
    /// <summary>
    /// Gets the video or audio selection summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video or audio selection summary value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoOrAudioSelectionSummary => GetVideoOrAudioSelectionSummaryForRender();
    /// <summary>
    /// Gets the selected clip name value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected clip name value exposed by <see cref="MediaStudio"/>.</value>
    private string SelectedClipName => GetSelectedClipNameForRender();
    /// <summary>
    /// Gets the send to converter menu text value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The send to converter menu text value exposed by <see cref="MediaStudio"/>.</value>
    private string SendToConverterMenuText => GetSendToConverterMenuTextForRender();
    /// <summary>
    /// Gets the selected segment timeline start value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected segment timeline start value exposed by <see cref="MediaStudio"/>.</value>
    private double SelectedSegmentTimelineStart => GetSelectedSegmentTimelineStartForRender();
    /// <summary>
    /// Gets the selected segment source start value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected segment source start value exposed by <see cref="MediaStudio"/>.</value>
    private double SelectedSegmentSourceStart => GetSelectedSegmentSourceStartForRender();
    /// <summary>
    /// Gets the selected segment source end value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected segment source end value exposed by <see cref="MediaStudio"/>.</value>
    private double SelectedSegmentSourceEnd => GetSelectedSegmentSourceEndForRender();
    /// <summary>
    /// Gets the selected segment effective rate value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected segment effective rate value exposed by <see cref="MediaStudio"/>.</value>
    private double SelectedSegmentEffectiveRate => GetSelectedSegmentEffectiveRateForRender();
    /// <summary>
    /// Gets a value indicating whether pending insert has range applies to the media studio state.
    /// </summary>
    /// <value>The pending insert has range value exposed by <see cref="MediaStudio"/>.</value>
    private bool PendingInsertHasRange => GetPendingInsertHasRangeForRender();
    /// <summary>
    /// Gets the video time overlay style value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video time overlay style value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoTimeOverlayStyle
    {
        get
        {
            var visibleStart = Math.Max(0, Math.Min(_duration, _trimStart));
            var visibleEnd = Math.Max(visibleStart + .01, Math.Min(Math.Max(.01, _duration), _trimEnd));
            var visibleSpan = Math.Max(.01, visibleEnd - visibleStart);
            var start = Math.Clamp((VideoSelectionStart - visibleStart) / visibleSpan, 0, 1);
            var end = Math.Clamp((VideoSelectionEnd - visibleStart) / visibleSpan, 0, 1);
            var point = Math.Clamp(((_videoSelectionIsPoint ? _videoSelectionPointSeconds : visibleStart) - visibleStart) / visibleSpan, 0, 1);
            return $"--video-time-start:{Percent(start)};--video-time-end:{Percent(end)};--video-time-playhead:{Percent(point)};";
        }
    }
    /// <summary>
    /// Gets the video sequence selection style value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video sequence selection style value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSequenceSelectionStyle
    {
        get
        {
            var index = SelectedSegmentIndex;
            if (index < 0 || index >= _segments.Count) return "left:0;width:0";
            var sequenceDuration = Math.Max(.01, SequenceDuration);
            var segment = _segments[index];
            var segmentStart = TimelineEdits.SegmentTimelineStart(_segments, index, _playbackRate);
            var rate = Math.Max(.1, _playbackRate) * Math.Max(.0001, Math.Abs(segment.Speed));
            var start = segmentStart + (Math.Clamp(VideoSelectionStart, segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds) - segment.TrimStartSeconds) / rate;
            var end = segmentStart + (Math.Clamp(VideoSelectionEnd, segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds) - segment.TrimStartSeconds) / rate;
            if (_videoSelectionIsPoint) return $"left:{Percent(start / sequenceDuration)};width:2px";
            return $"left:{Percent(start / sequenceDuration)};width:{Percent(Math.Max(0, end - start) / sequenceDuration)}";
        }
    }
    /// <summary>
    /// Gets the video sequence selection start style value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video sequence selection start style value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSequenceSelectionStartStyle => GetVideoSequenceSelectionStartStyleForRender();
    /// <summary>
    /// Gets the video sequence selection end style value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video sequence selection end style value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSequenceSelectionEndStyle => GetVideoSequenceSelectionEndStyleForRender();
    /// <summary>
    /// Gets the video sequence selection title value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video sequence selection title value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoSequenceSelectionTitle => GetVideoSequenceSelectionTitleForRender();
    /// <summary>
    /// Gets the frame polyline points value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame polyline points value exposed by <see cref="MediaStudio"/>.</value>
    private string FramePolylinePoints => GetFramePolylinePointsForRender();
    /// <summary>
    /// Gets the frame polygon points value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame polygon points value exposed by <see cref="MediaStudio"/>.</value>
    private string FramePolygonPoints => GetFramePolygonPointsForRender();
    /// <summary>
    /// Gets the frame dim path used by this media studio instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The frame dim path value exposed by <see cref="MediaStudio"/>.</value>
    private string FrameDimPath => GetFrameDimPathForRender();
    /// <summary>
    /// Gets the video preview style value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video preview style value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoPreviewStyle
    {
        get
        {
            var fit = _videoFitMode switch
            {
                PublicationVideoFitMode.Cover => "cover",
                PublicationVideoFitMode.Stretch => "fill",
                _ => "contain"
            };
            return $"object-fit:{fit};";
        }
    }
    /// <summary>
    /// Performs lt for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="text">Text value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string LT(string text) {
        try
        {
            return Localization.GetText(text);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(LT)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Gets the mouse mode label value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mouse mode label value exposed by <see cref="MediaStudio"/>.</value>
    private string MouseModeLabel => LT(_mouseMode switch
    {
        MediaStudioMouseMode.SelectSection => "Select time",
        MediaStudioMouseMode.PlacePlayhead => "Place playhead",
        MediaStudioMouseMode.AddCutLine => "Add cutline",
        MediaStudioMouseMode.FrameRegion => "Frame region",
        _ => "Select time"
    });
    /// <summary>
    /// Gets the video canvas mode help value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video canvas mode help value exposed by <see cref="MediaStudio"/>.</value>
    private string VideoCanvasModeHelp => LT(_mouseMode switch
    {
        MediaStudioMouseMode.PlacePlayhead => "Click or drag to scrub the selected clip",
        MediaStudioMouseMode.AddCutLine => "Click the selected clip where the cutline belongs",
        MediaStudioMouseMode.FrameRegion => "Click frame vertices; Enter or double-click applies",
        _ => "Click for one timestamp; drag for a selected range"
    });
    /// <summary>
    /// Gets the selected section summary value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected section summary value exposed by <see cref="MediaStudio"/>.</value>
    private string SelectedSectionSummary => GetSelectedSectionSummaryForRender();
    /// <summary>
    /// Gets the import menu text value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The import menu text value exposed by <see cref="MediaStudio"/>.</value>
    private string ImportMenuText => GetImportMenuTextForRender();
    /// <summary>
    /// Gets the accept value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The accept value exposed by <see cref="MediaStudio"/>.</value>
    private string Accept => GetAcceptForRender();
    /// <summary>
    /// Gets the MIME label value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME label value exposed by <see cref="MediaStudio"/>.</value>
    private string MimeLabel => GetMimeLabelForRender();
    /// <summary>
    /// Gets the source status value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source status value exposed by <see cref="MediaStudio"/>.</value>
    private string SourceStatus => GetSourceStatusForRender();
    /// <summary>
    /// Gets the apply button text value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The apply button text value exposed by <see cref="MediaStudio"/>.</value>
    private string ApplyButtonText => GetApplyButtonTextForRender();
    /// <summary>
    /// Gets the footer status value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The footer status value exposed by <see cref="MediaStudio"/>.</value>
    private string FooterStatus => GetFooterStatusForRender();
    /// <summary>
    /// Gets the display waveform collection maintained or exposed by this media studio instance for downstream processing.
    /// </summary>
    /// <value>The display waveform value exposed by <see cref="MediaStudio"/>.</value>
    private IEnumerable<double> DisplayWaveform => GetDisplayWaveformForRender();
    /// <summary>
    /// Gets the max fade value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max fade value exposed by <see cref="MediaStudio"/>.</value>
    private double MaxFade => GetMaxFadeForRender();
    /// <summary>
    /// Gets the range selector end value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The range selector end value exposed by <see cref="MediaStudio"/>.</value>
    private double RangeSelectorEnd => GetRangeSelectorEndForRender();
    /// <summary>
    /// Gets the range selector min range value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The range selector min range value exposed by <see cref="MediaStudio"/>.</value>
    private double RangeSelectorMinRange => GetRangeSelectorMinRangeForRender();
    /// <summary>
    /// Gets the major tick value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The major tick value exposed by <see cref="MediaStudio"/>.</value>
    private double MajorTick => GetMajorTickForRender();
    /// <summary>
    /// Gets the minor tick value that forms part of the media studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minor tick value exposed by <see cref="MediaStudio"/>.</value>
    private double MinorTick => GetMinorTickForRender();

    /// <summary>
    /// Handles the parameters set lifecycle or event notification for <see cref="MediaStudio"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task representing completion of the component lifecycle operation.</returns>
    protected override Task OnParametersSetAsync()
    {
        try
        {
            if (!Visible || SessionId == Guid.Empty || SessionId == _loadedSession) return base.OnParametersSetAsync();
            _loadedSession = SessionId;
            _recordingRecoveryAttemptedSession = Guid.Empty;
            RetirePreviewAssets();
            _previewAssetId = Guid.Empty;
            _previewSource = string.Empty;
            _error = null;
            _warning = null;
            _recording = false;
            _recordingStopping = false;
            _renderingVideoExport = false;
            _renderingTimelineSelectionExport = false;
            _recordingAdaptiveQuality = RecordingPolicy.AdaptiveQuality.Enabled;
            _recordingAdaptiveProfile = NormalizeAdaptiveProfile(RecordingPolicy.AdaptiveQuality.DefaultProfile);
            _recordingAdaptVideo = true;
            _recordingAdaptAudio = true;
            _recordingUseBrowserCapabilityProbe = RecordingPolicy.AdaptiveQuality.BrowserCapabilityProbeEnabled;
            _recordingPreserveNativeResolution = RecordingPolicy.AdaptiveQuality.PreserveNativeResolution;
            _recordingAllowFrameRateReduction = RecordingPolicy.AdaptiveQuality.AllowFrameRateReduction;
            _recordingAllowResolutionReduction = RecordingPolicy.AdaptiveQuality.AllowResolutionReduction;
            _recordingSizeMode = "source";
            try
            {
                _recordingViewportPresets = PreviewPresets.GetPresets()
                    .Where(preset => preset.Width >= RecordingPolicy.MinimumWidth
                        && preset.Width <= RecordingPolicy.MaximumWidth
                        && preset.Height >= RecordingPolicy.MinimumHeight
                        && preset.Height <= RecordingPolicy.MaximumHeight)
                    .ToList();
            }
            catch (Exception presetError)
            {
                _recordingViewportPresets = [];
                Logger.LogWarning(presetError, "Panel Studio viewport presets could not be reused by Media Studio; video standards and custom sizes remain available.");
            }
            _recordingCustomWidth = Math.Clamp(RecordingPolicy.MasterWidth, RecordingPolicy.MinimumWidth, RecordingPolicy.MaximumWidth);
            _recordingCustomHeight = Math.Clamp(RecordingPolicy.MasterHeight, RecordingPolicy.MinimumHeight, RecordingPolicy.MaximumHeight);
            _recordingFrameRate = RecordingPolicy.AdaptiveQuality.MaximumAutomaticFrameRate > 0
                ? Math.Clamp(RecordingPolicy.AdaptiveQuality.MaximumAutomaticFrameRate, RecordingPolicy.MinimumFrameRate, RecordingPolicy.MaximumFrameRate)
                : 0;
            _recordingVideoBitrateKbps = Math.Clamp(
                RecordingPolicy.BrowserRecordingVideoBitrateKbps > 0 ? RecordingPolicy.BrowserRecordingVideoBitrateKbps : RecordingPolicy.VideoBitrateKbps,
                RecordingPolicy.MinimumVideoBitrateKbps,
                RecordingPolicy.MaximumVideoBitrateKbps);
            _recordingAudioBitrateKbps = Math.Max(32, RecordingPolicy.BrowserRecordingAudioBitrateKbps > 0 ? RecordingPolicy.BrowserRecordingAudioBitrateKbps : RecordingPolicy.AudioBitrateKbps);
            _recordingCodecPreference = NormalizeRecordingCodecPreference(RecordingPolicy.BrowserRecordingCodecPreference);
            _saving = false;
            _embedding = false;
            _embeddingProgress = 0;
            _hasRetainedRecording = false;
            _retainedRecordingCommittedToSequence = false;
            _recordingStartPlacementRequired = false;
            _recordingStartPlacementBoundary = 0;
            _pendingRecordingSource = string.Empty;
            _plannedRecordingPlacementBoundary = null;
            _recordingPlacementRequired = false;
            _recordingPlacementBoundary = 0;
            _pendingRecordingInsertTimelineSeconds = null;
            _retainedRecordingSizeBytes = 0;
            _retainedRecordingFileName = string.Empty;
            _retainedRecordingObjectUrl = string.Empty;
            _segments.Clear();
            _selectedSegmentIds.Clear();
            _timelineSelectionAnchorId = null;
            _selectedSegmentId = null;
            _playhead = 0;
            _mouseMode = MediaStudioMouseMode.SelectSection;
            _frameClipPolygon.Clear();
            _morphClipPolygon.Clear();
            _frameDraft.Clear();
            _frameRegionEditing = false;
            _editingMorphRegion = false;
            _selectedFramePointIndex = null;
            _selectedVideoLayerId = null;
            _selectedVideoFilterId = null;
            _selectedCutSectionId = null;
            _newVideoFilterKind = VideoEffectFilterKind.ChromaKey;
            _videoSelectionIsPoint = false;
            _videoSelectionPointSeconds = 0;
            _videoSelectionStartSeconds = 0;
            _videoSelectionEndSeconds = 0;
            _videoFitMode = PublicationVideoFitMode.Stretch;
            _videoProject = null;
            _activeProjectTrackId = Guid.Empty;
            _videoProjectIssues = [];
            ClearPendingInsert();

            if (InitialMedia is { } media)
            {
                _volume = media.Volume;
                _playbackRate = media.PlaybackRate;
                _fadeIn = media.FadeInSeconds;
                _fadeOut = media.FadeOutSeconds;
                _muted = media.Muted;
                _loop = media.Loop;
                _autoPlay = media.AutoPlay;
                _playbackTrigger = media.PlaybackTrigger;
                IEnumerable<PublicationMediaSegment> sourceSegments;
                if (media is VideoElement projectVideo && projectVideo.VideoProject is { Tracks.Count: > 0 } project)
                {
                    _videoProject = TimelineEdits.CloneVideoProject(project);
                    _activeProjectTrackId = ResolveActiveProjectTrackId(_videoProject);
                    sourceSegments = TimelineEdits.CreateTrackProjection(_videoProject, _activeProjectTrackId);
                }
                else
                {
                    sourceSegments = media.Segments is { Count: > 0 }
                        ? media.Segments
                        : [new PublicationMediaSegment
                        {
                            Name = media.Name,
                            DataUrl = media.DataUrl,
                            MimeType = media.MimeType,
                            PosterDataUrl = media is VideoElement legacyVideo ? legacyVideo.PosterDataUrl : string.Empty,
                            DurationSeconds = media.DurationSeconds,
                            TrimStartSeconds = media.TrimStartSeconds,
                            TrimEndSeconds = media.EffectiveTrimEndSeconds,
                            WaveformSamples = [.. media.WaveformSamples]
                        }];
                }
                _segments.AddRange(TimelineEdits.Normalize(sourceSegments, IsVideo));
                if (media is VideoElement video)
                {
                    _videoFitMode = video.FitModeExplicit || video.FitMode != PublicationVideoFitMode.Contain
                        ? video.FitMode
                        : PublicationVideoFitMode.Stretch;
                    var legacyRegion = CloneFramePoints(video.FrameClipPolygon);
                    var firstVideoLayer = _segments.FirstOrDefault()?.VideoLayers.FirstOrDefault();
                    if (legacyRegion.Count >= 3 && firstVideoLayer is not null && firstVideoLayer.Region.IsFullFrame)
                    {
                        firstVideoLayer.Region.Name = "Legacy frame region";
                        firstVideoLayer.Region.Points = legacyRegion;
                    }
                }
            }
            else
            {
                _volume = 1;
                _playbackRate = 1;
                _fadeIn = _fadeOut = 0;
                _muted = _loop = false;
                _autoPlay = true;
                _playbackTrigger = PublicationMediaPlaybackTrigger.OnPageEnter;
            }

            if (InitialInsertedSegment is { } inserted)
            {
                var normalizedInsert = TimelineEdits.Normalize([inserted], IsVideo).FirstOrDefault();
                if (normalizedInsert is not null)
                {
                    normalizedInsert.Id = Guid.NewGuid();
                    _segments.Add(normalizedInsert);
                    _warning = $"Added {normalizedInsert.Name} to the {(IsVideo ? "video" : "audio")} sequence.";
                }
            }

            if (_segments.Count > 0)
            {
                var initial = _segments.FirstOrDefault(IsPlayableSegment)
                    ?? _segments.FirstOrDefault(segment => !segment.IsGap)
                    ?? _segments[0];
                SelectSegment(initial.Id, seek: false);
            }
            else ClearSelectedSegmentFields();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(OnParametersSetAsync)} failed.");
            throw;
        }
        return base.OnParametersSetAsync();
    }


    /// <summary>Localized title displayed in the Media Studio popup header.</summary>
    private string StudioTitle => IsVideo ? LT("Video Studio") : LT("Audio Studio");

    /// <summary>Localized activity description displayed in the Media Studio popup header.</summary>
    private string StudioSubtitle => EditingExisting ? LT("Edit the selected media object")
        : IsVideo ? LT("Import or record a video") : LT("Import, record, trim, and mix a simple audio clip");

    /// <summary>Preserves the original Save button enablement without handing editor state to the footer view.</summary>
    private bool CanApplyCurrentMedia => HasSource && !_recording && !_recordingStopping
        && !_recordingPlacementRequired && !_recordingStartPlacementRequired && !_renderingVideoExport
        && !_saving && !_embedding;
}
