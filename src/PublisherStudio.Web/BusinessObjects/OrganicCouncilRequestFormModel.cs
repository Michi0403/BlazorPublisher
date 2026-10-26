using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LocalGPT.WireProtocol;

namespace PublisherStudio.BusinessObjects;

/// <summary>Validated UI/HTTP submission values for a PublisherStudio Council request; the 1-Wire DTO remains a transport contract.</summary>
public sealed class OrganicCouncilRequestFormModel : IValidatableObject
{
    /// <summary>The requested Council team.</summary>
    /// <value>Advertised Council team identifier.</value>
    [Required(ErrorMessage = "Select a Council team.")]
    public string TeamKey { get; set; } = "general";

    /// <summary>The human assignment sent to LocalGPT.</summary>
    /// <value>Nonempty user-supplied prompt.</value>
    [Required(ErrorMessage = "Enter a user assignment.")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Serialized context supplied by the user, not invented by the form.</summary>
    /// <value>A JSON value that can be read by the connected Council service.</value>
    public string ExternalProjectContextJson { get; set; } = "{}";

    /// <summary>The requested number of Council rounds.</summary>
    /// <value>Positive round count.</value>
    [Range(1, int.MaxValue, ErrorMessage = "Council rounds must be positive.")]
    public int MaxRounds { get; set; } = 1;

    /// <summary>The requested parallelism cap for the Council job.</summary>
    /// <value>Positive number of parallel models.</value>
    [Range(1, int.MaxValue, ErrorMessage = "Parallel models must be positive.")]
    public int MaxParallelModels { get; set; } = 1;

    /// <summary>Creates a form draft from the existing shared 1-Wire transport contract.</summary>
    /// <param name="request">The existing transport request.</param>
    public void ReadFrom(OneWireCouncilRequest request)
    {
        TeamKey = request.TeamKey;
        Prompt = request.Prompt;
        ExternalProjectContextJson = request.ExternalProjectContextJson;
        MaxRounds = request.MaxRounds;
        MaxParallelModels = request.MaxParallelModels;
    }

    /// <summary>Copies only the user-editable form fields back to the existing 1-Wire request.</summary>
    /// <param name="request">The transport request whose other flags, routes and budgets must remain intact.</param>
    public void ApplyTo(OneWireCouncilRequest request)
    {
        request.TeamKey = TeamKey;
        request.Prompt = Prompt;
        request.ExternalProjectContextJson = ExternalProjectContextJson;
        request.MaxRounds = MaxRounds;
        request.MaxParallelModels = MaxParallelModels;
    }

    /// <summary>Checks cross-field form data before either a Blazor submit or an HTTP submission can dispatch transport work.</summary>
    /// <param name="validationContext">Form or controller validation context.</param>
    /// <returns>Member-associated validation failures.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Materialize validation results instead of using an iterator: model validation
        // must complete before control returns to EditForm or MVC model binding.
        var errors = new List<ValidationResult>();
        if (string.IsNullOrWhiteSpace(Prompt))
            errors.Add(new ValidationResult("Enter a user assignment.", [nameof(Prompt)]));
        if (string.IsNullOrWhiteSpace(TeamKey))
            errors.Add(new ValidationResult("Select a Council team.", [nameof(TeamKey)]));
        if (!string.IsNullOrWhiteSpace(ExternalProjectContextJson))
        {
            var validJson = true;
            try
            {
                using var json = JsonDocument.Parse(ExternalProjectContextJson);
            }
            catch (JsonException)
            {
                validJson = false;
            }
            if (!validJson)
                errors.Add(new ValidationResult("External project context must contain valid JSON.", [nameof(ExternalProjectContextJson)]));
        }
        return errors;
    }
}
