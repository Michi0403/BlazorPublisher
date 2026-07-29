using PublisherStudio.Domain;

namespace PublisherStudio.Services.OrganicPlugins;

// logging-policy: pure-helper
internal static class OrganicTransportSecurityPolicy
{
    public static bool RequiresProtectedTransport(OrganicWireMessageType messageType) => messageType is not (
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

    public static bool IsProtected(OrganicWireEnvelope envelope) =>
        envelope.SecurityMode is OneWireSecurityMode.Signed or OneWireSecurityMode.EncryptedAndSigned;
}
