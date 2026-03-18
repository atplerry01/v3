namespace Whycespace.CommandSystem.Registry;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandManifestAttribute : Attribute
{
    public string CommandId { get; }
    public string Domain { get; }
    public int Version { get; }
    public string? Description { get; set; }

    public CommandManifestAttribute(string commandId, string domain, int version = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);

        CommandId = commandId;
        Domain = domain;
        Version = version;
    }
}
