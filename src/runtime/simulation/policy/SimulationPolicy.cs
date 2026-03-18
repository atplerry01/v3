namespace Whycespace.SimulationRuntime.Policy;

using Whycespace.SimulationRuntime.Models;

public class SimulationPolicy
{
    private readonly HashSet<string> _blockedCommandTypes = new();

    public bool IsSimulationAllowed(SimulationCommand command)
    {
        return !_blockedCommandTypes.Contains(command.CommandType);
    }

    public void BlockCommandType(string commandType)
    {
        _blockedCommandTypes.Add(commandType);
    }

    public void AllowCommandType(string commandType)
    {
        _blockedCommandTypes.Remove(commandType);
    }
}
