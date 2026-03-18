namespace Whycespace.Systems.WhyceID.Models;

public sealed record IdentityRecord
{
    public Guid IdentityId { get; }
    public IdentityType Type { get; }
    public IdentityStatus Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }
    public string? PrimaryEmail { get; }
    public string? PrimaryPhone { get; }
    public string? ExternalReferenceId { get; }

    public IdentityRecord(
        Guid identityId,
        IdentityType type,
        IdentityStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        string? primaryEmail = null,
        string? primaryPhone = null,
        string? externalReferenceId = null)
    {
        if (identityId == Guid.Empty)
            throw new ArgumentException("IdentityId cannot be empty.", nameof(identityId));

        IdentityId = identityId;
        Type = type;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        PrimaryEmail = primaryEmail;
        PrimaryPhone = primaryPhone;
        ExternalReferenceId = externalReferenceId;
    }

    public IdentityRecord WithStatus(IdentityStatus newStatus)
    {
        return new IdentityRecord(
            IdentityId, Type, newStatus, CreatedAt,
            DateTime.UtcNow, PrimaryEmail, PrimaryPhone, ExternalReferenceId);
    }
}
