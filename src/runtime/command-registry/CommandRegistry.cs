namespace Whycespace.Runtime.CommandRegistry;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, List<CommandDescriptor>> _byId = new();
    private readonly Dictionary<Type, CommandDescriptor> _byType = new();
    private readonly CommandRegistryValidator _validator;

    internal CommandRegistry(CommandRegistryValidator validator)
    {
        _validator = validator;
    }

    internal void Register(CommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_byId.TryGetValue(descriptor.CommandId, out var versions))
        {
            versions = [];
            _byId[descriptor.CommandId] = versions;
        }

        versions.Add(descriptor);
        _byType[descriptor.CommandType] = descriptor;
    }

    public CommandDescriptor Resolve(Type commandType)
    {
        ArgumentNullException.ThrowIfNull(commandType);

        if (_byType.TryGetValue(commandType, out var descriptor))
            return descriptor;

        throw new CommandRegistryException(
            $"No command registered for type '{commandType.FullName}'.",
            commandType.Name);
    }

    public CommandDescriptor GetDescriptor(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        if (_byId.TryGetValue(commandId, out var versions) && versions.Count > 0)
            return versions.OrderByDescending(v => v.Version).First();

        throw new CommandRegistryException(
            $"No command registered with ID '{commandId}'.",
            commandId);
    }

    public CommandDescriptor GetDescriptor(string commandId, CommandVersion version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentNullException.ThrowIfNull(version);

        if (_byId.TryGetValue(commandId, out var versions))
        {
            var match = versions.FirstOrDefault(v => v.Version == version);
            if (match is not null)
                return match;
        }

        throw new CommandRegistryException(
            $"No command registered with ID '{commandId}' at version {version}.",
            commandId);
    }

    public IReadOnlyList<CommandDescriptor> GetAllDescriptors() =>
        _byType.Values.ToList().AsReadOnly();

    public IReadOnlyList<CommandDescriptor> GetByDomain(string domain) =>
        _byType.Values
            .Where(d => string.Equals(d.Domain, domain, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

    public IReadOnlyList<string> GetDomains() =>
        _byType.Values
            .Select(d => d.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

    public bool IsRegistered(string commandId) =>
        _byId.ContainsKey(commandId);

    public bool IsRegistered(Type commandType) =>
        _byType.ContainsKey(commandType);

    public int Count => _byType.Count;

    public CommandRegistrySnapshot CreateSnapshot()
    {
        var domains = GetDomains()
            .Select(domain => new DomainCommandGroup(domain, GetByDomain(domain)))
            .ToList()
            .AsReadOnly();

        return new CommandRegistrySnapshot(
            DateTimeOffset.UtcNow,
            Count,
            domains
        );
    }
}
