namespace Whycespace.CommandSystem.Validation;

using Whycespace.CommandSystem.Models;

public interface ICommandValidator
{
    void Validate(CommandEnvelope command);
}
