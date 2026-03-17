namespace Whycespace.Runtime.EngineRegistry;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EngineManifestAttribute : Attribute
{
    public string EngineId { get; }
    public EngineTier Tier { get; }
    public Type CommandType { get; }
    public Type ResultType { get; }

    public EngineManifestAttribute(
        string engineId,
        EngineTier tier,
        Type commandType,
        Type resultType)
    {
        EngineId = engineId;
        Tier = tier;
        CommandType = commandType;
        ResultType = resultType;
    }
}
