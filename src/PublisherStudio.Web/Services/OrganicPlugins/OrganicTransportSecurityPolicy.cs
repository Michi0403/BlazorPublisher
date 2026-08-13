using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Represents an organic transport security policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicTransportSecurityPolicy(
    ILogger<OrganicTransportSecurityPolicy> logger) : IOrganicTransportSecurityPolicy
{
    /// <summary>
    /// Performs requires protected transport for <see cref="OrganicTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding organic transport security policy workflow.
    /// </summary>
    /// <param name="messageType">Message type value supplied to the organic transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool RequiresProtectedTransport(OrganicWireMessageType messageType)
    {
        try
        {
            return messageType is not (
                OrganicWireMessageType.Hello or
                OrganicWireMessageType.HelloAck or
                OrganicWireMessageType.LinkRequest or
                OrganicWireMessageType.LinkStatus or
                OrganicWireMessageType.SecurityProfileRequest or
                OrganicWireMessageType.SecurityProfileResponse or
                OrganicWireMessageType.MfaChallenge or
                OrganicWireMessageType.MfaProof or
                OrganicWireMessageType.TrustEstablished or
                OrganicWireMessageType.TrustRevoked or
                OrganicWireMessageType.Ping or
                OrganicWireMessageType.Pong or
                OrganicWireMessageType.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not evaluate organic transport protection for {messageType}.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether protected for <see cref="OrganicTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding organic transport security policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsProtected(OrganicWireEnvelope envelope)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(envelope);
            return envelope.SecurityMode is OneWireSecurityMode.Signed or OneWireSecurityMode.EncryptedAndSigned;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not evaluate organic envelope protection for message {envelope?.MessageId}.");
            throw;
        }
    }
}
