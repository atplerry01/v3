namespace Whycespace.System.WhyceID.Commands;

using Whycespace.System.WhyceID.Models;

public sealed record CreateIdentityCommand(
    Guid IdentityId,
    IdentityType Type);
