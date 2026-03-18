namespace Whycespace.Engines.T0U.WhyceID.Identity.Attributes;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityAttributeEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityAttributeStore _store;

    public IdentityAttributeEngine(
        IdentityRegistry registry,
        IdentityAttributeStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void AddAttribute(Guid identityId, string key, string value)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}");
        }

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Attribute key cannot be empty.");

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Attribute value cannot be empty.");

        var attribute = new IdentityAttribute(key, value, DateTime.UtcNow);

        _store.Add(identityId, attribute);
    }

    public IReadOnlyList<IdentityAttribute> GetAttributes(Guid identityId)
    {
        return _store.Get(identityId);
    }
}
