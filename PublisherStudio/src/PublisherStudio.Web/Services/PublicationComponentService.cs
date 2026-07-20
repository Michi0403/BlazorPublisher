using System.Text.Json;
using System.Text.Json.Serialization;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

/// <summary>
/// Creates and normalizes browser-native DevExtreme publication components. The service
/// deliberately produces a plain JSON contract so editor preview, presentation export and
/// website export all execute the same runtime code.
/// </summary>
public sealed class PublicationComponentService
{
    private readonly PublicationDataService _data;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PublicationComponentService(PublicationDataService data) => _data = data;

    public DevExtremeComponentElement Clone(DevExtremeComponentElement source)
    {
        var json = JsonSerializer.Serialize(source, _json);
        return JsonSerializer.Deserialize<DevExtremeComponentElement>(json, _json) ?? new DevExtremeComponentElement();
    }

    public void CopyConfiguration(DevExtremeComponentElement source, DevExtremeComponentElement target, bool preservePlacement = true)
    {
        var id = target.Id;
        var x = target.X;
        var y = target.Y;
        var width = target.Width;
        var height = target.Height;
        var rotation = target.Rotation;
        var zIndex = target.ZIndex;
        var visible = target.Visible;
        var locked = target.Locked;
        var hidden = target.HiddenAtPresentationStart;
        var group = target.GroupId;
        var animations = target.Animations;
        var interaction = target.Interaction;
        var clone = Clone(source);
        foreach (var property in typeof(DevExtremeComponentElement).GetProperties().Where(property => property.CanRead && property.CanWrite))
            property.SetValue(target, property.GetValue(clone));
        target.Id = id;
        if (!preservePlacement) return;
        target.X = x;
        target.Y = y;
        target.Width = width;
        target.Height = height;
        target.Rotation = rotation;
        target.ZIndex = zIndex;
        target.Visible = visible;
        target.Locked = locked;
        target.HiddenAtPresentationStart = hidden;
        target.GroupId = group;
        target.Animations = animations;
        target.Interaction = interaction;
    }

    public DevExtremeComponentElement Create(PublicationDocument document, PublicationComponentKind kind)
    {
        _data.EnsureBuiltInObjects(document);
        var data = EnsureDataObject(document);
        var element = new DevExtremeComponentElement
        {
            Name = ComponentName(kind),
            Title = ComponentName(kind),
            ComponentKind = kind,
            Connection = new PublicationComponentConnection
            {
                Mode = PublicationComponentDataMode.PublicationDataObject,
                DataObjectId = data.Id
            },
            Width = DefaultSize(kind).Width,
            Height = DefaultSize(kind).Height,
            ShowTitle = kind is PublicationComponentKind.DataGrid or PublicationComponentKind.TreeList or PublicationComponentKind.Scheduler
                or PublicationComponentKind.Form or PublicationComponentKind.Gallery or PublicationComponentKind.TileView
                or PublicationComponentKind.PivotGrid or PublicationComponentKind.Splitter or PublicationComponentKind.TabPanel
                or PublicationComponentKind.MultiView or PublicationComponentKind.ScrollView or PublicationComponentKind.Map or PublicationComponentKind.VectorMap,
            ShowFilterRow = kind is PublicationComponentKind.DataGrid or PublicationComponentKind.TreeList,
            ShowSearchPanel = kind is PublicationComponentKind.DataGrid or PublicationComponentKind.TreeList or PublicationComponentKind.PivotGrid,
            AllowPaging = kind is PublicationComponentKind.DataGrid or PublicationComponentKind.TreeList,
            EditMode = kind is PublicationComponentKind.Form ? PublicationComponentEditMode.Form : PublicationComponentEditMode.ReadOnly,
            SelectionMode = kind is PublicationComponentKind.DataGrid or PublicationComponentKind.TreeList or PublicationComponentKind.TileView
                ? PublicationComponentSelectionMode.Single
                : PublicationComponentSelectionMode.None
        };
        if (kind is PublicationComponentKind.Menu or PublicationComponentKind.ContextMenu)
        {
            element.MenuSourceMode = PublicationMenuSourceMode.ManualItems;
            element.MenuItems.Add(new PublicationMenuItem { Text = "Menu item" });
        }
        if (kind == PublicationComponentKind.VectorMap)
        {
            element.MapCenterLatitude = 20;
            element.MapCenterLongitude = 0;
            element.MapZoom = 1;
        }
        ApplyFieldsFromDataObject(document, element, replace: true);
        ApplyKindDefaults(document, element);
        Normalize(document, element);
        return element;
    }

    public void Normalize(PublicationDocument document, DevExtremeComponentElement item)
    {
        _data.EnsureBuiltInObjects(document);
        item.Connection ??= new PublicationComponentConnection();
        item.Connection.Headers ??= [];
        item.Fields ??= [];
        item.Actions ??= [];
        item.Panels ??= [];
        item.MenuItems ??= [];
        item.VectorFeatures ??= [];
        item.Title = string.IsNullOrWhiteSpace(item.Title) ? ComponentName(item.ComponentKind) : item.Title.Trim();
        item.Name = string.IsNullOrWhiteSpace(item.Name) ? item.Title : item.Name.Trim();
        item.PageSize = Math.Clamp(item.PageSize <= 0 ? 20 : item.PageSize, 1, 1000);
        item.ColumnCount = Math.Clamp(item.ColumnCount <= 0 ? 1 : item.ColumnCount, 1, 12);
        item.BorderWidthMm = Math.Clamp(item.BorderWidthMm, 0, 8);
        item.ContentOffsetX = Math.Clamp(item.ContentOffsetX, -500, 500);
        item.ContentOffsetY = Math.Clamp(item.ContentOffsetY, -500, 500);
        item.ContentScale = Math.Clamp(item.ContentScale <= 0 ? 1 : item.ContentScale, .1, 12);
        item.MapCenterLatitude = Math.Clamp(item.MapCenterLatitude, -90, 90);
        item.MapCenterLongitude = Math.Clamp(item.MapCenterLongitude, -180, 180);
        item.MapZoom = Math.Clamp(item.MapZoom <= 0 ? 1 : item.MapZoom, 1, 20);
        item.MapProvider = string.IsNullOrWhiteSpace(item.MapProvider) ? "google" : item.MapProvider.Trim();
        item.MapType = string.IsNullOrWhiteSpace(item.MapType) ? "roadmap" : item.MapType.Trim();
        item.VectorProjection = string.IsNullOrWhiteSpace(item.VectorProjection) ? "mercator" : item.VectorProjection.Trim();
        item.CustomCssClass = SanitizeCssClass(item.CustomCssClass);
        item.CustomCss = SanitizeInlineCss(item.CustomCss);
        foreach (var feature in item.VectorFeatures)
        {
            if (feature.Id == Guid.Empty) feature.Id = Guid.NewGuid();
            feature.Name = string.IsNullOrWhiteSpace(feature.Name) ? "Feature" : feature.Name.Trim();
            feature.Points ??= [];
            feature.Points = feature.Points.Where(point => double.IsFinite(point.Longitude) && double.IsFinite(point.Latitude)).Select(point => new PublicationMapPoint
            {
                Longitude = Math.Clamp(point.Longitude, -180, 180),
                Latitude = Math.Clamp(point.Latitude, -90, 90)
            }).ToList();
            feature.Opacity = Math.Clamp(feature.Opacity, 0, 1);
            feature.Width = Math.Clamp(feature.Width <= 0 ? 1 : feature.Width, .25, 40);
            feature.Size = Math.Clamp(feature.Size <= 0 ? 10 : feature.Size, 2, 100);
        }
        item.Width = Math.Max(24, item.Width);
        item.Height = Math.Max(12, item.Height);
        item.AdvancedOptionsJson = NormalizeJsonObject(item.AdvancedOptionsJson);
        item.Connection.ODataVersion = item.Connection.ODataVersion is 2 or 3 or 4 ? item.Connection.ODataVersion : 4;
        item.Connection.KeyField = string.IsNullOrWhiteSpace(item.Connection.KeyField) ? item.KeyField : item.Connection.KeyField.Trim();
        item.Connection.KeyType = NormalizeODataKeyType(item.Connection.KeyType);
        item.Connection.Url ??= string.Empty;
        item.Connection.InsertUrl ??= string.Empty;
        item.Connection.UpdateUrl ??= string.Empty;
        item.Connection.DeleteUrl ??= string.Empty;
        item.Connection.JsonPath ??= string.Empty;
        item.Connection.LoadBody ??= string.Empty;
        foreach (var header in item.Connection.Headers)
        {
            header.Name ??= string.Empty;
            header.Value ??= string.Empty;
        }

        var dataObject = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.Connection.DataObjectId);
        if (item.Connection.Mode is PublicationComponentDataMode.PublicationDataObject or PublicationComponentDataMode.StaticSnapshot)
        {
            if (dataObject is null && document.DataObjects.Count > 0)
            {
                item.Connection.DataObjectId = document.DataObjects[0].Id;
                dataObject = document.DataObjects[0];
            }
            if (item.Fields.Count == 0 && dataObject is not null)
                ApplyFieldsFromDataObject(document, item, replace: true);
        }

        var seenFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        item.Fields = item.Fields
            .Where(field => !string.IsNullOrWhiteSpace(field.DataField))
            .Where(field => seenFields.Add(field.DataField.Trim()))
            .Select(field =>
            {
                field.DataField = field.DataField.Trim();
                field.Caption = string.IsNullOrWhiteSpace(field.Caption) ? Friendly(field.DataField) : field.Caption.Trim();
                field.Width = Math.Clamp(field.Width, 0, 2000);
                field.Format ??= string.Empty;
                field.LookupDataField ??= string.Empty;
                field.LookupDisplayField ??= string.Empty;
                if (field.Id == Guid.Empty) field.Id = Guid.NewGuid();
                return field;
            }).ToList();

        var menuIds = new HashSet<Guid>();
        foreach (var menuItem in item.MenuItems)
        {
            if (menuItem.Id == Guid.Empty || !menuIds.Add(menuItem.Id))
            {
                menuItem.Id = Guid.NewGuid();
                menuIds.Add(menuItem.Id);
            }
            menuItem.Text = string.IsNullOrWhiteSpace(menuItem.Text) ? "Menu item" : menuItem.Text.Trim();
            menuItem.Url ??= string.Empty;
            menuItem.IconCssClass = SanitizeCssClass(menuItem.IconCssClass);
            if (menuItem.ParentId == menuItem.Id || (menuItem.ParentId is { } parentId && !item.MenuItems.Any(candidate => candidate.Id == parentId))) menuItem.ParentId = null;
            if (menuItem.TargetPageId is { } targetPageId && document.Pages.All(page => page.Id != targetPageId)) menuItem.TargetPageId = null;
            if (menuItem.Destination == PublicationMenuDestinationKind.Page && menuItem.TargetPageId is null) menuItem.Destination = PublicationMenuDestinationKind.None;
            if (menuItem.Destination == PublicationMenuDestinationKind.ExternalUrl && string.IsNullOrWhiteSpace(menuItem.Url)) menuItem.Destination = PublicationMenuDestinationKind.None;
        }

        foreach (var action in item.Actions)
        {
            if (action.Id == Guid.Empty) action.Id = Guid.NewGuid();
            action.Url ??= string.Empty;
            action.MailTo ??= string.Empty;
            action.MailSubject ??= string.Empty;
            action.MailBody ??= string.Empty;
            action.ConfirmationText ??= string.Empty;
            action.SourceField ??= string.Empty;
            action.TargetField ??= string.Empty;
            action.ValueTemplate ??= "{{value}}";
            action.Script ??= string.Empty;
            if (action.TargetPageId is { } pageId && document.Pages.All(page => page.Id != pageId)) action.TargetPageId = null;
            if (action.TargetElementId is { } elementId)
            {
                var target = document.Pages.SelectMany(page => page.Elements).FirstOrDefault(element => element.Id == elementId);
                if (target is null)
                {
                    action.TargetElementId = null;
                }
                else if (target is DevExtremeComponentElement targetComponent && targetComponent.SharedComponentId is { } sharedTargetId)
                {
                    action.TargetSharedComponentId = sharedTargetId;
                }
            }
            if (action.TargetSharedComponentId is { } targetSharedId
                && document.Pages.SelectMany(page => page.Elements).OfType<DevExtremeComponentElement>()
                    .All(component => component.SharedComponentId != targetSharedId))
            {
                action.TargetSharedComponentId = null;
            }
            if (!item.AllowCustomScript && action.Action == PublicationComponentActionKind.CustomScript) action.Action = PublicationComponentActionKind.None;
        }

        foreach (var panel in item.Panels)
        {
            if (panel.Id == Guid.Empty) panel.Id = Guid.NewGuid();
            panel.Title = string.IsNullOrWhiteSpace(panel.Title) ? "Panel" : panel.Title.Trim();
            panel.Size ??= string.Empty;
            panel.MinSize ??= string.Empty;
            panel.MaxSize ??= string.Empty;
            panel.ContentHtml = PublicationFileService.SanitizePreviewHtml(panel.ContentHtml ?? string.Empty);
            panel.Fields ??= [];
            if (panel.DataObjectId == Guid.Empty && dataObject is not null) panel.DataObjectId = dataObject.Id;
        }

        ApplyKindDefaults(document, item, onlyMissing: true);
        if (item.Scope == PublicationComponentScope.Document)
            item.SharedComponentId ??= Guid.NewGuid();
        else
            item.SharedComponentId = null;
    }

    public void ApplyFieldsFromDataObject(PublicationDocument document, DevExtremeComponentElement item, bool replace)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.Connection.DataObjectId);
        var columns = _data.ResolveColumns(data);
        if (!replace && item.Fields.Count > 0) return;
        item.Fields = columns.Select((column, index) => new PublicationComponentField
        {
            DataField = column.Name,
            Caption = Friendly(column.Name),
            ValueKind = column.ValueKind,
            Editor = EditorFor(column.ValueKind),
            Area = index == 0 ? PublicationComponentFieldArea.Row : column.ValueKind == PublicationDataValueKind.Number
                ? PublicationComponentFieldArea.Data
                : PublicationComponentFieldArea.Column,
            Editable = true,
            Visible = true
        }).ToList();
        ApplyMappingsFromFields(item);
    }

    public object BuildClientConfiguration(PublicationDocument document, DevExtremeComponentElement item, Guid currentPageId, bool designerMode = false)
    {
        Normalize(document, item);
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == item.Connection.DataObjectId);
        var columns = data is null ? Array.Empty<PublicationDataColumn>() : _data.ResolveColumns(data).ToArray();
        var rows = data is null ? Array.Empty<Dictionary<string, object?>>() : _data.ResolveRows(document, data, currentPageId)
            .Select(row => row.Values.ToDictionary(pair => pair.Key, pair => (object?)ConvertValue(pair.Value, columns.FirstOrDefault(column => string.Equals(column.Name, pair.Key, StringComparison.OrdinalIgnoreCase))?.ValueKind), StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var live = BuildLiveData(document, data);
        var panelConfigs = item.Panels.Select(panel => BuildPanelConfiguration(document, panel, currentPageId)).ToArray();
        return new
        {
            id = item.Id,
            designerMode,
            sharedComponentId = item.SharedComponentId,
            kind = item.ComponentKind.ToString(),
            scope = item.Scope.ToString(),
            item.Title,
            item.ShowTitle,
            item.ShowBorders,
            item.ShowFilterRow,
            item.ShowHeaderFilter,
            item.ShowSearchPanel,
            item.ShowGroupPanel,
            item.ShowColumnChooser,
            item.AllowSorting,
            item.AllowFiltering,
            item.AllowPaging,
            item.AllowReordering,
            item.AllowResizing,
            item.WordWrap,
            item.AutoExpandAll,
            item.PageSize,
            editMode = item.EditMode.ToString(),
            selectionMode = item.SelectionMode.ToString(),
            item.KeyField,
            item.ParentField,
            item.TextField,
            item.ValueField,
            item.DisplayField,
            item.ImageField,
            item.StartDateField,
            item.EndDateField,
            item.AllDayField,
            item.TargetPageField,
            item.UrlField,
            item.CurrentView,
            item.Orientation,
            menuSourceMode = item.MenuSourceMode.ToString(),
            menuItems = BuildMenuItems(item),
            item.ColumnCount,
            item.ButtonText,
            item.Placeholder,
            item.InitialValue,
            item.Background,
            item.CustomCssClass,
            item.CustomCss,
            item.MapProvider,
            item.MapType,
            item.MapApiKey,
            item.MapCenterLatitude,
            item.MapCenterLongitude,
            item.MapZoom,
            item.MapControls,
            item.MapAutoAdjust,
            item.MapShowRoutes,
            item.LatitudeField,
            item.LongitudeField,
            item.AddressField,
            item.MarkerTooltipField,
            item.MapRouteField,
            item.MapOrderField,
            vectorBaseLayer = item.VectorBaseLayer.ToString(),
            item.VectorProjection,
            item.VectorShowLabels,
            item.VectorLabelField,
            item.VectorValueField,
            item.VectorColorField,
            vectorFeatures = item.VectorFeatures.Select(feature => new
            {
                feature.Id, feature.Name, kind = feature.Kind.ToString(), feature.Points, feature.Color, feature.BorderColor,
                feature.Opacity, feature.Width, feature.Size, feature.Label, feature.Value
            }).ToArray(),
            item.AdvancedOptionsJson,
            item.AllowCustomScript,
            fields = BuildFields(document, item.Fields, currentPageId),
            actions = BuildActions(document, item, currentPageId),
            panels = panelConfigs,
            rows,
            columnKinds = columns.ToDictionary(column => column.Name, column => column.ValueKind.ToString(), StringComparer.OrdinalIgnoreCase),
            connection = BuildConnection(item.Connection,
                item.Connection.Mode == PublicationComponentDataMode.PublicationDataObject ? live : null),
            pages = document.Pages.Select(page => new { id = page.Id, page.Name }).ToArray(),
            elements = document.Pages.SelectMany(page => page.Elements).Select(element => new { id = element.Id, element.Name, pageId = document.Pages.First(page => page.Elements.Contains(element)).Id }).ToArray()
        };
    }

    public string BuildClientConfigurationBase64(PublicationDocument document, DevExtremeComponentElement item, Guid currentPageId, bool designerMode = false) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(BuildClientConfiguration(document, item, currentPageId, designerMode), _json));


    private static object[] BuildMenuItems(DevExtremeComponentElement item)
        => item.MenuItems.Where(menuItem => menuItem.Visible).Select(menuItem => (object)new
        {
            id = menuItem.Id,
            parentId = menuItem.ParentId,
            text = menuItem.Text,
            destination = menuItem.Destination.ToString(),
            targetPageId = menuItem.Destination == PublicationMenuDestinationKind.Page ? menuItem.TargetPageId : null,
            url = menuItem.Destination == PublicationMenuDestinationKind.ExternalUrl ? menuItem.Url : string.Empty,
            openInNewWindow = menuItem.OpenInNewWindow,
            enabled = menuItem.Enabled,
            disabled = !menuItem.Enabled,
            visible = menuItem.Visible,
            icon = menuItem.IconCssClass
        }).ToArray();

    private object[] BuildFields(PublicationDocument document, IEnumerable<PublicationComponentField> fields, Guid currentPageId)
    {
        return fields.Select(field =>
        {
            object? lookup = null;
            if (field.LookupDataObjectId is { } lookupId)
            {
                var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == lookupId);
                if (data is not null)
                {
                    var rows = _data.ResolveRows(document, data, currentPageId)
                        .Select(row => row.Values.ToDictionary(pair => pair.Key, pair => (object?)ConvertValue(pair.Value,
                            _data.ResolveColumns(data).FirstOrDefault(column => string.Equals(column.Name, pair.Key, StringComparison.OrdinalIgnoreCase))?.ValueKind),
                            StringComparer.OrdinalIgnoreCase))
                        .ToArray();
                    var columns = _data.ResolveColumns(data);
                    var valueExpr = string.IsNullOrWhiteSpace(field.LookupDataField)
                        ? columns.FirstOrDefault()?.Name ?? field.DataField
                        : field.LookupDataField;
                    var displayExpr = string.IsNullOrWhiteSpace(field.LookupDisplayField)
                        ? columns.Skip(1).FirstOrDefault()?.Name ?? valueExpr
                        : field.LookupDisplayField;
                    lookup = new { rows, valueExpr, displayExpr };
                }
            }
            return (object)new
            {
                field.Id,
                field.DataField,
                field.Caption,
                valueKind = field.ValueKind.ToString(),
                editor = field.Editor.ToString(),
                field.Visible,
                field.Editable,
                field.Required,
                field.Width,
                field.Format,
                area = field.Area.ToString(),
                summaryType = field.SummaryType.ToString(),
                field.LookupDataField,
                field.LookupDisplayField,
                field.LookupDataObjectId,
                lookup
            };
        }).ToArray();
    }

    private static object[] BuildActions(PublicationDocument document, DevExtremeComponentElement item, Guid currentPageId)
    {
        var currentPage = document.Pages.FirstOrDefault(page => page.Id == currentPageId);
        return item.Actions.Select(action =>
        {
            var targetElementId = action.TargetElementId;
            if (action.TargetSharedComponentId is { } sharedTargetId && currentPage is not null)
            {
                targetElementId = currentPage.Elements.OfType<DevExtremeComponentElement>()
                    .FirstOrDefault(component => component.SharedComponentId == sharedTargetId)?.Id
                    ?? targetElementId;
            }
            return (object)new
            {
                action.Id,
                trigger = action.Trigger.ToString(),
                action = action.Action.ToString(),
                action.TargetPageId,
                TargetElementId = targetElementId,
                action.TargetSharedComponentId,
                action.Url,
                action.OpenInNewWindow,
                action.MailTo,
                action.MailSubject,
                action.MailBody,
                action.ConfirmationText,
                action.SourceField,
                action.TargetField,
                action.ValueTemplate,
                action.Script
            };
        }).ToArray();
    }

    private object BuildPanelConfiguration(PublicationDocument document, PublicationComponentPanel panel, Guid currentPageId)
    {
        var data = document.DataObjects.FirstOrDefault(candidate => candidate.Id == panel.DataObjectId);
        var rows = data is null ? Array.Empty<Dictionary<string, object?>>() : _data.ResolveRows(document, data, currentPageId)
            .Select(row => row.Values.ToDictionary(pair => pair.Key, pair => (object?)ConvertValue(pair.Value,
                _data.ResolveColumns(data).FirstOrDefault(column => string.Equals(column.Name, pair.Key, StringComparison.OrdinalIgnoreCase))?.ValueKind),
                StringComparer.OrdinalIgnoreCase)).ToArray();
        var fields = panel.Fields.Count > 0
            ? panel.Fields
            : data is null
                ? new List<PublicationComponentField>()
                : _data.ResolveColumns(data).Select(column => new PublicationComponentField
                {
                    DataField = column.Name,
                    Caption = Friendly(column.Name),
                    ValueKind = column.ValueKind,
                    Editor = EditorFor(column.ValueKind)
                }).ToList();
        return new
        {
            id = panel.Id,
            panel.Title,
            panel.Size,
            panel.MinSize,
            panel.MaxSize,
            panel.Collapsible,
            panel.Collapsed,
            childKind = panel.ChildKind.ToString(),
            panel.ContentHtml,
            fields = BuildFields(document, fields, currentPageId),
            rows,
            live = BuildLiveData(document, data)
        };
    }

    private object BuildConnection(PublicationComponentConnection connection, object? dataObjectLive) => new
    {
        mode = connection.Mode.ToString(),
        processingMode = connection.ProcessingMode.ToString(),
        connection.Url,
        loadMethod = connection.LoadMethod.ToString(),
        connection.LoadBody,
        connection.JsonPath,
        connection.KeyField,
        connection.KeyType,
        connection.ODataVersion,
        connection.WithCredentials,
        connection.AllowLoad,
        connection.AllowInsert,
        connection.AllowUpdate,
        connection.AllowDelete,
        connection.InsertUrl,
        connection.UpdateUrl,
        connection.DeleteUrl,
        insertMethod = connection.InsertMethod.ToString(),
        updateMethod = connection.UpdateMethod.ToString(),
        deleteMethod = connection.DeleteMethod.ToString(),
        connection.AppendKeyToWriteUrl,
        connection.Headers,
        dataObjectLive
    };

    private object? BuildLiveData(PublicationDocument document, PublicationDataObject? data)
    {
        if (data?.SourceKind != PublicationDataSourceKind.Web) return null;
        return new
        {
            enabled = data.Web.Enabled,
            transport = data.Web.Transport.ToString(),
            method = data.Web.Method.ToString(),
            url = data.Web.AllowExportedHtmlFetch ? data.Web.Url : string.Empty,
            headers = data.Web.AllowExportedHtmlFetch
    ? data.Web.Headers
    : new List<PublicationWebHeader>(),
            body = data.Web.AllowExportedHtmlFetch ? data.Web.RequestBody : string.Empty,
            responseFormat = data.Web.ResponseFormat.ToString(),
            jsonPath = data.Web.JsonPath,
            delimiter = data.Web.Delimiter,
            firstRowContainsHeaders = data.Web.FirstRowContainsHeaders,
            refreshIntervalSeconds = data.Web.RefreshIntervalSeconds,
            allowExportedHtmlFetch = data.Web.AllowExportedHtmlFetch,
            useSnapshotOnFailure = data.Web.UseSnapshotOnFailure,
            monolithRowsUrl = data.Web.AllowExportedHtmlFetch
                ? $"/api/publisher/exports/{document.Id}/data/{data.Id}/{data.Web.ExportAccessToken}/rows"
                : string.Empty
        };
    }

    private PublicationDataObject EnsureDataObject(PublicationDocument document)
    {
        _data.EnsureBuiltInObjects(document);
        var existing = document.DataObjects.FirstOrDefault(data => data.SourceKind is not PublicationDataSourceKind.PublicationPages
            and not PublicationDataSourceKind.PublicationDocument and not PublicationDataSourceKind.DocumentObjects);
        if (existing is not null) return existing;
        var data = _data.CreateSample();
        document.DataObjects.Add(data);
        return data;
    }

    private static object? ConvertValue(string value, PublicationDataValueKind? kind)
    {
        if (kind == PublicationDataValueKind.Number && double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var number)) return number;
        if (kind == PublicationDataValueKind.Boolean && bool.TryParse(value, out var boolean)) return boolean;
        if (kind == PublicationDataValueKind.DateTime && DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var date)) return date;
        return value;
    }

    private static void ApplyMappingsFromFields(DevExtremeComponentElement item)
    {
        var fields = item.Fields;
        if (fields.Count == 0) return;
        item.KeyField = Find(fields, "id", "key") ?? fields[0].DataField;
        item.ParentField = Find(fields, "parentid", "parent") ?? item.ParentField;
        item.TextField = Find(fields, "text", "title", "name", "subject") ?? fields[0].DataField;
        item.DisplayField = item.TextField;
        item.ValueField = Find(fields, "value", "amount", "total", "id") ?? fields[0].DataField;
        item.ImageField = Find(fields, "image", "imageurl", "photo", "url") ?? item.ImageField;
        item.StartDateField = Find(fields, "startdate", "start", "from") ?? item.StartDateField;
        item.EndDateField = Find(fields, "enddate", "end", "to") ?? item.EndDateField;
        item.AllDayField = Find(fields, "allday", "isfullday") ?? item.AllDayField;
        item.TargetPageField = Find(fields, "targetpageid", "pageid", "page") ?? item.TargetPageField;
        item.UrlField = Find(fields, "url", "link", "href") ?? item.UrlField;
        item.LatitudeField = Find(fields, "latitude", "lat") ?? item.LatitudeField;
        item.LongitudeField = Find(fields, "longitude", "lng", "lon", "long") ?? item.LongitudeField;
        item.AddressField = Find(fields, "address", "location", "place") ?? item.AddressField;
        item.MarkerTooltipField = Find(fields, "tooltip", "label", "text", "title", "name") ?? item.MarkerTooltipField;
        item.MapRouteField = Find(fields, "routeid", "route", "group") ?? item.MapRouteField;
        item.MapOrderField = Find(fields, "order", "sequence", "index", "position") ?? item.MapOrderField;
        item.VectorLabelField = Find(fields, "label", "name", "text", "title") ?? item.VectorLabelField;
        item.VectorValueField = Find(fields, "value", "amount", "total", "population") ?? item.VectorValueField;
        item.VectorColorField = Find(fields, "color", "colour", "fill") ?? item.VectorColorField;
        item.Connection.KeyField = item.KeyField;
    }

    private static string? Find(IEnumerable<PublicationComponentField> fields, params string[] names)
    {
        foreach (var field in fields)
        {
            var normalized = new string(field.DataField.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (names.Any(name => normalized == name)) return field.DataField;
        }
        return null;
    }

    private static void ApplyKindDefaults(PublicationDocument document, DevExtremeComponentElement item, bool onlyMissing = false)
    {
        var dataId = item.Connection.DataObjectId;
        if (item.IsLayoutContainer && item.Panels.Count == 0)
        {
            item.Panels =
            [
                new PublicationComponentPanel { Title = "Data", Size = "55%", ChildKind = PublicationComponentKind.DataGrid, DataObjectId = dataId },
                new PublicationComponentPanel { Title = "Details", ChildKind = PublicationComponentKind.Form, DataObjectId = dataId }
            ];
        }
        if (item.ComponentKind == PublicationComponentKind.Button && item.Actions.Count == 0)
            item.Actions.Add(new PublicationComponentAction { Trigger = PublicationComponentActionTrigger.Click, Action = PublicationComponentActionKind.NextPage });
        if (item.ComponentKind == PublicationComponentKind.Form
            && item.Actions.All(action => action.Trigger != PublicationComponentActionTrigger.Submit)
            && item.Connection.Mode is PublicationComponentDataMode.Rest or PublicationComponentDataMode.OData
            && (item.Connection.AllowInsert || item.Connection.AllowUpdate || !string.IsNullOrWhiteSpace(item.Connection.InsertUrl)))
        {
            item.Actions.Add(new PublicationComponentAction
            {
                Trigger = PublicationComponentActionTrigger.Submit,
                Action = PublicationComponentActionKind.SubmitRest
            });
        }
        if ((item.ComponentKind is PublicationComponentKind.Menu or PublicationComponentKind.ContextMenu)
            && item.Actions.All(action => action.Trigger != PublicationComponentActionTrigger.ItemClick || action.Action == PublicationComponentActionKind.None))
            item.Actions.Add(new PublicationComponentAction { Trigger = PublicationComponentActionTrigger.ItemClick, Action = PublicationComponentActionKind.Navigate });
        if (item.ComponentKind == PublicationComponentKind.Scheduler)
        {
            item.EditMode = item.EditMode == PublicationComponentEditMode.ReadOnly ? PublicationComponentEditMode.Form : item.EditMode;
            item.CurrentView = string.IsNullOrWhiteSpace(item.CurrentView) ? "week" : item.CurrentView;
        }
        if (item.ComponentKind == PublicationComponentKind.PivotGrid)
        {
            var visible = item.Fields.Where(field => field.Visible).ToList();
            if (visible.Count > 0 && visible.All(field => field.Area == PublicationComponentFieldArea.None))
            {
                visible[0].Area = PublicationComponentFieldArea.Row;
                if (visible.Count > 1) visible[1].Area = visible[1].ValueKind == PublicationDataValueKind.Number ? PublicationComponentFieldArea.Data : PublicationComponentFieldArea.Column;
                foreach (var field in visible.Skip(2).Where(field => field.ValueKind == PublicationDataValueKind.Number)) field.Area = PublicationComponentFieldArea.Data;
            }
        }
    }

    private static PublicationComponentEditorKind EditorFor(PublicationDataValueKind kind) => kind switch
    {
        PublicationDataValueKind.Number => PublicationComponentEditorKind.NumberBox,
        PublicationDataValueKind.Boolean => PublicationComponentEditorKind.CheckBox,
        PublicationDataValueKind.DateTime => PublicationComponentEditorKind.DateBox,
        _ => PublicationComponentEditorKind.TextBox
    };

    private static (double Width, double Height) DefaultSize(PublicationComponentKind kind) => kind switch
    {
        PublicationComponentKind.Button => (42, 14),
        PublicationComponentKind.CheckBox => (48, 14),
        PublicationComponentKind.TextBox or PublicationComponentKind.NumberBox or PublicationComponentKind.DateBox or PublicationComponentKind.SelectBox or PublicationComponentKind.TagBox => (85, 18),
        PublicationComponentKind.TextArea => (95, 38),
        PublicationComponentKind.Menu => (150, 18),
        PublicationComponentKind.Gallery or PublicationComponentKind.TileView => (145, 82),
        PublicationComponentKind.Form => (130, 90),
        PublicationComponentKind.Scheduler or PublicationComponentKind.PivotGrid => (175, 110),
        PublicationComponentKind.Map or PublicationComponentKind.VectorMap => (180, 115),
        _ => (160, 95)
    };

    public static string ComponentName(PublicationComponentKind kind) => kind switch
    {
        PublicationComponentKind.DataGrid => "Data Grid",
        PublicationComponentKind.TreeList => "Tree List",
        PublicationComponentKind.Scheduler => "Scheduler",
        PublicationComponentKind.TextBox => "Text Box",
        PublicationComponentKind.TextArea => "Text Area",
        PublicationComponentKind.NumberBox => "Number Box",
        PublicationComponentKind.DateBox => "Date Box",
        PublicationComponentKind.CheckBox => "Check Box",
        PublicationComponentKind.SelectBox => "Select Box",
        PublicationComponentKind.TagBox => "Tag Box",
        PublicationComponentKind.TileView => "Tile View",
        PublicationComponentKind.ContextMenu => "Context Menu",
        PublicationComponentKind.TabPanel => "Tab Panel",
        PublicationComponentKind.MultiView => "Multi View",
        PublicationComponentKind.ScrollView => "Scroll View",
        PublicationComponentKind.PivotGrid => "Pivot Grid",
        PublicationComponentKind.Map => "Map",
        PublicationComponentKind.VectorMap => "Vector Map",
        _ => Friendly(kind.ToString())
    };

    public static string Friendly(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var result = System.Text.RegularExpressions.Regex.Replace(value.Replace('_', ' '), "([a-z0-9])([A-Z])", "$1 $2");
        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    private static string SanitizeCssClass(string? value) => string.Join(' ', (value ?? string.Empty)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(token => token.All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
        .Take(8));

    private static string SanitizeInlineCss(string? value)
    {
        var source = (value ?? string.Empty).Replace("{", string.Empty).Replace("}", string.Empty);
        var blocked = new[] { "javascript:", "expression(", "@import", "</style", "behavior:", "-moz-binding" };
        if (blocked.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase))) return string.Empty;
        return string.Join(';', source.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(declaration => declaration.Contains(':'))
            .Take(64));
    }

    private static string NormalizeJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.GetRawText() : "{}";
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    private static string NormalizeODataKeyType(string? value)
    {
        var normalized = value?.Trim();
        return normalized is "String" or "Int32" or "Int64" or "Guid" ? normalized : "Int32";
    }
}
