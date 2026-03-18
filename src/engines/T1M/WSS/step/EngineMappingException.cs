namespace Whycespace.Engines.T1M.WSS.Step;

public sealed class EngineMappingException : Exception
{
    public string EngineName { get; }

    public EngineMappingException(string engineName)
        : base($"Engine mapping not found: '{engineName}'")
    {
        EngineName = engineName;
    }
}
