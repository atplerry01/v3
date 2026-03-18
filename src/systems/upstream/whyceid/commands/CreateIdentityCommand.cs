namespace Whycespace.Systems.WhyceID.Commands;

using Whycespace.Systems.WhyceID.Models;

public sealed record CreateIdentityCommand(
    Guid IdentityId,
    IdentityType Type);
