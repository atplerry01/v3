namespace Whycespace.Engines.T0U.WhyceID.Revocation.Request;

public enum RevocationScope
{
    FullIdentityRevocation = 0,
    RoleRevocation = 1,
    PermissionRevocation = 2,
    DeviceRevocation = 3,
    SessionRevocation = 4,
    CredentialRevocation = 5
}

public enum RevocationReason
{
    CompromisedAccount = 0,
    FraudDetected = 1,
    PolicyViolation = 2,
    RegulatorySuspension = 3,
    ManualGovernanceAction = 4
}

public sealed record IdentityRevocationRequest(
    Guid IdentityId,
    DateTime RequestedAt,
    RevocationReason RevocationReason,
    Guid RequestedBy,
    RevocationScope RevocationScope,
    string Evidence,
    string SourceSystem);
