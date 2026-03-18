namespace Whycespace.CommandSystem.Core.Validation;

using Whycespace.CommandSystem.Core.Models;

public interface ICommandValidator
{
    void Validate(CommandEnvelope command);
}
