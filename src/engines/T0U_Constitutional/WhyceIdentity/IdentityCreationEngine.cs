namespace Whycespace.Engine.Identity;

using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Commands;
using Whycespace.System.WhyceID.Events;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

public sealed class IdentityCreationEngine
{
    public IdentityCreatedEvent Execute(
        CreateIdentityCommand command,
        IdentityRegistry registry)
    {
        if (registry.Exists(command.IdentityId))
            throw new InvalidOperationException("Identity already exists");

        var identity = new IdentityAggregate(
            new IdentityId(command.IdentityId),
            command.Type,
            DateTime.UtcNow);

        registry.Register(identity);

        return new IdentityCreatedEvent(
            command.IdentityId,
            command.Type,
            identity.CreatedAt);
    }
}
