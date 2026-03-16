namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Commands;
using Whycespace.Systems.WhyceID.Events;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

public sealed class IdentityCreationEngine
{
    public IdentityCreatedEvent Execute(
        CreateIdentityCommand command,
        IdentityRegistry registry)
    {
        if (registry.Exists(command.IdentityId))
            throw new InvalidOperationException("Identity already exists");

        var identity = new IdentityAggregate(
            IdentityId.From(command.IdentityId),
            command.Type);

        registry.Register(identity);

        return new IdentityCreatedEvent(
            command.IdentityId,
            command.Type,
            identity.CreatedAt);
    }
}
