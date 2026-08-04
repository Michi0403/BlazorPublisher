using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

public sealed class OrganicTransportSecurityPolicy(
    ILogger<OrganicTransportSecurityPolicy> logger) : IOrganicTransportSecurityPolicy
{
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
