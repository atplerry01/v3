namespace Whycespace.WorkerPoolRuntime.Models;

public sealed class EngineExecutionTask
{
    public string EngineName { get; }

    public object Input { get; }

    public EngineExecutionTask(string engineName, object input)
    {
        EngineName = engineName;
        Input = input;
    }
}
