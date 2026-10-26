using Microsoft.AspNetCore.Components;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Components.OrganicPlugins;

/// <summary>Reusable DevExpress Council submission form with its own 1-Wire service execution and notification boundary.</summary>
public partial class OrganicCouncilRequestForm
{
    /// <summary>Shared Council request populated by this editor.</summary>
    /// <value>Request supplied by the hosting route or other consumer.</value>
    [Parameter, EditorRequired]
    public OrganicCouncilPromptRequest Request { get; set; } = default!;

    /// <summary>Available advertised Council team choices.</summary>
    /// <value>Team identifiers and human labels from negotiated capabilities.</value>
    [Parameter]
    public IReadOnlyList<(string Key, string Name)> Teams { get; set; } = [];

    /// <summary>Whether the connected peer allows submitting Council work.</summary>
    /// <value>True only when the parent has confirmed the current connection capability.</value>
    [Parameter]
    public bool CanRunCouncil { get; set; }

    /// <summary>Explicit preferred full model identifiers, if selected by the user.</summary>
    /// <value>Configured provider/model identifiers.</value>
    [Parameter]
    public IReadOnlyList<string> ConfiguredModelNames { get; set; } = [];

    /// <summary>Notifies the parent after successful submission to reset its editable request.</summary>
    /// <value>Completed Council team key.</value>
    [Parameter]
    public EventCallback<string> OnSubmitted { get; set; }

    /// <summary>The DataAnnotations-validated form BusinessObject, independent of the transport DTO.</summary>
    private OrganicCouncilRequestFormModel Draft = new();

    /// <summary>The transport request currently bound to this form.</summary>
    private OrganicCouncilPromptRequest? BoundRequest;

    /// <summary>Prevents overlapping remote submissions from the same form.</summary>
    private bool Submitting;

    /// <summary>Refreshes draft inputs only when the parent supplies a different 1-Wire request.</summary>
    protected override Task OnParametersSetAsync()
    {
        try
        {
            if (ReferenceEquals(BoundRequest, Request)) return base.OnParametersSetAsync();
            BoundRequest = Request;
            Draft = new OrganicCouncilRequestFormModel();
            Draft.ReadFrom(Request);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Initializing the Council form BusinessObject failed.");
            throw;
        }
        return base.OnParametersSetAsync();
    }

    /// <summary>Gets the stable selectable team identifiers.</summary>
    /// <value>Available Council team keys.</value>
    private IReadOnlyList<string> TeamKeys => Teams.Select(team => team.Key).ToArray();

    /// <summary>Runs the user-authorized Council request through the existing 1-Wire service.</summary>
    /// <returns>A task completing after response and UI notification.</returns>
    private async Task SubmitAsync()
    {
        if (Submitting || !CanRunCouncil) return;
        try
        {
            Submitting = true;
            Draft.ApplyTo(Request);
            Request.RequestedOrganicCapabilities = Request.TeamKey switch
            {
                "openscad-team" => ["publisher.business-context", "publisher.openscad.generate", "publisher.screen.capture", "publisher.screen.capture.result"],
                "spreadsheet-team" => ["publisher.business-context", "publisher.spreadsheet.inspect", "publisher.screen.capture", "publisher.screen.capture.result", "publisher.input.execute", "publisher.input.result"],
                "learning-round" => ["publisher.business-context"],
                _ => ["publisher.business-context"]
            };
            Request.ModelNames = ConfiguredModelNames.ToList();
            var id = await Connection.SendCouncilRequestAsync(Request).ConfigureAwait(true);
            Notifications.Success($"Council request {id} was submitted.", "Council round started", nameof(OrganicCouncilRequestForm));
            await OnSubmitted.InvokeAsync(Request.TeamKey).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not submit the advanced Council request.");
            Notifications.Error(exception.Message, "Council request failed", nameof(OrganicCouncilRequestForm));
        }
        finally
        {
            Submitting = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }
}
