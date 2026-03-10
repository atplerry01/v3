namespace Whycespace.CommandSystem.Validation;

using Whycespace.CommandSystem.Models;

public sealed class CommandValidator : ICommandValidator
{
    public void Validate(CommandEnvelope command)
    {
        if (command.CommandId == Guid.Empty)
            throw new InvalidOperationException("CommandId must not be empty.");

        if (string.IsNullOrWhiteSpace(command.CommandType))
            throw new InvalidOperationException("CommandType must not be null or empty.");

        if (command.Payload is null)
            throw new InvalidOperationException("Payload must not be null.");
    }
}
